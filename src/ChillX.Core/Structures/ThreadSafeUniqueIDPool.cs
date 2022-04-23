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
    public class ThreadSafeUniqueIDPool : ThreadsafeCounter
    {
        public ThreadSafeUniqueIDPool(int _minValue = 0, int _maxValue = 1000000)
           : base(_minValue, _maxValue)
        {

        }

        private readonly Queue<int> AvailableIDQueue = new Queue<int>();
        private readonly ReaderWriterLockSlim Lock = new ReaderWriterLockSlim();

        public override int NextID()
        {
            Lock.EnterReadLock();
            try
            {
                if (AvailableIDQueue.Count > 0)
                {
                    return AvailableIDQueue.Dequeue();
                }
            }
            finally
            {
                Lock.ExitReadLock();
            }
            return base.NextID();
        }
        public void ReturnID(int _iD)
        {
            Lock.EnterWriteLock();
            try
            {
                AvailableIDQueue.Enqueue(_iD);
            }
            finally
            {
                Lock.ExitWriteLock();
            }
        }
    }
}
