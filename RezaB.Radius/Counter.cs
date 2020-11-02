using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RezaB.Radius
{
    public static class Counter
    {
        private static byte _counter = 0;
        private static readonly object _locker = new object();

        public static byte Next()
        {
            lock (_locker)
            {
                if (_counter >= byte.MaxValue)
                    _counter = 0;
                return ++_counter;
            }
        }
    }
}
