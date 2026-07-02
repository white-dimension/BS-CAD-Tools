using System;
using System.IO;
using System.IO.Pipes;
using System.Text.Json;

namespace BS.CAD.Tools.Utils
{
    public static class CadImeBridge
    {
        private const string PipeName = "BS_IME_Assistant_Pipe";

        public static bool Notify(string eventName, string preferredIme, string mode)
        {
            try
            {
                var payload = new
                {
                    source = "AutoCAD",
                    @event = eventName,
                    processName = "acad.exe",
                    preferredIme,
                    mode
                };

                using var pipe = new NamedPipeClientStream(".", PipeName, PipeDirection.Out);
                pipe.Connect(120);
                using var writer = new StreamWriter(pipe) { AutoFlush = true };
                writer.WriteLine(JsonSerializer.Serialize(payload));
                return true;
            }
            catch (Exception ex)
            {
                Logger.Warn($"CAD IME bridge notify failed: {ex.Message}");
                return false;
            }
        }
    }
}
