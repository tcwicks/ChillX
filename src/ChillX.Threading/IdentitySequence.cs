using System;
using System.Threading;

namespace ChillX.Threading
{
    internal static class IdentitySequence
    {
        private const int MaxValue = int.MaxValue - 100000;
        private static volatile int _value = 0;
        private static object _lock = new object();
        public static int Value
        {
            get { return _value; }
            set
            {
                Interlocked.Exchange(ref _value, value);
            }
        }

        public static int NextID()
        {
            int result = Interlocked.Increment(ref _value);
            if (result > MaxValue)
            {
                lock (_lock)
                {
                    result = _value;
                    if (result > MaxValue)
                    {
                        Interlocked.Exchange(ref _value, 0);
                    }
                }
                result = Interlocked.Increment(ref _value);
            }
            return result;
        }

    }
}
