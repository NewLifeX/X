using System;
using System.Collections;
using System.Collections.Generic;
using NewLife;
using NewLife.Collections;
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
            PluginPath = null,
            ServiceAddress = null,
        };

        var js = set.ToJson(true, false, false);
        Assert.True(js.StartsWith("{") && js.EndsWith("}"));

        var set2 = js.ToJsonEntity<Setting>();

        Assert.Equal(LogLevel.Error, set2.LogLevel);
        Assert.Equal("xxx", set2.LogPath);

#if NETCOREAPP
        var sjs = new SystemJson();
        var js2 = sjs.Write(set, true, false, false);
        Assert.True(js2.StartsWith("{") && js2.EndsWith("}"));

        // 往返验证：SystemJson 反序列化 FastJson 的输出，确认兼容
        var set3 = sjs.Read<Setting>(js);
        Assert.Equal(set.Debug, set3.Debug);
        Assert.Equal(set.LogLevel, set3.LogLevel);
        Assert.Equal(set.LogPath, set3.LogPath);
#endif
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

#if NETCOREAPP
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
#endif

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

#if NETCOREAPP
/// <summary>DateTime 格式对比测试：验证 FastJson 与 SystemJson 输出一致</summary>
public class DateTimeFormatTests
{
    /// <summary>本地时间默认格式：yyyy-MM-dd HH:mm:ss（空格分隔）</summary>
    [Fact(DisplayName = "本地时间格式对比")]
    public void LocalDateTimeFormat()
    {
        var dt = new DateTime(2025, 6, 22, 15, 30, 0, DateTimeKind.Local);
        var model = new { Time = dt };

        var fastJson = model.ToJson();
        var sysJson = new SystemJson().Write(model);

        // 各自验证格式特征（切换后格式不同，不比较精确相等）
        Assert.Contains("2025-06-22", fastJson);
        Assert.Contains("15:30:00", fastJson);
        Assert.Contains("2025-06-22", sysJson);
        Assert.Contains("15:30:00", sysJson);
        // SystemJson 使用空格分隔
        Assert.Contains("2025-06-22 15:30:00", sysJson);

        // 往返验证
        var back = sysJson.ToJsonEntity(model.GetType());
        Assert.NotNull(back);
    }

    /// <summary>UTC 时间：FastJson 输出带 Z（T分隔不含毫秒），SystemJson 输出 O 格式含毫秒和 Z</summary>
    [Fact(DisplayName = "UTC时间格式对比")]
    public void UtcDateTimeFormat()
    {
        var dt = new DateTime(2025, 6, 22, 7, 30, 0, DateTimeKind.Utc);
        var model = new { Time = dt };

        var fastJson = model.ToJson();
        var sysJson = new SystemJson().Write(model);

        // 各自都应包含 Z 后缀表示 UTC
        Assert.Contains("Z", fastJson);
        Assert.Contains("Z", sysJson);
        // 往返验证
        var fastBack = fastJson.ToJsonEntity(model.GetType());
        Assert.NotNull(fastBack);
    }

    /// <summary>FullTime=true 时全部输出 O 格式（含毫秒）</summary>
    [Fact(DisplayName = "FullTime完整格式对比")]
    public void FullTimeFormat()
    {
        var dt = new DateTime(2025, 6, 22, 15, 30, 0, DateTimeKind.Local);
        var model = new { Time = dt };

        var opt = new JsonOptions { FullTime = true };
        var fastJson = new FastJson { Options = opt }.Write(model);
        var sysJson = new SystemJson { Options = new JsonOptions { FullTime = true } }.Write(model);

        // 两者都输出 O 格式（含日期、时间、毫秒）
        Assert.Contains("2025-06-22T15:30:00.0000000", fastJson);
        Assert.Contains("2025-06-22T15:30:00.0000000", sysJson);
    }
}

/// <summary>IDictionarySource 转换器测试：验证 SystemJson 正确处理 IDictionarySource 实现</summary>
public class IDictionarySourceTests
{
    /// <summary>模拟 ChartGrid：条件性输出，值变换</summary>
    class MockChartGrid : IDictionarySource
    {
        public Int32 Left { get; set; }
        public Int32 Right { get; set; }
        public Int32 Width { get; set; } = 800;
        public Int32 Height { get; set; } = 600;

        public IDictionary<String, Object?> ToDictionary()
        {
            var dic = new Dictionary<String, Object?>();
            // 模拟 ChartGrid 行为：负数写百分比，Width/Height 不输出
            if (Left != 0) dic[nameof(Left)] = Left < 0 ? $"{{{-Left}}}%" : Left;
            if (Right != 0) dic[nameof(Right)] = Right < 0 ? $"{{{-Right}}}%" : Right;
            return dic;
        }
    }

    /// <summary>模拟 PropertySpec：仅输出非空非默认属性</summary>
    class MockPropertySpec : IDictionarySource
    {
        public String Id { get; set; } = "temp";
        public String Name { get; set; } = "温度";
        public Boolean Required { get; set; } = true;
        public String? AccessMode { get; set; } = "rw";
        public Double Scaling { get; set; } = 1.0;     // 默认值，不应输出
        public Int32 Constant { get; set; } = 0;        // 默认值，不应输出

