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
        private Rendering.AnimationEngine? _animationEngine;
        private DMDForm? _dmdForm;
        private SizeF _scaleFactor = new SizeF(1.0f, 1.0f);

        public BackglassForm()
        {
            InitializeComponent();
            InitializeBackglass();
        }

        private void InitializeBackglass()
        {
            // Load settings for this table
            B2SSettings.Instance.Load(Program.TableFileName);

            // Set window properties
            this.FormBorderStyle = FormBorderStyle.None;
            this.StartPosition = FormStartPosition.Manual;
            this.BackColor = Color.Black;
            this.DoubleBuffered = true;
            this.Text = "B2S Backglass Server (C#)";
            this.KeyPreview = true;

            // DPI awareness
            this.AutoScaleMode = AutoScaleMode.None;
            
            // Set style for better rendering
            this.SetStyle(ControlStyles.AllPaintingInWmPaint | 
                         ControlStyles.UserPaint |
                         ControlStyles.OptimizedDoubleBuffer, true);

            // Position on correct screen
            var screen = Utilities.ScreenManager.GetBackglassScreen();
            this.Location = new Point(screen.Bounds.X + 100, screen.Bounds.Y + 100);

            // Apply settings
            if (B2SSettings.Instance.FormToFront)
            {
                this.TopMost = true;
            }

            if (B2SSettings.Instance.HideBackglass)
            {
                this.Visible = false;
            }

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
                    // Get proper window size from screen settings (not .directb2s file)
                    var targetSize = Utilities.ScreenManager.GetBackglassSize();
                    var backglassFileSize = _backglassData.BackglassSize;

                    System.Diagnostics.Debug.WriteLine($"Backglass file size: {backglassFileSize}");
                    System.Diagnostics.Debug.WriteLine($"Target window size: {targetSize}");

                    // Set window size from screen settings
                    this.ClientSize = targetSize;

                    // Calculate scale factor from file size to window size
                    _scaleFactor = Utilities.ImageScaler.GetScaleFactor(backglassFileSize, targetSize);
                    
                    System.Diagnostics.Debug.WriteLine($"Scale factor: {_scaleFactor.Width}x{_scaleFactor.Height}");

                    // Position window from screen settings
                    this.Location = Utilities.ScreenManager.GetBackglassLocation();
                    
                    System.Diagnostics.Debug.WriteLine($"Window location: {this.Location}");
                    System.Diagnostics.Debug.WriteLine($"Window size: {this.ClientSize}");

                    // Initialize lamp states and visibility
                    System.Diagnostics.Debug.WriteLine($"[INIT] Initializing {_backglassData.Illuminations.Count} illuminations...");
                    foreach (var illumination in _backglassData.Illuminations)
                    {
                        _lampStates[illumination.RomID] = illumination.InitialState == 1;
                        illumination.IsOn = illumination.InitialState == 1;
                        
                        // Illuminations start visible only if InitialState = 1
                        // VB code: If InitialState = 1 Then picbox.Visible = True
                        illumination.Visible = (illumination.InitialState == 1);
                        
                        System.Diagnostics.Debug.WriteLine($"[INIT] Illu {illumination.ID} '{illumination.Name}': RomID={illumination.RomID}, Init={illumination.InitialState}, IsOn={illumination.IsOn}, Visible={illumination.Visible}, OnImg={illumination.OnImage != null}, OffImg={illumination.OffImage != null}, Pos=({illumination.Location.X},{illumination.Location.Y}), Size=({illumination.Size.Width}x{illumination.Size.Height})");
                    }

                    // Create animation engine
                    _animationEngine = new Rendering.AnimationEngine(_backglassData, this);

                    // Create DMD if needed
                    if (!B2SSettings.Instance.HideDMD && HasDMDIlluminations())
                    {
                        _dmdForm = new DMDForm(_backglassData);
                        
                        var dmdLocation = Utilities.ScreenManager.GetDMDLocation(
                            this.Location,
                            this.ClientSize,
                            _backglassData.DMDLocation,
                            _backglassData.DMDSize
                        );
                        
                        _dmdForm.Location = dmdLocation;
                        Utilities.ScreenManager.EnsureVisibleOnScreen(_dmdForm);
                        _dmdForm.Show(this);
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

        private bool HasDMDIlluminations()
        {
            if (_backglassData == null)
                return false;

            return _backglassData.Illuminations.Any(i => 
                i.Parent.Equals("DMD", StringComparison.OrdinalIgnoreCase));
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
                // Always use high quality for scaling
                g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.HighQualityBicubic;
                g.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.HighQuality;
                g.PixelOffsetMode = System.Drawing.Drawing2D.PixelOffsetMode.HighQuality;
                
                // Draw background scaled to window size
                g.DrawImage(_backglassData.BackgroundImage, 0, 0, this.ClientSize.Width, this.ClientSize.Height);
            }
            else
            {
                // No background image - draw debug text
                using (var font = new Font("Arial", 16))
                {
                    g.DrawString("No background image in .directb2s file", font, Brushes.Red, 10, 10);
                }
            }

            // Render illuminations in Z-order
            var sortedIlluminations = _backglassData.Illuminations
                .Where(i => i.Parent == "Backglass")
                .OrderBy(i => i.ZOrder)
                .ToList();

            int renderedCount = 0;
            foreach (var illumination in sortedIlluminations)
            {
                if (!illumination.Visible)
                    continue;

                // Determine which image to show based on state
                Image? imageToRender = null;
                
                if (illumination.IsOn && illumination.OnImage != null)
                {
                    imageToRender = illumination.OnImage;
                }
                else if (!illumination.IsOn && illumination.OffImage != null)
                {
                    imageToRender = illumination.OffImage;
                }
                else if (illumination.OnImage != null)
                {
                    // Fallback to OnImage if OffImage doesn't exist
                    imageToRender = illumination.OnImage;
                }
                else if (illumination.OffImage != null)
                {
                    // Fallback to OffImage if OnImage doesn't exist
                    imageToRender = illumination.OffImage;
                }

                if (imageToRender != null)
                {
                    // Apply scaling to both position AND size
                    // The illumination.Location and illumination.Size are from the .directb2s file
                    // and need to be scaled to match the window size
                    var scaledLocation = Utilities.ImageScaler.ScalePoint(illumination.Location, _scaleFactor);
                    var scaledSize = Utilities.ImageScaler.ScaleSize(illumination.Size, _scaleFactor);
                    
                    // Draw the image scaled to the target size
                    g.DrawImage(imageToRender, scaledLocation.X, scaledLocation.Y, 
                        scaledSize.Width, scaledSize.Height);
                    renderedCount++;
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
            System.Diagnostics.Debug.WriteLine($"[REGISTRY] Lamps changed: {e.States.Length} lamps");
            
            if (_backglassData == null)
            {
                System.Diagnostics.Debug.WriteLine("[REGISTRY] No backglass data loaded!");
                return;
            }

            int changedCount = 0;
            
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
                            illumination.Visible = true; // Make visible when state changes
                            changedCount++;
                            System.Diagnostics.Debug.WriteLine($"[REGISTRY] Lamp {i} -> {shouldBeOn} (Illum: {illumination.Name})");
                        }
                    }
                }
            }

            if (changedCount > 0)
            {
                System.Diagnostics.Debug.WriteLine($"[REGISTRY] {changedCount} illuminations changed, invalidating...");
                this.Invalidate();
            }
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
            if (_animationEngine == null)
                return;

            // Process animation commands
            // Format: "name1=state1\x01name2=state2\x01..."
            // State: 0=Stop, 1=Start, 2=Start Reverse
            foreach (var animCmd in e.Animations)
            {
                if (string.IsNullOrEmpty(animCmd))
                    continue;

                var parts = animCmd.Split('=');
                if (parts.Length != 2)
                    continue;

                string animName = parts[0].Trim();
                if (int.TryParse(parts[1], out int state))
                {
                    switch (state)
                    {
                        case 0: // Stop
                            _animationEngine.StopAnimation(animName);
                            break;
                        case 1: // Start
                            _animationEngine.StartAnimation(animName, false);
                            break;
                        case 2: // Start Reverse
                            _animationEngine.StartAnimation(animName, true);
                            break;
                    }
                }
            }

            System.Diagnostics.Debug.WriteLine($"Animations changed: {e.Animations.Length} commands");
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
            _animationEngine?.Dispose();
            
            // Close DMD
            if (_dmdForm != null)
            {
                _dmdForm.Close();
                _dmdForm.Dispose();
            }

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

            // Handle F1 key to show settings
            if (e.KeyCode == Keys.F1)
            {
                ShowSettings();
            }
        }

        private void ShowSettings()
        {
            var settingsForm = new SettingsForm();
            if (settingsForm.ShowDialog(this) == DialogResult.OK)
            {
                // Settings saved, reload if needed
                System.Diagnostics.Debug.WriteLine("Settings updated");
            }
        }
    }
}
