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
