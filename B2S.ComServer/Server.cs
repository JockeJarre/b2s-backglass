using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using Microsoft.Win32;

namespace B2S.ComServer
{
    [ComVisible(true)]
    [Guid("09e233a3-cc79-457a-b49e-f637588891e5")]
    [ClassInterface(ClassInterfaceType.None)]
    [ProgId("B2S.Server")]
    [ComDefaultInterface(typeof(IB2SServer))]
    public class Server : IB2SServer, IDisposable
    {
        private const string EXE_NAME = "B2SBackglassServerEXE.exe";
        
        private object? _vpinmame;
        private Process? _process;
        private System.Timers.Timer? _timer;
        private PluginHost? _pluginHost;
        
        private IntPtr _tableHandle;
        private bool _isBackglassKilled;
        private bool _hidden;
        private bool _launchBackglass = true;
        private string _tableName = string.Empty;
        private string _b2sName = string.Empty;
        private string _gameName = string.Empty;
        private int _b2sSetDataCallCount = 0;

        [DllImport("user32.dll")]
        private static extern bool IsWindow(IntPtr hWnd);
        
        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern IntPtr SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);
        
        private const uint WM_SYSCOMMAND = 0x0112;
        private const int SC_CLOSE = 0xF060;

        public Server()
        {
            Logger.LogSeparator("NEW B2S.Server INSTANCE");
            Logger.Log($"Constructor called. Assembly: {typeof(Server).Assembly.Location}");
            Logger.Log($"Current directory: {Directory.GetCurrentDirectory()}");
            Logger.Log($"AppDomain base: {AppDomain.CurrentDomain.BaseDirectory}");
            
            try
            {
                RegistryHelper.InitializeRegistry();
                Logger.Log("Registry initialized");
                
                LoadSettings();
                Logger.Log("Settings loaded");
                
                InitializePlugins();
                Logger.Log("Plugins initialized");
                
                InitializeTimer();
                Logger.Log("Timer initialized");
                
                Logger.Log("Constructor completed successfully");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Constructor");
                throw;
            }
        }

        private void LoadSettings()
        {
            Logger.Log("Loading settings...");
            
            bool pluginsOn = ReadRegistrySetting("Software\\B2S", "ArePluginsOn", "0") == "1";
            Logger.Log($"Plugins enabled: {pluginsOn}");
            
            if (pluginsOn)
            {
                try
                {
                    _pluginHost = new PluginHost(true);
                    Logger.Log($"PluginHost created, count: {_pluginHost.PluginCount}");
                    RegistryHelper.SetValue("Plugins", _pluginHost.PluginCount);
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Creating PluginHost");
                }
            }
            else
            {
                RegistryHelper.SetValue("Plugins", 0);
            }
        }

        private void InitializePlugins()
        {
            // Plugins are initialized in LoadSettings
        }

        private void InitializeTimer()
        {
            _timer = new System.Timers.Timer(37); // ~27 FPS polling
            _timer.Elapsed += Timer_Elapsed;
        }

        private void Timer_Elapsed(object? sender, ElapsedEventArgs e)
        {
            try
            {
                if (_tableHandle != IntPtr.Zero && !IsWindow(_tableHandle))
                {
                    _timer?.Stop();
                    Stop();
                    Dispose();
                }
            }
            catch
            {
                // Silently handle timer errors
            }
        }

        private object VPinMAME
        {
            get
            {
                if (_vpinmame == null)
                {
                    Logger.Log("Creating VPinMAME.Controller instance...");
                    Type? type = Type.GetTypeFromProgID("VPinMAME.Controller");
                    if (type != null)
                    {
                        _vpinmame = Activator.CreateInstance(type);
                        Logger.Log($"VPinMAME.Controller created: {_vpinmame?.GetType().FullName}");
                    }
                    else
                    {
                        Logger.Log("ERROR: VPinMAME.Controller ProgID not found!");
                        throw new COMException("VPinMAME.Controller not found");
                    }
                }
                return _vpinmame;
            }
        }

