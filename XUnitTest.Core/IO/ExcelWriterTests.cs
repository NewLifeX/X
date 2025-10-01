using System.ComponentModel;
using System.IO.Compression;
using System.Text;
using NewLife.IO;
using Xunit;

namespace XUnitTest.IO;

public class ExcelWriterTests
{
    [Fact, DisplayName("单Sheet全类型往返")] 
    public void SingleSheet_AllTypes_Roundtrip()
    {
        using var ms = new MemoryStream();
        var writer = new ExcelWriter(ms);

        writer.WriteHeader(null!, new[] { "Name", "Percent", "Date", "DateTime", "Time", "Int", "Long", "DecFrac", "DecInt", "DoubleFrac", "BoolT", "BoolF", "BigNum", "LeadingZero", "IdCard", "PercentTextFail", "GapTest" });

        var dateOnly = new DateTime(2024, 7, 1);
        var dateTime = new DateTime(2024, 7, 1, 12, 34, 56);
        var time = TimeSpan.FromHours(5) + TimeSpan.FromMinutes(6) + TimeSpan.FromSeconds(7); // 05:06:07
        // 各列说明：Name(string), Percent("12.5%" 成功解析), Date(DateOnly), DateTime(Date+Time), Time(TimeSpan), Int, Long, DecFrac(有小数), DecInt(无小数), DoubleFrac, BoolT, BoolF, BigNum(>12位), LeadingZero(前导0), IdCard(含X), PercentTextFail("abc%"失败分支), GapTest(中间前面留一个null)
        var row = new Object?[]
        {
            "Alice", "12.5%", dateOnly, dateTime, time, 123, 2147483648L, 123.45m, 456m, 0.125d, true, false,
            "1234567890123", "00123", "12345619900101888X", "abc%", null
        };
        writer.WriteRows(null, new[] { row });

        writer.Save();

        //File.WriteAllBytes("ew.xlsx", ms.ToArray());

        // 用 ExcelReader 读取验证类型与数值
        ms.Position = 0;
        var reader = new ExcelReader(ms, Encoding.UTF8);
        var rows = reader.ReadRows().ToList();
        Assert.Equal(2, rows.Count); // header + 1 数据行
        var header = rows[0].Select(e => e + "").ToArray();
        Assert.Equal("Name", header[0]);

        var data = rows[1];
        // Percent => Double 0.125
        Assert.Equal("Alice", data[0]);
        Assert.True(data[1] is Double && Math.Abs((Double)data[1]! - 0.125d) < 1e-9);
        Assert.True(data[2] is DateTime && ((DateTime)data[2]!).Date == dateOnly.Date && ((DateTime)data[2]!).TimeOfDay == TimeSpan.Zero);
        Assert.True(data[3] is DateTime && (DateTime)data[3]! == dateTime);
        Assert.True(data[4] is TimeSpan && (TimeSpan)data[4]! == time);
        Assert.Equal("123", data[5] + "");
        Assert.Equal(2147483648L, (Int64)data[6]!); // long
        Assert.True(data[7] is Decimal or Double); // 小数
        Assert.True(data[8] is Int32 || data[8] is Int64 || data[8] is Decimal); // 整数小数样式不变
        Assert.True(data[9] is Decimal or Double);
        Assert.True(data[10] is Boolean && (Boolean)data[10]!);
        Assert.True(data[11] is Boolean && !(Boolean)data[11]!);
        Assert.Equal("1234567890123", data[12]); // 大数字保留文本
        Assert.Equal("00123", data[13]); // 前导0保留
        Assert.Equal("12345619900101888X", data[14]); // 身份证含X
        Assert.Equal("abc%", data[15]); // 百分比解析失败 -> 文本
        // GapTest (最后列前提供 null) => ExcelWriter 跳过，读取时为缺失列应自动补 null
        Assert.Null(data[16]);
    }

    [Fact, DisplayName("多Sheet导出与读取")] 
    public void MultiSheet_Export_Read()
    {
        using var ms = new MemoryStream();
        var w = new ExcelWriter(ms);
        w.WriteHeader("Users", new[] { "Id", "Name" });
        w.WriteRows("Users", new[] { new Object?[] { 1, "Tom" }, new Object?[] { 2, "Jerry" } });

        w.WriteHeader("Stats", new[] { "Metric", "Value" });
        w.WriteRows("Stats", new[] { new Object?[] { "Count", 2 }, new Object?[] { "Rate", "50%" } });
        w.Save();

        ms.Position = 0;
        var r = new ExcelReader(ms, Encoding.UTF8);
        var users = r.ReadRows("Users").ToList();
        Assert.Equal(3, users.Count); // header + 2
        var stats = r.ReadRows("Stats").ToList();
        Assert.Equal(3, stats.Count);

        // 百分比在第二个sheet中解析为 Double 0.5
        Assert.True(stats[2][1] is Double d && Math.Abs(d - 0.5) < 1e-9);
    }

    [Fact, DisplayName("无字符串时不生成sharedStrings")] 
    public void NoSharedStrings_FileStructure()
    {
        using var ms = new MemoryStream();
        var w = new ExcelWriter(ms);
        // 全数字/日期/时间，不含字符串
        w.WriteRows(null, new[] { new Object?[] { 1, 2.5m, DateTime.Today, TimeSpan.FromMinutes(30) } });
        w.Save();

        ms.Position = 0;
        using var za = new ZipArchive(ms, ZipArchiveMode.Read, true, Encoding.UTF8);
        Assert.Null(za.GetEntry("xl/sharedStrings.xml")); // 不存在
        Assert.NotNull(za.GetEntry("xl/styles.xml"));
        Assert.NotNull(za.GetEntry("xl/worksheets/sheet1.xml"));
    }

