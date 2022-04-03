using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ChillXLogging
{
    public struct LogEntry
    {
        public LogSeverity Severity;
        public string MessageText;
        public Exception MessageException;
        public DateTime EventTime;
        public string ToFormattedText()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(EventTime.ToString(@"yyyy/MM/dd HH:mm:ss"));
            switch (Severity)
            {
                case LogSeverity.debug:
                    sb.Append(@" Debug: ");
                    break;
                case LogSeverity.info:
                    sb.Append(@" Info: ");
                    break;
                case LogSeverity.warning:
                    sb.Append(@" Warning: ");
                    break;
                case LogSeverity.error:
                    sb.Append(@" Error: ");
                    break;
                case LogSeverity.unhandled:
                    sb.Append(@" Unhandled Exception: ");
                    break;
                case LogSeverity.fatal:
                    sb.Append(@" Fatal: ");
                    break;
            }
            if (string.IsNullOrEmpty(MessageText))
            {
                MessageText = string.Empty;
            }
            if (!string.IsNullOrEmpty(MessageText.Trim()))
            {
                sb.Append(MessageText.Trim());
            }
            if (MessageException != null)
            {
                sb.Append(@" || Exception: ");
                sb.Append(MessageException.ToString());
            }
            return sb.ToString();
        }
        public override string ToString()
        {
            return ToFormattedText();
        }
    }
}
