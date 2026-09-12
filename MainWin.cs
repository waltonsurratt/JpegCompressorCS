// Application: JPEG Compressor CS
// Developer:   Walton Surratt
// Copyright (c) 2026 Surratt Solutions. All rights reserved.

using System.Buffers;
using System.Collections.Concurrent;
using MozJpegSharp;

namespace JpegCompressorCS
{
    public partial class MainWin : Form
    {
        private const int MaxFiles = 16_384;

        // Max parallel workers: use logical core count, capped at 16 so we
        // don't saturate I/O or exhaust GDI+ handle limits on large machines.
        private static readonly int MaxWorkers =
            Math.Min(Environment.ProcessorCount, 16);

        private List<string> InputFiles = new();
        private string OutputDirectory = string.Empty;
        private int Quality = 80;
        private bool RemoveMetadata = true;
        private CancellationTokenSource? _cts;

        // ── Thread-safe output filename deduplication ──────────────────
        // Multiple workers can finish at the same time; this lock guards
        // the File.Exists check + path assignment so no two workers pick
        // the same output path.
        private readonly object _pathLock = new();

        // ── Smooth progress animation ──────────────────────────────────
        // _completedCount is incremented atomically by each worker thread.
        // The animation timer reads it on the UI thread to drive the bar.
        private int _completedCount = 0;
        private int _totalCount = 0;
        private System.Windows.Forms.Timer? _animTimer;
        private volatile int _targetProgress = 0;

        private void StartProgressAnimation(int totalFiles)
        {
            _completedCount = 0;
            _totalCount = totalFiles;
            _targetProgress = 0;
            statusStripProgressBar.Value = 0;

            _animTimer = new System.Windows.Forms.Timer { Interval = 16 }; // ~60 fps
            _animTimer.Tick += (_, _) =>
            {
                int target = _targetProgress;
                int current = statusStripProgressBar.Value;

                if (current >= target)
                    return;

                // Exponential ease-out: close gap by ~14%, minimum 1 step.
                int step = Math.Max(1, (target - current) / 7);
                statusStripProgressBar.Value = Math.Min(current + step, target);
            };
            _animTimer.Start();
        }

        private void StopProgressAnimation()
        {
            _animTimer?.Stop();
            _animTimer?.Dispose();
            _animTimer = null;
            statusStripProgressBar.Value = 0;
            _targetProgress = 0;
        }

        public MainWin()
        {
            InitializeComponent();
            AllowDrop = true;

            DragEnter += MainWin_DragEnter;
            DragDrop += MainWin_DragDrop;

            chkRemoveMetadata.Checked = true;
        }

        // Launches AboutBox when the "About" menu item is clicked
        private void helpToolStripAbout_Click(object? sender, EventArgs e)
        {
            using (AboutBox about = new AboutBox())
            {
                about.ShowDialog(this);
            }
        }

        // Restarts the application when the "Restart" menu item is clicked
        private void fileToolStripRestart_Click(object sender, EventArgs e)
        {
            Application.Restart();
            Environment.Exit(0);
        }

        // Closes the application when the "Exit" menu item is clicked
        private void fileToolStripExit_Click(object sender, EventArgs e)
        {
            this.Close();
        }

        // ==============================
        // FILE SELECTION
        // ==============================
        private void btnSelectInput_Click(object sender, EventArgs e)
        {
            using OpenFileDialog ofd = new()
            {
                Filter = "JPEG (*.jpg;*.jpeg)|*.jpg;*.jpeg",
                Multiselect = true
            };

            if (ofd.ShowDialog() == DialogResult.OK)
            {
                List<string> selected = ofd.FileNames.ToList();

                if (selected.Count > MaxFiles)
                {
                    InputFiles = selected.Take(MaxFiles).ToList();
                    statusStripStatusLbl.Text =
                        $"Loaded {MaxFiles:N0} file(s) — limit reached " +
                        $"({selected.Count - MaxFiles:N0} file(s) ignored)";
                }
                else
                {
                    InputFiles = selected;
                    statusStripStatusLbl.Text = $"Loaded {InputFiles.Count} file(s)";
                }
            }
        }

        private void btnSelectOutputDir_Click(object sender, EventArgs e)
        {
            using FolderBrowserDialog fbd = new();
            if (fbd.ShowDialog() == DialogResult.OK)
            {
                txtOutputDir.Text = fbd.SelectedPath;
            }
        }

