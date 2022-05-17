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
