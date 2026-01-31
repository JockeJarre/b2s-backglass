using System;
using System.Drawing;
using System.Windows.Forms;

namespace B2SBackglassServerEXE.Forms
{
    public partial class SettingsForm : Form
    {
        private Core.B2SSettings _settings;

        public SettingsForm()
        {
            _settings = Core.B2SSettings.Instance;
            InitializeComponent();
            LoadSettings();
        }

        private void LoadSettings()
        {
            this.Text = $"B2S Settings - {Program.TableFileName}";

            chkHideBackglass.Checked = _settings.HideBackglass;
            chkHideDMD.Checked = _settings.HideDMD;
            chkFormToFront.Checked = _settings.FormToFront;
            chkStartAsEXE.Checked = _settings.StartAsEXE;
            
            numLampsSkipFrames.Value = _settings.LampsSkipFrames;
            numSolenoidsSkipFrames.Value = _settings.SolenoidsSkipFrames;
            numGISkipFrames.Value = _settings.GIStringsSkipFrames;
            numLEDsSkipFrames.Value = _settings.LEDsSkipFrames;

            chkDebugLog.Checked = false; // Not implemented yet
            chkLampsLog.Checked = _settings.IsLampsStateLogOn;
            chkSolenoidsLog.Checked = _settings.IsSolenoidsStateLogOn;
            chkGILog.Checked = _settings.IsGIStringsStateLogOn;
        }

        private void SaveSettings()
        {
            _settings.HideBackglass = chkHideBackglass.Checked;
            _settings.HideDMD = chkHideDMD.Checked;
            _settings.FormToFront = chkFormToFront.Checked;
            _settings.StartAsEXE = chkStartAsEXE.Checked;
            
            _settings.LampsSkipFrames = (int)numLampsSkipFrames.Value;
            _settings.SolenoidsSkipFrames = (int)numSolenoidsSkipFrames.Value;
            _settings.GIStringsSkipFrames = (int)numGISkipFrames.Value;
            _settings.LEDsSkipFrames = (int)numLEDsSkipFrames.Value;

            _settings.IsLampsStateLogOn = chkLampsLog.Checked;
            _settings.IsSolenoidsStateLogOn = chkSolenoidsLog.Checked;
            _settings.IsGIStringsStateLogOn = chkGILog.Checked;

            // Save to XML - simplified for now
            MessageBox.Show("Settings saved!\nNote: Some settings require restart.",
                "Settings Saved",
                MessageBoxButtons.OK,
                MessageBoxIcon.Information);
        }

        private void btnSave_Click(object sender, EventArgs e)
        {
            SaveSettings();
            this.DialogResult = DialogResult.OK;
            this.Close();
        }

        private void btnCancel_Click(object sender, EventArgs e)
        {
            this.DialogResult = DialogResult.Cancel;
            this.Close();
        }

        private void btnApply_Click(object sender, EventArgs e)
        {
            SaveSettings();
        }
    }
}
