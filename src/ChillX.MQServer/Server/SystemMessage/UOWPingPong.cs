using ChillX.Core.CapabilityInterfaces;
using ChillX.Serialization;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.MQServer.Server.SystemMessage
{
    [SerializedEntity(4)]
    public class UOWPingPong : IEqualityComparer<UOWPingPong>
    {
        public static UOWPingPong Ping()
        {
            return new UOWPingPong();
        }
        public static UOWPingPong Pong()
        {
            return new UOWPingPong(true);
        }
        public UOWPingPong()
        {
            UniqueID = CXMQUtility.PingPongNextID();
            TimeStampTicks = DateTime.UtcNow.Ticks;
            IsPong = false;
        }
        public UOWPingPong(bool _isPong = false)
        {
            UniqueID = CXMQUtility.PingPongNextID();
            TimeStampTicks = DateTime.UtcNow.Ticks;
            IsPong = _isPong;
        }
        public UOWPingPong(int uniqueID, long timeStampTicks, bool isPong)
        {
            UniqueID = uniqueID;
            TimeStampTicks = timeStampTicks;
            IsPong = isPong;
        }
        [SerializedMember(0)]
        public int UniqueID { get; private set; }

        [SerializedMember(1)]
        public long TimeStampTicks { get; private set; }

        [SerializedMember(2)]
        public bool IsPong { get; private set; }

        public override bool Equals(object obj)
        {
            if (obj is UOWPingPong)
            {
                return UniqueID == ((UOWPingPong)obj).UniqueID;
            }
            return false;
        }

        public override int GetHashCode()
        {
            return UniqueID.GetHashCode();
        }

        public bool Equals(UOWPingPong x, UOWPingPong y)
        {
            if (x.UniqueID == y.UniqueID) { return true; }
            return false;
        }

        public int GetHashCode(UOWPingPong obj)
        {
            return obj.UniqueID.GetHashCode();
        }
    }

}
