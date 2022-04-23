/*
ChillX Framework Library
Copyright (C) 2022  Tikiri Chintana Wickramasingha 

Contact Details: (info at chillx dot com)

This program is free software: you can redistribute it and/or modify
it under the terms of the GNU General Public License as published by
the Free Software Foundation, either version 3 of the License, or
(at your option) any later version.

This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the
GNU General Public License for more details.

You should have received a copy of the GNU General Public License
along with this program.  If not, see <http://www.gnu.org/licenses/>.

*/

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;

namespace ChillX.Logging
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
