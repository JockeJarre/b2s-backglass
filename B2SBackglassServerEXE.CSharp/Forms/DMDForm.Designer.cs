namespace B2SBackglassServerEXE.Forms
{
    partial class DMDForm
    {
        private System.ComponentModel.IContainer components = null;

        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        private void InitializeComponent()
        {
            this.SuspendLayout();
            // 
            // DMDForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(128, 32);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.None;
            this.Name = "DMDForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.Manual;
            this.Text = "B2S DMD";
            this.ResumeLayout(false);
        }
    }
}
