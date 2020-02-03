using System.IO;
using System.Linq;
using System.Net.Http;
using NewLife.Http;
using NewLife.Log;
using XCode.Membership;
using Xunit;
using static XCode.Membership.Area;

namespace XUnitTest.XCode.Membership
{
    public class AreaTests
    {
        [Fact]
        public async void Download()
        {
            var url = "http://www.mca.gov.cn/article/sj/xzqh/2019/2019/201912251506.html";
            var file = "area.html".GetFullPath();
            //if (!File.Exists(file))
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
        }

        [Fact]
        public void ParseAndSave()
        {
            var file = "area.html".GetFullPath();
            var txt = File.ReadAllText(file);

            var rs = Area.ParseAndSave(txt);
            //Assert.True(rs > 3000);

            var r = Area.Find(_.Name == "上海");
            Assert.NotNull(r);
            Assert.Equal("上海市", r.FullName);

            r = Area.Find(_.Name == "广西");
            Assert.NotNull(r);
            Assert.Equal("广西壮族自治区", r.FullName);

            r = Area.Find(_.Name == "仙桃");
            Assert.NotNull(r);
            Assert.NotNull(r.Parent);
            Assert.Equal("湖北", r.Parent.Name);
        }

        [Fact]
        public void FetchAndSave()
        {
            var rs = Area.FetchAndSave();

            var r = Area.Find(_.Name == "上海");
            Assert.NotNull(r);
            Assert.Equal("上海市", r.FullName);

            r = Area.Find(_.Name == "广西");
            Assert.NotNull(r);
            Assert.Equal("广西壮族自治区", r.FullName);
        }
    }
}