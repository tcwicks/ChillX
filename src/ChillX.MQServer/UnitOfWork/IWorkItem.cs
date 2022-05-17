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
using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.MQServer.UnitOfWork
{
    public interface IWorkItem : IDisposable, ISupportPriority<MQPriority>, ISupportUniqueID, ISupportRouting, ISupportSerialization, ICloneable, IEqualityComparer<IWorkItem>
    {
        bool IsSystemRequest { get; }
    }

    public interface IWorkItemTyped<TRequest, TResponse> : IWorkItem
        where TRequest: new()
        where TResponse : new()
    {
        WorkItemDetail<TRequest> RequestDetail { get; }
        WorkItemDetail<TResponse> ResponseDetail { get; }
        WorkItemBase<TRequest, TResponse> CreateReply(TResponse _responseData, ResponseStatusCode _responseStatus, string _message);
        WorkItemBase<TRequest, TResponse> CreateImpersonatedReply(TResponse _responseData, int _fromServiceType, int _fromServiceModule, int _fromServiceFunction, ResponseStatusCode _responseStatus, string _message);
        WorkItemBase<TRequest, TResponse> CreateForward(int _toServiceType, int _toServiceModule, int _toServiceFunction, TRequest _newRequest, ResponseStatusCode _responseStatus, string _message);
        WorkItemBase<TRequest, TResponse> CreateImpersonatedForward(int _fromServiceType, int _fromServiceModule, int _fromServiceFunction, int _toServiceType, int _toServiceModule, int _toServiceFunction, TRequest _newRequest, ResponseStatusCode _responseStatus, string _message);
    }
}
