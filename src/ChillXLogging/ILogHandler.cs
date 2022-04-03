using System;
using System.Collections.Generic;
using System.Text;

namespace ChillXLogging
{
    public interface ILogHandler: IDisposable
    {
        void WriteLogEntries(IEnumerable<LogEntry> _entries);
    }
}
