using Microsoft.Win32;
using System;
using System.Text;

namespace B2S.ComServer
{
    internal static class RegistryHelper
    {
        private const string B2S_REGISTRY_KEY = @"Software\B2S";
        private const string VPINMAME_REGISTRY_KEY = @"Software\B2S\VPinMAME";

        public static void InitializeRegistry()
        {
            Logger.Log($"InitializeRegistry() - Creating {B2S_REGISTRY_KEY}");
            
            try
            {
                using (var key = Registry.CurrentUser.CreateSubKey(B2S_REGISTRY_KEY))
                {
                    // Clear startup values
                    key.DeleteValue("B2SGameName", false);
                    key.DeleteValue("B2SB2SName", false);
                    Logger.Log("Cleared B2SGameName and B2SB2SName");
                }
                
                Registry.CurrentUser.CreateSubKey(VPINMAME_REGISTRY_KEY);
                Logger.Log($"Created {VPINMAME_REGISTRY_KEY}");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "InitializeRegistry");
                throw;
            }
        }

        public static void SetValue(string valueName, object value)
        {
            using (var key = Registry.CurrentUser.CreateSubKey(B2S_REGISTRY_KEY))
            {
                key.SetValue(valueName, value);
            }
        }

        public static object? GetValue(string valueName, object? defaultValue = null)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(B2S_REGISTRY_KEY))
            {
                return key?.GetValue(valueName, defaultValue);
            }
        }

        public static void DeleteValue(string valueName)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(B2S_REGISTRY_KEY, true))
            {
                key?.DeleteValue(valueName, false);
            }
        }

        public static void SetLampState(int lampId, bool state)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(B2S_REGISTRY_KEY, true))
            {
                if (key == null) return;
                
                var sb = new StringBuilder(GetValue("B2SLamps", new string('0', 401))?.ToString() ?? new string('0', 401));
                
                if (lampId < sb.Length)
                {
                    sb[lampId] = state ? '1' : '0';
                    key.SetValue("B2SLamps", sb.ToString());
                }
            }
        }

        public static void SetSolenoidState(int solenoidId, bool state)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(B2S_REGISTRY_KEY, true))
            {
                if (key == null) return;
                
                var sb = new StringBuilder(GetValue("B2SSolenoids", new string('0', 251))?.ToString() ?? new string('0', 251));
                
                if (solenoidId < sb.Length)
                {
                    sb[solenoidId] = state ? '1' : '0';
                    key.SetValue("B2SSolenoids", sb.ToString());
                }
            }
        }

        public static void SetGIStringState(int giStringId, int state)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(B2S_REGISTRY_KEY, true))
            {
                if (key == null) return;
                
                var sb = new StringBuilder(GetValue("B2SGIStrings", new string('0', 251))?.ToString() ?? new string('0', 251));
                
                if (giStringId < sb.Length)
                {
                    sb[giStringId] = state > 4 ? '5' : '0';
                    key.SetValue("B2SGIStrings", sb.ToString());
                }
            }
        }

        public static void SetDataValue(int id, int value)
        {
            if (id > 250) return;
            
            using (var key = Registry.CurrentUser.OpenSubKey(B2S_REGISTRY_KEY, true))
            {
                if (key == null) return;
                
                var sb = new StringBuilder(GetValue("B2SSetData", new string('\0', 251))?.ToString() ?? new string('\0', 251));
                
                if (id < sb.Length)
                {
                    sb[id] = (char)value;
                    key.SetValue("B2SSetData", sb.ToString());
                }
            }
        }

        public static void SetLEDValue(int digit, object value)
        {
            SetValue($"B2SLED{digit}", value);
        }

        public static void SetAnimation(string animationName, int state)
        {
            using (var key = Registry.CurrentUser.OpenSubKey(B2S_REGISTRY_KEY, true))
            {
                if (key == null) return;
                
                var currentAnimations = (GetValue("B2SAnimations", string.Empty)?.ToString() ?? string.Empty).Split('\x01');
                bool found = false;
                
                for (int i = 0; i < currentAnimations.Length; i++)
                {
                    if (currentAnimations[i].StartsWith(animationName + "="))
                    {
                        currentAnimations[i] = $"{animationName}={state}";
                        found = true;
                        break;
                    }
                }
                
                if (!found)
                {
                    Array.Resize(ref currentAnimations, currentAnimations.Length + 1);
                    currentAnimations[currentAnimations.Length - 1] = $"{animationName}={state}";
                }
                
                key.SetValue("B2SAnimations", string.Join("\x01", currentAnimations));
            }
        }

        public static void CleanupBackglassRegistry()
        {
            using (var key = Registry.CurrentUser.OpenSubKey(B2S_REGISTRY_KEY, true))
            {
                if (key == null) return;
                
                key.DeleteValue("B2SAnimations", false);
                key.DeleteValue("B2SRotations", false);
                key.DeleteValue("B2SSounds", false);
                key.DeleteValue("B2SLamps", false);
                key.DeleteValue("B2SSolenoids", false);
                key.DeleteValue("B2SGIStrings", false);
                key.DeleteValue("B2SSetData", false);
                key.DeleteValue("B2SReloadSettings", false);
                key.DeleteValue("B2SHideScoreDisplays", false);
                key.DeleteValue("B2SIlluGroupsByName", false);
                key.DeleteValue("B2SPositions", false);
                
                for (int i = 1; i <= 5; i++)
                {
                    key.DeleteValue($"B2SMechs{i}", false);
                }
                
                for (int i = 0; i <= 200; i++)
                {
                    key.DeleteValue($"B2SLED{i}", false);
                    key.DeleteValue($"B2SReel{i}", false);
                    key.DeleteValue($"B2SScoreDigit{i}", false);
                    key.DeleteValue($"B2SScoreDigits{i}", false);
                    key.DeleteValue($"B2SScoreDisplay{i}", false);
                    key.DeleteValue($"B2SScorePlayer{i}", false);
                }
                
                key.DeleteValue("B2SBackglassFileVersion", false);
            }
        }
    }
}
