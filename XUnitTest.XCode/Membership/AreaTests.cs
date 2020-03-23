using System.IO;
using System.Text;
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
        public void FindByNamesTest()
        {
            var r = Area.FindByNames("广西", "玉林", "容县");
            Assert.NotNull(r);
            Assert.Equal(450921, r.ID);
            Assert.Equal("广西/玉林/容县", r.Path);

            r = Area.FindByNames("上海", "青浦");
            Assert.NotNull(r);
            Assert.Equal(310118, r.ID);
            Assert.Equal("上海/青浦", r.Path);

            r = Area.FindByNames("重庆", "万州");
            Assert.NotNull(r);
            Assert.Equal(500101, r.ID);
            Assert.Equal("重庆/万州", r.Path);

            r = Area.FindByNames("重庆", "巫山");
            Assert.NotNull(r);
            Assert.Equal(500237, r.ID);
            Assert.Equal("重庆/巫山", r.Path);

            r = Area.FindByNames("湖北", "仙桃");
            Assert.NotNull(r);
            Assert.Equal(429004, r.ID);
            Assert.Equal("湖北/仙桃", r.Path);

            r = Area.FindByNames("湖北", "神农架");
            Assert.NotNull(r);
            Assert.Equal(429021, r.ID);
            Assert.Equal("湖北/神农架", r.Path);
        }

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

            var rs = Parse(txt).ToList();
            Assert.NotNull(rs);
            Assert.True(rs.Count > 3000);
        }

        [Fact]
        public void ParseLevel4Test()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            var url = "http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2018/45/09/450921.html";
            var http = new HttpClient();
            var buf = http.GetByteArrayAsync(url).Result;
            var html = Encoding.GetEncoding("gb2312").GetString(buf);

            var rs = ParseLevel4(html).ToList();
            Assert.NotNull(rs);
            Assert.Equal(15, rs.Count);
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
            Assert.Equal("直辖县", r.Parent.Name);
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