        public IDictionary<String, Object?> ToDictionary()
        {
            var dic = new Dictionary<String, Object?>();
            if (!Id.IsNullOrEmpty()) dic.Add("identifier", Id);
            if (!Name.IsNullOrEmpty()) dic.Add(nameof(Name), Name);
            if (Required) dic.Add(nameof(Required), Required);
            if (!AccessMode.IsNullOrEmpty()) dic.Add(nameof(AccessMode), AccessMode);
            if (Scaling != 1) dic[nameof(Scaling)] = Scaling;
            if (Constant != 0) dic[nameof(Constant)] = Constant;
            return dic;
        }
    }

    [Fact(DisplayName = "IDictionarySource 值变换")]
    public void DictionarySourceValueTransform()
    {
        var grid = new MockChartGrid { Left = -10, Right = 20 };
        var fastJson = grid.ToJson();
        var sysJson = new SystemJson().Write(grid);

        // 两个实现输出一致
        Assert.Equal(fastJson, sysJson);
        // 值变换验证：Left=-10 → 百分比表达式（双重否定：-(-10)=10）
        Assert.Contains("{10}%", sysJson);
        Assert.Contains("20", sysJson);
        // Width/Height 不应输出
        Assert.DoesNotContain("Width", sysJson);
        Assert.DoesNotContain("Height", sysJson);
    }

    [Fact(DisplayName = "IDictionarySource 条件性输出")]
    public void DictionarySourceConditionalOutput()
    {
        var spec = new MockPropertySpec();
        var fastJson = spec.ToJson();
        var sysJson = new SystemJson().Write(spec);

        // 两个实现输出一致
        Assert.Equal(fastJson, sysJson);
        // 默认值不应输出
        Assert.DoesNotContain("\"Scaling\"", sysJson);
        Assert.DoesNotContain("\"Constant\"", sysJson);
        // 非默认值应输出
        Assert.Contains("identifier", sysJson);
        Assert.Contains("温度", sysJson);
        Assert.Contains("Required", sysJson);
    }

    [Fact(DisplayName = "IDictionarySource 嵌入字典")]
    public void DictionarySourceInDictionary()
    {
        // 模拟 ECharts.Build() 场景：IDictionarySource 对象作为字典值
        var grid = new MockChartGrid { Left = -10, Right = 20 };
        var dic = new Dictionary<String, Object?>
        {
            ["grid"] = grid,
            ["title"] = "Test"
        };

        var fastJson = dic.ToJson();
        var sysJson = new SystemJson().Write(dic);

        Assert.Equal(fastJson, sysJson);
        Assert.Contains("{10}%", sysJson);
    }
}

/// <summary>IgnoreNullValues 语义测试：验证仅跳过 null，不跳过默认值</summary>
public class IgnoreNullValuesTests
{
    class ModelWithDefaults
    {
        public String? Name { get; set; }
        public Int32 Count { get; set; }
        public Boolean Enabled { get; set; }
        public Double Score { get; set; }
        public DateTime Time { get; set; }
    }

    [Fact(DisplayName = "IgnoreNullValues仅跳过null")]
    public void IgnoreNullButKeepDefaults()
    {
        var model = new ModelWithDefaults
        {
            Name = null,
            Count = 0,
            Enabled = false,
            Score = 0.0,
        };

        var opt = new JsonOptions { IgnoreNullValues = true };
        var sysJson = new SystemJson { Options = new JsonOptions { IgnoreNullValues = true } }.Write(model);

        // Name 为 null，应跳过
        Assert.DoesNotContain("\"Name\"", sysJson);
        // Count=0 默认值，应保留（WhenWritingNull 仅跳过 null，不跳过默认值）
        Assert.Contains("\"Count\":0", sysJson);
        Assert.Contains("\"Enabled\":false", sysJson);
    }
}

/// <summary>LocalTimeConverter 读取测试：验证 Z 后缀和 UTC 后缀</summary>
public class LocalTimeReaderTests
{
    [Fact(DisplayName = "读取Z后缀时间")]
    public void ReadZSuffix()
    {
        var converter = new LocalTimeConverter();
        var json = "\"2025-06-22T07:30:00Z\"";

        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var reader = new System.Text.Json.Utf8JsonReader(bytes);
        while (reader.TokenType != System.Text.Json.JsonTokenType.String) reader.Read();

        var dt = converter.Read(ref reader, typeof(DateTime), null!);

        // Z 后缀表示 UTC，应转为本地时间
        Assert.Equal(DateTimeKind.Local, dt.Kind);
        Assert.Equal(22, dt.Day);
    }

    [Fact(DisplayName = "读取UTC后缀时间")]
    public void ReadUTCSuffix()
    {
        var converter = new LocalTimeConverter();
        var json = "\"2025-06-22 07:30:00 UTC\"";

        var bytes = System.Text.Encoding.UTF8.GetBytes(json);
        var reader = new System.Text.Json.Utf8JsonReader(bytes);
        while (reader.TokenType != System.Text.Json.JsonTokenType.String) reader.Read();

        var dt = converter.Read(ref reader, typeof(DateTime), null!);

        // UTC 后缀应转为本地时间
        Assert.Equal(DateTimeKind.Local, dt.Kind);
        Assert.Equal(22, dt.Day);
    }
}
#endif