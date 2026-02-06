using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.ComponentModel.Composition.Hosting;
using System.IO;
using System.Linq;

#if HAS_PLUGIN_INTERFACE
using B2SServerPluginInterface;
#endif

namespace B2S.ComServer
{
    public class PluginHost : IDisposable
    {
        private CompositionContainer? _container;
        
#if HAS_PLUGIN_INTERFACE
        [ImportMany(typeof(IDirectPlugin))]
        public IEnumerable<IDirectPlugin> Plugins { get; set; } = Array.Empty<IDirectPlugin>();
#else
        public IEnumerable<object> Plugins { get; set; } = Array.Empty<object>();
#endif

        public int PluginCount => Plugins?.Count() ?? 0;

        public PluginHost(bool loadPlugins)
        {
            if (loadPlugins)
            {
                LoadPlugins();
            }
        }

        private void LoadPlugins()
        {
            try
            {
                string pluginPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Plugin");
                
                if (!Directory.Exists(pluginPath))
                {
                    return;
                }

                var catalog = new AggregateCatalog();
                catalog.Catalogs.Add(new DirectoryCatalog(pluginPath));
                
                _container = new CompositionContainer(catalog);
                _container.ComposeParts(this);
            }
            catch
            {
                // Silently fail if plugin loading encounters issues
            }
        }

        public void PluginInit(string tableFilename, string romName)
        {
#if HAS_PLUGIN_INTERFACE
            if (Plugins == null) return;
            
            foreach (var plugin in Plugins)
            {
                try
                {
                    plugin.PluginInit(tableFilename, romName);
                }
                catch
                {
                    // Continue with other plugins if one fails
                }
            }
#endif
        }

        public void PluginFinish()
        {
#if HAS_PLUGIN_INTERFACE
            if (Plugins == null) return;
            
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
            if (Plugins == null) return;
            
            // Convert object(,) array to individual calls
            if (data is object[,] array)
            {
                for (int i = 0; i <= array.GetUpperBound(0); i++)
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
            _container?.Dispose();
        }
    }
}
