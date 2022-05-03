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
using System.Threading;

namespace ChillX.Threading.BulkProcessor
{
    internal static class IdentitySequence
    {
        private const int MaxValue = int.MaxValue - 100000;
        private static volatile int _value = 0;
        private static object _lock = new object();
        public static int Value
        {
            get { return _value; }
            set
            {
                Interlocked.Exchange(ref _value, value);
            }
        }

        public static int NextID()
        {
            int result = Interlocked.Increment(ref _value);
            if (result > MaxValue)
            {
                lock (_lock)
                {
                    result = _value;
                    if (result > MaxValue)
                    {
                        Interlocked.Exchange(ref _value, 0);
                    }
                }
                result = Interlocked.Increment(ref _value);
            }
            return result;
        }

    }

}
