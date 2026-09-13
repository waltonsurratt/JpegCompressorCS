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
        private void CompressJpeg(
            string inputPath,
            string outputDir,
            int quality,
            bool removeMetadata,
            CancellationToken token)
        {
            token.ThrowIfCancellationRequested();

            // ── Load raw bytes once — used for CMYK sniff + GDI+ decode ────
            byte[] rawBytes = File.ReadAllBytes(inputPath);
            token.ThrowIfCancellationRequested();

            // ── Detect CMYK/YCCK before handing to GDI+ ────────────────────
            bool isCmyk = JpegHasFourComponents(rawBytes);

            // ── Decode to a 24bpp RGB Bitmap ────────────────────────────────
            Bitmap bitmap;

            if (isCmyk)
            {
                // CMYK / YCCK JPEG — GDI+ refuses to construct a Bitmap from
                // these directly; convert channel-by-channel to RGB first.
                bitmap = ConvertCmykJpegToRgbBitmap(rawBytes);
            }
            else
            {
                // Standard YCbCr / greyscale JPEG.
                using MemoryStream ms = new(rawBytes, writable: false);
                Bitmap source = new(ms);

                if (source.PixelFormat ==
                    System.Drawing.Imaging.PixelFormat.Format24bppRgb)
                {
                    bitmap = source;
                }
                else
                {
                    bitmap = new Bitmap(source.Width, source.Height,
                        System.Drawing.Imaging.PixelFormat.Format24bppRgb);
                    using Graphics g = Graphics.FromImage(bitmap);
                    g.DrawImage(source, 0, 0, source.Width, source.Height);
                    source.Dispose();
                }
            }

            try
            {
                token.ThrowIfCancellationRequested();

                // ── Read orientation BEFORE stripping EXIF ──────────────────
                // Parsed directly from the raw JFIF bytes so byte order is
                // handled correctly for both iPhone (big-endian) and Android
                // (little-endian) Exif streams.
                RotateFlipType rotation = ReadExifOrientation(rawBytes);

                // ── Strip EXIF ──────────────────────────────────────────────
                if (removeMetadata)
                    StripExifData(bitmap);

                // ── Apply orientation to pixel data ─────────────────────────
                // GDI+ Bitmap.RotateFlip physically transforms the pixel grid
                // so the output is correctly oriented regardless of any EXIF
                // tag — viewers that ignore EXIF will still see it right-way-up.
                // This is a no-op (RotateNoneFlipNone) for normally-oriented
                // images, so there is zero cost for the common case.
                if (rotation != RotateFlipType.RotateNoneFlipNone)
                    bitmap.RotateFlip(rotation);

                token.ThrowIfCancellationRequested();

                // ── MozJPEG encode ──────────────────────────────────────────
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

                // ── Resolve output path (thread-safe) ───────────────────────
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
                    File.WriteAllBytes(outputPath, Array.Empty<byte>());
                }

                // ── Write to disk (outside lock) ────────────────────────────
                byte[] rented = ArrayPool<byte>.Shared.Rent(compressed.Length);
                try
                {
                    Buffer.BlockCopy(compressed, 0, rented, 0, compressed.Length);
                    using FileStream fs = new(
                        outputPath,
                        FileMode.Create,
                        FileAccess.Write,
                        FileShare.None,
                        bufferSize: 65_536,
                        useAsync: false);
                    fs.Write(rented, 0, compressed.Length);
                }
                finally
                {
                    ArrayPool<byte>.Shared.Return(rented);
                }
            }
            finally
            {
                bitmap.Dispose();
            }
        }

        // ==============================
        // CMYK / YCCK DETECTION
        // ==============================
        // Walks the JFIF marker stream looking for a SOF (Start Of Frame)
        // segment.  The SOF payload byte at offset 7 is the component count:
        //   3 → YCbCr   4 → CMYK or YCCK
        // Returns true only when a 4-component SOF is found; defaults to
        // false (safe / RGB assumed) for any malformed or unknown stream.
        private static bool JpegHasFourComponents(byte[] jpeg)
        {
            int i = 2; // skip SOI marker (FF D8)
            while (i + 3 < jpeg.Length)
            {
                if (jpeg[i] != 0xFF)
                    break; // not a valid marker boundary

                byte marker = jpeg[i + 1];
                i += 2;

                // Markers with no length field
                if (marker == 0xD8 || marker == 0xD9 || marker == 0x01 ||
                    (marker >= 0xD0 && marker <= 0xD7))
                    continue;

                if (i + 2 > jpeg.Length)
                    break;

                int segLen = (jpeg[i] << 8) | jpeg[i + 1];

                // SOF markers: C0-C3, C5-C7, C9-CB, CD-CF
                bool isSof = (marker >= 0xC0 && marker <= 0xC3) ||
                             (marker >= 0xC5 && marker <= 0xC7) ||
                             (marker >= 0xC9 && marker <= 0xCB) ||
                             (marker >= 0xCD && marker <= 0xCF);

                if (isSof && i + 7 < jpeg.Length)
                    return jpeg[i + 7] == 4; // component count byte

                i += segLen;
            }
            return false;
        }

        // ==============================
        // CMYK → RGB CONVERSION
        // ==============================
        // GDI+ can load CMYK JPEG dimensions and raw pixel bytes via
        // LockBits even though it refuses to construct a Bitmap from the file
        // path.  We exploit that: load as an Image, lock the pixel data, and
        // convert each CMYK (or YCCK) quad to an RGB triple.
        //
        // CMYK → RGB:  R = C×K/255,  G = M×K/255,  B = Y×K/255
        // (values are stored inverted in JPEG: 255 = 0% ink)
        private static Bitmap ConvertCmykJpegToRgbBitmap(byte[] rawBytes)
        {
            // Image.FromStream can parse CMYK JPEG headers — unlike Bitmap ctor.
            using MemoryStream ms = new(rawBytes, writable: false);
            using Image img = Image.FromStream(ms, useEmbeddedColorManagement: false,
                                               validateImageData: false);

            int w = img.Width;
            int h = img.Height;

            // Draw onto a 32bpp surface so GDI+ gives us accessible pixel data.
            using Bitmap tmp = new(w, h, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (Graphics g = Graphics.FromImage(tmp))
                g.DrawImage(img, 0, 0, w, h);

            Bitmap rgb = new(w, h, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            var srcRect = new Rectangle(0, 0, w, h);
            var srcData = tmp.LockBits(srcRect,
                               System.Drawing.Imaging.ImageLockMode.ReadOnly,
                               System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            var dstData = rgb.LockBits(srcRect,
                               System.Drawing.Imaging.ImageLockMode.WriteOnly,
                               System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            try
            {
                unsafe
                {
                    byte* src = (byte*)srcData.Scan0;
                    byte* dst = (byte*)dstData.Scan0;

                    for (int y = 0; y < h; y++)
                    {
                        byte* srcRow = src + y * srcData.Stride;
                        byte* dstRow = dst + y * dstData.Stride;

                        for (int x = 0; x < w; x++)
                        {
                            // GDI+ stores CMYK JPEG as BGRA where the channels
                            // map to:  B=C  G=M  R=Y  A=K  (all inverted: 255=0%)
                            byte c = srcRow[x * 4 + 0]; // Cyan
                            byte m = srcRow[x * 4 + 1]; // Magenta
                            byte yr = srcRow[x * 4 + 2]; // Yellow
                            byte k = srcRow[x * 4 + 3]; // Key (Black)

                            dstRow[x * 3 + 0] = (byte)(c * k / 255); // B
                            dstRow[x * 3 + 1] = (byte)(m * k / 255); // G
                            dstRow[x * 3 + 2] = (byte)(yr * k / 255); // R
                        }
                    }
                }
            }
            finally
            {
                tmp.UnlockBits(srcData);
                rgb.UnlockBits(dstData);
            }

            return rgb;
        }

        // ==============================
        // EXIF ORIENTATION
        // ==============================
        // EXIF tag 0x0112 encodes how the camera was held when the photo was
        // taken.  Values 1-8 map to the eight possible orientations defined by
        // the EXIF spec.  GDI+ loads the raw pixel grid exactly as stored on
        // disk and does NOT auto-rotate — so a portrait shot stored sideways
        // with an orientation tag of 6 will appear rotated 90° unless we apply
        // the corresponding RotateFlip transform to the pixel data ourselves.
        //
        // We read the tag BEFORE stripping EXIF so the value is still present,
        // then apply the rotation AFTER stripping so the output has neither a
        // stale orientation tag nor a mismatch between tag and pixel data.
        //
        // CRITICAL — byte order:
        // GDI+ PropertyItem.Value returns the tag bytes exactly as they are
        // stored in the Exif stream — it does NOT normalise to little-endian.
        // iPhones write big-endian Exif (Motorola "MM"), so for orientation=6
        // the two Value bytes are [0x00, 0x06]. Reading only Value[0] gives 0,
        // which maps to "no rotation" — silently wrong for every iPhone photo.
        // We detect the byte order from the raw Exif APP1 segment and read the
        // UINT16 correctly for both 'II' (little-endian) and 'MM' (big-endian).
        private static RotateFlipType ReadExifOrientation(byte[] rawJpegBytes)
        {
            const int ExifOrientationTag = 0x0112;

            try
            {
                // Walk the JFIF marker stream to find the APP1 (FF E1) segment.
                int pos = 2; // skip SOI
                while (pos + 3 < rawJpegBytes.Length)
                {
                    if (rawJpegBytes[pos] != 0xFF)
                        break;

                    byte marker = rawJpegBytes[pos + 1];
                    pos += 2;

                    // Standalone markers — no length field
                    if (marker == 0xD8 || marker == 0xD9 ||
                        marker == 0x01 || (marker >= 0xD0 && marker <= 0xD7))
                        continue;

                    if (pos + 2 > rawJpegBytes.Length) break;
                    int segLen = (rawJpegBytes[pos] << 8) | rawJpegBytes[pos + 1];

                    if (marker == 0xE1 && segLen > 8)
                    {
                        // APP1 payload starts after the 2-byte length field.
                        // Layout: "Exif\0\0" (6 bytes) then the TIFF/IFD block.
                        int payloadStart = pos + 2;
                        if (rawJpegBytes[payloadStart] == 'E' &&
                            rawJpegBytes[payloadStart + 1] == 'x' &&
                            rawJpegBytes[payloadStart + 2] == 'i' &&
                            rawJpegBytes[payloadStart + 3] == 'f')
                        {
                            int tiffBase = payloadStart + 6;
                            RotateFlipType result =
                                ReadOrientationFromTiff(rawJpegBytes, tiffBase);
                            return result;
                        }
                    }

                    pos += segLen;
                }
            }
            catch
            {
                // Any parse failure — default to no rotation.
            }

            return RotateFlipType.RotateNoneFlipNone;
        }

        private static RotateFlipType ReadOrientationFromTiff(
            byte[] data, int tiffBase)
        {
            // First two bytes of the TIFF block are the byte-order mark.
            // "II" = Intel = little-endian.  "MM" = Motorola = big-endian.
            if (tiffBase + 8 > data.Length)
                return RotateFlipType.RotateNoneFlipNone;

            bool bigEndian = data[tiffBase] == 'M' && data[tiffBase + 1] == 'M';

            ushort ReadU16(int offset) => bigEndian
                ? (ushort)((data[tiffBase + offset] << 8) | data[tiffBase + offset + 1])
                : (ushort)(data[tiffBase + offset] | data[tiffBase + offset + 1] << 8);

            uint ReadU32(int offset) => bigEndian
                ? (uint)((data[tiffBase + offset] << 24) | (data[tiffBase + offset + 1] << 16) |
                         (data[tiffBase + offset + 2] << 8) | data[tiffBase + offset + 3])
                : (uint)(data[tiffBase + offset] | (data[tiffBase + offset + 1] << 8) |
                         (data[tiffBase + offset + 2] << 16) | ((uint)data[tiffBase + offset + 3] << 24));

            // Offset to IFD0 is at bytes 4-7 of the TIFF block.
            int ifd0Offset = (int)ReadU32(4);
            if (tiffBase + ifd0Offset + 2 > data.Length)
                return RotateFlipType.RotateNoneFlipNone;

            int entryCount = ReadU16(ifd0Offset);

            for (int i = 0; i < entryCount; i++)
            {
                int entryOffset = ifd0Offset + 2 + i * 12;
                if (tiffBase + entryOffset + 12 > data.Length) break;

                ushort tag = ReadU16(entryOffset);
                if (tag != 0x0112) continue;

                // Found the orientation tag.
                // Type should be SHORT (3); value fits in the 4-byte value field.
                // For big-endian: value occupies the first 2 bytes of the field.
                // For little-endian: value occupies the first 2 bytes as well
                // but in LE order — ReadU16 handles both.
                ushort orientation = ReadU16(entryOffset + 8);

                // EXIF orientation → GDI+ RotateFlipType:
                //  1 = normal          3 = 180°    6 = 90° CW    8 = 90° CCW
                //  2 = flip H          4 = flip V  5 = 90°CW+flipH  7 = 90°CCW+flipH
                return orientation switch
                {
                    2 => RotateFlipType.RotateNoneFlipX,
                    3 => RotateFlipType.Rotate180FlipNone,
                    4 => RotateFlipType.RotateNoneFlipY,
                    5 => RotateFlipType.Rotate90FlipX,
                    6 => RotateFlipType.Rotate90FlipNone,
                    7 => RotateFlipType.Rotate270FlipX,
                    8 => RotateFlipType.Rotate270FlipNone,
                    _ => RotateFlipType.RotateNoneFlipNone,
                };
            }

            return RotateFlipType.RotateNoneFlipNone;
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
