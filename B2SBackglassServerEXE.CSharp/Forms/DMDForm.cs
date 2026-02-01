using System;
using System.Drawing;
using System.Windows.Forms;

namespace B2SBackglassServerEXE.Forms
{
    public partial class DMDForm : Form
    {
        private Models.BackglassData? _backglassData;
        private const int WM_MOUSEACTIVATE = 0x21;
        private const int MA_NOACTIVATE = 3;

        public DMDForm(Models.BackglassData backglassData)
        {
            _backglassData = backglassData;
            InitializeComponent();
            InitializeDMD();
        }

        private void InitializeDMD()
        {
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.Text = "B2S DMD";

            if (_backglassData != null)
            {
                this.ClientSize = _backglassData.DMDSize;
                
                if (Core.B2SSettings.Instance.FormToFront)
                {
                    this.TopMost = true;
                }
            }

            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
        }

        protected override void WndProc(ref Message m)
        {
            if (m.Msg == WM_MOUSEACTIVATE)
            {
                m.Result = new IntPtr(MA_NOACTIVATE);
                return;
            }
            base.WndProc(ref m);
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            if (_backglassData == null)
                return;

            Graphics g = e.Graphics;
            g.Clear(Color.Black);

            var dmdIlluminations = System.Linq.Enumerable.Where(_backglassData.Illuminations, 
                i => i.Parent.Equals("DMD", StringComparison.OrdinalIgnoreCase));

            var sorted = System.Linq.Enumerable.OrderBy(dmdIlluminations, i => i.ZOrder);

            foreach (var illumination in sorted)
            {
                if (!illumination.Visible)
                    continue;

                Image? imageToRender = illumination.IsOn ? illumination.OnImage : illumination.OffImage;
                
                if (imageToRender == null && illumination.IsOn)
                    imageToRender = illumination.OnImage;
                    
                if (imageToRender == null)
                    imageToRender = illumination.OffImage;

                if (imageToRender != null)
                {
                    g.DrawImage(imageToRender, illumination.Location.X, illumination.Location.Y, 
                        illumination.Size.Width, illumination.Size.Height);
                }
            }
        }

        public void UpdateDisplay()
        {
            this.Invalidate();
        }

        protected override void OnMouseClick(MouseEventArgs e)
        {
            base.OnMouseClick(e);
            
            if (e.Button == MouseButtons.Right)
            {
                this.Close();
            }
        }
    }
}
