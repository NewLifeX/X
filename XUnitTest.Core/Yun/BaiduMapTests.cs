using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife.Yun;
using Xunit;

namespace XUnitTest.Yun
{
    public class BaiduMapTests
    {
        [Fact]
        public async void IpLocation()
        {
            var map = new BaiduMap();
            var rs = await map.IpLocationAsync("");

            Assert.NotNull(rs);

            var addrs = (rs["full_address"] + "").Split('|');
            Assert.Equal(7, addrs.Length);
        }
    }
}