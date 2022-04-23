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
