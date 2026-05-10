using System.Reflection;

namespace JpegCompressorCS
{
    public partial class AboutBox : Form
    {
        public AboutBox()
        {
            InitializeComponent();
        }

        private void AboutBox_Load(object sender, EventArgs e)
        {
            // Initialize the about box to display the product information from the assembly information.
            this.lblProductName.Text = "JPEG Compressor CS";
            this.lblVersion.Text = "Version: 1.0.0";
            this.lblDeveloper.Text = "Developer: Walton Surratt";
            this.lblCopyright.Text = "Copyright © 2026 Surratt Solutions";
        }
    }
}
