using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;
using static System.Net.Mime.MediaTypeNames;

namespace ControlDeviceServer.Services
{
    public sealed class LogBuffer
    {
        readonly object _lock = new object();
        readonly StringBuilder _sb = new StringBuilder();

        public event Action<string> LineAdded;
        public event Action Cleared;

        public void Info(string text) => Append("INFO", text);
        public void Warn(string text) => Append("WARN", text);
        public void Error(string text) => Append("ERR", text);

        public void Clear()
        {
            lock (_lock)
                _sb.Clear();

            Cleared?.Invoke();
        }

        void Append(string lvl, string msg)
        {
            var line = $"{DateTime.Now:HH:mm:ss.fff} [{lvl}] {msg}\n";
            lock (_lock) _sb.Append(line);
            var h = LineAdded; if (h != null) h(line);
        }
    }
}
