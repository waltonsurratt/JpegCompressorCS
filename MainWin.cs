// Application: JPEG Compressor CS
// Developer:   Walton Surratt
// Copyright (c) 2026 Surratt Solutions. All rights reserved.

using System.Drawing.Imaging;

namespace JpegCompressorCS
{
    public partial class MainWin : Form
    {
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
                InputFiles = ofd.FileNames.ToList();
                statusStripStatusLbl.Text = $"Loaded {InputFiles.Count} file(s)";
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
                _cts.Dispose();
                _cts = null;
                btnStart.Text = "Start";
                statusStripProgressBar.Value = 0;
            }
        }

        // ==============================
        // REAL-TIME COMPRESSION
        // ==============================
        private void CompressJpegWithProgress(
            string inputPath,
            string outputDir,
            int finalQuality,
            bool removeMetadata,
            IProgress<int> progress,
            CancellationToken token = default)
        {
            using Bitmap bitmap = new(inputPath);

            if (removeMetadata)
                StripExifData(bitmap);

            ImageCodecInfo jpegCodec = ImageCodecInfo
                .GetImageEncoders()
                .First(c => c.FormatID == ImageFormat.Jpeg.Guid);

            string baseName = Path.GetFileNameWithoutExtension(inputPath);
            string outputPath = Path.Combine(outputDir, $"{baseName}_mini.jpg");

            int dup = 1;
            while (File.Exists(outputPath))
            {
                outputPath = Path.Combine(outputDir, $"{baseName}_mini_{dup}.jpg");
                dup++;
            }

            // ✅ REAL-TIME PROGRESS SIMULATION
            const int steps = 20;
            int stepSize = Math.Max(1, finalQuality / steps);

            for (int q = stepSize; q <= finalQuality; q += stepSize)
            {
                token.ThrowIfCancellationRequested();

                using EncoderParameters encParams = new(1);
                encParams.Param[0] = new EncoderParameter(
                    System.Drawing.Imaging.Encoder.Quality, q);

                using MemoryStream ms = new();

                bitmap.Save(ms, jpegCodec, encParams);

                int percent = (int)((q / (double)finalQuality) * 100);
                progress.Report(percent);
            }

            // ✅ Final write to disk — skipped if cancelled mid-loop
            token.ThrowIfCancellationRequested();

            using EncoderParameters finalParams = new(1);
            finalParams.Param[0] = new EncoderParameter(
                System.Drawing.Imaging.Encoder.Quality, finalQuality);

            bitmap.Save(outputPath, jpegCodec, finalParams);

            progress.Report(100);
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
        private void MainWin_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Copy;
        }

        private void MainWin_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            InputFiles = files
                .Where(f => f.EndsWith(".jpg", StringComparison.OrdinalIgnoreCase) ||
                            f.EndsWith(".jpeg", StringComparison.OrdinalIgnoreCase))
                .ToList();

            statusStripStatusLbl.Text =
                $"Loaded {InputFiles.Count} file(s) via drag-drop";
        }
    }
}
