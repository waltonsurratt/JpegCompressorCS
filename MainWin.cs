// Application: JPEG Compressor CS
// Developer: Walton Surratt
// Copyright (c) 2026 Surratt Solutions. All rights reserved.
using System.Drawing.Imaging;

namespace JpegCompressorCS
{
    public partial class MainWin : Form
    {
        private string InputFile = string.Empty;
        private string OutputDirectory = string.Empty;
        private int Quality = 80;
        private bool RemoveMetadata = true;

        // Drag-and-drop related fields
        private Panel? dragDropOverlay;
        private Label? dragDropOverlayLabel;

        private readonly Color normalBackColor = SystemColors.Control;
        private readonly Color dragActiveBackColor = Color.FromArgb(232, 244, 255);
        private readonly Color dragBorderColor = Color.FromArgb(0, 120, 212);

        // Batch processing support
        private List<string> InputFiles = new List<string>();

        public MainWin()
        {
            InitializeComponent();
            InitializeDragDropFeature();


            chkRemoveMetadata.Checked = RemoveMetadata;
        }

        private void InitializeDragDropFeature()
        {
            AllowDrop = true;

            DragEnter += MainWin_DragEnter;
            DragOver += MainWin_DragOver;
            DragLeave += MainWin_DragLeave;
            DragDrop += MainWin_DragDrop;

            dragDropOverlay = new Panel
            {
                Visible = false,
                BackColor = dragActiveBackColor,
                BorderStyle = BorderStyle.FixedSingle,
                Width = ClientSize.Width - 32,
                Height = ClientSize.Height - menuStrip1.Height - statusStrip.Height - 32,
                Left = 16,
                Top = menuStrip1.Height + 16,
                Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right
            };

            dragDropOverlayLabel = new Label
            {
                AutoSize = false,
                Dock = DockStyle.Fill,
                TextAlign = ContentAlignment.MiddleCenter,
                Font = new Font(Font.FontFamily, 14F, FontStyle.Bold),
                ForeColor = dragBorderColor,
                Text = "Drop JPEG file here"
            };

            dragDropOverlay.Controls.Add(dragDropOverlayLabel);

            Controls.Add(dragDropOverlay);
            dragDropOverlay.BringToFront();
        }

        // Launches AboutBox when the "About" menu item is clicked
        private void helpToolStripAbout_Click(object? sender, EventArgs e)
        {
            using (AboutBox about = new AboutBox())
            {
                about.ShowDialog(this);
            }
        }


        private void MainWin_DragEnter(object? sender, DragEventArgs e)
        {
            if (IsValidJpegDrag(e))
            {
                e.Effect = DragDropEffects.Copy;
                ShowDragDropOverlay(true);
                statusStripStatusLbl.Text = "Release to select JPEG file.";
            }
            else
            {
                e.Effect = DragDropEffects.None;
                ShowDragDropOverlay(false);
                statusStripStatusLbl.Text = "Only JPEG files are supported.";
            }
        }

        private void MainWin_DragOver(object? sender, DragEventArgs e)
        {
            if (IsValidJpegDrag(e))
            {
                e.Effect = DragDropEffects.Copy;
            }
            else
            {
                e.Effect = DragDropEffects.None;
            }
        }

        private void MainWin_DragLeave(object? sender, EventArgs e)
        {
            ShowDragDropOverlay(false);
            statusStripStatusLbl.Text = "Ready";
        }

        private void MainWin_DragDrop(object? sender, DragEventArgs e)
        {
            ShowDragDropOverlay(false);

            if (!e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                statusStripStatusLbl.Text = "Error: No files detected.";
                return;
            }

            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);

            InputFiles = files
                .Where(f => IsJpegFile(f))
                .ToList();

            if (InputFiles.Count == 0)
            {
                statusStripStatusLbl.Text = "Error: No valid JPEG files.";
                return;
            }

            statusStripStatusLbl.Text =
                $"Loaded {InputFiles.Count} file(s) via drag-and-drop.";
        }

        private void chkRemoveMetadata_CheckedChanged(object sender, EventArgs e)
        {            
            RemoveMetadata = chkRemoveMetadata.Checked;
        }


        private bool IsValidJpegDrag(DragEventArgs e)
        {
            IDataObject? dataObject = e.Data;

            if (dataObject == null)
                return false;

            if (!dataObject.GetDataPresent(DataFormats.FileDrop))
                return false;

            string[]? files = dataObject.GetData(DataFormats.FileDrop) as string[];

            if (files == null || files.Length == 0)
                return false;

            return IsJpegFile(files[0]);
        }


        private bool IsJpegFile(string filePath)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return false;

            if (!File.Exists(filePath))
                return false;

            string extension = Path.GetExtension(filePath);

