using System;
using System.Collections;
using System.Collections.Generic;
using NewLife;
using NewLife.Log;
using NewLife.Model;
using NewLife.Security;
using NewLife.Serialization;
using Xunit;

namespace XUnitTest.Serialization;

public class JsonTest : JsonTestBase
{
    [Fact(DisplayName = "基础测试")]
    public void Test1()
    {
        var set = new Setting
        {
            LogLevel = LogLevel.Error,
            LogPath = "xxx",
            NetworkLog = null,
            DataPath = null,
            BackupPath = null,
            ServiceAddress = null,
        };

        var js = set.ToJson(true, false, false);
        Assert.True(js.StartsWith("{") && js.EndsWith("}"));

        var set2 = js.ToJsonEntity<Setting>();

        Assert.Equal(LogLevel.Error, set2.LogLevel);
        Assert.Equal("xxx", set2.LogPath);

        var sjs = new SystemJson();
        var js2 = sjs.Write(set, true, false, false);
        Assert.Equal(js, js2);
        Assert.True(js2.StartsWith("{") && js2.EndsWith("}"));

        var set3 = sjs.Read<Setting>(js);
        Assert.Equal(set.Debug, set3.Debug);
        Assert.Equal(set.LogLevel, set3.LogLevel);
        Assert.Equal(set.LogPath, set3.LogPath);
    }

    [Fact]
    public void DateTimeTest()
    {
        var model = new Model();
        Rand.Fill(model);
        model.Roles = ["admin", "user"];
        model.Scores = [1, 2, 3];
        var js = model.ToJson(true);

        var models = _json_value.ToJsonEntity<Model[]>();
        Assert.Equal(2, models.Length);

        CheckModel(models);
    }

    [Fact]
    public void InterfaceTest()
    {
        var list = new List<IDuck>
        {
            new DuckB { Name = "123" },
            new DuckB { Name = "456" },
            new DuckB { Name = "789" }
        };
        var model = new ModelA
        {
            ID = 2233,
            Childs = list,
        };

        // 上对象容器。必须在使用前注册好服务
        ObjectContainer.Current.AddTransient<IDuck, DuckB>();

        var json = model.ToJson();

        //// 直接反序列化会抛出异常
        //Assert.Throws<Exception>(() => json.ToJsonEntity<ModelA>());

        // 再来一次反序列化
        var model2 = json.ToJsonEntity<ModelA>();
        Assert.NotNull(model2);
        Assert.Equal(2233, model2.ID);
        Assert.Equal(3, model2.Childs.Count);
        Assert.Equal("123", model.Childs[0].Name);
        Assert.Equal("456", model.Childs[1].Name);
        Assert.Equal("789", model.Childs[2].Name);
    }

    interface IDuck
    {
        public String Name { get; set; }
    }

    class ModelA
    {
        public Int32 ID { get; set; }

        public IList<IDuck> Childs { get; set; }

        public Object Body { get; set; }
    }

    class DuckB : IDuck
    {
        public String Name { get; set; }
    }

    [Fact]
    public void ObjectTest()
    {
        var model = new ModelB
        {
            ID = 2233,
            Body = new
            {
                aaa = 123,
                bbb = 456,
                ccc = 789,
            },
        };

        // 序列化
        var json = model.ToJson();

        // 反序列化
        var model2 = json.ToJsonEntity<ModelB>();
        Assert.NotNull(model2);
        Assert.Equal(2233, model2.ID);

        //var dic = model2.Body as IDictionary<String, Object>;
        var dic = model2.Body.ToDictionary();
        Assert.Equal(3, dic.Count);
        Assert.Equal(123, dic["aaa"]);
        Assert.Equal(456, dic["bbb"]);
        Assert.Equal(789, dic["ccc"]);
    }

    class ModelB
    {
        public Int32 ID { get; set; }

        public Object Body { get; set; }
    }

    [Fact]
    public void Decode()
    {
        var model = new ModelB
        {
            ID = 2233,
            Body = new
            {
                aaa = 123,
                bbb = 456,
                ccc = 789,
            },
        };

        // 序列化
        var json = model.ToJson();

        var dic = json.DecodeJson();
        Assert.NotNull(dic);

        Assert.Equal(2233, dic["id"]);

        dic = dic["body"] as IDictionary<String, Object>;
        Assert.NotNull(dic);
        Assert.Equal(3, dic.Count);
        Assert.Equal(123, dic["aaa"]);
        Assert.Equal(456, dic["bbb"]);
        Assert.Equal(789, dic["ccc"]);
    }

