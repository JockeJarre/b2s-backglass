using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using B2SBackglassServerEXE.Core;

namespace B2SBackglassServerEXE.Forms
{
    public partial class BackglassForm : Form
    {
        private RegistryMonitor? _registryMonitor;
        private Timer? _renderTimer;
        private Models.BackglassData? _backglassData;
        private Dictionary<int, bool> _lampStates = new Dictionary<int, bool>();

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
            try
            {
                var loader = new Core.BackglassLoader();
                _backglassData = loader.Load(Program.TableFileName);

                if (_backglassData != null)
                {
                    // Resize form to backglass size
                    this.ClientSize = _backglassData.BackglassSize;

                    // Initialize lamp states
                    foreach (var illumination in _backglassData.Illuminations)
                    {
                        _lampStates[illumination.RomID] = illumination.InitialState == 1;
                        illumination.IsOn = illumination.InitialState == 1;
                    }

                    System.Diagnostics.Debug.WriteLine($"Loaded backglass: {_backglassData.Name}");
                    System.Diagnostics.Debug.WriteLine($"Size: {_backglassData.BackglassSize}");
                    System.Diagnostics.Debug.WriteLine($"Illuminations: {_backglassData.Illuminations.Count}");
                    System.Diagnostics.Debug.WriteLine($"Animations: {_backglassData.Animations.Count}");
                }
            }
            catch (FileNotFoundException ex)
            {
                System.Diagnostics.Debug.WriteLine($"Backglass file not found: {ex.Message}");
                // Show placeholder - backglass file not available
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading backglass: {ex.Message}");
                MessageBox.Show($"Error loading backglass: {ex.Message}",
                    "B2S Backglass Server",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
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

            if (_backglassData == null)
            {
                // Show placeholder
                using (var font = new Font("Arial", 24))
                {
                    string text = $"B2S Backglass Server (C#)\n{Program.TableFileName}\n\nBackglass file not found";
                    var size = g.MeasureString(text, font);
                    g.DrawString(text, font, Brushes.White, 
                        (this.Width - size.Width) / 2,
                        (this.Height - size.Height) / 2);
                }
                return;
            }

            // Render background image
            if (_backglassData.BackgroundImage != null)
            {
                g.DrawImage(_backglassData.BackgroundImage, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            }

            // Render illuminations in Z-order
            var sortedIlluminations = _backglassData.Illuminations
                .Where(i => i.Parent == "Backglass")
                .OrderBy(i => i.ZOrder);

            foreach (var illumination in sortedIlluminations)
            {
                if (!illumination.Visible)
                    continue;

                // Determine which image to show based on state
                Image? imageToRender = illumination.IsOn ? illumination.OnImage : illumination.OffImage;
                
                if (imageToRender == null && illumination.IsOn)
                    imageToRender = illumination.OnImage;
                    
                if (imageToRender == null)
                    imageToRender = illumination.OffImage;

                if (imageToRender != null)
                {
                    // Draw the illumination image at its location
                    g.DrawImage(imageToRender, illumination.Location.X, illumination.Location.Y, 
                        illumination.Size.Width, illumination.Size.Height);
                }
            }

            // Debug info
            using (var font = new Font("Arial", 10))
            {
                string info = $"{_backglassData.Name} | {_backglassData.Illuminations.Count} lamps";
                g.DrawString(info, font, Brushes.Yellow, 10, this.Height - 25);
            }
        }

        private void RegistryMonitor_LampsChanged(object? sender, LampStateChangedEventArgs e)
        {
            if (_backglassData == null)
                return;

            // Update lamp states
            for (int i = 0; i < e.States.Length && i < 401; i++)
            {
                bool newState = e.States[i];
                
                // Find illuminations controlled by this lamp
                foreach (var illumination in _backglassData.Illuminations)
                {
                    if (illumination.RomIDType == 0 && illumination.RomID == i)
                    {
                        bool shouldBeOn = illumination.RomInverted ? !newState : newState;
                        
                        if (illumination.IsOn != shouldBeOn)
                        {
                            illumination.IsOn = shouldBeOn;
                            // Trigger redraw
                            this.Invalidate();
                        }
                    }
                }
            }

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
