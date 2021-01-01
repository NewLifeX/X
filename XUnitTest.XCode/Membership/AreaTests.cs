using System.IO;
using System.Text;
using System.Linq;
using System.Net.Http;
using NewLife.Http;
using NewLife.Log;
using XCode.Membership;
using Xunit;
using static XCode.Membership.Area;
using System;
using System.Threading;
using NewLife;

namespace XUnitTest.XCode.Membership
{
    public class AreaTests
    {
        static AreaTests()
        {
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
        }

        [Fact]
        public void QueryTest()
        {
            var r = Root;
            Assert.NotNull(r);
            Assert.NotNull(r.Childs);
            Assert.Equal(34, r.Childs.Count);
            Assert.Equal("北京", r.Childs[0].Name);

            r = Area.FindByName(0, "北京");
            Assert.Equal(17, r.AllChilds.Count);
            Assert.Equal("北京/延庆", r.AllChilds[^1].Path);

            r = Area.FindByID(450921);
            Assert.Equal("容县", r.Name);
            Assert.Equal(2, r.AllParents.Count);
            Assert.Equal("广西/玉林", r.ParentPath);
            Assert.Equal(15, r.AllChilds.Count);
            Assert.Equal("广西/玉林/容县/容州", r.AllChilds[0].Path);

            var r2 = Area.FindByIDs(450921102, 450921, 450900, 450000);
            Assert.Equal("杨梅", r2.Name);

            var r3 = Area.FindByName(450900, "北流");
            Assert.Equal("北流", r3.Name);

            var rs = Area.FindAllByName("华容");
            Assert.Equal(2, rs.Count);
            Assert.Equal("湖北/鄂州/华容", rs[0].Path);
            Assert.Equal("湖南/岳阳/华容", rs[1].Path);

            var r4 = Area.FindByFullName("华容县");
            Assert.Equal("湖南/岳阳/华容", r4.Path);

            var rs2 = Area.FindAllByParentID(450000);
            Assert.Equal(14, rs2.Count);
            Assert.Equal("南宁", rs2[0].Name);
            Assert.Equal("广西", rs2[0].Parent.Name);
        }

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
        public void VirtualTest()
        {
            {
                var rs = Area.FindAllByName("东莞");
                Assert.Single(rs);

                rs = rs[0].Childs;
                Assert.Single(rs);
                Assert.Equal("直辖镇", rs[0].Name);
            }
            {
                var rs = Area.FindAllByName("中山市");
                Assert.Single(rs);

                rs = rs[0].Childs;
                Assert.Single(rs);
                Assert.Equal("直辖镇", rs[0].Name);
            }
            {
                var rs = Area.FindAllByName("儋州");
                Assert.Single(rs);

                rs = rs[0].Childs;
                Assert.Single(rs);
                Assert.Equal("直辖镇", rs[0].Name);
            }
            {
                var rs = Area.FindAllByName("嘉峪关");
                Assert.Single(rs);

                rs = rs[0].Childs;
                Assert.Single(rs);
                Assert.Equal("直辖镇", rs[0].Name);
            }
            {
                var rs = Area.FindAllByName("仙桃");
                Assert.Single(rs);
                Assert.Equal("县级市", rs[0].Kind);
                Assert.Equal("直辖县", rs[0].Parent.Name);
            }
        }

        //[Fact]
        //public async void Download()
        //{
        //    var url = "http://www.mca.gov.cn/article/sj/xzqh/2020/2020/2020092500801.html";
        //    var file = "area.html".GetFullPath();
        //    //if (!File.Exists(file))
        //    {
        //        var http = new HttpClient();
        //        await http.DownloadFileAsync(url, file);
        //    }

        //    Assert.True(File.Exists(file));
        //}

        //[Fact]
        //public void ParseTest()
        //{
        //    var file = "area.html".GetFullPath();
        //    var txt = File.ReadAllText(file);
        //    //foreach (var item in Area.Parse(txt))
        //    //{
        //    //    XTrace.WriteLine("{0} {1}", item.ID, item.Name);
        //    //}

        //    var rs = Parse(txt).ToList();
        //    Assert.NotNull(rs);
        //    Assert.True(rs.Count > 3000);
        //}

        //[Fact]
        //public void ParseLevel4Test()
        //{
        //    Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

        //    var url = "http://www.stats.gov.cn/tjsj/tjbz/tjyqhdmhcxhfdm/2018/45/09/450921.html";
        //    var http = new HttpClient();
        //    var buf = http.GetByteArrayAsync(url).Result;
        //    var html = Encoding.GetEncoding("gb2312").GetString(buf);

        //    var rs = ParseLevel4(html).ToList();
        //    Assert.NotNull(rs);
        //    Assert.Equal(15, rs.Count);
        //}

        //[Fact]
        //public void ParseAndSave()
        //{
        //    var file = "area.html".GetFullPath();
        //    var txt = File.ReadAllText(file);

        //    var rs = Area.ParseAndSave(txt);
        //    //Assert.True(rs > 3000);

        //    var r = Area.Find(_.ParentID == 0 & _.Name == "上海");
        //    Assert.NotNull(r);
        //    Assert.Equal("上海市", r.FullName);

