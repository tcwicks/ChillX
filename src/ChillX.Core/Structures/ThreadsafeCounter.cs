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

namespace ChillX.Core.Structures
{
    public class ThreadsafeCounter
    {
        public ThreadsafeCounter(int _minValue = 0, int _maxValue = 1000000)
        {
            if (_maxValue <= MinValue) { throw new ArgumentException(@"_maxValue must be greater than _minValue"); }
            m_MinValue = _minValue;
            m_MaxValue = _maxValue;
            m_ResetValue = _minValue - 1;
            _value = m_ResetValue;
        }
        private readonly object SyncLock = new object();
        private int m_MinValue = 1;
        public int MinValue { get { return m_MinValue; } }
        private int m_MaxValue = 100000;
        public int MaxValue { get { return m_MaxValue; } }
        private int m_ResetValue = 1;
        public int ResetValue { get { return m_ResetValue; } }

        private volatile int _value = 0;
        public int Value
        {
            get { return _value; }
            set
            {
                Interlocked.Exchange(ref _value, value);
            }
        }

        public virtual int NextID()
        {
            int result = Interlocked.Increment(ref _value);
            if (result > m_MaxValue)
            {
                lock(SyncLock)
                {
                    result = _value;
                    if (result > m_MaxValue)
                    {
                        Interlocked.Exchange(ref _value, m_ResetValue);
                    }
                }
                result = Interlocked.Increment(ref _value);
            }
            return result;
        }

    }

}
