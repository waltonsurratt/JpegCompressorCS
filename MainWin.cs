// Application: JPEG Compressor CS
// Developer: Walton Surratt
// Copyright (c) 2026 Surratt Solutions. All rights reserved.
using System.Drawing.Imaging;

namespace JpegCompressorCS
{
    public partial class MainWin : Form
    {
        public static string InputFile = string.Empty;
        public static string OutputFile = string.Empty;
        public static string OutputDirectory = string.Empty;
        public static int Quality = 80;

        public MainWin()
        {
            InitializeComponent();
        }

        // Launches AboutBox when the "About" menu item is clicked
        private void helpToolStripAbout_Click(object sender, EventArgs e)
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

        // Placeholder for btnSelectInput click event handler
        private void btnSelectInput_Click(object sender, EventArgs e)
        {

            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Title = "Select a JPEG Image";
                ofd.Filter = "JPEG Images (*.jpg;*.jpeg;*.jpe)|*.jpg;*.jpeg;*.jpe";
                ofd.Multiselect = false;
                ofd.CheckFileExists = true;
                ofd.CheckPathExists = true;

                if (ofd.ShowDialog() == DialogResult.OK)
                {
                    InputFile = ofd.FileName;

                    // ✅ Update StatusStrip label with full path + filename
                    statusStripStatusLbl.Text = $"Loaded file: {InputFile}";
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
            // Input validation
            if (string.IsNullOrWhiteSpace(InputFile))
            {
                statusStripStatusLbl.Text = "Error: No input file selected.";
                return;
            }

            if (string.IsNullOrWhiteSpace(txtOutputDir.Text) || !Directory.Exists(txtOutputDir.Text))
            {
                statusStripStatusLbl.Text = "Error: No valid output directory selected.";
                return;
            }

            btnStart.Enabled = false;
            statusStripProgressBar.Style = ProgressBarStyle.Marquee;
            statusStripStatusLbl.Text = "Compressing JPEG...";

            Quality = qualityTrackbar.Value;
            OutputDirectory = txtOutputDir.Text;

            try
            {
                await Task.Run(() =>
                {
                    CompressJpeg(InputFile, OutputDirectory, Quality);
                });

                statusStripStatusLbl.Text = "Compression completed successfully.";
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
        private void CompressJpeg(string inputPath, string outputDir, int quality)
        {
            using (Bitmap bitmap = new Bitmap(inputPath))
            {
                ImageCodecInfo jpegCodec = ImageCodecInfo
                    .GetImageEncoders()
                    .First(c => c.FormatID == ImageFormat.Jpeg.Guid);

                EncoderParameters encoderParams = new EncoderParameters(1);
                encoderParams.Param[0] = new EncoderParameter(
                    System.Drawing.Imaging.Encoder.Quality,
                    quality
                );

                string outputFileName =
                    Path.GetFileNameWithoutExtension(inputPath) + "_mini.jpg";

                string outputPath = Path.Combine(outputDir, outputFileName);

                bitmap.Save(outputPath, jpegCodec, encoderParams);
            }
        }
    }
}