        private void qualityTrackbar_Scroll(object sender, EventArgs e)
        {
            Quality = qualityTrackbar.Value;
            lblQualityPercent.Text = $"{Quality}%";
        }

        // ==============================
        // START / CANCEL TOGGLE
        // ==============================
        private async void btnStart_Click(object sender, EventArgs e)
        {
            // ── CANCEL branch ──────────────────────────────────────────
            if (_cts != null)
            {
                _cts.Cancel();
                return;
            }

            // ── START branch ───────────────────────────────────────────
            if (InputFiles.Count == 0)
            {
                statusStripStatusLbl.Text = "No files selected.";
                return;
            }

            if (!Directory.Exists(txtOutputDir.Text))
            {
                statusStripStatusLbl.Text = "Invalid output directory.";
                return;
            }

            OutputDirectory = txtOutputDir.Text;
            RemoveMetadata = chkRemoveMetadata.Checked;

            int totalFiles = InputFiles.Count;
            int capturedQuality = Quality;
            bool capturedRemoveMetadata = RemoveMetadata;
            string capturedOutputDir = OutputDirectory;

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            btnStart.Text = "Cancel";
            StartProgressAnimation(totalFiles);

            // Errors from worker threads are collected here and surfaced
            // to the user after all workers finish.
            var errors = new ConcurrentBag<string>();

            try
            {
                var parallelOptions = new ParallelOptions
                {
                    MaxDegreeOfParallelism = MaxWorkers,
                    CancellationToken = token
                };

                await Parallel.ForEachAsync(InputFiles, parallelOptions,
                    async (file, workerToken) =>
                    {
                        // Offload CPU-intensive compression to the thread pool.
                        await Task.Run(() =>
                        {
                            try
                            {
                                CompressJpeg(
                                    file,
                                    capturedOutputDir,
                                    capturedQuality,
                                    capturedRemoveMetadata,
                                    workerToken);
                            }
                            catch (OperationCanceledException) { throw; }
                            catch (Exception ex)
                            {
                                errors.Add($"{Path.GetFileName(file)}: {ex.Message}");
                            }
                        }, workerToken);

                        // Atomically increment and drive the progress bar.
                        int done = Interlocked.Increment(ref _completedCount);
                        _targetProgress = (int)((done / (double)totalFiles) * 100);

                        // Update the status label on the UI thread.
                        // ToolStripStatusLabel is a ToolStripItem, not a Control,
                        // so Invoke must be called on the parent statusStrip instead.
                        int captured = done;
                        statusStrip.Invoke(() =>
                            statusStripStatusLbl.Text =
                                $"{captured}/{totalFiles} file(s) compressed " +
                                $"— {MaxWorkers} thread(s)");
                    });

                _targetProgress = 100;
                await Task.Delay(300, CancellationToken.None);

                statusStripStatusLbl.Text = errors.IsEmpty
                    ? $"✅ Completed {totalFiles} file(s) — {MaxWorkers} thread(s)"
                    : $"⚠️ Completed with {errors.Count} error(s) — see details";

                if (!errors.IsEmpty)
                    MessageBox.Show(
                        string.Join(Environment.NewLine, errors),
                        "Compression Errors",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
            }
            catch (OperationCanceledException)
            {
                statusStripStatusLbl.Text = "⛔ Cancelled.";
            }
            catch (Exception ex)
            {
                statusStripStatusLbl.Text = $"Error: {ex.Message}";
            }
            finally
            {
                _cts?.Dispose();
                _cts = null;
                btnStart.Text = "Start";
                StopProgressAnimation();
            }
        }

        // ==============================
        // MOZJPEG COMPRESSION (per file)
        // ==============================
        // Called concurrently from multiple thread-pool threads.
        // • No shared mutable state except _pathLock (output naming only).
        // • ArrayPool<byte> avoids repeated LOH allocations for the
        //   compressed output buffer across hundreds or thousands of files.
        // • Each worker creates its own TJCompressor — the native MozJPEG
        //   context is not thread-safe and must not be shared.
        private void CompressJpeg(
            string inputPath,
            string outputDir,
            int quality,
            bool removeMetadata,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // ── Load ────────────────────────────────────────────────────
            using Bitmap source = new(inputPath);
            token.ThrowIfCancellationRequested();

            // ── Normalise pixel format ──────────────────────────────────
            // MozJpegSharp only accepts 24bpp RGB. Some JPEGs decode to
            // 32bpp ARGB or indexed colour, so redraw into a safe format.
            Bitmap bitmap;
            bool ownsBitmap;

            if (source.PixelFormat ==
                System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            {
                bitmap = source;
                ownsBitmap = false;
            }
            else
            {
                bitmap = new Bitmap(
                    source.Width, source.Height,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                using Graphics g = Graphics.FromImage(bitmap);
                g.DrawImage(source, 0, 0, source.Width, source.Height);
                ownsBitmap = true;
            }

            try
            {
                // ── Strip EXIF ──────────────────────────────────────────
                if (removeMetadata)
                    StripExifData(bitmap);

                token.ThrowIfCancellationRequested();

                // ── MozJPEG encode ──────────────────────────────────────
                // Each worker owns its TJCompressor; the native context is
                // not thread-safe and must not be shared across threads.
                byte[] compressed;
                using (TJCompressor compressor = new())
                {
                    compressed = compressor.Compress(
                        bitmap,
                        TJSubsamplingOption.Chrominance420,
                        quality,
                        TJFlags.None);
                }

                token.ThrowIfCancellationRequested();

                // ── Resolve output path (thread-safe) ───────────────────
                // Lock only long enough to find a unique filename; actual
                // disk write happens outside the lock so other workers
                // aren't stalled during I/O.
                string outputPath;
                lock (_pathLock)
                {
                    string baseName = Path.GetFileNameWithoutExtension(inputPath);
                    outputPath = Path.Combine(outputDir, $"{baseName}_mini.jpg");
                    int dup = 1;
                    while (File.Exists(outputPath))
                    {
                        outputPath = Path.Combine(
                            outputDir, $"{baseName}_mini_{dup}.jpg");
                        dup++;
                    }

                    // Reserve the path by creating the file immediately so
                    // no other worker can claim the same name between the
                    // exists-check above and the WriteAllBytes below.
                    File.WriteAllBytes(outputPath, Array.Empty<byte>());
                }

                // ── Write to disk (outside lock) ────────────────────────
                // ArrayPool<byte> prevents the compressed buffer from
                // being promoted to the Large Object Heap (LOH), which
                // would otherwise cause Gen2 GC pauses on large batches.
                byte[] rented = ArrayPool<byte>.Shared.Rent(compressed.Length);
                try
                {
                    Buffer.BlockCopy(compressed, 0, rented, 0, compressed.Length);
                    using FileStream fs = new(
                        outputPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 65_536,       // 64 KB write buffer
                        useAsync: false);          // sequential write; sync is faster
                    fs.Write(rented, 0, compressed.Length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
            finally
            {
                if (ownsBitmap)
                    bitmap.Dispose();
            }
        }

        // ==============================
        // METADATA STRIP
        // ==============================
        private static void StripExifData(Image image)
        {
            foreach (int id in image.PropertyIdList)
            {
                try { image.RemovePropertyItem(id); }
                catch { }
            }
        }

        // ==============================
        // DRAG + DROP
        // ==============================
        private void MainWin_DragEnter(object? sender, DragEventArgs e)
        {
            if (e.Data?.GetDataPresent(DataFormats.FileDrop) == true)
                e.Effect = DragDropEffects.Copy;
        }

        private void MainWin_DragDrop(object? sender, DragEventArgs e)
        {
            string[]? files = e.Data?.GetData(DataFormats.FileDrop) as string[];

            if (files is null)
                return;

            List<string> dropped = files
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                .ToList();

            if (dropped.Count > MaxFiles)
            {
                InputFiles = dropped.Take(MaxFiles).ToList();
                statusStripStatusLbl.Text =
                    $"Loaded {MaxFiles:N0} file(s) via drag-drop — limit reached " +
                    $"({dropped.Count - MaxFiles:N0} file(s) ignored)";
            }
            else
            {
                InputFiles = dropped;
                statusStripStatusLbl.Text =
                    $"Loaded {InputFiles.Count} file(s) via drag-drop";
            }
        }
    }
}
