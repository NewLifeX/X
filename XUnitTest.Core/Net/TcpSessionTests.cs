﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife;
using NewLife.Net;
using NewLife.Security;
using Xunit;

namespace XUnitTest.Net
{
    public class TcpSessionTests
    {
        [Fact]
        public void BindTest()
        {
            var addr = NetHelper.GetIPsWithCache().FirstOrDefault(e => e.IsIPv4() && !IPAddress.IsLoopback(e));
            Assert.NotNull(addr);

            var uri = new NetUri(NetType.Udp, addr, 12345);
            var client = uri.CreateClient();
            client.Open();
        }

        [Fact]
        public void BindTest2()
        {
            var addr = NetHelper.GetIPsWithCache().FirstOrDefault(e => e.IsIPv4() && !IPAddress.IsLoopback(e));
            Assert.NotNull(addr);

            var uri = new NetUri("https://www.newlifex.com");
            var client = uri.CreateRemote() as TcpSession;
            client.Local.Address = addr;
            client.Open();

            Assert.True(client.RemoteAddress.IsIPv4());
        }

        [Fact(Skip = "跳过")]
        public void BindTest3()
        {
            var addr = NetHelper.GetIPsWithCache().FirstOrDefault(e => e.IsIPv4() && !IPAddress.IsLoopback(e));
            Assert.NotNull(addr);

            var uri = new NetUri("https://www.newlifex.com");
            var client = uri.CreateRemote() as TcpSession;
            client.Open();

            Assert.True(client.RemoteAddress.IsIPv4());
        }

        [Fact]
        public void BindTest4()
        {
            Assert.True(Socket.OSSupportsIPv4);
            Assert.True(Socket.OSSupportsIPv6);

            var entry = Dns.GetHostEntry("www.newlifex.com");
            //var entry = Dns.GetHostEntry("www.newlifex.com.w.cdngslb.com");
            Assert.NotNull(entry);

            var addr = NetHelper.GetIPsWithCache().FirstOrDefault(e => !e.IsIPv4() && !IPAddress.IsLoopback(e));
            Assert.NotNull(addr);

            if (entry.AddressList.Any(_ => !_.IsIPv4()))
            {
                var uri = new NetUri("https://www.newlifex.com");
                var client = uri.CreateRemote();
                client.Local.Address = addr;
                client.Open();
            }
        }
    }
}