    [Fact(DisplayName = "多层数组嵌套FastJson")]
    public void MutilLevelTest()
    {
        var lines = new List<LineModel>();
        for (var i = 0; i < 3; i++)
        {
            var line = new LineModel();
            Rand.Fill(line);

            for (var j = 0; j < 3; j++)
            {
                var nb = new NodeBillModel();
                Rand.Fill(nb);

                for (var k = 0; k < 3; k++)
                {
                    var b = new LineBill();
                    Rand.Fill(b);
                    nb.Bills.Add(b);
                }

                line.NodeBills.Add(nb);
            }

            for (var j = 0; j < 3; j++)
            {
                var b = new LineBill();
                Rand.Fill(b);
                line.Bills.Add(b);
            }

            // 制造引用
            line.Bills.Add(line.NodeBills[2].Bills[1]);
            line.Bills.Add(line.NodeBills[1].Bills[2]);
            line.Bills.Add(line.NodeBills[0].Bills[0]);

            lines.Add(line);
        }

        var opt = new JsonOptions
        {
            WriteIndented = true,
            IgnoreCycles = false
        };
        var js = lines.ToJson(opt);

        var lines2 = js.ToJsonEntity<LineModel[]>();

        Assert.Equal(lines.Count, lines2.Length);

        for (var i = 0; i < lines.Count; i++)
        {
            var line1 = lines[i];
            var line2 = lines2[i];

            Assert.Equal(line1.First, line2.First);
            Assert.Equal(line1.Last, line2.Last);
            Assert.Equal(line1.Weight, line2.Weight);
            Assert.Equal(line1.Volume, line2.Volume);
            Assert.Equal(line1.NodeBills.Count, line2.NodeBills.Count);
            Assert.Equal(line1.Bills.Count, line2.Bills.Count);

            for (var j = 0; j < line1.NodeBills.Count; j++)
            {
                var nb1 = line1.NodeBills[j];
                var nb2 = line2.NodeBills[j];

                Assert.Equal(nb1.LoadNode, nb2.LoadNode);
                Assert.Equal(nb1.UnLoadNode, nb2.UnLoadNode);
                Assert.Equal(nb1.Bills.Count, nb2.Bills.Count);

                for (var k = 0; k < nb1.Bills.Count; k++)
                {
                    var b1 = nb1.Bills[k];
                    var b2 = nb2.Bills[k];

                    Assert.Equal(b1.Id, b2.Id);
                    Assert.Equal(b1.FirstId, b2.FirstId);
                    Assert.Equal(b1.LastId, b2.LastId);
                    Assert.Equal(b1.Weight, b2.Weight);
                    Assert.Equal(b1.Volume, b2.Volume);
                }
            }

            for (var j = 0; j < line1.Bills.Count; j++)
            {
                var b1 = line1.Bills[j];
                var b2 = line2.Bills[j];

                Assert.Equal(b1.Id, b2.Id);
                Assert.Equal(b1.FirstId, b2.FirstId);
                Assert.Equal(b1.LastId, b2.LastId);
                Assert.Equal(b1.Weight, b2.Weight);
                Assert.Equal(b1.Volume, b2.Volume);
            }
        }
    }

