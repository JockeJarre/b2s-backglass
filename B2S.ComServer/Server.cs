using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Text;
using System.Timers;
using Microsoft.Win32;

namespace B2S.ComServer
{
    [ComVisible(true)]
    [Guid("09e233a3-cc79-457a-b49e-f637588891e5")]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    [ProgId("B2S.Server")]
    public class Server : IB2SServer, IReflect, IDisposable
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
            try
            {
                var vpm = VPinMAME;
                return vpm.GetType().InvokeMember(
                    "Games",
                    System.Reflection.BindingFlags.GetProperty,
                    null,
                    vpm,
                    new object[] { gamename }) ?? new object();
            }
            catch
            {
                return new object();
            }
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
                int handleValue = (handle != null && handle != Type.Missing && !(handle is System.Reflection.Missing)) ? Convert.ToInt32(handle) : 0;
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

        // Indexed properties thresholds
        private int _lampThreshold = 0;
        private int _giStringThreshold = 4;

        // Accessor objects for indexed properties
        private SwitchAccessor? _switchAccessor;
        private LampAccessor? _lampAccessor;
        private SolenoidAccessor? _solenoidAccessor;
        private GIStringAccessor? _giStringAccessor;
        private MechAccessor? _mechAccessor;
        private GetMechAccessor? _getMechAccessor;
        private DipAccessor? _dipAccessor;
        private SolMaskAccessor? _solMaskAccessor;

        // Properties that return accessor objects - VBScript calls Controller.Switch(32)
        public SwitchAccessor Switch => _switchAccessor ??= new SwitchAccessor(this);
        public LampAccessor Lamp => _lampAccessor ??= new LampAccessor(this);
        public SolenoidAccessor Solenoid => _solenoidAccessor ??= new SolenoidAccessor(this);
        public GIStringAccessor GIString => _giStringAccessor ??= new GIStringAccessor(this);
        public MechAccessor Mech => _mechAccessor ??= new MechAccessor(this);
        public GetMechAccessor GetMech => _getMechAccessor ??= new GetMechAccessor(this);
        public DipAccessor Dip => _dipAccessor ??= new DipAccessor(this);
        public SolMaskAccessor SolMask => _solMaskAccessor ??= new SolMaskAccessor(this);

        // Indexed property implementations - interface methods
        public bool get_Switch(object number) => InvokeVPMIndexedProperty<bool>("Switch", number);
        public void set_Switch(object number, bool value)
        {
            SetVPMIndexedProperty("Switch", number, value);
            if (_pluginHost != null && IsNumeric(number))
            {
                _pluginHost.DataReceive('W', Convert.ToInt32(number), value ? 1 : 0);
            }
        }

        public bool get_Lamp(object number) => InvokeVPMIndexedProperty<bool>("Lamp", number);
        public bool get_Solenoid(object number) => InvokeVPMIndexedProperty<bool>("Solenoid", number);
        public bool get_GIString(object number) => InvokeVPMIndexedProperty<bool>("GIString", number);
        
        public int get_Mech(object number)
        {
            int value = InvokeVPMIndexedProperty<int>("Mech", number);
            if (_pluginHost != null && IsNumeric(number))
            {
                _pluginHost.DataReceive('M', Convert.ToInt32(number), value);
            }
            return value;
        }
        
        public void set_Mech(object number, int value)
        {
            SetVPMIndexedProperty("Mech", number, value);
            if (_pluginHost != null && IsNumeric(number))
            {
                _pluginHost.DataReceive('M', Convert.ToInt32(number), value);
            }
        }

        public object get_GetMech(object number)
        {
            object? result = InvokeVPMIndexedProperty<object>("GetMech", number);
            
            // Write mech values to registry for EXE mode (like VB version does)
            if (IsNumeric(number) && IsNumeric(result))
            {
                int mechId = Convert.ToInt32(number);
                int mechValue = Convert.ToInt32(result);
                
                if (mechId >= 1 && mechId <= 5)
                {
                    RegistryHelper.SetValue($"B2SMechs{mechId}", mechValue);
                }
            }
            
            if (_pluginHost != null && IsNumeric(number) && IsNumeric(result))
            {
                _pluginHost.DataReceive('N', Convert.ToInt32(number), Convert.ToInt32(result));
            }
            return result ?? 0;
        }

        public int get_Dip(object number) => InvokeVPMIndexedProperty<int>("Dip", number);
        public void set_Dip(object number, int value) => SetVPMIndexedProperty("Dip", number, value);

