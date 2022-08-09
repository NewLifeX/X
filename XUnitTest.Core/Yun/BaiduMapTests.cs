using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Http;
using NewLife.Yun;
using Xunit;
using NewLife;
using System.Net.Http;

namespace XUnitTest.Yun
{
    public class BaiduMapTests
    {
        [Fact]
        public async void IpLocation()
        {
            var html = new HttpClient().GetString("http://myip.ipip.net");
            var ip = html?.Substring("IP：", " ");
            Assert.NotEmpty(ip);

            var map = new BaiduMap();
            var rs = await map.IpLocationAsync(ip);

            Assert.NotNull(rs);

            var addrs = (rs["full_address"] + "").Split('|');
            Assert.Equal(7, addrs.Length);
        }
    }
}