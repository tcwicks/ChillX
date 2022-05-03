using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.Logging.Handlers
{
    public class LogHandlerConsole : ILogHandler, IDisposable
    {
        private object SyncRoot { get; } = new object();

        private bool m_IsDisposed = false;
        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_IsDisposed = true;
                GC.SuppressFinalize(this);
            }
        }

        public void WriteLogEntries(IEnumerable<LogEntry> _entries)
        {
            foreach (LogEntry entry in _entries)
            {
                Console.WriteLine(entry.ToFormattedText());
            }
        }
    }
}
