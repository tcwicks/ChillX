using ChillX.Serialization;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChillX.MQServer.Test
{
    [SerializedEntity(10)]
    public class TestUOW 
    {
        [SerializedMemberAttribute(0)]
        public int PrimeTarget { get; set; }
        [SerializedMemberAttribute(1)]
        public long PrimeResult { get; set; }

    }
}
