using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;
using System.Reflection;
using Microsoft.Win32;

#if HAS_PLUGIN_INTERFACE
using B2SServerPluginInterface;
#endif

namespace B2S.ComServer
{
    public class PluginHost : IDisposable
    {
        private CompositionContainer? _container;
        private readonly List<DirectoryCatalog> _catalogs = new List<DirectoryCatalog>();
        
#if HAS_PLUGIN_INTERFACE
        [ImportMany(typeof(IDirectPlugin))]
        public IEnumerable<IDirectPlugin> Plugins { get; set; } = Array.Empty<IDirectPlugin>();
#else
        public IEnumerable<object> Plugins { get; set; } = Array.Empty<object>();
#endif

        public int PluginCount => Plugins?.Count() ?? 0;
        public string PluginsFilePath { get; private set; } = string.Empty;

        public PluginHost(bool loadPlugins)
        {
            if (loadPlugins)
            {
                LoadPlugins();
            }
        }

        /// <summary>
        /// Loads plugins from Plugin/Plugin64/Plugins/Plugins64 directories and their subdirectories.
        /// Uses the "64" suffix when running in a 64-bit process.
        /// </summary>
        private void LoadPlugins()
        {
            Logger.Log("LoadPlugins starting");
            var loadedDirectories = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            
            try
            {
                string? assemblyDir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
                if (string.IsNullOrEmpty(assemblyDir))
                {
                    Logger.Log("ERROR: Could not determine assembly directory");
                    return;
                }
                
                PluginsFilePath = assemblyDir;
                Logger.Log($"Assembly directory: {assemblyDir}");
                Logger.Log($"Is64BitProcess: {Environment.Is64BitProcess}");
                
                // Determine suffix based on process bitness
                string suffix64 = Environment.Is64BitProcess ? "64" : "";
                
                // Plugin directory names to search (with and without 64 suffix, with and without 's')
                var pluginDirNames = new[]
                {
                    "plugin" + suffix64,
                    "plugins" + suffix64
                };
                
                var catalog = new AggregateCatalog();
                
                foreach (var dir in Directory.GetDirectories(assemblyDir))
                {
                    string dirName = Path.GetFileName(dir).ToLowerInvariant();
                    
                    if (pluginDirNames.Contains(dirName))
                    {
                        Logger.Log($"Found plugin directory: {dir}");
                        
                        // Load from subdirectories (each plugin in its own folder)
                        foreach (var subDir in Directory.GetDirectories(dir))
                        {
                            string subDirName = Path.GetFileName(subDir);
                            
                            // Skip directories starting with "-"
                            if (subDirName.StartsWith("-"))
                            {
                                Logger.Log($"Skipping disabled directory: {subDirName}");
                                continue;
                            }
                            
                            if (!loadedDirectories.Contains(subDir))
                            {
                                LoadDirectoryPlugins(catalog, subDir);
                                loadedDirectories.Add(subDir);
                            }
                        }
                        
                        // Also handle .lnk shortcuts to plugin directories
                        foreach (var lnkFile in Directory.GetFiles(dir, "*.lnk"))
                        {
                            string lnkName = Path.GetFileName(lnkFile);
                            if (lnkName.StartsWith("-")) continue;
                            
                            try
                            {
                                string? targetPath = ResolveShortcut(lnkFile);
                                if (!string.IsNullOrEmpty(targetPath) && Directory.Exists(targetPath))
                                {
                                    if (!loadedDirectories.Contains(targetPath))
                                    {
                                        Logger.Log($"Loading shortcut target: {targetPath}");
                                        LoadDirectoryPlugins(catalog, targetPath);
                                        loadedDirectories.Add(targetPath);
                                    }
                                }
                            }
                            catch (Exception ex)
                            {
                                Logger.Log($"Error resolving shortcut {lnkFile}: {ex.Message}");
                            }
                        }
                    }
                }
                
                if (_catalogs.Count > 0)
                {
                    foreach (var cat in _catalogs)
                    {
                        catalog.Catalogs.Add(cat);
                    }
                    
                    _container = new CompositionContainer(catalog);
                    _container.ComposeParts(this);
                    
                    Logger.Log($"Loaded {PluginCount} plugins");
                    
#if HAS_PLUGIN_INTERFACE
                    foreach (var plugin in Plugins)
                    {
                        Logger.Log($"  Plugin: {plugin.Name}");
                    }
#endif
                }
                else
                {
                    Logger.Log("No plugin directories found");
                }
                
                // Write plugin count to registry for EXE mode
                WritePluginCountToRegistry();
            }
            catch (Exception ex)
            {
                Logger.LogException(ex, "LoadPlugins");
            }
            
            Logger.Log("LoadPlugins completed");
        }

        private void LoadDirectoryPlugins(AggregateCatalog catalog, string directory)
        {
            try
            {
                Logger.Log($"Loading plugins from: {directory}");
                var dirCatalog = new DirectoryCatalog(directory);
                _catalogs.Add(dirCatalog);
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to load plugins from {directory}: {ex.Message}");
            }
        }