        public int get_SolMask(object number) => InvokeVPMIndexedProperty<int>("SolMask", number);
        public void set_SolMask(object number, int value)
        {
            SetVPMIndexedProperty("SolMask", number, value);
            if (Convert.ToInt32(number) == 2)
            {
                _lampThreshold = value == 2 ? 64 : 0;
                _giStringThreshold = value == 2 ? 64 : 4;
            }
        }

        // DMD Properties - return raw VPinMAME results directly like VB version
        public int RawDmdWidth => InvokeVPMProperty<int>("RawDmdWidth");
        public int RawDmdHeight => InvokeVPMProperty<int>("RawDmdHeight");
        
        public object RawDmdPixels
        {
            get
            {
                try
                {
                    // VB version: Return VPinMAME.RawDmdPixels
                    return VPinMAME.GetType().InvokeMember(
                        "RawDmdPixels",
                        System.Reflection.BindingFlags.GetProperty,
                        null,
                        VPinMAME,
                        null)!;
                }
                catch
                {
                    return null!;
                }
            }
        }
        
        public object RawDmdColoredPixels
        {
            get
            {
                try
                {
                    // VB version: Return VPinMAME.RawDmdColoredPixels
                    return VPinMAME.GetType().InvokeMember(
                        "RawDmdColoredPixels",
                        System.Reflection.BindingFlags.GetProperty,
                        null,
                        VPinMAME,
                        null)!;
                }
                catch
                {
                    return null!;
                }
            }
        }
        
        public object ChangedNVRAM
        {
            get
            {
                try
                {
                    return VPinMAME.GetType().InvokeMember(
                        "ChangedNVRAM",
                        System.Reflection.BindingFlags.GetProperty,
                        null,
                        VPinMAME,
                        null)!;
                }
                catch
                {
                    return null!;
                }
            }
        }
        
        public object NVRAM
        {
            get
            {
                try
                {
                    return VPinMAME.GetType().InvokeMember(
                        "NVRAM",
                        System.Reflection.BindingFlags.GetProperty,
                        null,
                        VPinMAME,
                        null)!;
                }
                catch
                {
                    return null!;
                }
            }
        }
        
        public int SoundMode
        {
            get => InvokeVPMProperty<int>("SoundMode");
            set => InvokeVPMProperty("SoundMode", value);
        }

        private bool IsNumeric(object? value)
        {
            if (value == null) return false;
            return value is sbyte || value is byte || value is short || value is ushort ||
                   value is int || value is uint || value is long || value is ulong ||
                   value is float || value is double || value is decimal;
        }

        private T InvokeVPMIndexedProperty<T>(string propertyName, object index)
        {
            try
            {
                var vpm = VPinMAME;
                object? result = vpm.GetType().InvokeMember(
                    propertyName,
                    System.Reflection.BindingFlags.GetProperty,
                    null,
                    vpm,
                    new object[] { index });
                return result != null ? (T)Convert.ChangeType(result, typeof(T)) : default!;
            }
            catch
            {
                return default!;
            }
        }

        private void SetVPMIndexedProperty(string propertyName, object index, object value)
        {
            try
            {
                var vpm = VPinMAME;
                vpm.GetType().InvokeMember(
                    propertyName,
                    System.Reflection.BindingFlags.SetProperty,
                    null,
                    vpm,
                    new object[] { index, value });
            }
            catch (Exception ex)
            {
                Logger.Log($"SetVPMIndexedProperty({propertyName}, {index}, {value}) FAILED: {ex.Message}");
            }
        }

        // Empty 2D array to return when VPinMAME returns null
        // This matches what VPinMAME returns when there are no changes (an empty SAFEARRAY)
        private static readonly object[,] EmptyChangedArray = new object[0, 2];
        
        public object ChangedLamps
        {
            get
            {
                // ChangedLamps is a property in VPinMAME (VB syntax uses () for property getters too)
                object? result = InvokeVPMProperty<object>("ChangedLamps");
                
                // Debug: log first few returns
                if (_changedLampsCallCount < 5)
                {
                    _changedLampsCallCount++;
                    string resultInfo = result == null ? "null" : (result is object[,] arr ? $"object[,] with {arr.GetLength(0)} rows" : result.GetType().Name);
                    Logger.Log($"ChangedLamps returned: {resultInfo}");
                }
                
                if (result != null && result is object[,] array)
                {
                    if (array.GetLength(0) > 0)
                    {
                        ProcessLamps(array);
                    }
                    if (_pluginHost != null)
                    {
                        _pluginHost.DataReceive('L', result);
                    }
                    return result;
                }
                // Return empty array when VPinMAME returns null
                return EmptyChangedArray;
            }
        }
        private int _changedLampsCallCount = 0;

