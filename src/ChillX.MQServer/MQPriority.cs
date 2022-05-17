using System;
using System.Collections.Generic;
using System.Text;

namespace ChillX.MQServer
{
    public enum MQPriority
    {
        System = 0,
        Realtime = 1,
        HighPriority = 2,
        NormalPriority = 3,
        LowPriority = 4,
        Background = 5,
    }
}
