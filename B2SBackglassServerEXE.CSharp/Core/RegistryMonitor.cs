using System;
using System.Text;
using System.Windows.Forms;
using Microsoft.Win32;

namespace B2SBackglassServerEXE.Core
{
    /// <summary>
    /// Monitors Windows Registry for changes from the COM server
    /// </summary>
    public class RegistryMonitor : IDisposable
    {
        private const string REGISTRY_KEY = "Software\\B2S";
        private readonly Timer _pollTimer;
        
        // Cached values for change detection
        private string _lastLampsValue = string.Empty;
        private string _lastSolenoidsValue = string.Empty;
        private string _lastGIStringsValue = string.Empty;
        private string _lastAnimationsValue = string.Empty;
        private string _lastSetDataValue = string.Empty;

        // Events for state changes
        public event EventHandler<LampStateChangedEventArgs>? LampsChanged;
        public event EventHandler<SolenoidStateChangedEventArgs>? SolenoidsChanged;
        public event EventHandler<GIStringStateChangedEventArgs>? GIStringsChanged;
        public event EventHandler<AnimationChangedEventArgs>? AnimationsChanged;
        public event EventHandler<DataChangedEventArgs>? DataChanged;

        public bool IsMonitoring { get; private set; }

        public RegistryMonitor()
        {
            _pollTimer = new Timer();
            _pollTimer.Interval = 37; // ~27 FPS, matches VB version
            _pollTimer.Tick += PollTimer_Tick;
        }

        public void StartMonitoring()
        {
            if (IsMonitoring)
                return;

            // Initialize cached values
            _lastLampsValue = ReadRegistryValue("B2SLamps") ?? string.Empty;
            _lastSolenoidsValue = ReadRegistryValue("B2SSolenoids") ?? string.Empty;
            _lastGIStringsValue = ReadRegistryValue("B2SGIStrings") ?? string.Empty;
            _lastAnimationsValue = ReadRegistryValue("B2SAnimations") ?? string.Empty;
            _lastSetDataValue = ReadRegistryValue("B2SSetData") ?? string.Empty;

            _pollTimer.Start();
            IsMonitoring = true;
        }

        public void StopMonitoring()
        {
            _pollTimer.Stop();
            IsMonitoring = false;
        }

        private void PollTimer_Tick(object? sender, EventArgs e)
        {
            try
            {
                CheckForChanges();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Registry poll error: {ex.Message}");
            }
        }

        private void CheckForChanges()
        {
            // Check lamps
            string currentLamps = ReadRegistryValue("B2SLamps") ?? string.Empty;
            if (currentLamps != _lastLampsValue)
            {
                ProcessLampChanges(currentLamps);
                _lastLampsValue = currentLamps;
            }

            // Check solenoids
            string currentSolenoids = ReadRegistryValue("B2SSolenoids") ?? string.Empty;
            if (currentSolenoids != _lastSolenoidsValue)
            {
                ProcessSolenoidChanges(currentSolenoids);
                _lastSolenoidsValue = currentSolenoids;
            }

            // Check GI strings
            string currentGIStrings = ReadRegistryValue("B2SGIStrings") ?? string.Empty;
            if (currentGIStrings != _lastGIStringsValue)
            {
                ProcessGIStringChanges(currentGIStrings);
                _lastGIStringsValue = currentGIStrings;
            }

            // Check animations
            string currentAnimations = ReadRegistryValue("B2SAnimations") ?? string.Empty;
            if (currentAnimations != _lastAnimationsValue)
            {
                ProcessAnimationChanges(currentAnimations);
                _lastAnimationsValue = currentAnimations;
            }

            // Check set data
            string currentSetData = ReadRegistryValue("B2SSetData") ?? string.Empty;
            if (currentSetData != _lastSetDataValue)
            {
                ProcessDataChanges(currentSetData);
                _lastSetDataValue = currentSetData;
            }
        }

        private void ProcessLampChanges(string lampsValue)
        {
            if (LampsChanged == null || string.IsNullOrEmpty(lampsValue))
                return;

            // Parse lamp states (string of '0' and '1' chars)
            var lampStates = new bool[lampsValue.Length];
            for (int i = 0; i < lampsValue.Length; i++)
            {
                lampStates[i] = lampsValue[i] == '1';
            }

            LampsChanged?.Invoke(this, new LampStateChangedEventArgs(lampStates));
        }

        private void ProcessSolenoidChanges(string solenoidsValue)
        {
            if (SolenoidsChanged == null || string.IsNullOrEmpty(solenoidsValue))
                return;

            var solenoidStates = new bool[solenoidsValue.Length];
            for (int i = 0; i < solenoidsValue.Length; i++)
            {
                solenoidStates[i] = solenoidsValue[i] == '1';
            }

            SolenoidsChanged?.Invoke(this, new SolenoidStateChangedEventArgs(solenoidStates));
        }

        private void ProcessGIStringChanges(string giStringsValue)
        {
            if (GIStringsChanged == null || string.IsNullOrEmpty(giStringsValue))
                return;

            var giStates = new int[giStringsValue.Length];
            for (int i = 0; i < giStringsValue.Length; i++)
            {
                giStates[i] = giStringsValue[i] - '0'; // Convert char to int
            }

            GIStringsChanged?.Invoke(this, new GIStringStateChangedEventArgs(giStates));
        }

        private void ProcessAnimationChanges(string animationsValue)
        {
            if (AnimationsChanged == null || string.IsNullOrEmpty(animationsValue))
                return;

            // Format: "name1=state1\x01name2=state2\x01..."
            var animations = animationsValue.Split('\x01');
            
            AnimationsChanged?.Invoke(this, new AnimationChangedEventArgs(animations));
        }

        private void ProcessDataChanges(string setDataValue)
        {
            if (DataChanged == null || string.IsNullOrEmpty(setDataValue))
                return;

            DataChanged?.Invoke(this, new DataChangedEventArgs(setDataValue));
        }

        private string? ReadRegistryValue(string valueName)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(REGISTRY_KEY))
                {
                    return key?.GetValue(valueName) as string;
                }
            }
            catch
            {
                return null;
            }
        }

        public void Dispose()
        {
            _pollTimer?.Stop();
            _pollTimer?.Dispose();
        }
    }

    // Event argument classes
    public class LampStateChangedEventArgs : EventArgs
    {
        public bool[] States { get; }
        public LampStateChangedEventArgs(bool[] states) => States = states;
    }

    public class SolenoidStateChangedEventArgs : EventArgs
    {
        public bool[] States { get; }
        public SolenoidStateChangedEventArgs(bool[] states) => States = states;
    }

    public class GIStringStateChangedEventArgs : EventArgs
    {
        public int[] States { get; }
        public GIStringStateChangedEventArgs(int[] states) => States = states;
    }

    public class AnimationChangedEventArgs : EventArgs
    {
        public string[] Animations { get; }
        public AnimationChangedEventArgs(string[] animations) => Animations = animations;
    }

    public class DataChangedEventArgs : EventArgs
    {
        public string Data { get; }
        public DataChangedEventArgs(string data) => Data = data;
    }
}
