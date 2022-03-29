using System;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Common
{
    public class LockTests
    {
        [Fact(DisplayName = "测试lock是否阻塞本线程")]
        public void TestLock()
        {
            lock (_lock)
            {
                Test(3);
            }
        }

        private Object _lock = new Object();
        void Test(Int32 n)
        {
            // lock并不会阻塞本线程，同一个线程第二次lock同一个对象时，直接进去
            XTrace.WriteLine("LockTestA {0}", n);
            lock (_lock)
            {
                XTrace.WriteLine("LockTestB {0}", n);
                if (n > 1) Test(n - 1);
                XTrace.WriteLine("LockTestC {0}", n);
            }
        }
    }
}