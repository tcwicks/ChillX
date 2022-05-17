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

using ChillX.Core.Structures;
using ChillX.MQServer.UnitOfWork;
using ChillX.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.MQServer.Server.SystemMessage
{
    [SerializedEntity(5)]
    public class UOWDiscovery : IDisposable
    {
        public UOWDiscovery()
        {

        }

        public UOWDiscovery(IList<int> _serviceKeyList)
        {
            int numKeys = _serviceKeyList.Count;
            ServiceKeys = RentedBuffer<int>.Shared.Rent(numKeys);
            for (int i = 0; i < numKeys; i++)
            {
                ServiceKeys[i] = _serviceKeyList[i];
            }
        }

        [SerializedMemberAttribute(0)]
        public int OriginID { get; set; }

        [SerializedMemberAttribute(1)]
        public RentedBuffer<int> ServiceKeys { get; set; }

        private bool m_IsDisposed = false;
        public void Dispose()
        {
            if (!m_IsDisposed)
            {
                m_IsDisposed = true;
                if (ServiceKeys != null)
                {
                    ServiceKeys.Return();
                    ServiceKeys = null;
                }
            }
        }
    }
}