        //    r = Area.Find(_.Name == "广西");
        //    Assert.NotNull(r);
        //    Assert.Equal("广西壮族自治区", r.FullName);

        //    r = Area.Find(_.Name == "仙桃");
        //    Assert.NotNull(r);
        //    Assert.NotNull(r.Parent);
        //    Assert.Equal("直辖县", r.Parent.Name);
        //}

        //[Fact]
        //public void FetchAndSave()
        //{
        //    Area.Meta.Session.Dal.Db.ShowSQL = false;

        //    var rs = Area.FetchAndSave();

        //    var r = Area.Find(_.ParentID == 0 & _.Name == "上海");
        //    Assert.NotNull(r);
        //    Assert.Equal("上海市", r.FullName);

        //    r = Area.Find(_.Name == "广西");
        //    Assert.NotNull(r);
        //    Assert.Equal("广西壮族自治区", r.FullName);

        //    Area.Meta.Session.Dal.Db.ShowSQL = true;
        //}

        [Fact]
        public void Import()
        {
            Area.Meta.Session.Dal.Db.ShowSQL = false;

            if (Area.Meta.Count == 0)
            {
                var file = "http://x.newlifex.com/Area.csv.gz";

                Area.Meta.Session.Truncate();
                var rs = Area.Import(file, true, 3);
                Assert.Equal(3639, rs);

                Area.Meta.Session.Truncate();
                rs = Area.Import(file, true, 4);
                Assert.Equal(46533, rs);
            }

            Area.Meta.Session.Dal.Db.ShowSQL = true;
        }

        [Fact]
        public void Export()
        {
            var file = $"Data/Area_{DateTime.Now:yyyyMMdd}.csv.gz";
            if (File.Exists(file.GetFullPath())) File.Delete(file.GetFullPath());

            var rs = Area.Export(file);
            Assert.Equal(46533, rs);
            Assert.True(File.Exists(file.GetFullPath()));

            //File.Delete(file.GetFullPath());
        }

        [Theory]
        [InlineData(0, "北京", 1, 110000)]
        [InlineData(0, "北京市", 1, 110000)]
        [InlineData(-1, "北京", 10, 110000)]
        [InlineData(0, "玉林", 1, 450900, "玉林")]
        [InlineData(-1, "玉林", 4, 450900)]
        [InlineData(-1, "yulin", 10, 450900, "玉林")]
        [InlineData(-1, "YL", 10, 230123, "依兰")]
        [InlineData(-1, "537000", 10, 450900, "玉林")]
        public void SearchByKey(Int32 pid, String key, Int32 count, Int32 rid, String name = null)
        {
            XTrace.WriteLine("SearchByKey=>: {0} {1}", pid, key);
            var list = Area.Search(pid, key, true, 10);
            XTrace.WriteLine("SearchByKey<=: {0}", list.Join(",", e => e.Path));

            Assert.Equal(count, list.Count);
            if (list.Count > 0)
            {
                Assert.Equal(rid, list[0].ID);
                if (!name.IsNullOrEmpty()) Assert.Equal(name, list[0].Name);
            }
        }

        [Theory]
        [InlineData(310104, "虹梅街道", 10, 310104012, "虹梅路")]
        [InlineData(310104, "康健街道", 10, 310104013, "康健新村")]
        [InlineData(650000, "乌市", 10, 650100, "乌鲁木齐")]
        [InlineData(110100, "城区", 2, 110101, "东城")]
        [InlineData(110000, "城区", 2, 110101, "东城")]
        [InlineData(0, "中山", 1, 442000, "中山")]
        public void SearchLike(Int32 pid, String key, Int32 count, Int32 rid, String name = null)
        {
            XTrace.WriteLine("SearchLike=>: {0} {1}", pid, key);
            var list = Area.Search(pid, key, true, 10);
            XTrace.WriteLine("SearchLike<=: {0}", list.Join(",", e => e.Path));

            Assert.Equal(count, list.Count);
            if (list.Count > 0)
            {
                Assert.Equal(rid, list[0].ID);
                if (!name.IsNullOrEmpty()) Assert.Equal(name, list[0].Name);
            }
        }

        [Theory]
        [InlineData("上海市虹梅路2588弄", 310104012, "上海/徐汇/虹梅路")]
        [InlineData("上海市华新中学", 310118107, "上海/青浦/华新")]
        [InlineData("广西容县杨梅镇", 450921102, "广西/玉林/容县/杨梅")]
        [InlineData("湖北神农木鱼", 429021102, "湖北/神农架/木鱼")]
        public void SearchAddress(String address, Int32 rid, String target)
        {
            XTrace.WriteLine("SearchAddress=>: {0}", address);
            var set = Area.SearchAddress(address);
            XTrace.WriteLine("SearchAddress<=: {0}", set.Join(",", e => $"[{e.Value:n2}]{e.Key.Path}"));

            if (set.Count > 0)
            {
                var r = set[0].Key;
                Assert.Equal(rid, r.ID);
                Assert.Equal(target, r.Path);
            }
        }
    }
}