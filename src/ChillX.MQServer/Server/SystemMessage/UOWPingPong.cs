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