        public object ChangedSolenoids
        {
            get
            {
                // ChangedSolenoids is a property in VPinMAME
                object? result = InvokeVPMProperty<object>("ChangedSolenoids");
                if (result != null && result is object[,] array)
                {
                    if (array.GetLength(0) > 0)
                    {
                        ProcessSolenoids(array);
                    }
                    if (_pluginHost != null)
                    {
                        _pluginHost.DataReceive('S', result);
                    }
                    return result;
                }
                return EmptyChangedArray;
            }
        }

        public object ChangedGIStrings
        {
            get
            {
                // ChangedGIStrings is a property in VPinMAME
                object? result = InvokeVPMProperty<object>("ChangedGIStrings");
                if (result != null && result is object[,] array)
                {
                    if (array.GetLength(0) > 0)
                    {
                        ProcessGIStrings(array);
                    }
                    if (_pluginHost != null)
                    {
                        _pluginHost.DataReceive('G', result);
                    }
                    return result;
                }
                return EmptyChangedArray;
            }
        }

        public object ChangedLEDs(object mask2, object mask1, object mask3 = null!, object mask4 = null!)
        {
            // ChangedLEDs is an indexed property in VPinMAME, not a method
            // VB syntax: VPinMAME.ChangedLEDs(mask2, mask1, mask3, mask4)
            try
            {
                object? result = VPinMAME.GetType().InvokeMember(
                    "ChangedLEDs",
                    System.Reflection.BindingFlags.GetProperty,
                    null,
                    VPinMAME,
                    new object[] { mask2, mask1, mask3 ?? 0, mask4 ?? 0 });
                    
                if (result != null && result is object[,] array)
                {
                    ProcessLEDs(array);
                    if (_pluginHost != null)
                    {
                        _pluginHost.DataReceive('D', result);
                    }
                }
                return result ?? EmptyChangedArray;
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                Logger.Log($"ChangedLEDs FAILED: {inner.Message}");
                return EmptyChangedArray;
            }
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
            // Write LED data to registry for the backglass EXE to read
            // VB version: digit = leds(i, 0), value = leds(i, 2), writes to B2SLED{digit+1}
            try
            {
                using (var regkey = Registry.CurrentUser.OpenSubKey("Software\\B2S", true))
                {
                    if (regkey != null)
                    {
                        for (int i = 0; i <= leds.GetUpperBound(0); i++)
                        {
                            int digit = Convert.ToInt32(leds[i, 0]);
                            int value = Convert.ToInt32(leds[i, 2]); // Column 2 contains the value
                            regkey.SetValue($"B2SLED{digit + 1}", value);
                        }
                    }
                }
            }
            catch
            {
                // Silently handle registry errors
            }
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
            int playerNum = Convert.ToInt32(playerno);
            int scoreValue = Convert.ToInt32(score);
            
