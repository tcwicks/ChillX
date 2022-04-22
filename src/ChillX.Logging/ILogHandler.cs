using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.Logging
{
    public interface ILogHandler: IDisposable
    {
        void WriteLogEntries(IEnumerable<LogEntry> _entries);
    }
}
