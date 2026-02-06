using System;
using System.Collections.Concurrent;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;

namespace B2S.ComServer
{
    /// <summary>
    /// Efficient file-based logger for debugging COM server issues.
    /// Uses buffered writes and call counting to minimize overhead.
    /// </summary>
    public static class Logger
    {
        private static readonly object _lock = new object();
        private static readonly string _logPath;
        private static bool _enabled = true;
        private static readonly StringBuilder _buffer = new StringBuilder();
        private static readonly ConcurrentDictionary<string, int> _callCounts = new ConcurrentDictionary<string, int>();
        private static readonly Timer _flushTimer;
        private static DateTime _lastFlush = DateTime.Now;
        private const int FlushIntervalMs = 1000;
        private const int MaxBufferSize = 8192;

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
                
                // Setup flush timer
                _flushTimer = new Timer(_ => FlushBuffer(), null, FlushIntervalMs, FlushIntervalMs);
            }
            catch
            {
                _logPath = @"C:\Temp\B2S.ComServer.log";
                _flushTimer = new Timer(_ => { }, null, Timeout.Infinite, Timeout.Infinite);
            }
        }

        public static string LogPath => _logPath;
        public static bool Enabled { get => _enabled; set => _enabled = value; }

        /// <summary>
        /// Log a message. For high-frequency calls, use LogCounted instead.
        /// </summary>
        public static void Log(string message, 
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            if (!_enabled) return;

            try
            {
                string fileName = Path.GetFileName(filePath);
                string timestamp = DateTime.Now.ToString("HH:mm:ss.fff");
                string logLine = $"[{timestamp}] [{fileName}:{lineNumber}] {memberName}: {message}{Environment.NewLine}";
                
                lock (_lock)
                {
                    _buffer.Append(logLine);
                    if (_buffer.Length > MaxBufferSize)
                    {
                        FlushBufferInternal();
                    }
                }
            }
            catch
            {
                // Never throw from logger
            }
        }

        /// <summary>
        /// Log a high-frequency call - counts occurrences and logs summary periodically.
        /// </summary>
        public static void LogCounted(string key, string message = "")
        {
            if (!_enabled) return;
            
            int count = _callCounts.AddOrUpdate(key, 1, (k, v) => v + 1);
            
            // Only log every 100th call or first call
            if (count == 1 || count % 100 == 0)
            {
                Log($"{key}: {message} (call #{count})");
            }
        }

        /// <summary>
        /// Flush call counts to log - call at end of session.
        /// </summary>
        public static void FlushCallCounts()
        {
            if (_callCounts.IsEmpty) return;
            
            var sb = new StringBuilder();
            sb.AppendLine("=== Call Count Summary ===");
            foreach (var kvp in _callCounts)
            {
                sb.AppendLine($"  {kvp.Key}: {kvp.Value} calls");
            }
            Log(sb.ToString());
            _callCounts.Clear();
        }

        public static void LogException(Exception ex, string context = "",
            [CallerMemberName] string memberName = "",
            [CallerFilePath] string filePath = "",
            [CallerLineNumber] int lineNumber = 0)
        {
            Log($"EXCEPTION in {context}: {ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", memberName, filePath, lineNumber);
            FlushBuffer(); // Immediately flush on exception
        }

        public static void LogSeparator(string title = "")
        {
            string line = string.IsNullOrEmpty(title) 
                ? new string('=', 80) 
                : $"===== {title} {new string('=', Math.Max(10, 70 - title.Length))}";
            Log(line);
        }

        public static void Clear()
        {
            try
            {
                lock (_lock)
                {
                    _buffer.Clear();
                    _callCounts.Clear();
                    File.WriteAllText(_logPath, $"B2S.ComServer Log - Cleared at {DateTime.Now} - {(Environment.Is64BitProcess ? "64-bit" : "32-bit")} process{Environment.NewLine}");
                }
            }
            catch { }
        }

        public static void FlushBuffer()
        {
            lock (_lock)
            {
                FlushBufferInternal();
            }
        }

        private static void FlushBufferInternal()
        {
            if (_buffer.Length == 0) return;
            
            try
            {
                File.AppendAllText(_logPath, _buffer.ToString());
                _buffer.Clear();
                _lastFlush = DateTime.Now;
            }
            catch { }
        }
    }
}