        private string? ResolveShortcut(string lnkPath)
        {
            try
            {
                // Use WScript.Shell to resolve shortcuts
                Type? shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType == null) return null;
                
                dynamic? shell = Activator.CreateInstance(shellType);
                if (shell == null) return null;
                
                dynamic shortcut = shell.CreateShortcut(lnkPath);
                string targetPath = shortcut.TargetPath;
                
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shortcut);
                System.Runtime.InteropServices.Marshal.ReleaseComObject(shell);
                
                return targetPath;
            }
            catch
            {
                return null;
            }
        }

        private void WritePluginCountToRegistry()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\B2S", true))
                {
                    if (key != null)
                    {
                        key.SetValue("Plugins", PluginCount);
                        Logger.Log($"Wrote plugin count to registry: {PluginCount}");
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.Log($"Failed to write plugin count to registry: {ex.Message}");
            }
        }

        public void PluginInit(string tableFilename, string romName)
        {
#if HAS_PLUGIN_INTERFACE
            if (Plugins == null) return;
            
            Logger.Log($"PluginInit: table={tableFilename}, rom={romName}");
            
            foreach (var plugin in Plugins)
            {
                try
                {
                    Logger.Log($"  Initializing plugin: {plugin.Name}");
                    plugin.PluginInit(tableFilename, romName);
                }
                catch (Exception ex)
                {
                    Logger.Log($"  Plugin {plugin.Name} init failed: {ex.Message}");
                }
            }
#endif
        }

        public void PluginFinish()
        {
#if HAS_PLUGIN_INTERFACE
            if (Plugins == null) return;
            
            Logger.Log("PluginFinish");
            Logger.FlushCallCounts(); // Flush call statistics
            
            foreach (var plugin in Plugins)
            {
                try
                {
                    plugin.PluginFinish();
                }
                catch
                {
                }
            }
#endif
        }

        public void PinMameRun()
        {
#if HAS_PLUGIN_INTERFACE
            if (Plugins == null) return;
            
            Logger.Log("PinMameRun");
            
            foreach (var plugin in Plugins)
            {
                try
                {
                    if (plugin is IDirectPluginPinMame pinMamePlugin)
                    {
                        pinMamePlugin.PinMameRun();
                    }
                }
                catch
                {
                }
            }
#endif
        }

        public void PinMamePause()
        {
#if HAS_PLUGIN_INTERFACE
            if (Plugins == null) return;
            
            foreach (var plugin in Plugins)
            {
                try
                {
                    if (plugin is IDirectPluginPinMame pinMamePlugin)
                    {
                        pinMamePlugin.PinMamePause();
                    }
                }
                catch
                {
                }
            }
#endif
        }

        public void PinMameContinue()
        {
#if HAS_PLUGIN_INTERFACE
            if (Plugins == null) return;
            
            foreach (var plugin in Plugins)
            {
                try
                {
                    if (plugin is IDirectPluginPinMame pinMamePlugin)
                    {
                        pinMamePlugin.PinMameContinue();
                    }
                }
                catch
                {
                }
            }
#endif
        }

        public void PinMameStop()
        {
#if HAS_PLUGIN_INTERFACE
            if (Plugins == null) return;
            
            Logger.Log("PinMameStop");
            
            foreach (var plugin in Plugins)
            {
                try
                {
                    if (plugin is IDirectPluginPinMame pinMamePlugin)
                    {
                        pinMamePlugin.PinMameStop();
                    }
                }
                catch
                {
                }
            }
#endif
        }

        public void DataReceive(char tableElementTypeChar, int number, int value)
        {
#if HAS_PLUGIN_INTERFACE
            if (Plugins == null) return;
            
            // Use counted logging for high-frequency calls
            Logger.LogCounted($"DataReceive_{tableElementTypeChar}", $"num={number}, val={value}");
            
            foreach (var plugin in Plugins)
            {
                try
                {
                    plugin.DataReceive(tableElementTypeChar, number, value);
                }
                catch
                {
                }
            }
#endif
        }

        public void DataReceive(char tableElementTypeChar, object data)
        {
            if (Plugins == null || data == null) return;
            
            // Convert object(,) array to individual calls
            if (data is object[,] array)
            {
                int rows = array.GetUpperBound(0) + 1;
                Logger.LogCounted($"DataReceive_{tableElementTypeChar}_Array", $"rows={rows}");
                
                for (int i = 0; i < rows; i++)
                {
                    int number = Convert.ToInt32(array[i, 0]);
                    // For 'D' (LEDs/Digits), value is in column 2, otherwise column 1
                    int value = tableElementTypeChar == 'D' 
                        ? Convert.ToInt32(array[i, 2]) 
                        : Convert.ToInt32(array[i, 1]);
                    DataReceive(tableElementTypeChar, number, value);
                }
            }
        }

        public void Dispose()
        {
            Logger.Log("PluginHost disposing");
            Logger.FlushCallCounts();
            Logger.FlushBuffer();
            _container?.Dispose();
            foreach (var catalog in _catalogs)
            {
                catalog.Dispose();
            }
            _catalogs.Clear();
        }
    }
}
