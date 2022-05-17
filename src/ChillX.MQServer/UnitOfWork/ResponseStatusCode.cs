using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.MQServer.UnitOfWork
{
    public enum ResponseStatusCode
    {
        Pending = 0,
        Success = 1,
        DestinationUnreachable = 2,
        TransmissionTimeout = 3,
        TransmissionError = 4,
        Unauthorized = 5,
        InvalidRequest = 6,
        ProcessingError = 7,
    }
}
