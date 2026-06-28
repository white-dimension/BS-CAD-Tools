using System;
using System.IO;

namespace BS.CAD.Tools.Utils
{
    /// <summary>
    /// 无锁、无缓冲、直接写盘的追踪器，用于定位崩溃位置。
    /// 每次调用立即 Flush，确保崩溃前最后一行就是崩溃点。
    /// </summary>
    public static class TraceLog
    {
        private static readonly string Path = System.IO.Path.Combine(
            System.IO.Path.GetDirectoryName(typeof(TraceLog).Assembly.Location) ?? ".",
            "trace.log");

        private static readonly object _l = new();

        static TraceLog()
        {
            try { File.WriteAllText(Path, ""); } catch { }
        }

        public static void Step(string msg)
        {
            try
            {
                string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{System.Threading.Thread.CurrentThread.ManagedThreadId}] {msg}\n";
                lock (_l) File.AppendAllText(Path, line);
            }
            catch { }
        }
    }
}
