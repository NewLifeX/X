using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Log
{
    public class NetworkLogTests
    {
        [Fact]
        public void UdpLog()
        {
            using var netLog = new NetworkLog();
            netLog.Write(LogLevel.Info, "This is {0}", Environment.MachineName);
            netLog.Write(LogLevel.Info, "I am {0}", Environment.UserName);
        }

        [Fact]
        public void TcpLog()
        {
            using var netLog = new NetworkLog("tcp://127.0.0.1:514");
            netLog.Write(LogLevel.Info, "This is {0}", Environment.MachineName);
            netLog.Write(LogLevel.Info, "I am {0}", Environment.UserName);
        }

        [Fact]
        public void HttpLog()
        {
            using var netLog = new NetworkLog("http://baidu.com/log");
            netLog.Write(LogLevel.Info, "This is {0}", Environment.MachineName);
            netLog.Write(LogLevel.Info, "I am {0}", Environment.UserName);

            for (var i = 0; i < 10; i++)
            {
                netLog.Write(LogLevel.Info, "Hello +" + i);
            }
        }
    }
}