            if (playerNum > 0)
            {
                // In EXE mode, we need to write score digits to the registry
                // The backglass EXE reads B2SScorePlayer{n} to know the digit layout
                // and reads B2SLED{digit} for each digit value
                try
                {
                    using (var regkey = Registry.CurrentUser.OpenSubKey("Software\\B2S", true))
                    {
                        if (regkey != null)
                        {
                            string? scoreInfo = regkey.GetValue($"B2SScorePlayer{playerNum}", string.Empty)?.ToString();
                            if (!string.IsNullOrEmpty(scoreInfo))
                            {
                                // Parse score info: format is "reeltype,ledtype,startdigit,digits;..."
                                var infos = scoreInfo.Split(';');
                                int totalDigits = 0;
                                var digitConfigs = new System.Collections.Generic.List<(int reeltype, int ledtype, int startdigit, int digits)>();
                                
                                foreach (var info in infos)
                                {
                                    var parts = info.Split(',');
                                    if (parts.Length == 4)
                                    {
                                        int reeltype = int.Parse(parts[0]);
                                        int ledtype = int.Parse(parts[1]);
                                        int startdigit = int.Parse(parts[2]);
                                        int digits = int.Parse(parts[3]);
                                        totalDigits += digits;
                                        digitConfigs.Add((reeltype, ledtype, startdigit, digits));
                                    }
                                }
                                
                                if (totalDigits > 0)
                                {
                                    // Format score as string with leading zeros
                                    string scoreStr = scoreValue.ToString().PadLeft(totalDigits, '0');
                                    if (scoreStr.Length > totalDigits)
                                        scoreStr = scoreStr.Substring(scoreStr.Length - totalDigits);
                                    
                                    int charIndex = 0;
                                    foreach (var config in digitConfigs)
                                    {
                                        for (int i = 0; i < config.digits && charIndex < scoreStr.Length; i++)
                                        {
                                            int digit = config.startdigit + i;
                                            int digitValue = ConvertCharToLEDValue(scoreStr[charIndex], config.ledtype);
                                            regkey.SetValue($"B2SLED{digit}", digitValue);
                                            charIndex++;
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Logger.Log($"B2SSetScorePlayer error: {ex.Message}");
                }
            }
            
            if (_pluginHost != null)
            {
                _pluginHost.DataReceive('C', playerNum, scoreValue);
            }
        }
        
        private int ConvertCharToLEDValue(char c, int ledType)
        {
            // Convert character to 7-segment LED value
            // LED type 0 = standard 7-segment
            switch (c)
            {
                case '0': return 63;  // 0111111
                case '1': return 6;   // 0000110
                case '2': return 91;  // 1011011
                case '3': return 79;  // 1001111
                case '4': return 102; // 1100110
                case '5': return 109; // 1101101
                case '6': return 125; // 1111101
                case '7': return 7;   // 0000111
                case '8': return 127; // 1111111
                case '9': return 111; // 1101111
                case ' ': return 0;   // blank
                default: return 0;
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
            // Write LED value directly to registry for EXE mode
            int digitNum = Convert.ToInt32(digit);
            int digitValue = Convert.ToInt32(value);
            
            if (digitNum > 0)
            {
                try
                {
                    using (var regkey = Registry.CurrentUser.OpenSubKey("Software\\B2S", true))
                    {
                        regkey?.SetValue($"B2SLED{digitNum}", digitValue);
                    }
                }
                catch
                {
                }
            }
            
            if (_pluginHost != null)
            {
                _pluginHost.DataReceive('B', digitNum, digitValue);
            }
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
            RegistryHelper.SetSound(soundname, 1);
        }

        public void B2SPlaySound(string soundname)
        {
            B2SStartSound(soundname);
        }

        public void B2SStopSound(string soundname)
        {
            RegistryHelper.SetSound(soundname, 0);
        }

        public void B2SMapSound(object digit, string soundname)
        {
            // Sound mapping - not needed for EXE mode
        }

        #endregion

        #region Helper Methods
        
        // =====================================================================================
        // VPinMAME COM Object Method/Property Treatment Documentation
        // =====================================================================================
        // 
        // VPinMAME exposes its interface via COM IDispatch. When calling from C#, we must use
        // the correct BindingFlags for each member type:
        //
        // PROPERTIES (use InvokeVPMProperty with BindingFlags.GetProperty/SetProperty):
        // - GameName, ROMName, TableName, Version
        // - HandleKeyboard, HandleMechanics, ShowTitle, ShowFrame, ShowDMDOnly, Hidden, Pause
        // - SplashInfoLine, ShowPinDMD, LockDisplay, DoubleSize, SoundMode
        // - ChangedLamps, ChangedSolenoids, ChangedGIStrings (read-only, return SAFEARRAY)
        // - RawDmdWidth, RawDmdHeight, RawDmdPixels, RawDmdColoredPixels
        // - ChangedNVRAM, NVRAM, Games
        //
        // INDEXED PROPERTIES (use InvokeVPMIndexedProperty/SetVPMIndexedProperty):
        // - Switch(n), Lamp(n), Solenoid(n), GIString(n), Mech(n), Dip(n), SolMask(n)
        //
        // METHODS (use InvokeVPMMethod with BindingFlags.InvokeMethod):
        // - Run(handle), Stop(), Pause
        // - ChangedLEDs(mask2, mask1, mask3, mask4) - this one IS a method, takes parameters
        //
        // IMPORTANT: VB syntax allows calling properties with () like methods (e.g., ChangedLamps())
        // but they are still properties and must be accessed with GetProperty binding flag.
        // Using InvokeMethod on a property will cause DISP_E_MEMBERNOTFOUND error.
        // =====================================================================================

        private T? InvokeVPMProperty<T>(string propertyName)
        {
            try
            {
                object? result = VPinMAME.GetType().InvokeMember(
                    propertyName,
                    System.Reflection.BindingFlags.GetProperty,
                    null,
                    VPinMAME,
                    null);
                if (result is T typedResult)
                    return typedResult;
                if (result != null)
                    return (T)Convert.ChangeType(result, typeof(T));
            }
            catch (Exception ex)
            {
                var inner = ex.InnerException ?? ex;
                Logger.Log($"InvokeVPMProperty GET {propertyName} FAILED: {inner.Message}");
            }
            return default;
        }

        private void InvokeVPMProperty(string propertyName, object value)
        {
            try
            {
                VPinMAME.GetType().InvokeMember(
                    propertyName,
                    System.Reflection.BindingFlags.SetProperty,
                    null,
                    VPinMAME,
                    new object[] { value });
            }
            catch (Exception ex)
            {
                Logger.Log($"InvokeVPMProperty SET {propertyName} FAILED: {ex.Message}");
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
            catch (Exception ex)
            {
                // Get inner exception for more details
                var innerEx = ex.InnerException ?? ex;
                Logger.Log($"InvokeVPMMethod({methodName}) FAILED: {innerEx.Message}");
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

        #region IReflect Implementation

        private static readonly string[] IndexedPropertyNames = { "Switch", "Lamp", "Solenoid", "GIString", "Mech", "GetMech", "Dip", "SolMask" };

        Type IReflect.UnderlyingSystemType => typeof(Server);

        FieldInfo? IReflect.GetField(string name, BindingFlags bindingAttr) => GetType().GetField(name, bindingAttr);
        FieldInfo[] IReflect.GetFields(BindingFlags bindingAttr) => GetType().GetFields(bindingAttr);
        MemberInfo[] IReflect.GetMember(string name, BindingFlags bindingAttr) => GetType().GetMember(name, bindingAttr);
        MemberInfo[] IReflect.GetMembers(BindingFlags bindingAttr) => GetType().GetMembers(bindingAttr);
        MethodInfo? IReflect.GetMethod(string name, BindingFlags bindingAttr) => GetType().GetMethod(name, bindingAttr);
        MethodInfo? IReflect.GetMethod(string name, BindingFlags bindingAttr, Binder? binder, Type[] types, ParameterModifier[]? modifiers) 
            => GetType().GetMethod(name, bindingAttr, binder, types, modifiers);
        MethodInfo[] IReflect.GetMethods(BindingFlags bindingAttr) => GetType().GetMethods(bindingAttr);
        PropertyInfo[] IReflect.GetProperties(BindingFlags bindingAttr) => GetType().GetProperties(bindingAttr);
        PropertyInfo? IReflect.GetProperty(string name, BindingFlags bindingAttr) => GetType().GetProperty(name, bindingAttr);
        PropertyInfo? IReflect.GetProperty(string name, BindingFlags bindingAttr, Binder? binder, Type? returnType, Type[] types, ParameterModifier[]? modifiers) 
            => GetType().GetProperty(name, bindingAttr, binder, returnType, types, modifiers);

        object? IReflect.InvokeMember(string name, BindingFlags invokeAttr, Binder? binder, object? target, object?[]? args, ParameterModifier[]? modifiers, CultureInfo? culture, string[]? namedParameters)
        {
            // Skip logging for high-frequency polling methods to avoid slowdown
            bool isPollingMethod = name == "ChangedLamps" || name == "ChangedSolenoids" || name == "ChangedGIStrings" || 
                                   name == "RawDmdPixels" || name == "RawDmdWidth" || name == "RawDmdHeight";
            
            if (!isPollingMethod)
            {
                Logger.Log($"IReflect.InvokeMember: name={name}, invokeAttr={invokeAttr}, args={(args != null ? args.Length.ToString() : "null")}");
            }
            
            // Handle indexed properties - VBScript calls Switch(32) or Switch(32) = True
            if (IndexedPropertyNames.Contains(name))
            {
                // PutDispProperty is what COM uses for property sets
                bool isSet = (invokeAttr & BindingFlags.SetProperty) != 0 || (invokeAttr & BindingFlags.PutDispProperty) != 0;
                bool isGet = (invokeAttr & BindingFlags.GetProperty) != 0;
                
                if (isSet && args != null && args.Length >= 2)
                {
                    object index = args[0]!;
                    object value = args[1]!;
                    
                    switch (name)
                    {
                        case "Switch": set_Switch(index, Convert.ToInt32(value) != 0); return null;
                        case "Mech": set_Mech(index, Convert.ToInt32(value)); return null;
                        case "Dip": set_Dip(index, Convert.ToInt32(value)); return null;
                        case "SolMask": set_SolMask(index, Convert.ToInt32(value)); return null;
                    }
                }
                else if ((isGet || !isSet) && args != null && args.Length >= 1)
                {
                    object index = args[0]!;
                    
                    switch (name)
                    {
                        case "Switch": return get_Switch(index);
                        case "Lamp": return get_Lamp(index);
                        case "Solenoid": return get_Solenoid(index);
                        case "GIString": return get_GIString(index);
                        case "Mech": return get_Mech(index);
                        case "GetMech": return get_GetMech(index);
                        case "Dip": return get_Dip(index);
                        case "SolMask": return get_SolMask(index);
                    }
                }
            }

            // Default behavior - use reflection on this instance
            try
            {
                // Convert COM dispatch flags to .NET BindingFlags
                BindingFlags fixedFlags = invokeAttr;
                
                // PutDispProperty needs to be converted to SetProperty for .NET reflection
                if ((invokeAttr & BindingFlags.PutDispProperty) != 0)
                {
                    fixedFlags = (fixedFlags & ~BindingFlags.PutDispProperty) | BindingFlags.SetProperty;
                    
                    // Handle type coercion for property setters (VBScript passes Int16 for booleans, etc.)
                    if (args != null && args.Length == 1)
                    {
                        var prop = GetType().GetProperty(name);
                        if (prop != null && prop.CanWrite)
                        {
                            Type targetType = prop.PropertyType;
                            object? value = args[0];
                            
                            if (value != null && value.GetType() != targetType)
                            {
                                try
                                {
                                    if (targetType == typeof(bool))
                                    {
                                        // VBScript passes -1 for True, 0 for False
                                        args[0] = Convert.ToInt32(value) != 0;
                                    }
                                    else
                                    {
                                        args[0] = Convert.ChangeType(value, targetType);
                                    }
                                    Logger.Log($"  Converted arg from {value.GetType().Name} to {targetType.Name}");
                                }
                                catch { }
                            }
                        }
                    }
                }
                
                return GetType().InvokeMember(name, fixedFlags, binder, this, args, modifiers, culture, namedParameters);
            }
            catch (Exception ex)
            {
                Logger.Log($"  InvokeMember failed: {ex.Message}");
                throw;
            }
        }

        #endregion
    }

    // Accessor classes with default indexers for COM indexed property access
    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class SwitchAccessor
    {
        private readonly Server _server;
        internal SwitchAccessor(Server server) => _server = server;
        
        [DispId(0)] // Default member
        public bool this[object index]
        {
            get => _server.get_Switch(index);
            set => _server.set_Switch(index, value);
        }
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class LampAccessor
    {
        private readonly Server _server;
        internal LampAccessor(Server server) => _server = server;
        
        [DispId(0)]
        public bool this[object index] => _server.get_Lamp(index);
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class SolenoidAccessor
    {
        private readonly Server _server;
        internal SolenoidAccessor(Server server) => _server = server;
        
        [DispId(0)]
        public bool this[object index] => _server.get_Solenoid(index);
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class GIStringAccessor
    {
        private readonly Server _server;
        internal GIStringAccessor(Server server) => _server = server;
        
        [DispId(0)]
        public bool this[object index] => _server.get_GIString(index);
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class MechAccessor
    {
        private readonly Server _server;
        internal MechAccessor(Server server) => _server = server;
        
        [DispId(0)]
        public int this[object index]
        {
            get => _server.get_Mech(index);
            set => _server.set_Mech(index, value);
        }
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class GetMechAccessor
    {
        private readonly Server _server;
        internal GetMechAccessor(Server server) => _server = server;
        
        [DispId(0)]
        public object this[object index] => _server.get_GetMech(index);
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class DipAccessor
    {
        private readonly Server _server;
        internal DipAccessor(Server server) => _server = server;
        
        [DispId(0)]
        public int this[object index]
        {
            get => _server.get_Dip(index);
            set => _server.set_Dip(index, value);
        }
    }

    [ComVisible(true)]
    [ClassInterface(ClassInterfaceType.AutoDispatch)]
    public class SolMaskAccessor
    {
        private readonly Server _server;
        internal SolMaskAccessor(Server server) => _server = server;
        
        [DispId(0)]
        public int this[object index]
        {
            get => _server.get_SolMask(index);
            set => _server.set_SolMask(index, value);
        }
    }
}
