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
using System.Threading;
using ChillX.Core.CapabilityInterfaces;

namespace ChillX.Core.CapabilityBase
{
    public abstract class PoolingBase : ExpiryBase, ISupportPooling
    {
        private bool m_IsRented = false;
        public bool IsRented 
        {
            get { return m_IsRented; }
        }

        //private volatile int m_RentCount = 0;
        //private readonly Queue<string> m_RenterQueue = new Queue<string>();
        //private readonly Queue<string> m_ReturnerQueue = new Queue<string>();

        protected virtual void HandleOnRented(int capacity) { }
        public void OnRented(int capacity)
        {
            //if (Common.EnableDebug)
            //{
            //    Interlocked.Increment(ref m_RentCount);
            //    //StackTrace st = new StackTrace();
            //    //lock (this) { m_RenterQueue.Enqueue(st.ToString()); }
            //    if (m_RentCount > 1)
            //    {

            //    }
            //}
            try
            {
                m_IsRented = true;
                LastUsedTimeUTC = DateTime.UtcNow;
                HandleOnRented(capacity);
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        protected virtual void HandleOnReturned() { }
        public void OnReturned()
        {
            //if (Common.EnableDebug)
            //{
            //    Interlocked.Decrement(ref m_RentCount);
            //    //StackTrace st = new StackTrace();
            //    //lock (this) { m_ReturnerQueue.Enqueue(st.ToString()); }
            //    if (m_RentCount < 0)
            //    {

            //    }
            //}
            m_IsRented = false;
            LastUsedTimeUTC = DateTime.UtcNow;
            HandleOnReturned();
        }
    }
}