    [Fact, DisplayName("Dispose自动保存文件路径")] 
    public void Dispose_AutoSave_File()
    {
        var path = Path.Combine(Path.GetTempPath(), "excelwriter_test_" + Guid.NewGuid().ToString("N") + ".xlsx");
        try
        {
            using (var w = new ExcelWriter(path))
            {
                w.WriteHeader(null!, new[] { "A" });
                w.WriteRows(null, new[] { new Object?[] { 123 } });
            } // Dispose 触发保存

            using var fs = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
            var r = new ExcelReader(fs, Encoding.UTF8);
            var rows = r.ReadRows().ToList();
            Assert.Equal(2, rows.Count);
            Assert.Equal("123", rows[1][0] + "");
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Fact, DisplayName("空Writer保存生成空Sheet")] 
    public void EmptyWriter_Save()
    {
        using var ms = new MemoryStream();
        var w = new ExcelWriter(ms);
        w.Save();
        ms.Position = 0;
        var r = new ExcelReader(ms, Encoding.UTF8);
        var list = r.ReadRows().ToList();
        Assert.Empty(list); // 无数据行
    }

    [Fact, DisplayName("Int64使用整数样式避免科学计数")] 
    public void Int64_Uses_Integer_Style()
    {
        using var ms = new MemoryStream();
        var w = new ExcelWriter(ms);
        w.WriteHeader(null!, new[] { "LongVal" });
        var longVal = 1234567890123456789L; // 19位，易被科学计数法处理
        w.WriteRows(null, new[] { new Object?[] { longVal } });
        w.Save();

        ms.Position = 0;
        using var za = new ZipArchive(ms, ZipArchiveMode.Read, true, Encoding.UTF8);
        var sheet = za.GetEntry("xl/worksheets/sheet1.xml");
        Assert.NotNull(sheet);
        using var sr = new StreamReader(sheet!.Open(), Encoding.UTF8);
        var xml = sr.ReadToEnd();
        // 第二行第一列 (A2) 应该写入 s="1" (整数样式) 且直接数值字符串
        Assert.Contains("<c r=\"A2\" s=\"1\"><v>1234567890123456789</v></c>", xml);

        // 读取验证回转为 Int64
        ms.Position = 0;
        var r2 = new ExcelReader(ms, Encoding.UTF8);
        var rows = r2.ReadRows().ToList();
        Assert.Equal(longVal, (Int64)rows[1][0]!);
    }

    [Fact, DisplayName("三个不同Sheet表头与数据互不干扰")] 
    public void MultiSheet_ThreeSheets_DifferentHeaders_And_Data()
    {
        using var ms = new MemoryStream();
        var w = new ExcelWriter(ms);

        // Sheet 1: Users
        w.WriteHeader("Users", new[] { "UserId", "UserName", "Active" });
        w.WriteRows("Users", new[]
        {
            new Object?[] { 1, "Tom", true },
            new Object?[] { 2, "Jerry", false }
        });

        // Sheet 2: Orders
        w.WriteHeader("Orders", new[] { "OrderId", "Amount", "Date" });
        var orderDate = new DateTime(2024, 1, 2);
        w.WriteRows("Orders", new[]
        {
            new Object?[] { 1001, 123.45m, orderDate },
            new Object?[] { 1002, 200m, orderDate.AddDays(1) },
            new Object?[] { 1003, 0.5m, orderDate.AddDays(2) }
        });

        // Sheet 3: Logs （包含时间与文本混合，不同列数）
        w.WriteHeader("Logs", new[] { "Seq", "Level", "Message", "Time" });
        var t0 = DateTime.Now.Date.AddHours(8).AddMinutes(15).AddSeconds(30);
        w.WriteRows("Logs", new[]
        {
            new Object?[] { 1, "INFO", "Start", t0 },
            new Object?[] { 2, "WARN", "Latency", t0.AddMinutes(5) },
            new Object?[] { 3, "ERROR", "Failed", t0.AddMinutes(10) },
            new Object?[] { 4, "INFO", "Done", t0.AddMinutes(15) }
        });

        w.Save();

        //File.WriteAllBytes("ew.xlsx", ms.ToArray());

        ms.Position = 0;
        var r = new ExcelReader(ms, Encoding.UTF8);
        // 验证 sheet 名称集合包含三个
        var sheets = r.Sheets?.ToList();
        Assert.NotNull(sheets);
        Assert.Contains("Users", sheets!);
        Assert.Contains("Orders", sheets!);
        Assert.Contains("Logs", sheets!);

        // Users
        var users = r.ReadRows("Users").ToList();
        Assert.Equal(3, users.Count); // header + 2
        Assert.Equal("UserId", users[0][0]);
        Assert.Equal(1, users[1][0]);
        Assert.True(users[2][2] is Boolean && !(Boolean)users[2][2]!); // Active 列第二行 false

        // Orders
        var orders = r.ReadRows("Orders").ToList();
        Assert.Equal(4, orders.Count); // header + 3
        Assert.Equal("Amount", orders[0][1]);
        Assert.True(orders[2][2] is DateTime dt2 && dt2.Date == orderDate.AddDays(1).Date);
        Assert.True(orders[3][1] is Decimal or Double); // 金额小数

        // Logs
        var logs = r.ReadRows("Logs").ToList();
        Assert.Equal(5, logs.Count); // header + 4
        Assert.Equal("Level", logs[0][1]);
        Assert.Equal("ERROR", logs[3][1]); // 第3条日志（数据行 Seq=3）
        Assert.True(logs[4][3] is DateTime); // 时间列

        // 互不串表：确认 Users 的列数 != Logs 的列数
        Assert.NotEqual(users[0].Length, logs[0].Length);
    }
}
