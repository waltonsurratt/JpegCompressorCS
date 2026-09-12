// Application: JPEG Compressor CS
// Developer:   Walton Surratt
// Copyright (c) 2026 Surratt Solutions. All rights reserved.

using MozJpegSharp;

namespace JpegCompressorCS
{
    public partial class MainWin : Form
    {
        private const int MaxFiles = 16_384;

        private List<string> InputFiles = new();
        private string OutputDirectory = string.Empty;
        private int Quality = 80;
        private bool RemoveMetadata = true;
        private CancellationTokenSource? _cts;

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

            _cts = new CancellationTokenSource();
            CancellationToken token = _cts.Token;

            btnStart.Text = "Cancel";

            try
            {
                int completed = 0;

                for (int i = 0; i < InputFiles.Count; i++)
                {
                    token.ThrowIfCancellationRequested();

                    string file = InputFiles[i];

                    var progress = new Progress<int>(percent =>
                    {
                        statusStripProgressBar.Value = percent;
                        statusStripStatusLbl.Text =
                            $"File {i + 1}/{InputFiles.Count} - {percent}% - {Path.GetFileName(file)}";
                    });

                    await Task.Run(() =>
                        CompressJpegWithProgress(
                            file,
                            OutputDirectory,
                            Quality,
                            RemoveMetadata,
                            progress,
                            token),
                        token);

                    completed++;
                }

                statusStripStatusLbl.Text = $"✅ Completed {completed} file(s)";
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
                statusStripProgressBar.Value = 0;
            }
        }

        // ==============================
        // MOZJPEG COMPRESSION
        // ==============================
        private void CompressJpegWithProgress(
            string inputPath,
            string outputDir,
            int quality,
            bool removeMetadata,
            IProgress<int> progress,
            CancellationToken token = default)
        {
            // Phase 1 — Load (0 → 25%)
            progress.Report(0);
            using Bitmap source = new(inputPath);
            token.ThrowIfCancellationRequested();
            progress.Report(25);

            // Phase 2 — Normalise pixel format.
            // MozJpegSharp only accepts 24bpp RGB; some JPEGs decode to
            // 32bpp ARGB or indexed colour, so we redraw into a safe format.
            Bitmap bitmap;
            if (source.PixelFormat == System.Drawing.Imaging.PixelFormat.Format24bppRgb)
            {
                bitmap = source;
            }
            else
            {
                bitmap = new Bitmap(source.Width, source.Height,
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                using Graphics g = Graphics.FromImage(bitmap);
                g.DrawImage(source, 0, 0, source.Width, source.Height);
            }

            try
            {
                // Phase 3 — Strip EXIF metadata (40%)
                if (removeMetadata)
                    StripExifData(bitmap);
                token.ThrowIfCancellationRequested();
                progress.Report(40);

                // Phase 4 — MozJPEG encode (40 → 90%)
                // TJSubsamplingOption.Chrominance420 (4:2:0) gives the best
                // size reduction; TJFlags.None lets MozJPEG apply its own
                // optimised Huffman tables and trellis quantisation.
                using TJCompressor compressor = new();
                byte[] compressed = compressor.Compress(
                    bitmap,
                    TJSubsamplingOption.Chrominance420,
                    quality,
                    TJFlags.None);

                token.ThrowIfCancellationRequested();
                progress.Report(90);

                // Phase 5 — Write to disk (90 → 100%)
                string baseName = Path.GetFileNameWithoutExtension(inputPath);
                string outputPath = Path.Combine(outputDir, $"{baseName}_mini.jpg");

                int dup = 1;
                while (File.Exists(outputPath))
                {
                    outputPath = Path.Combine(outputDir, $"{baseName}_mini_{dup}.jpg");
                    dup++;
                }

                token.ThrowIfCancellationRequested();
                File.WriteAllBytes(outputPath, compressed);
                progress.Report(100);
            }
            finally
            {
                // Dispose the normalised copy only if we created a new one
                if (!ReferenceEquals(bitmap, source))
                    bitmap.Dispose();
            }
        }

        // ==============================
        // METADATA STRIP
        // ==============================
        private void StripExifData(Image image)
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
