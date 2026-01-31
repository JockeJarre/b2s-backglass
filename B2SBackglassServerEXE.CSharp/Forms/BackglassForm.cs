using System;
using System.Drawing;
using System.Windows.Forms;
using B2SBackglassServerEXE.Core;

namespace B2SBackglassServerEXE.Forms
{
    public partial class BackglassForm : Form
    {
        private RegistryMonitor? _registryMonitor;
        private Timer? _renderTimer;

        public BackglassForm()
        {
            InitializeComponent();
            InitializeBackglass();
        }

        private void InitializeBackglass()
        {
            // Load settings for this table
            B2SSettings.Instance.Load(Program.TableFileName);

            // Apply settings
            if (B2SSettings.Instance.FormToFront)
            {
                this.TopMost = true;
            }

            if (B2SSettings.Instance.HideBackglass)
            {
                this.Visible = false;
            }

            // Set up window
            this.Text = $"B2S Backglass - {Program.TableFileName}";
            this.FormBorderStyle = FormBorderStyle.None;
            this.WindowState = FormWindowState.Normal;
            this.BackColor = Color.Black;
            
            // Enable double buffering for smooth rendering
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);
            this.DoubleBuffered = true;

            // Set up registry monitor
            _registryMonitor = new RegistryMonitor();
            _registryMonitor.LampsChanged += RegistryMonitor_LampsChanged;
            _registryMonitor.SolenoidsChanged += RegistryMonitor_SolenoidsChanged;
            _registryMonitor.GIStringsChanged += RegistryMonitor_GIStringsChanged;
            _registryMonitor.AnimationsChanged += RegistryMonitor_AnimationsChanged;
            _registryMonitor.DataChanged += RegistryMonitor_DataChanged;

            // Set up render timer
            _renderTimer = new Timer();
            _renderTimer.Interval = 33; // ~30 FPS
            _renderTimer.Tick += RenderTimer_Tick;
        }

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            try
            {
                // TODO: Load .directb2s file
                LoadBackglassFile();

                // Start monitoring registry
                _registryMonitor?.StartMonitoring();

                // Start rendering
                _renderTimer?.Start();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading backglass: {ex.Message}",
                    "B2S Backglass Server",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void LoadBackglassFile()
        {
            // TODO: Implement .directb2s file loading
            // This is where we'll parse the XML and load images
            System.Diagnostics.Debug.WriteLine($"Loading backglass for: {Program.TableFileName}");
        }

        private void RenderTimer_Tick(object? sender, EventArgs e)
        {
            // Invalidate to trigger OnPaint
            this.Invalidate();
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);

            Graphics g = e.Graphics;
            g.Clear(Color.Black);

            // TODO: Render backglass layers
            // For now, just show a placeholder
            using (var font = new Font("Arial", 24))
            {
                string text = $"B2S Backglass Server (C#)\n{Program.TableFileName}";
                var size = g.MeasureString(text, font);
                g.DrawString(text, font, Brushes.White, 
                    (this.Width - size.Width) / 2,
                    (this.Height - size.Height) / 2);
            }
        }

        private void RegistryMonitor_LampsChanged(object? sender, LampStateChangedEventArgs e)
        {
            // TODO: Update lamp states and trigger render
            System.Diagnostics.Debug.WriteLine($"Lamps changed: {e.States.Length} lamps");
        }

        private void RegistryMonitor_SolenoidsChanged(object? sender, SolenoidStateChangedEventArgs e)
        {
            // TODO: Update solenoid states
            System.Diagnostics.Debug.WriteLine($"Solenoids changed: {e.States.Length} solenoids");
        }

        private void RegistryMonitor_GIStringsChanged(object? sender, GIStringStateChangedEventArgs e)
        {
            // TODO: Update GI string states
            System.Diagnostics.Debug.WriteLine($"GI Strings changed: {e.States.Length} strings");
        }

        private void RegistryMonitor_AnimationsChanged(object? sender, AnimationChangedEventArgs e)
        {
            // TODO: Process animation commands
            System.Diagnostics.Debug.WriteLine($"Animations changed: {e.Animations.Length} animations");
        }

        private void RegistryMonitor_DataChanged(object? sender, DataChangedEventArgs e)
        {
            // TODO: Process data changes
            System.Diagnostics.Debug.WriteLine("Data changed");
        }

        protected override void OnFormClosing(FormClosingEventArgs e)
        {
            // Stop monitoring and rendering
            _renderTimer?.Stop();
            _registryMonitor?.StopMonitoring();
            _registryMonitor?.Dispose();

            base.OnFormClosing(e);
        }

        protected override void OnKeyDown(KeyEventArgs e)
        {
            base.OnKeyDown(e);

            // Handle escape key to close
            if (e.KeyCode == Keys.Escape)
            {
                this.Close();
            }
        }
    }
}
