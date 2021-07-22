using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using NewLife.Net;
using Xunit;

namespace XUnitTest.Net
{
    public class NetUriTests
    {
        [Fact]
        public void TestIPv6()
        {
            var addr = IPAddress.IPv6Loopback;
            Assert.Equal("::1", addr + "");

            var ep = new IPEndPoint(addr, 80);
            Assert.Equal("[::1]:80", ep + "");

            var uri = new NetUri(NetType.Tcp, ep);
            Assert.Equal("Tcp://[::1]:80", uri.ToString());
            Assert.Equal(ep, uri.EndPoint);
            Assert.True(uri.IsTcp);

            uri.Port = 0;
            Assert.Equal("Tcp://::1", uri + "");

            var uri2 = new NetUri("http://[::1]:80");
            Assert.Equal(NetType.Http, uri2.Type);
            Assert.False(uri2.IsUdp);
            Assert.Null(uri2.Host);
            Assert.Equal(IPAddress.IPv6Loopback, uri2.Address);
            Assert.Equal(80, uri2.Port);
            Assert.Equal("Http://[::1]:80", uri2.ToString());

            var uri3 = new NetUri("wss://::1");
            Assert.Equal(NetType.WebSocket, uri3.Type);
            Assert.Null(uri3.Host);
            Assert.Equal(IPAddress.IPv6Loopback, uri3.Address);
            Assert.Equal(443, uri3.Port);
            Assert.Equal("wss://[::1]:443", uri3.ToString());

            var uri4 = new NetUri();
            uri4.Parse("ws://[240e:e0:9930:2100:9914:b410:c7d8:c0a6]");
            Assert.Equal(NetType.WebSocket, uri4.Type);
            Assert.Null(uri4.Host);
            Assert.Equal("240e:e0:9930:2100:9914:b410:c7d8:c0a6", uri4.Address + "");
            Assert.Equal(80, uri4.Port);
            Assert.Equal("ws://[240e:e0:9930:2100:9914:b410:c7d8:c0a6]:80", uri4.ToString());
        }

        [Fact]
        public void ParseAddress()
        {
            var addrs = NetUri.ParseAddress("www.newlifex.com");
            Assert.NotNull(addrs);
            //Assert.Equal(3, addrs.Length);
            Assert.True(addrs.Length > 0);
            Assert.Contains(addrs, e => e.AddressFamily == AddressFamily.InterNetwork);
            if (addrs.Length > 1)
                Assert.Contains(addrs, e => e.AddressFamily == AddressFamily.InterNetworkV6);

            var addrs2 = NetUri.ParseAddress("240e:e0:9930:2100:9914:b410:c7d8:c0a6");
            Assert.NotNull(addrs2);
            Assert.Single(addrs2);
            Assert.Equal("240e:e0:9930:2100:9914:b410:c7d8:c0a6", addrs2[0] + "");

            var uri = new NetUri("https://www.newlifex.com");
            Assert.Equal("www.newlifex.com", uri.Host);
            var addrs3 = uri.GetAddresses();
            //Assert.Equal(3, addrs3.Length);
            Assert.True(addrs.Length > 0);
        }
    }
}