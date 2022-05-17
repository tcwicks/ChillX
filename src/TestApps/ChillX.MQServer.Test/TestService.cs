using ChillX.MQServer.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChillX.MQServer.Test
{
    public enum ServiceTypes
    {
        Test = 1
    }

    public enum ModuleTypes
    {
        BenchMark = 0
    }

    public class TestService : MQServiceBase
    {
        public override int ServiceType => (int)ServiceTypes.Test;

        public override void OnShutdown()
        {
        }

        private TestModule moduleTest = new TestModule();

        protected override IEnumerable<IMQServiceModule> CreateServiceModules()
        {
            return new IMQServiceModule[1] { moduleTest };
        }

        protected override void OnDispose()
        {
        }

        protected override bool OnStartup()
        {
            return true;
        }
    }
}
