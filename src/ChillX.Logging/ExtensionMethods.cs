using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.Logging
{
    public static class ExtensionMethods
    {
        public static LogEntry Log(this LogSeverity _severity, string _message, Exception _ex = null, DateTime? _eventTime = null)
        {
            return Logger.Instance.Log(_severity, _message, _ex, _eventTime);
        }

        public static LogEntry Log(this string _message, LogSeverity _severity, Exception _ex = null, DateTime? _eventTime = null)
        {
            return Logger.Instance.Log(_severity, _message, _ex, _eventTime);
        }

        public static LogEntry Log (this Exception _ex, string _message, LogSeverity _severity, DateTime? _eventTime = null)
        {
            return Logger.Instance.Log(_severity, _message, _ex, _eventTime);
        }
    }
}