    [Fact(DisplayName = "多层数组嵌套SystemJson")]
    public void MutilLevelTest_System()
    {
        var lines = new List<LineModel>();
        for (var i = 0; i < 3; i++)
        {
            var line = new LineModel();
            Rand.Fill(line);

            for (var j = 0; j < 3; j++)
            {
                var nb = new NodeBillModel();
                Rand.Fill(nb);

                for (var k = 0; k < 3; k++)
                {
                    var b = new LineBill();
                    Rand.Fill(b);
                    nb.Bills.Add(b);
                }

                line.NodeBills.Add(nb);
            }

            for (var j = 0; j < 3; j++)
            {
                var b = new LineBill();
                Rand.Fill(b);
                line.Bills.Add(b);
            }

            // 制造引用
            line.Bills.Add(line.NodeBills[2].Bills[1]);
            line.Bills.Add(line.NodeBills[1].Bills[2]);
            line.Bills.Add(line.NodeBills[0].Bills[0]);

            lines.Add(line);
        }

        //var js = lines.ToJson(true);
        var js = new SystemJson().Write(lines, true, false, false);

        var lines2 = js.ToJsonEntity<LineModel[]>();

        Assert.Equal(lines.Count, lines2.Length);

        for (var i = 0; i < lines.Count; i++)
        {
            var line1 = lines[i];
            var line2 = lines2[i];

            Assert.Equal(line1.First, line2.First);
            Assert.Equal(line1.Last, line2.Last);
            Assert.Equal(line1.Weight, line2.Weight);
            Assert.Equal(line1.Volume, line2.Volume);
            Assert.Equal(line1.NodeBills.Count, line2.NodeBills.Count);
            Assert.Equal(line1.Bills.Count, line2.Bills.Count);

            for (var j = 0; j < line1.NodeBills.Count; j++)
            {
                var nb1 = line1.NodeBills[j];
                var nb2 = line2.NodeBills[j];

                Assert.Equal(nb1.LoadNode, nb2.LoadNode);
                Assert.Equal(nb1.UnLoadNode, nb2.UnLoadNode);
                Assert.Equal(nb1.Bills.Count, nb2.Bills.Count);

                for (var k = 0; k < nb1.Bills.Count; k++)
                {
                    var b1 = nb1.Bills[k];
                    var b2 = nb2.Bills[k];

                    Assert.Equal(b1.Id, b2.Id);
                    Assert.Equal(b1.FirstId, b2.FirstId);
                    Assert.Equal(b1.LastId, b2.LastId);
                    Assert.Equal(b1.Weight, b2.Weight);
                    Assert.Equal(b1.Volume, b2.Volume);
                }
            }

            for (var j = 0; j < line1.Bills.Count; j++)
            {
                var b1 = line1.Bills[j];
                var b2 = line2.Bills[j];

                Assert.Equal(b1.Id, b2.Id);
                Assert.Equal(b1.FirstId, b2.FirstId);
                Assert.Equal(b1.LastId, b2.LastId);
                Assert.Equal(b1.Weight, b2.Weight);
                Assert.Equal(b1.Volume, b2.Volume);
            }
        }
    }

    class LineModel
    {
        #region 属性
        /// <summary>首节点</summary>
        public String First { get; set; } = null!;

        /// <summary>末节点</summary>
        public String Last { get; set; } = null!;

        /// <summary>重量</summary>
        public Double Weight { get; set; }

        /// <summary>体积</summary>
        public Double Volume { get; set; }

        /// <summary>节点装卸货流向</summary>
        public List<NodeBillModel> NodeBills { get; set; } = [];

        /// <summary>流向明细</summary>
        public List<LineBill> Bills { get; set; } = [];
        #endregion
    }

    class NodeBillModel
    {
        #region 属性
        /// <summary>装货节点</summary>
        public String LoadNode { get; set; } = null!;

        /// <summary>卸货节点</summary>
        public String UnLoadNode { get; set; } = null!;

        public List<LineBill> Bills { get; set; } = [];
        #endregion
    }
    partial class LineBill
    {
        #region 属性
        /// <summary>编号</summary>
        public Int64 Id { get; set; }

        /// <summary>始发中心ID</summary>
        public String FirstId { get; set; }

        /// <summary>末中心ID</summary>
        public String LastId { get; set; }

        /// <summary>总重量。初始</summary>
        public Double Weight { get; set; }

        /// <summary>总体积。初始</summary>
        public Double Volume { get; set; }
        #endregion
    }

    [Fact]
    public void Convert()
    {
        var infos = new List<LineBill>
        {
            new() { Id = 1, FirstId = "1", LastId = "2", Weight = 1, Volume = 1 },
            new() { Id = 2, FirstId = "2", LastId = "3", Weight = 2, Volume = 2 },
            new() { Id = 3, FirstId = "3", LastId = "4", Weight = 3, Volume = 3 }
        };
        var p = new
        {
            infos,
        };
        var json = p.ToJson();

        var dic = JsonParser.Decode(json);
        var data = dic["infos"];
        Assert.NotNull(data);

        var fs = JsonHelper.Convert<IList<LineBill>>(data);
        Assert.NotNull(fs);
        Assert.Equal(infos.Count, fs.Count);

        var arr = JsonHelper.Convert<LineBill[]>(data);
        Assert.NotNull(arr);
        Assert.Equal(infos.Count, arr.Length);
    }
}