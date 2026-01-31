namespace B2SBackglassServerEXE.Forms
{
    partial class SettingsForm
    {
        private System.ComponentModel.IContainer components = null;
        private System.Windows.Forms.GroupBox grpDisplay;
        private System.Windows.Forms.CheckBox chkHideBackglass;
        private System.Windows.Forms.CheckBox chkHideDMD;
        private System.Windows.Forms.CheckBox chkFormToFront;
        private System.Windows.Forms.CheckBox chkStartAsEXE;
        private System.Windows.Forms.GroupBox grpPerformance;
        private System.Windows.Forms.Label lblLamps;
        private System.Windows.Forms.NumericUpDown numLampsSkipFrames;
        private System.Windows.Forms.Label lblSolenoids;
        private System.Windows.Forms.NumericUpDown numSolenoidsSkipFrames;
        private System.Windows.Forms.Label lblGI;
        private System.Windows.Forms.NumericUpDown numGISkipFrames;
        private System.Windows.Forms.Label lblLEDs;
        private System.Windows.Forms.NumericUpDown numLEDsSkipFrames;
        private System.Windows.Forms.GroupBox grpLogging;
        private System.Windows.Forms.CheckBox chkDebugLog;
        private System.Windows.Forms.CheckBox chkLampsLog;
        private System.Windows.Forms.CheckBox chkSolenoidsLog;
        private System.Windows.Forms.CheckBox chkGILog;
        private System.Windows.Forms.Button btnSave;
        private System.Windows.Forms.Button btnCancel;
        private System.Windows.Forms.Button btnApply;

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
            this.grpDisplay = new System.Windows.Forms.GroupBox();
            this.chkStartAsEXE = new System.Windows.Forms.CheckBox();
            this.chkFormToFront = new System.Windows.Forms.CheckBox();
            this.chkHideDMD = new System.Windows.Forms.CheckBox();
            this.chkHideBackglass = new System.Windows.Forms.CheckBox();
            this.grpPerformance = new System.Windows.Forms.GroupBox();
            this.numLEDsSkipFrames = new System.Windows.Forms.NumericUpDown();
            this.lblLEDs = new System.Windows.Forms.Label();
            this.numGISkipFrames = new System.Windows.Forms.NumericUpDown();
            this.lblGI = new System.Windows.Forms.Label();
            this.numSolenoidsSkipFrames = new System.Windows.Forms.NumericUpDown();
            this.lblSolenoids = new System.Windows.Forms.Label();
            this.numLampsSkipFrames = new System.Windows.Forms.NumericUpDown();
            this.lblLamps = new System.Windows.Forms.Label();
            this.grpLogging = new System.Windows.Forms.GroupBox();
            this.chkGILog = new System.Windows.Forms.CheckBox();
            this.chkSolenoidsLog = new System.Windows.Forms.CheckBox();
            this.chkLampsLog = new System.Windows.Forms.CheckBox();
            this.chkDebugLog = new System.Windows.Forms.CheckBox();
            this.btnSave = new System.Windows.Forms.Button();
            this.btnCancel = new System.Windows.Forms.Button();
            this.btnApply = new System.Windows.Forms.Button();
            this.grpDisplay.SuspendLayout();
            this.grpPerformance.SuspendLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLEDsSkipFrames)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGISkipFrames)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSolenoidsSkipFrames)).BeginInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLampsSkipFrames)).BeginInit();
            this.grpLogging.SuspendLayout();
            this.SuspendLayout();
            // 
            // grpDisplay
            // 
            this.grpDisplay.Controls.Add(this.chkStartAsEXE);
            this.grpDisplay.Controls.Add(this.chkFormToFront);
            this.grpDisplay.Controls.Add(this.chkHideDMD);
            this.grpDisplay.Controls.Add(this.chkHideBackglass);
            this.grpDisplay.Location = new System.Drawing.Point(12, 12);
            this.grpDisplay.Name = "grpDisplay";
            this.grpDisplay.Size = new System.Drawing.Size(250, 130);
            this.grpDisplay.TabIndex = 0;
            this.grpDisplay.TabStop = false;
            this.grpDisplay.Text = "Display Options";
            // 
            // chkStartAsEXE
            // 
            this.chkStartAsEXE.AutoSize = true;
            this.chkStartAsEXE.Location = new System.Drawing.Point(15, 95);
            this.chkStartAsEXE.Name = "chkStartAsEXE";
            this.chkStartAsEXE.Size = new System.Drawing.Size(92, 17);
            this.chkStartAsEXE.TabIndex = 3;
            this.chkStartAsEXE.Text = "Start as EXE";
            this.chkStartAsEXE.UseVisualStyleBackColor = true;
            // 
            // chkFormToFront
            // 
            this.chkFormToFront.AutoSize = true;
            this.chkFormToFront.Location = new System.Drawing.Point(15, 72);
            this.chkFormToFront.Name = "chkFormToFront";
            this.chkFormToFront.Size = new System.Drawing.Size(96, 17);
            this.chkFormToFront.TabIndex = 2;
            this.chkFormToFront.Text = "Form to Front";
            this.chkFormToFront.UseVisualStyleBackColor = true;
            // 
            // chkHideDMD
            // 
            this.chkHideDMD.AutoSize = true;
            this.chkHideDMD.Location = new System.Drawing.Point(15, 49);
            this.chkHideDMD.Name = "chkHideDMD";
            this.chkHideDMD.Size = new System.Drawing.Size(79, 17);
            this.chkHideDMD.TabIndex = 1;
            this.chkHideDMD.Text = "Hide DMD";
            this.chkHideDMD.UseVisualStyleBackColor = true;
            // 
            // chkHideBackglass
            // 
            this.chkHideBackglass.AutoSize = true;
            this.chkHideBackglass.Location = new System.Drawing.Point(15, 26);
            this.chkHideBackglass.Name = "chkHideBackglass";
            this.chkHideBackglass.Size = new System.Drawing.Size(106, 17);
            this.chkHideBackglass.TabIndex = 0;
            this.chkHideBackglass.Text = "Hide Backglass";
            this.chkHideBackglass.UseVisualStyleBackColor = true;
            // 
            // grpPerformance
            // 
            this.grpPerformance.Controls.Add(this.numLEDsSkipFrames);
            this.grpPerformance.Controls.Add(this.lblLEDs);
            this.grpPerformance.Controls.Add(this.numGISkipFrames);
            this.grpPerformance.Controls.Add(this.lblGI);
            this.grpPerformance.Controls.Add(this.numSolenoidsSkipFrames);
            this.grpPerformance.Controls.Add(this.lblSolenoids);
            this.grpPerformance.Controls.Add(this.numLampsSkipFrames);
            this.grpPerformance.Controls.Add(this.lblLamps);
            this.grpPerformance.Location = new System.Drawing.Point(268, 12);
            this.grpPerformance.Name = "grpPerformance";
            this.grpPerformance.Size = new System.Drawing.Size(250, 160);
            this.grpPerformance.TabIndex = 1;
            this.grpPerformance.TabStop = false;
            this.grpPerformance.Text = "Performance (Skip Frames)";
            // 
            // numLEDsSkipFrames
            // 
            this.numLEDsSkipFrames.Location = new System.Drawing.Point(150, 113);
            this.numLEDsSkipFrames.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numLEDsSkipFrames.Name = "numLEDsSkipFrames";
            this.numLEDsSkipFrames.Size = new System.Drawing.Size(80, 20);
            this.numLEDsSkipFrames.TabIndex = 7;
            // 
            // lblLEDs
            // 
            this.lblLEDs.AutoSize = true;
            this.lblLEDs.Location = new System.Drawing.Point(15, 115);
            this.lblLEDs.Name = "lblLEDs";
            this.lblLEDs.Size = new System.Drawing.Size(35, 13);
            this.lblLEDs.TabIndex = 6;
            this.lblLEDs.Text = "LEDs:";
            // 
            // numGISkipFrames
            // 
            this.numGISkipFrames.Location = new System.Drawing.Point(150, 83);
            this.numGISkipFrames.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numGISkipFrames.Name = "numGISkipFrames";
            this.numGISkipFrames.Size = new System.Drawing.Size(80, 20);
            this.numGISkipFrames.TabIndex = 5;
            // 
            // lblGI
            // 
            this.lblGI.AutoSize = true;
            this.lblGI.Location = new System.Drawing.Point(15, 85);
            this.lblGI.Name = "lblGI";
            this.lblGI.Size = new System.Drawing.Size(59, 13);
            this.lblGI.TabIndex = 4;
            this.lblGI.Text = "GI Strings:";
            // 
            // numSolenoidsSkipFrames
            // 
            this.numSolenoidsSkipFrames.Location = new System.Drawing.Point(150, 53);
            this.numSolenoidsSkipFrames.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numSolenoidsSkipFrames.Name = "numSolenoidsSkipFrames";
            this.numSolenoidsSkipFrames.Size = new System.Drawing.Size(80, 20);
            this.numSolenoidsSkipFrames.TabIndex = 3;
            // 
            // lblSolenoids
            // 
            this.lblSolenoids.AutoSize = true;
            this.lblSolenoids.Location = new System.Drawing.Point(15, 55);
            this.lblSolenoids.Name = "lblSolenoids";
            this.lblSolenoids.Size = new System.Drawing.Size(58, 13);
            this.lblSolenoids.TabIndex = 2;
            this.lblSolenoids.Text = "Solenoids:";
            // 
            // numLampsSkipFrames
            // 
            this.numLampsSkipFrames.Location = new System.Drawing.Point(150, 23);
            this.numLampsSkipFrames.Maximum = new decimal(new int[] { 10, 0, 0, 0 });
            this.numLampsSkipFrames.Name = "numLampsSkipFrames";
            this.numLampsSkipFrames.Size = new System.Drawing.Size(80, 20);
            this.numLampsSkipFrames.TabIndex = 1;
            // 
            // lblLamps
            // 
            this.lblLamps.AutoSize = true;
            this.lblLamps.Location = new System.Drawing.Point(15, 25);
            this.lblLamps.Name = "lblLamps";
            this.lblLamps.Size = new System.Drawing.Size(42, 13);
            this.lblLamps.TabIndex = 0;
            this.lblLamps.Text = "Lamps:";
            // 
            // grpLogging
            // 
            this.grpLogging.Controls.Add(this.chkGILog);
            this.grpLogging.Controls.Add(this.chkSolenoidsLog);
            this.grpLogging.Controls.Add(this.chkLampsLog);
            this.grpLogging.Controls.Add(this.chkDebugLog);
            this.grpLogging.Location = new System.Drawing.Point(12, 148);
            this.grpLogging.Name = "grpLogging";
            this.grpLogging.Size = new System.Drawing.Size(250, 130);
            this.grpLogging.TabIndex = 2;
            this.grpLogging.TabStop = false;
            this.grpLogging.Text = "Logging";
            // 
            // chkGILog
            // 
            this.chkGILog.AutoSize = true;
            this.chkGILog.Location = new System.Drawing.Point(15, 95);
            this.chkGILog.Name = "chkGILog";
            this.chkGILog.Size = new System.Drawing.Size(95, 17);
            this.chkGILog.TabIndex = 3;
            this.chkGILog.Text = "Log GI Strings";
            this.chkGILog.UseVisualStyleBackColor = true;
            // 
            // chkSolenoidsLog
            // 
            this.chkSolenoidsLog.AutoSize = true;
            this.chkSolenoidsLog.Location = new System.Drawing.Point(15, 72);
            this.chkSolenoidsLog.Name = "chkSolenoidsLog";
            this.chkSolenoidsLog.Size = new System.Drawing.Size(94, 17);
            this.chkSolenoidsLog.TabIndex = 2;
            this.chkSolenoidsLog.Text = "Log Solenoids";
            this.chkSolenoidsLog.UseVisualStyleBackColor = true;
            // 
            // chkLampsLog
            // 
            this.chkLampsLog.AutoSize = true;
            this.chkLampsLog.Location = new System.Drawing.Point(15, 49);
            this.chkLampsLog.Name = "chkLampsLog";
            this.chkLampsLog.Size = new System.Drawing.Size(78, 17);
            this.chkLampsLog.TabIndex = 1;
            this.chkLampsLog.Text = "Log Lamps";
            this.chkLampsLog.UseVisualStyleBackColor = true;
            // 
            // chkDebugLog
            // 
            this.chkDebugLog.AutoSize = true;
            this.chkDebugLog.Location = new System.Drawing.Point(15, 26);
            this.chkDebugLog.Name = "chkDebugLog";
            this.chkDebugLog.Size = new System.Drawing.Size(98, 17);
            this.chkDebugLog.TabIndex = 0;
            this.chkDebugLog.Text = "Debug Logging";
            this.chkDebugLog.UseVisualStyleBackColor = true;
            // 
            // btnSave
            // 
            this.btnSave.Location = new System.Drawing.Point(268, 245);
            this.btnSave.Name = "btnSave";
            this.btnSave.Size = new System.Drawing.Size(75, 30);
            this.btnSave.TabIndex = 3;
            this.btnSave.Text = "Save";
            this.btnSave.UseVisualStyleBackColor = true;
            this.btnSave.Click += new System.EventHandler(this.btnSave_Click);
            // 
            // btnCancel
            // 
            this.btnCancel.DialogResult = System.Windows.Forms.DialogResult.Cancel;
            this.btnCancel.Location = new System.Drawing.Point(443, 245);
            this.btnCancel.Name = "btnCancel";
            this.btnCancel.Size = new System.Drawing.Size(75, 30);
            this.btnCancel.TabIndex = 5;
            this.btnCancel.Text = "Cancel";
            this.btnCancel.UseVisualStyleBackColor = true;
            this.btnCancel.Click += new System.EventHandler(this.btnCancel_Click);
            // 
            // btnApply
            // 
            this.btnApply.Location = new System.Drawing.Point(355, 245);
            this.btnApply.Name = "btnApply";
            this.btnApply.Size = new System.Drawing.Size(75, 30);
            this.btnApply.TabIndex = 4;
            this.btnApply.Text = "Apply";
            this.btnApply.UseVisualStyleBackColor = true;
            this.btnApply.Click += new System.EventHandler(this.btnApply_Click);
            // 
            // SettingsForm
            // 
            this.AcceptButton = this.btnSave;
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.CancelButton = this.btnCancel;
            this.ClientSize = new System.Drawing.Size(534, 291);
            this.Controls.Add(this.btnApply);
            this.Controls.Add(this.btnCancel);
            this.Controls.Add(this.btnSave);
            this.Controls.Add(this.grpLogging);
            this.Controls.Add(this.grpPerformance);
            this.Controls.Add(this.grpDisplay);
            this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.MinimizeBox = false;
            this.Name = "SettingsForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterParent;
            this.Text = "B2S Settings";
            this.grpDisplay.ResumeLayout(false);
            this.grpDisplay.PerformLayout();
            this.grpPerformance.ResumeLayout(false);
            this.grpPerformance.PerformLayout();
            ((System.ComponentModel.ISupportInitialize)(this.numLEDsSkipFrames)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numGISkipFrames)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numSolenoidsSkipFrames)).EndInit();
            ((System.ComponentModel.ISupportInitialize)(this.numLampsSkipFrames)).EndInit();
            this.grpLogging.ResumeLayout(false);
            this.grpLogging.PerformLayout();
            this.ResumeLayout(false);
        }
    }
}