        private string FindBackglassFile()
        {
            string filename = $"{_tableName}.directb2s";
            
            if (File.Exists(filename))
            {
                return filename;
            }
            
            if (!string.IsNullOrEmpty(_gameName) && File.Exists($"{_gameName}.directb2s"))
            {
                return $"{_gameName}.directb2s";
            }
            
            // Fuzzy matching logic
            if (!string.IsNullOrEmpty(_tableName))
            {
                var searchPattern = Path.GetFileNameWithoutExtension(_tableName) + "*.directb2s";
                var files = Directory.GetFiles(Directory.GetCurrentDirectory(), searchPattern);
                if (files.Length > 0)
                {
                    return Path.GetFileName(files[0]);
                }
            }
            
            return filename;
        }

        private void StartBackglassEXE()
        {
            Logger.Log("=== StartBackglassEXE() BEGIN ===");
            Logger.Log($"Looking for: {EXE_NAME}");
            Logger.Log($"Current directory: {Directory.GetCurrentDirectory()}");
            Logger.Log($"AppDomain base: {AppDomain.CurrentDomain.BaseDirectory}");
            
            // Get the directory where B2S.ComServer.dll is located
            string? dllDirectory = Path.GetDirectoryName(typeof(Server).Assembly.Location);
            Logger.Log($"DLL directory: {dllDirectory}");
            
            string? exePath = null;
            
            string path1 = Path.Combine(Directory.GetCurrentDirectory(), EXE_NAME);
            string path2 = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, EXE_NAME);
            string path3 = !string.IsNullOrEmpty(dllDirectory) ? Path.Combine(dllDirectory, EXE_NAME) : string.Empty;
            
            Logger.Log($"Checking path 1 (current dir): {path1} - Exists: {File.Exists(path1)}");
            Logger.Log($"Checking path 2 (AppDomain): {path2} - Exists: {File.Exists(path2)}");
            Logger.Log($"Checking path 3 (DLL dir): {path3} - Exists: {(!string.IsNullOrEmpty(path3) ? File.Exists(path3).ToString() : "N/A")}");

            if (!string.IsNullOrEmpty(path3) && File.Exists(path3))
            {
                exePath = dllDirectory;
                Logger.Log($"Using DLL directory: {exePath}");
            }
            else if (File.Exists(path1))
            {
                exePath = Directory.GetCurrentDirectory();
                Logger.Log($"Using current directory: {exePath}");
            }
            else if (File.Exists(path2))
            {
                exePath = AppDomain.CurrentDomain.BaseDirectory;
                Logger.Log($"Using AppDomain base: {exePath}");
            }
            
            if (exePath == null)
            {
                Logger.Log($"ERROR: Cannot find '{EXE_NAME}' in any location!");
                
                // List files in DLL directory for debugging
                if (!string.IsNullOrEmpty(dllDirectory))
                {
                    try
                    {
                        var files = Directory.GetFiles(dllDirectory, "*.exe");
                        Logger.Log($"EXE files in DLL dir: {string.Join(", ", files.Select(Path.GetFileName))}");
                    }
                    catch (Exception ex)
                    {
                        Logger.Log($"Could not list DLL directory: {ex.Message}");
                    }
                }
                
                throw new FileNotFoundException($"Cannot find '{EXE_NAME}'");
            }
            
            Logger.Log($"Setting registry values: GameName='{_gameName}', B2SName='{_b2sName}'");
            RegistryHelper.SetValue("B2SGameName", _gameName);
            RegistryHelper.SetValue("B2SB2SName", _b2sName);
            RegistryHelper.CleanupBackglassRegistry();
            Logger.Log("Registry cleanup done");
            
            string fullExePath = Path.Combine(exePath, EXE_NAME);
            string arguments = $"\"{_tableName}\" \"0\"";
            
            Logger.Log($"Starting process: {fullExePath}");
            Logger.Log($"Arguments: {arguments}");
            
