using System;
using System.Collections.Generic;
using System.IO;
using System.Xml.Linq;
using Microsoft.Win32;

namespace B2SBackglassServerEXE.Core
{
    /// <summary>
    /// Manages B2S settings from B2STableSettings.xml and registry
    /// </summary>
    public class B2SSettings
    {
        private const string SETTINGS_FILENAME = "B2STableSettings.xml";
        private const string REGISTRY_KEY = "Software\\B2S";

        public static B2SSettings Instance { get; } = new B2SSettings();

        // Settings properties
        public bool StartAsEXE { get; set; } = true;
        public bool HideBackglass { get; set; } = false;
        public bool HideDMD { get; set; } = false;
        public int DMDType { get; set; } = 0;
        public bool FormToFront { get; set; } = true;
        public bool FormToBack { get; set; } = false;
        public bool FormNoFocus { get; set; } = false;
        
        public int LampsSkipFrames { get; set; } = 0;
        public int SolenoidsSkipFrames { get; set; } = 0;
        public int GIStringsSkipFrames { get; set; } = 0;
        public int LEDsSkipFrames { get; set; } = 0;
        
        public bool IsLampsStateLogOn { get; set; } = false;
        public bool IsSolenoidsStateLogOn { get; set; } = false;
        public bool IsGIStringsStateLogOn { get; set; } = false;
        public bool IsLEDsStateLogOn { get; set; } = false;
        
        public string ScreenResFileName { get; set; } = "ScreenRes.txt";
        public string ResFileEnding { get; set; } = ".res";

        private B2SSettings()
        {
            LoadFromRegistry();
        }

        public void Load(string tableName)
        {
            string settingsPath = GetSettingsFilePath(tableName);
            
            if (!File.Exists(settingsPath))
            {
                // Use defaults
                return;
            }

            try
            {
                var doc = XDocument.Load(settingsPath);
                var root = doc.Root;
                
                if (root == null)
                    return;

                // Parse settings from XML
                StartAsEXE = ParseBool(root.Element("StartAsEXE"), true);
                HideBackglass = ParseBool(root.Element("HideB2SBackglass"), false);
                HideDMD = ParseBool(root.Element("HideB2SDMD"), false);
                DMDType = ParseInt(root.Element("DMDType"), 0);
                FormToFront = ParseBool(root.Element("FormToFront"), true);
                FormToBack = ParseBool(root.Element("FormToBack"), false);
                FormNoFocus = ParseBool(root.Element("FormNoFocus"), false);
                
                LampsSkipFrames = ParseInt(root.Element("LampsSkipFrames"), 0);
                SolenoidsSkipFrames = ParseInt(root.Element("SolenoidsSkipFrames"), 0);
                GIStringsSkipFrames = ParseInt(root.Element("GIStringsSkipFrames"), 0);
                LEDsSkipFrames = ParseInt(root.Element("LEDsSkipFrames"), 0);
                
                IsLampsStateLogOn = ParseBool(root.Element("IsLampsStateLogOn"), false);
                IsSolenoidsStateLogOn = ParseBool(root.Element("IsSolenoidsStateLogOn"), false);
                IsGIStringsStateLogOn = ParseBool(root.Element("IsGIStringsStateLogOn"), false);
                IsLEDsStateLogOn = ParseBool(root.Element("IsLEDsStateLogOn"), false);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");
            }
        }

        private void LoadFromRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    if (key != null)
                    {
                        ScreenResFileName = key.GetValue("B2SScreenResFileNameOverride", ScreenResFileName) as string ?? ScreenResFileName;
                        ResFileEnding = key.GetValue("B2SResFileEndingOverride", ResFileEnding) as string ?? ResFileEnding;
                    }
                }
            }
            catch
            {
                // Use defaults if registry read fails
            }
        }

        private string GetSettingsFilePath(string tableName)
        {
            // Try table-specific settings first
            string tableSpecific = Path.Combine(Directory.GetCurrentDirectory(), $"{tableName}.{SETTINGS_FILENAME}");
            if (File.Exists(tableSpecific))
                return tableSpecific;

            // Fall back to global settings
            return Path.Combine(Directory.GetCurrentDirectory(), SETTINGS_FILENAME);
        }

        private bool ParseBool(XElement? element, bool defaultValue)
        {
            if (element == null)
                return defaultValue;

            if (bool.TryParse(element.Value, out bool result))
                return result;

            // Handle "1"/"0" format
            if (element.Value == "1")
                return true;
            if (element.Value == "0")
                return false;

            return defaultValue;
        }

        private int ParseInt(XElement? element, int defaultValue)
        {
            if (element == null)
                return defaultValue;

            if (int.TryParse(element.Value, out int result))
                return result;

            return defaultValue;
        }

        public static string SafeReadRegistry(string keyName, string valueName, string defaultValue)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(keyName))
                {
                    return key?.GetValue(valueName, defaultValue) as string ?? defaultValue;
                }
            }
            catch
            {
                return defaultValue;
            }
        }
    }
}
