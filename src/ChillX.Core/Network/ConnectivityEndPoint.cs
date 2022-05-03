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
using System.Net;
using System.Text;

namespace ChillX.Core.Network
{
    public class ConnectivityEndPoint : IEqualityComparer<ConnectivityEndPoint>
    {
        public ConnectivityEndPoint(int _uniqueID, IPAddress _iPAddress, int _listeningPort)
        {
            UniqueID = _uniqueID;
            IPAddress = _iPAddress;
            Listening_Port = _listeningPort;
        }
        public int UniqueID { get; private set; }
        public IPAddress IPAddress { get; private set; }
        public int Listening_Port { get; private set; }
        public override int GetHashCode()
        {
            return UniqueID.GetHashCode();
        }
        public override bool Equals(object obj)
        {
            ConnectivityEndPoint target = obj as ConnectivityEndPoint;
            if (target == null)
            {
                return base.Equals(obj);
            }
            return target.UniqueID.Equals(UniqueID);
        }

        public bool Equals(ConnectivityEndPoint x, ConnectivityEndPoint y)
        {
            if (x == null && y == null) { return true; }
            if (x == null || y == null) { return false; }
            return x.UniqueID.Equals(UniqueID) && y.UniqueID.Equals(y.UniqueID);
        }

        public int GetHashCode(ConnectivityEndPoint obj)
        {
            return (obj == null ? 0 : obj.UniqueID.GetHashCode());
        }
    }

}