            return extension.Equals(".jpg", StringComparison.OrdinalIgnoreCase)
                || extension.Equals(".jpeg", StringComparison.OrdinalIgnoreCase);
        }

        private void ShowDragDropOverlay(bool show)
        {
            if (dragDropOverlay == null)
                return;

            dragDropOverlay.Visible = show;

            if (show)
            {
                dragDropOverlay.BringToFront();
                BackColor = dragActiveBackColor;
            }
            else
            {
                BackColor = normalBackColor;
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

        // Placeholder for btnSelectInput click event handler
        private void btnSelectInput_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select JPEG Image(s)";
                ofd.Filter = "JPEG Images (*.jpg;*.jpeg;*.jpe)|*.jpg;*.jpeg;*.jpe";
                ofd.Multiselect = true; // ✅ Enable batch selection
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    InputFiles = ofd.FileNames.ToList();

                    statusStripStatusLbl.Text =
                        $"Loaded {InputFiles.Count} file(s).";
                }
            }
        }

        private void btnSelectOutputDir_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog fbd = new FolderBrowserDialog())
            {
                fbd.Description = "Select Output Folder";
                fbd.ShowNewFolderButton = true;

                if (fbd.ShowDialog() == DialogResult.OK)
                {
                    string selectedPath = fbd.SelectedPath;

                    // ✅ Update text field
                    txtOutputDir.Text = selectedPath;

                    // ✅ Update status bar
                    statusStripStatusLbl.Text = $"Output directory set to: {selectedPath}";
                }
            }
        }

        // Updates the quality percentage text field
        private void qualityTrackbar_Scroll(object sender, EventArgs e)
        {
            Quality = qualityTrackbar.Value;
            lblQualityPercent.Text = $"{Quality}%";
        }


        // Compress file when user clicks Start button
        private async void btnStart_Click(object sender, EventArgs e)
        {
            if (InputFiles == null || InputFiles.Count == 0)
            {
                statusStripStatusLbl.Text = "Error: No input files selected.";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOutputDir.Text) ||
                !Directory.Exists(txtOutputDir.Text))
            {
                statusStripStatusLbl.Text = "Error: No valid output directory selected.";
                return;
            }

            btnStart.Enabled = false;
            statusStripProgressBar.Style = ProgressBarStyle.Marquee;

            Quality = qualityTrackbar.Value;
            OutputDirectory = txtOutputDir.Text;

            try
            {
                await Task.Run(() =>
                {
                    int count = 0;

                    foreach (var file in InputFiles)
                    {
                        CompressJpeg(file, OutputDirectory, Quality, RemoveMetadata);
                        count++;
                    }
                });

                statusStripStatusLbl.Text =
                    $"Batch complete: {InputFiles.Count} file(s) compressed.";
            }
            catch (Exception ex)
            {
                statusStripStatusLbl.Text = $"Error: {ex.Message}";
            }
            finally
            {
                statusStripProgressBar.Style = ProgressBarStyle.Blocks;
                btnStart.Enabled = true;
            }
        }

        // CompressJpeg Files
        private void CompressJpeg(string inputPath, string outputDir, int quality, bool removeMetadata)
        {
            using Bitmap bitmap = new Bitmap(inputPath);

            if (removeMetadata)
            {
                StripExifData(bitmap);
            }

            ImageCodecInfo jpegCodec = ImageCodecInfo
                .GetImageEncoders()
                .First(c => c.FormatID == ImageFormat.Jpeg.Guid);

            using EncoderParameters encoderParams = new EncoderParameters(1);
            encoderParams.Param[0] = new EncoderParameter(
                System.Drawing.Imaging.Encoder.Quality,
                quality);

            string outputFile =
                Path.GetFileNameWithoutExtension(inputPath) + "_mini.jpg";

            string outputPath = Path.Combine(outputDir, outputFile);

            // ✅ Ensure unique filename
            int i = 1;
            while (File.Exists(outputPath))
            {
                outputFile =
                    Path.GetFileNameWithoutExtension(inputPath) + $"_mini_{i}.jpg";
                outputPath = Path.Combine(outputDir, outputFile);
                i++;
            }


            bitmap.Save(outputPath, jpegCodec, encoderParams);
        }

        // Remove metadata for non-protected properties on the image file
        private void StripExifData(Image image)
        {
            if (image == null)
                return;

            // Copy property IDs first because removing items modifies the collection
            int[] propertyIds = image.PropertyIdList.ToArray();

            foreach (int propertyId in propertyIds)
            {
                try
                {
                    image.RemovePropertyItem(propertyId);
                }
                catch
                {
                    // Some images/codecs may reject removal of certain properties.
                    // Ignore safely and continue removing the rest.
                }
            }
        }
    }
}
