using ChillX.MQServer.Service;
using ChillX.MQServer.UnitOfWork;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ChillX.MQServer.Test
{
    public enum TestFunctions
    {
        Benchmark = 0,
    }
    public class TestModule : MQServiceModuleBase<TestFunctions>
    {
        public override int ModuleType => (int)ModuleTypes.BenchMark;

        public override IEnumerable<int> CreateServiceFunctionList()
        {
            return (int[])Enum.GetValues(typeof(TestFunctions));
        }

        protected override void OnDispose()
        {
        }

        protected override WorkItemBaseCore ProcessWorkItem(TestFunctions functionType, WorkItemBaseCore workItem)
        {
            switch (functionType)
            {
                case TestFunctions.Benchmark:
                    return Benchmark(workItem);
            }
            return workItem.CreateUnprocessedErrorReply(ResponseStatusCode.ProcessingError, @"Unknown Request");
        }

        private WorkItemBaseCore Benchmark(WorkItemBaseCore workItemBase)
        {
            WorkItemBase<TestUOW, TestUOW> workItem;
            workItem = new WorkItemBase<TestUOW, TestUOW>(workItemBase);
            TestUOW response = new TestUOW();

            response.PrimeResult = FindPrimeNumber(workItem.RequestDetail.WorkItemData.PrimeTarget); //set higher value for more time
            return workItem.CreateReply(response);
        }

        public long FindPrimeNumber(int n)
        {
            int count = 0;
            long a = 2;
            while (count < n)
            {
                long b = 2;
                int prime = 1;// to check if found a prime
                while (b * b <= a)
                {
                    if (a % b == 0)
                    {
                        prime = 0;
                        break;
                    }
                    b++;
                }
                if (prime > 0)
                {
                    count++;
                }
                a++;
            }
            return (--a);
        }
    }
}
