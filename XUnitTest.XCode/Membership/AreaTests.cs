using System.IO;
using System.Linq;
using System.Net.Http;
using NewLife.Http;
using NewLife.Log;
using XCode.Membership;
using Xunit;

namespace XUnitTest.XCode.Membership
{
    public class AreaTests
    {
        [Fact]
        public async void Download()
        {
            var url = "http://www.mca.gov.cn/article/sj/xzqh/2019/2019/201912251506.html";
            var file = "area.html".GetFullPath();
            if (!File.Exists(file))
            {
                var http = new HttpClient();
                await http.DownloadFileAsync(url, file);
            }

            Assert.True(File.Exists(file));
        }

        [Fact]
        public void ParseTest()
        {
            var file = "area.html".GetFullPath();
            var txt = File.ReadAllText(file);
            //foreach (var item in Area.Parse(txt))
            //{
            //    XTrace.WriteLine("{0} {1}", item.ID, item.Name);
            //}

            var rs = Area.Parse(txt).ToList();
            Assert.NotNull(rs);
            Assert.True(rs.Count > 3000);

            //var r = Area.FindByName(0, "上海");
            //Assert.NotNull(r);
        }
    }
}