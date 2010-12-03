using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NewLife.Threading;
using NewLife.Log;
using NewLife;
using System.Net;

namespace Test
{
    class ThreadPoolTest
    {
        static Random rnd = new Random((Int32)DateTime.Now.Ticks);

        public static void Main2(string[] args)
        {
            Object obj = new object();

            WeakReference wr = new WeakReference(obj);
            //wr.IsAlive
            Object target = wr.Target;
            if (target != null)
            {
                // do
            }
            //if (wr.IsAlive)
            //{
            //    // GC
            //    target = wr.Target;

            //    // todo
            //}

            IPAddress address = IPAddress.Any;
            WeakReference<IPAddress> wr2 = new WeakReference<IPAddress>(address);
            wr2 = address;
            address = wr2.Target;
            address = wr2;

            // GC

            //Type type=typeof()
        }
    }
}