            try
            {
                _process = new Process();
                _process.StartInfo.FileName = fullExePath;
                _process.StartInfo.Arguments = arguments;
                _process.StartInfo.UseShellExecute = true;
                _process.StartInfo.WorkingDirectory = Directory.GetCurrentDirectory();
                
                bool started = _process.Start();
                Logger.Log($"Process.Start() returned: {started}");
                Logger.Log($"Process ID: {_process.Id}");
                Logger.Log("=== StartBackglassEXE() END ===");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Process.Start()");
                throw;
            }
        }

        private static string ReadRegistrySetting(string keyPath, string valueName, string defaultValue)
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(keyPath))
                {
                    return key?.GetValue(valueName, defaultValue)?.ToString() ?? defaultValue;
                }
            }
            catch
            {
                return defaultValue;
            }
        }

        #region IB2SServer Implementation

        public string B2SServerVersion => B2SVersionInfo.B2S_VERSION_STRING;

        public double B2SBuildVersion
        {
            get
            {
                int major = int.Parse(B2SVersionInfo.B2S_VERSION_MAJOR);
                int minor = int.Parse(B2SVersionInfo.B2S_VERSION_MINOR);
                int revision = int.Parse(B2SVersionInfo.B2S_VERSION_REVISION);
                int build = int.Parse(B2SVersionInfo.B2S_VERSION_BUILD);
                return major * 10000 + minor * 100 + revision + build / 10000.0;
            }
        }

        public string B2SServerDirectory => System.Reflection.Assembly.GetExecutingAssembly().Location;

        public string GameName
        {
            get => InvokeVPMProperty<string>("GameName") ?? string.Empty;
            set
            {
                Logger.Log($"GameName SET: '{value}' (was: '{_gameName}')");
                InvokeVPMProperty("GameName", value);
                _gameName = value;
                _b2sName = string.Empty;
            }
        }

        public string ROMName => InvokeVPMProperty<string>("ROMName") ?? string.Empty;

        public string B2SName
        {
            get => _b2sName;
            set
            {
                Logger.Log($"B2SName SET: '{value}' (was: '{_b2sName}')");
                _b2sName = value.Replace(" ", "");
                _gameName = string.Empty;
            }
        }

        public string TableName
        {
            get => _tableName;
            set
            {
                Logger.Log($"TableName SET: '{value}' (was: '{_tableName}')");
                _tableName = value;
                // Backglass EXE is started in Run() - no auto-start here
                // For EM tables: script calls Run() explicitly
                // For ROM tables: VPinMAME integration handles it
            }
        }

        public string WorkingDir
        {
            set
            {
                Logger.Log($"WorkingDir SET: '{value}'");
                try
                {
                    Directory.SetCurrentDirectory(value);
                    Logger.Log($"Current directory now: {Directory.GetCurrentDirectory()}");
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Setting WorkingDir");
                    throw;
                }
            }
        }

        public void SetPath(string path)
        {
            Logger.Log($"SetPath called: '{path}'");
            try
            {
                Directory.SetCurrentDirectory(path);
                Logger.Log($"Current directory now: {Directory.GetCurrentDirectory()}");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "SetPath");
                throw;
            }
        }

        public object Games(string gamename)
        {
            return InvokeVPMMethod("Games", gamename) ?? new object();
        }

        public object Settings => InvokeVPMProperty<object>("Settings") ?? new object();

        public bool Running => InvokeVPMProperty<bool>("Running");

        public double TimeFence
        {
            set => InvokeVPMProperty("TimeFence", value);
        }

        public bool Pause
        {
            get => InvokeVPMProperty<bool>("Pause");
            set
            {
                InvokeVPMProperty("Pause", value);
                if (_pluginHost != null)
                {
                    if (value)
                        _pluginHost.PinMamePause();
                    else
                        _pluginHost.PinMameContinue();
                }
            }
        }

        public string Version => InvokeVPMProperty<string>("Version") ?? string.Empty;

        public double PMBuildVersion => InvokeVPMProperty<double>("PMBuildVersion");

        public void Run(object handle = null!)
        {
            Logger.LogSeparator("RUN CALLED");
            Logger.Log($"Run() called with handle: {handle}");
            Logger.Log($"Current state: TableName='{_tableName}', GameName='{_gameName}', B2SName='{_b2sName}'");
            Logger.Log($"LaunchBackglass={_launchBackglass}, CurrentDir='{Directory.GetCurrentDirectory()}'");
            
            try
            {
                int handleValue = handle != null ? Convert.ToInt32(handle) : 0;
                _tableHandle = new IntPtr(handleValue);
                Logger.Log($"Table handle: {_tableHandle}");
                
                if (_pluginHost != null)
                {
                    string tableFile = Path.Combine(Directory.GetCurrentDirectory(), $"{_tableName}.vpt");
                    string romName = !string.IsNullOrEmpty(B2SName) ? B2SName : GameName;
                    Logger.Log($"Initializing plugins: tableFile='{tableFile}', romName='{romName}'");
                    _pluginHost.PluginInit(tableFile, romName);
                }
                
                if (_launchBackglass)
                {
                    Logger.Log("About to launch backglass EXE...");
                    StartBackglassEXE();
                    Logger.Log("StartBackglassEXE() completed");
                }
                else
                {
                    Logger.Log("LaunchBackglass is FALSE - skipping backglass EXE");
                }
                
                Logger.Log("Calling VPinMAME.Run()...");
                InvokeVPMMethod("Run", handleValue);
                Logger.Log("VPinMAME.Run() completed");
                
                if (_pluginHost != null)
                {
                    _pluginHost.PinMameRun();
                    Logger.Log("Plugin PinMameRun() called");
                }
                
                _timer?.Start();
                Logger.Log("Timer started. Run() completed successfully.");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Run()");
                throw;
            }
        }

        public void Stop()
        {
            Logger.LogSeparator("STOP CALLED");
            Logger.Log("Stop() called");
            
            try
            {
                _timer?.Stop();
                Logger.Log("Timer stopped");
                
                if (_process != null)
                {
                    Logger.Log($"Process state: HasExited={_process.HasExited}");
                    if (!_process.HasExited)
                    {
                        _process.Kill();
                        Logger.Log("Process killed");
                    }
                    _process.Dispose();
                    _process = null;
                }
                
                Logger.Log("Calling VPinMAME.Stop()...");
                InvokeVPMMethod("Stop");
                
                if (_pluginHost != null)
                {
                    _pluginHost.PinMameStop();
                    _pluginHost.PluginFinish();
                    Logger.Log("Plugins stopped");
                }
                
                Logger.Log("Stop() completed");
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "Stop()");
            }
        }

        public bool LaunchBackglass
        {
            get => _launchBackglass;
            set
            {
                Logger.Log($"LaunchBackglass SET: {value} (was: {_launchBackglass})");
                _launchBackglass = value;
            }
        }

        public string SplashInfoLine
        {
            get => InvokeVPMProperty<string>("SplashInfoLine") ?? string.Empty;
            set => InvokeVPMProperty("SplashInfoLine", value);
        }

        public bool ShowFrame
        {
            get => InvokeVPMProperty<bool>("ShowFrame");
            set => InvokeVPMProperty("ShowFrame", value);
        }

        public bool ShowTitle
        {
            get => InvokeVPMProperty<bool>("ShowTitle");
            set => InvokeVPMProperty("ShowTitle", value);
        }

        public bool ShowDMDOnly
        {
            get => InvokeVPMProperty<bool>("ShowDMDOnly");
            set => InvokeVPMProperty("ShowDMDOnly", value);
        }

        public bool ShowPinDMD
        {
            get => InvokeVPMProperty<bool>("ShowPinDMD");
            set => InvokeVPMProperty("ShowPinDMD", value);
        }

        public bool LockDisplay
        {
            get => InvokeVPMProperty<bool>("LockDisplay");
            set => InvokeVPMProperty("LockDisplay", value);
        }

        public bool DoubleSize
        {
            get => InvokeVPMProperty<bool>("DoubleSize");
            set => InvokeVPMProperty("DoubleSize", value);
        }

        public bool Hidden
        {
            get => _hidden;
            set
            {
                _hidden = value;
                InvokeVPMProperty("hidden", value);
            }
        }

        public void SetDisplayPosition(object x, object y, object handle = null!)
        {
            if (handle != null)
            {
                InvokeVPMMethod("SetDisplayPosition", x, y, handle);
            }
            else
            {
                InvokeVPMMethod("SetDisplayPosition", x, y);
            }
        }

        public void ShowOptsDialog(object handle)
        {
            InvokeVPMMethod("ShowOptsDialog", handle);
        }

        public void ShowPathesDialog(object handle)
        {
            InvokeVPMMethod("ShowPathesDialog", handle);
        }

        public void ShowAboutDialog(object handle)
        {
            InvokeVPMMethod("ShowAboutDialog", handle);
        }

        public void CheckROMS(object showoptions, object handle)
        {
            InvokeVPMMethod("CheckROMS", showoptions, handle);
        }

        public bool PuPHide
        {
            get => false;
            set
            {
                // Intentionally empty - handled by plugins
            }
        }

        public bool HandleKeyboard
        {
            get => InvokeVPMProperty<bool>("HandleKeyboard");
            set => InvokeVPMProperty("HandleKeyboard", value);
        }

        public short HandleMechanics
        {
            get => InvokeVPMProperty<short>("HandleMechanics");
            set => InvokeVPMProperty("HandleMechanics", value);
        }

        public object ChangedLamps
        {
            get
            {
                object? result = InvokeVPMProperty<object>("ChangedLamps");
                if (result != null && result is object[,] array)
                {
                    ProcessLamps(array);
                    if (_pluginHost != null)
                    {
                        _pluginHost.DataReceive('L', result);
                    }
                }
                return result ?? new object[,] { };
            }
        }

        public object ChangedSolenoids
        {
            get
            {
                object? result = InvokeVPMProperty<object>("ChangedSolenoids");
                if (result != null && result is object[,] array)
                {
                    ProcessSolenoids(array);
                    if (_pluginHost != null)
                    {
                        _pluginHost.DataReceive('S', result);
                    }
                }
                return result ?? new object[,] { };
            }
        }

        public object ChangedGIStrings
        {
            get
            {
                object? result = InvokeVPMProperty<object>("ChangedGIStrings");
                if (result != null && result is object[,] array)
                {
                    ProcessGIStrings(array);
                    if (_pluginHost != null)
                    {
                        _pluginHost.DataReceive('G', result);
                    }
                }
                return result ?? new object[,] { };
            }
        }

        public object ChangedLEDs(object mask2, object mask1, object mask3 = null!, object mask4 = null!)
        {
            object? result = InvokeVPMMethod("ChangedLEDs", mask2, mask1, mask3 ?? 0, mask4 ?? 0);
            if (result != null && result is object[,] array)
            {
                ProcessLEDs(array);
                if (_pluginHost != null)
                {
                    _pluginHost.DataReceive('D', result);
                }
            }
            return result ?? new object[,] { };
        }

        public object NewSoundCommands => InvokeVPMProperty<object>("NewSoundCommands") ?? new object[,] { };

        #endregion

        #region Data Processing Methods

        private void ProcessLamps(object[,] lamps)
        {
            for (int i = 0; i <= lamps.GetUpperBound(0); i++)
            {
                int lampId = Convert.ToInt32(lamps[i, 0]);
                int lampState = Convert.ToInt32(lamps[i, 1]);
                RegistryHelper.SetLampState(lampId, lampState > 0);
            }
        }

        private void ProcessSolenoids(object[,] solenoids)
        {
            for (int i = 0; i <= solenoids.GetUpperBound(0); i++)
            {
                int solenoidId = Convert.ToInt32(solenoids[i, 0]);
                int solenoidState = Convert.ToInt32(solenoids[i, 1]);
                RegistryHelper.SetSolenoidState(solenoidId, solenoidState != 0);
            }
        }

        private void ProcessGIStrings(object[,] giStrings)
        {
            for (int i = 0; i <= giStrings.GetUpperBound(0); i++)
            {
                int giStringId = Convert.ToInt32(giStrings[i, 0]) + 1;
                int giStringState = Convert.ToInt32(giStrings[i, 1]);
                RegistryHelper.SetGIStringState(giStringId, giStringState);
            }
        }

        private void ProcessLEDs(object[,] leds)
        {
            // LEDs are processed but detailed logic would go here if needed
        }

        #endregion

        #region B2S Methods

        public void B2SSetData(object idORname, object value)
        {
            // Only log first few calls to avoid spam
            if (_b2sSetDataCallCount < 10)
            {
                Logger.Log($"B2SSetData({idORname}, {value})");
                _b2sSetDataCallCount++;
                if (_b2sSetDataCallCount == 10)
                    Logger.Log("(Suppressing further B2SSetData logs...)");
            }
            
            if (int.TryParse(idORname.ToString(), out int id))
            {
                RegistryHelper.SetDataValue(id, Convert.ToInt32(value));
                
                if (_pluginHost != null)
                {
                    _pluginHost.DataReceive('E', id, Convert.ToInt32(value));
                }
            }
        }

        public void B2SSetDataByName(object name, object value)
        {
            // Group name handling
            string groupName = name.ToString() ?? string.Empty;
            int val = Convert.ToInt32(value);
            
            using (var key = Registry.CurrentUser.OpenSubKey(@"Software\B2S", true))
            {
                if (key != null)
                {
                    string illuGroups = key.GetValue("B2SIlluGroupsByName", string.Empty)?.ToString() ?? string.Empty;
                    key.SetValue("B2SIlluGroupsByName", illuGroups + "\x01" + groupName + "=" + val);
                }
            }
        }

        public void B2SSetFlash(object idORname)
        {
            if (int.TryParse(idORname.ToString(), out int id))
            {
                RegistryHelper.SetDataValue(id, 1);
                RegistryHelper.SetDataValue(id, 0);
            }
        }

        public void B2SSetPos(object idORname, object xpos, object ypos)
        {
            if (int.TryParse(idORname.ToString(), out int id) && 
                int.TryParse(xpos.ToString(), out int x) && 
                int.TryParse(ypos.ToString(), out int y))
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\B2S", true))
                {
                    if (key != null)
                    {
                        string positions = key.GetValue("B2SPositions", string.Empty)?.ToString() ?? string.Empty;
                        key.SetValue("B2SPositions", positions + $"\x01{id},{x},{y}");
                    }
                }
            }
        }

        public void B2SSetIllumination(object name, object value)
        {
            B2SSetDataByName(name, value);
        }

        public void B2SSetLED(object digit, object valueORtext)
        {
            int digitNum = Convert.ToInt32(digit);
            RegistryHelper.SetLEDValue(digitNum, valueORtext);
        }

        public void B2SSetLEDDisplay(object display, object text)
        {
            // Display handling would go here
        }

        public void B2SSetReel(object digit, object value)
        {
            int digitNum = Convert.ToInt32(digit);
            RegistryHelper.SetLEDValue(digitNum, value);
        }

        public void B2SSetScore(object display, object value)
        {
            // Score handling
        }

        public void B2SSetScorePlayer(object playerno, object score)
        {
            if (_pluginHost != null)
            {
                _pluginHost.DataReceive('C', Convert.ToInt32(playerno), Convert.ToInt32(score));
            }
        }

        public void B2SSetScorePlayer1(object score) => B2SSetScorePlayer(1, score);
        public void B2SSetScorePlayer2(object score) => B2SSetScorePlayer(2, score);
        public void B2SSetScorePlayer3(object score) => B2SSetScorePlayer(3, score);
        public void B2SSetScorePlayer4(object score) => B2SSetScorePlayer(4, score);
        public void B2SSetScorePlayer5(object score) => B2SSetScorePlayer(5, score);
        public void B2SSetScorePlayer6(object score) => B2SSetScorePlayer(6, score);

        public void B2SSetScoreDigit(object digit, object value)
        {
            // Score digit handling
        }

        public void B2SSetScoreRollover(object id, object value) => B2SSetData(id, value);
        public void B2SSetScoreRolloverPlayer1(object value) => B2SSetData(25, value);
        public void B2SSetScoreRolloverPlayer2(object value) => B2SSetData(26, value);
        public void B2SSetScoreRolloverPlayer3(object value) => B2SSetData(27, value);
        public void B2SSetScoreRolloverPlayer4(object value) => B2SSetData(28, value);

        public void B2SSetCredits(object digitORvalue, object value = null!)
        {
            // Credits handling
        }

        public void B2SSetPlayerUp(object idORvalue, object value = null!)
        {
            if (value == null)
                B2SSetData(30, idORvalue);
            else
                B2SSetData(idORvalue, value);
        }

        public void B2SSetCanPlay(object idORvalue, object value = null!)
        {
            if (value == null)
                B2SSetData(31, idORvalue);
            else
                B2SSetData(idORvalue, value);
        }

        public void B2SSetBallInPlay(object idORvalue, object value = null!)
        {
            if (value == null)
                B2SSetData(32, idORvalue);
            else
                B2SSetData(idORvalue, value);
        }

        public void B2SSetTilt(object idORvalue, object value = null!)
        {
            if (value == null)
                B2SSetData(33, idORvalue);
            else
                B2SSetData(idORvalue, value);
        }

        public void B2SSetMatch(object idORvalue, object value = null!)
        {
            if (value == null)
                B2SSetData(34, idORvalue);
            else
                B2SSetData(idORvalue, value);
        }

        public void B2SSetGameOver(object idORvalue, object value = null!)
        {
            if (value == null)
                B2SSetData(35, idORvalue);
            else
                B2SSetData(idORvalue, value);
        }

        public void B2SSetShootAgain(object idORvalue, object value = null!)
        {
            if (value == null)
                B2SSetData(36, idORvalue);
            else
                B2SSetData(idORvalue, value);
        }

        public void B2SStartAnimation(string animationname, bool playreverse = false)
        {
            RegistryHelper.SetAnimation(animationname, playreverse ? 2 : 1);
        }

        public void B2SStartAnimationReverse(string animationname)
        {
            B2SStartAnimation(animationname, true);
        }

        public void B2SStopAnimation(string animationname)
        {
            RegistryHelper.SetAnimation(animationname, 0);
        }

        public void B2SStopAllAnimations()
        {
            // Stop all animations logic
        }

        public bool B2SIsAnimationRunning(string animationname)
        {
            return false; // Would need state tracking
        }

        public void StartAnimation(string animationname, bool playreverse = false)
        {
            B2SStartAnimation(animationname, playreverse);
        }

        public void StopAnimation(string animationname)
        {
            B2SStopAnimation(animationname);
        }

        public void B2SStartRotation()
        {
            RegistryHelper.SetValue("B2SRotations", "1");
        }

        public void B2SStopRotation()
        {
            RegistryHelper.SetValue("B2SRotations", "0");
        }

        public void B2SShowScoreDisplays()
        {
            RegistryHelper.SetValue("B2SHideScoreDisplays", 0);
        }

        public void B2SHideScoreDisplays()
        {
            RegistryHelper.SetValue("B2SHideScoreDisplays", 1);
        }

        public void B2SStartSound(string soundname)
        {
            // Sound handling
        }

        public void B2SPlaySound(string soundname)
        {
            B2SStartSound(soundname);
        }

        public void B2SStopSound(string soundname)
        {
            // Sound handling
        }

        public void B2SMapSound(object digit, string soundname)
        {
            // Sound mapping
        }

        #endregion

        #region Helper Methods

        private T? InvokeVPMProperty<T>(string propertyName)
        {
            try
            {
                var prop = VPinMAME.GetType().GetProperty(propertyName);
                if (prop != null)
                {
                    object? result = prop.GetValue(VPinMAME);
                    if (result is T typedResult)
                        return typedResult;
                    if (result != null && typeof(T).IsAssignableFrom(result.GetType()))
                        return (T)result;
                }
            }
            catch
            {
            }
            return default;
        }

        private void InvokeVPMProperty(string propertyName, object value)
        {
            try
            {
                var prop = VPinMAME.GetType().GetProperty(propertyName);
                prop?.SetValue(VPinMAME, value);
            }
            catch
            {
            }
        }

        private object? InvokeVPMMethod(string methodName, params object[] parameters)
        {
            try
            {
                return VPinMAME.GetType().InvokeMember(
                    methodName,
                    System.Reflection.BindingFlags.InvokeMethod,
                    null,
                    VPinMAME,
                    parameters);
            }
            catch
            {
                return null;
            }
        }

        #endregion

        #region IDisposable

        private bool _disposed;

        public void Dispose()
        {
            Logger.Log($"Dispose() called, _disposed={_disposed}");
            
            if (!_disposed)
            {
                try
                {
                    _timer?.Stop();
                    _timer?.Dispose();
                    Logger.Log("Timer disposed");
                    
                    _process?.Dispose();
                    Logger.Log("Process disposed");
                    
                    _pluginHost?.Dispose();
                    Logger.Log("PluginHost disposed");
                    
                    if (_vpinmame != null && Marshal.IsComObject(_vpinmame))
                    {
                        Marshal.ReleaseComObject(_vpinmame);
                        _vpinmame = null;
                        Logger.Log("VPinMAME COM object released");
                    }
                    
                    _disposed = true;
                    GC.SuppressFinalize(this);
                    Logger.Log("Dispose() completed");
                }
                catch (Exception ex)
                {
                    Logger.LogException(ex, "Dispose()");
                }
            }
        }

        ~Server()
        {
            Dispose();
        }

        #endregion
    }
}
