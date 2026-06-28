using System;
using System.IO;
using System.Runtime.CompilerServices;

namespace BS.CAD.Tools.Utils
{
    public static class Logger
    {
        private static readonly string LogDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "BS-CAD-Tools");
        private static readonly string LogPath = Path.Combine(LogDir, "debug.log");
        private static readonly object _lock = new();

        private static DateTime _lastWrite = DateTime.MinValue;
        private static string _lastMsg = "";

        static Logger()
        {
            try
            {
                Directory.CreateDirectory(LogDir);
                WriteInternal("INIT", $"Logger started, path={LogPath}", "");
            }
            catch { }
        }

        public static void Info(string msg, [CallerMemberName] string caller = "") =>
            Write("INFO", msg, caller);

        public static void Warn(string msg, [CallerMemberName] string caller = "") =>
            Write("WARN", msg, caller);

        public static void Error(Exception ex, [CallerMemberName] string caller = "") =>
            Write("ERROR", $"{ex.GetType().Name}: {ex.Message}\n{ex.StackTrace}", caller);

        public static void Error(string msg, [CallerMemberName] string caller = "") =>
            Write("ERROR", msg, caller);

        private static void Write(string level, string msg, string caller)
        {
            try
            {
                string dedupKey = $"{level}|{caller}|{msg}";
                lock (_lock)
                {
                    var now = DateTime.Now;
                    if (dedupKey == _lastMsg && (now - _lastWrite).TotalSeconds < 2)
                        return;
                    _lastMsg = dedupKey;
                    _lastWrite = now;

                    WriteInternal(level, msg, caller);
                }
            }
            catch { }
        }

        private static void WriteInternal(string level, string msg, string caller)
        {
            string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level}] [{caller}] {msg}";
            File.AppendAllText(LogPath, line + Environment.NewLine);
        }
    }
}
