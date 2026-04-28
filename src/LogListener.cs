using BepInEx.Logging;
using System.Collections.Generic;

namespace DebugMod
{
    public sealed class DebugLogListener : ILogListener
    {
        public static readonly List<string> Lines = new List<string>(200);
        private const int Max = 200;

        public void LogEvent(object sender, LogEventArgs e)
        {
            string line = $"[{e.Level}:{e.Source.SourceName}] {e.Data}";
            lock (Lines)
            {
                Lines.Add(line);
                while (Lines.Count > Max) Lines.RemoveAt(0);
            }
        }

        public void Dispose() { }
    }
}
