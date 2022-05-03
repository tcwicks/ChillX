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

using ChillX.Core.Network;
using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.Core.CapabilityInterfaces
{
    public interface ISupportRouting : ISupportUniqueID
    {
        DateTime CreationDate { get; }
        void CreationDateAssign(DateTime _creationDate);
        int DestinationServiceType { get; }
        void DestinationServiceTypeAssign(int _destinationServiceType);
        int DestinationServiceModule { get; }
        void DestinationServiceModuleAssign(int _destinationServiceModule);
        int DestinationServiceFunction { get; }
        void DestinationServiceFunctionAssign(int _destinationServiceFunction);
        int SourceServiceType { get; }
        void SourceServiceTypeAssign(int _sourceServiceType);
        int SourceServiceModule { get; }
        void SourceServiceModuleAssign(int _sourceServiceModule);
        int SourceServiceFunction { get; }
        void SourceServiceFunctionAssign(int _sourceServiceFunction);
        ConnectivityEndPoint SourceConnectivityEndPoint { get; }
        void SourceConnectivityEndPointAssign(ConnectivityEndPoint _connectivityEndPoint);
    }

}
