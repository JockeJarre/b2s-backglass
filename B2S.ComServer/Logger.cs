using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace B2S.ComServer
{
    /// <summary>
    /// Simple file-based logger for debugging COM server issues.
    /// Logs to %TEMP%\B2S.ComServer.log
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static readonly string _logPath;
        private static bool _enabled = true;

        static Logger()
        {
            try
            {
                // Log to temp folder - always writable
                _logPath = Path.Combine(Path.GetTempPath(), "B2S.ComServer.log");
                
                // Also try to log next to the DLL if possible
                string? assemblyDir = Path.GetDirectoryName(typeof(Logger).Assembly.Location);
                if (!string.IsNullOrEmpty(assemblyDir))
                {
                    string localLog = Path.Combine(assemblyDir, "B2S.ComServer.log");
                    try
                    {
                        // Test if we can write there
                        File.AppendAllText(localLog, "");
                        _logPath = localLog;
                    }
                    catch
                    {
                        // Stay with temp folder
                    }
                }
            }
            catch
            {
                _logPath = @"C:\Temp\B2S.ComServer.log";
            }
        }

        public static string LogPath => _logPath;

        public static void Log(string message, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!_enabled) return;

            try
            {
                lock (_lock)
                {
                    string fileName = Path.GetFileName(filePath);
                    string timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
                    string logLine = $"[{timestamp}] [{fileName}:{lineNumber}] {memberName}: {message}{Environment.NewLine}";
                    File.AppendAllText(_logPath, logLine);
                }
            }
            catch
            {
                // Never throw from logger
            }
        }

        public static void LogException(Exception ex, string context = "",
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Log($"EXCEPTION in {context}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", memberName, filePath, lineNumber);
        }

        public static void LogSeparator(string title = "")
        {
            try
            {
                lock (_lock)
                {
                    string line = string.IsNullOrEmpty(title) 
                        ? new string('=', 80) 
                        : $"===== {title} {new string('=', 70 - title.Length)}";
                    File.AppendAllText(_logPath, $"{line}{Environment.NewLine}");
                }
            }
            catch { }
        }

        public static void Clear()
        {
            try
            {
                lock (_lock)
                {
                    File.WriteAllText(_logPath, $"B2S.ComServer Log - Cleared at {DateTime.Now}{Environment.NewLine}");
                }
            }
            catch { }
        }
    }
}
