namespace JpegCompressorCS
{
    partial class MainWin
    {
        /// <summary>
        ///  Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        ///  Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        ///  Required method for Designer support - do not modify
        ///  the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(MainWin));
            menuStrip1 = new MenuStrip();
            fileToolStripFile = new ToolStripMenuItem();
            fileToolStripRestart = new ToolStripMenuItem();
            fileToolStripExit = new ToolStripMenuItem();
            helpToolStripHelp = new ToolStripMenuItem();
            helpToolStripAbout = new ToolStripMenuItem();
            statusStrip = new StatusStrip();
            statusStripStatusLbl = new ToolStripStatusLabel();
            statusStripProgressBar = new ToolStripProgressBar();
            label1 = new Label();
            label2 = new Label();
            label3 = new Label();
            btnSelectInput = new Button();
            txtOutputDir = new TextBox();
            btnSelectOutputDir = new Button();
            qualityTrackbar = new TrackBar();
            lblQualityPercent = new Label();
            btnStart = new Button();
            menuStrip1.SuspendLayout();
            statusStrip.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)qualityTrackbar).BeginInit();
            SuspendLayout();
            // 
            // menuStrip1
            // 
            menuStrip1.ImageScalingSize = new Size(24, 24);
            menuStrip1.Items.AddRange(new ToolStripItem[] { fileToolStripFile, helpToolStripHelp });
            menuStrip1.Location = new Point(0, 0);
            menuStrip1.Name = "menuStrip1";
            menuStrip1.Size = new Size(478, 33);
            menuStrip1.TabIndex = 0;
            menuStrip1.Text = "menuStrip1";
            // 
            // fileToolStripFile
            // 
            fileToolStripFile.DropDownItems.AddRange(new ToolStripItem[] { fileToolStripRestart, fileToolStripExit });
            fileToolStripFile.Name = "fileToolStripFile";
            fileToolStripFile.Size = new Size(54, 29);
            fileToolStripFile.Text = "&File";
            // 
            // fileToolStripRestart
            // 
            fileToolStripRestart.Name = "fileToolStripRestart";
            fileToolStripRestart.Size = new Size(168, 34);
            fileToolStripRestart.Text = "&Restart";
            fileToolStripRestart.Click += fileToolStripRestart_Click;
            // 
            // fileToolStripExit
            // 
            fileToolStripExit.Name = "fileToolStripExit";
            fileToolStripExit.Size = new Size(168, 34);
            fileToolStripExit.Text = "E&xit";
            fileToolStripExit.Click += fileToolStripExit_Click;
            // 
            // helpToolStripHelp
            // 
            helpToolStripHelp.DropDownItems.AddRange(new ToolStripItem[] { helpToolStripAbout });
            helpToolStripHelp.Name = "helpToolStripHelp";
            helpToolStripHelp.Size = new Size(65, 29);
            helpToolStripHelp.Text = "&Help";
            // 
            // helpToolStripAbout
            // 
            helpToolStripAbout.Name = "helpToolStripAbout";
            helpToolStripAbout.Size = new Size(164, 34);
            helpToolStripAbout.Text = "&About";
            helpToolStripAbout.Click += helpToolStripAbout_Click;
            // 
            // statusStrip
            // 
            statusStrip.ImageScalingSize = new Size(24, 24);
            statusStrip.Items.AddRange(new ToolStripItem[] { statusStripStatusLbl, statusStripProgressBar });
            statusStrip.Location = new Point(0, 232);
            statusStrip.Name = "statusStrip";
            statusStrip.Size = new Size(478, 32);
            statusStrip.TabIndex = 1;
            statusStrip.Text = "statusStrip";
            // 
            // statusStripStatusLbl
            // 
            statusStripStatusLbl.AutoSize = false;
            statusStripStatusLbl.Margin = new Padding(0, 4, 20, 3);
            statusStripStatusLbl.Name = "statusStripStatusLbl";
            statusStripStatusLbl.Size = new Size(330, 25);
            statusStripStatusLbl.Text = "Ready";
            statusStripStatusLbl.TextAlign = ContentAlignment.MiddleLeft;
            // 
            // statusStripProgressBar
            // 
            statusStripProgressBar.Name = "statusStripProgressBar";
            statusStripProgressBar.Size = new Size(100, 24);
            // 
            // label1
            // 
            label1.AutoSize = true;
            label1.Location = new Point(12, 52);
            label1.Name = "label1";
            label1.Size = new Size(107, 25);
            label1.TabIndex = 2;
            label1.Text = "Choose File:";
            // 
            // label2
            // 
            label2.AutoSize = true;
            label2.Location = new Point(12, 94);
            label2.Name = "label2";
            label2.Size = new Size(128, 25);
            label2.TabIndex = 3;
            label2.Text = "Output Folder:";
            // 
            // label3
            // 
            label3.AutoSize = true;
            label3.Location = new Point(12, 139);
            label3.Name = "label3";
            label3.Size = new Size(72, 25);
            label3.TabIndex = 4;
            label3.Text = "Quality:";
            // 
            // btnSelectInput
            // 
            btnSelectInput.Location = new Point(125, 47);
            btnSelectInput.Name = "btnSelectInput";
            btnSelectInput.Size = new Size(112, 34);
            btnSelectInput.TabIndex = 5;
            btnSelectInput.Text = "Browse...";
            btnSelectInput.UseVisualStyleBackColor = true;
            btnSelectInput.Click += btnSelectInput_Click;
            // 
            // txtOutputDir
            // 
            txtOutputDir.Location = new Point(146, 91);
            txtOutputDir.Name = "txtOutputDir";
            txtOutputDir.Size = new Size(150, 31);
            txtOutputDir.TabIndex = 6;
            // 
            // btnSelectOutputDir
            // 
            btnSelectOutputDir.Location = new Point(302, 89);
            btnSelectOutputDir.Name = "btnSelectOutputDir";
            btnSelectOutputDir.Size = new Size(112, 34);
            btnSelectOutputDir.TabIndex = 7;
            btnSelectOutputDir.Text = "Browse";
            btnSelectOutputDir.UseVisualStyleBackColor = true;
            btnSelectOutputDir.Click += btnSelectOutputDir_Click;
            // 
            // qualityTrackbar
            // 
            qualityTrackbar.LargeChange = 10;
            qualityTrackbar.Location = new Point(86, 139);
            qualityTrackbar.Maximum = 100;
            qualityTrackbar.Name = "qualityTrackbar";
            qualityTrackbar.Size = new Size(279, 69);
            qualityTrackbar.TabIndex = 8;
            qualityTrackbar.TickFrequency = 10;
            qualityTrackbar.Value = 80;
            qualityTrackbar.Scroll += qualityTrackbar_Scroll;
            // 
            // lblQualityPercent
            // 
            lblQualityPercent.AutoSize = true;
            lblQualityPercent.Location = new Point(367, 139);
            lblQualityPercent.Name = "lblQualityPercent";
            lblQualityPercent.Size = new Size(47, 25);
            lblQualityPercent.TabIndex = 9;
            lblQualityPercent.Text = "80%";
            // 
            // btnStart
            // 
            btnStart.Location = new Point(190, 180);
            btnStart.Name = "btnStart";
            btnStart.Size = new Size(112, 34);
            btnStart.TabIndex = 10;
            btnStart.Text = "Start";
            btnStart.UseVisualStyleBackColor = true;
            btnStart.Click += btnStart_Click;
            // 
            // MainWin
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(478, 264);
            Controls.Add(btnStart);
            Controls.Add(lblQualityPercent);
            Controls.Add(qualityTrackbar);
            Controls.Add(btnSelectOutputDir);
            Controls.Add(txtOutputDir);
            Controls.Add(btnSelectInput);
            Controls.Add(label3);
            Controls.Add(label2);
            Controls.Add(label1);
            Controls.Add(statusStrip);
            Controls.Add(menuStrip1);
            Icon = (Icon)resources.GetObject("$this.Icon");
            MainMenuStrip = menuStrip1;
            Name = "MainWin";
            Text = "Jpeg Compressor CS";
            menuStrip1.ResumeLayout(false);
            menuStrip1.PerformLayout();
            statusStrip.ResumeLayout(false);
            statusStrip.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)qualityTrackbar).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private MenuStrip menuStrip1;
        private ToolStripMenuItem fileToolStripFile;
        private ToolStripMenuItem fileToolStripRestart;
        private ToolStripMenuItem fileToolStripExit;
        private ToolStripMenuItem helpToolStripHelp;
        private ToolStripMenuItem helpToolStripAbout;
        private StatusStrip statusStrip;
        private ToolStripStatusLabel statusStripStatusLbl;
        private ToolStripProgressBar statusStripProgressBar;
        private Label label1;
        private Label label2;
        private Label label3;
        private Button btnSelectInput;
        private TextBox txtOutputDir;
        private Button btnSelectOutputDir;
        private TrackBar qualityTrackbar;
        private Label lblQualityPercent;
        private Button btnStart;
    }
}
