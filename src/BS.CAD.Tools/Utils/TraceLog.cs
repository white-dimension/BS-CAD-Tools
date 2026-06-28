using System;
using System.IO;

namespace BS.CAD.Tools.Utils
{
    /// <summary>
    /// Lightweight trace logger for locating crash points during CAD startup and UI actions.
    /// Each call writes immediately so the last line usually points to the failing step.
    /// </summary>
    public static class TraceLog
    {
        private static readonly string Path = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(typeof(TraceLog).Assembly.Location) ?? ".",
            "trace.log");

        private static readonly object _lock = new();

        static TraceLog()
        {
            try { File.WriteAllText(Path, ""); } catch { }
        }

        public static void Step(string msg)
        {
            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{System.Threading.Thread.CurrentThread.ManagedThreadId}] {msg}\n";
                lock (_lock) File.AppendAllText(Path, line);
            }
            catch { }
        }
    }
}
