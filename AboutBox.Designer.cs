namespace JpegCompressorCS
{
    partial class AboutBox
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
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
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            System.ComponentModel.ComponentResourceManager resources = new System.ComponentModel.ComponentResourceManager(typeof(AboutBox));
            lblProductName = new Label();
            lblVersion = new Label();
            lblDeveloper = new Label();
            lblCopyright = new Label();
            pictureBox1 = new PictureBox();
            ((System.ComponentModel.ISupportInitialize)pictureBox1).BeginInit();
            SuspendLayout();
            // 
            // lblProductName
            // 
            lblProductName.AutoSize = true;
            lblProductName.Font = new Font("Segoe UI", 9F, FontStyle.Bold, GraphicsUnit.Point, 0);
            lblProductName.Location = new Point(260, 35);
            lblProductName.Name = "lblProductName";
            lblProductName.Size = new Size(134, 25);
            lblProductName.TabIndex = 0;
            lblProductName.Text = "Product Name";
            // 
            // lblVersion
            // 
            lblVersion.AutoSize = true;
            lblVersion.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblVersion.Location = new Point(260, 75);
            lblVersion.Name = "lblVersion";
            lblVersion.Size = new Size(70, 25);
            lblVersion.TabIndex = 1;
            lblVersion.Text = "Version";
            // 
            // lblDeveloper
            // 
            lblDeveloper.AutoSize = true;
            lblDeveloper.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblDeveloper.Location = new Point(260, 115);
            lblDeveloper.Name = "lblDeveloper";
            lblDeveloper.Size = new Size(93, 25);
            lblDeveloper.TabIndex = 2;
            lblDeveloper.Text = "Developer";
            // 
            // lblCopyright
            // 
            lblCopyright.AutoSize = true;
            lblCopyright.Font = new Font("Segoe UI", 9F, FontStyle.Regular, GraphicsUnit.Point, 0);
            lblCopyright.Location = new Point(260, 155);
            lblCopyright.Name = "lblCopyright";
            lblCopyright.Size = new Size(91, 25);
            lblCopyright.TabIndex = 3;
            lblCopyright.Text = "Copyright";
            // 
            // pictureBox1
            // 
            pictureBox1.Image = Properties.Resources.JpegCompressor;
            pictureBox1.Location = new Point(12, 12);
            pictureBox1.Name = "pictureBox1";
            pictureBox1.Size = new Size(226, 220);
            pictureBox1.SizeMode = PictureBoxSizeMode.StretchImage;
            pictureBox1.TabIndex = 4;
            pictureBox1.TabStop = false;
            // 
            // AboutBox
            // 
            AutoScaleDimensions = new SizeF(10F, 25F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(578, 244);
            Controls.Add(pictureBox1);
            Controls.Add(lblCopyright);
            Controls.Add(lblDeveloper);
            Controls.Add(lblVersion);
            Controls.Add(lblProductName);
            Icon = (Icon)resources.GetObject("$this.Icon");
            Name = "AboutBox";
            ShowIcon = false;
            Text = "About JPEG Compressor";
            Load += AboutBox_Load;
            ((System.ComponentModel.ISupportInitialize)pictureBox1).EndInit();
            ResumeLayout(false);
            PerformLayout();
        }

        #endregion

        private Label lblProductName;
        private Label lblVersion;
        private Label lblDeveloper;
        private Label lblCopyright;
        private PictureBox pictureBox1;
    }
}