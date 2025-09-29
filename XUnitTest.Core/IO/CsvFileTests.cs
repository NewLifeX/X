using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife;
using NewLife.IO;
using Xunit;

namespace XUnitTest.IO;

public class CsvFileTests
{
    [Fact]
    public void MemoryTest()
    {
        var ms = new MemoryStream();

        var list = new List<Object[]>
        {
            new Object[] { 1234, "Stone", true, DateTime.Now },
            new Object[] { 5678, "NewLife", false, DateTime.Today }
        };

        {
            using var csv = new CsvFile(ms, true);
            csv.Separator = ',';
            csv.Encoding = Encoding.UTF8;

            csv.WriteLine(new Object?[] { "Code", "Name", "Enable", "CreateTime" });
            csv.WriteAll(list);
        }

        var txt = ms.ToArray().ToStr();
#if NET462
        var lines = txt.Split(new[] { Environment.NewLine }, StringSplitOptions.RemoveEmptyEntries);
#else
        var lines = txt.Split(Environment.NewLine, StringSplitOptions.RemoveEmptyEntries);
#endif
        Assert.Equal(3, lines.Length);
        Assert.Equal("Code,Name,Enable,CreateTime", lines[0]);
        Assert.Equal($"1234,Stone,1,{((DateTime)list[0][3]).ToFullString()}", lines[1]);
        Assert.Equal($"5678,NewLife,0,{((DateTime)list[1][3]).ToFullString()}", lines[2]);

        {
            ms.Position = 0;
            using var csv = new CsvFile(ms);
            var headers = csv.ReadLine();
            var all = csv.ReadAll().ToList();

            Assert.Equal(4, headers.Length);
            Assert.Equal("Code", headers[0]);
            Assert.Equal("Name", headers[1]);

            Assert.Equal(2, all.Count);
        }
    }

    [Fact]
    public void FileTest()
    {
        var file = "data/test.csv";

        var list = new List<Object[]>
        {
            new Object[] { 1234, "Stone", true, DateTime.Now },
            new Object[] { 5678, "NewLife", false, DateTime.Today }
        };

        {
            using var csv = new CsvFile(file, true);
            csv.Separator = ',';
            csv.Encoding = UTF8Encoding.UTF8;

            csv.WriteLine(new Object?[] { "Code", "Name", "Enable", "CreateTime" });
            csv.WriteAll(list);
        }

        var lines = File.ReadAllLines(file.GetFullPath());
        Assert.Equal(3, lines.Length);
        Assert.Equal("Code,Name,Enable,CreateTime", lines[0]);
        Assert.Equal($"1234,Stone,1,{((DateTime)list[0][3]).ToFullString()}", lines[1]);
        Assert.Equal($"5678,NewLife,0,{((DateTime)list[1][3]).ToFullString()}", lines[2]);

        {
            using var csv = new CsvFile(file);
            var headers = csv.ReadLine();
            var all = csv.ReadAll().ToList();

            Assert.Equal(4, headers.Length);
            Assert.Equal("Code", headers[0]);
            Assert.Equal("Name", headers[1]);

            Assert.Equal(2, all.Count);
        }
    }

    [Fact]
    public void BigString()
    {
        var ms = new MemoryStream();

        var list = new List<Object[]>
        {
            new Object[] { 1234, "Stone", true, DateTime.Now },
            new Object[] { 5678, "Hello\r\n\r\nNewLife in \"2025\"", false, DateTime.Today }
        };

        {
            using var csv = new CsvFile(ms, true);
            csv.Separator = ',';
            csv.Encoding = Encoding.UTF8;

            csv.WriteLine(new Object?[] { "Code", "Name", "Enable", "CreateTime" });
            csv.WriteAll(list);
        }

        var txt = ms.ToArray().ToStr();
        var lines = txt.Split(new[] { Environment.NewLine }, StringSplitOptions.None);
        // 该行包含多行与引号，按 RFC4180 应整体包裹在引号内且内部引号成对出现
        Assert.Equal("Code,Name,Enable,CreateTime", lines[0]);
        Assert.Contains("\"Hello", txt); // 被引号包裹
        Assert.Contains("\"\"2025\"\"", txt); // 内部双引号转义为两个

        {
            ms.Position = 0;
            using var csv = new CsvFile(ms);
            var headers = csv.ReadLine();
            var all = csv.ReadAll().ToList();

            Assert.Equal(4, headers.Length);
            Assert.Equal("Code", headers[0]);
            Assert.Equal("Name", headers[1]);

            Assert.Equal(2, all.Count);
            Assert.Equal("Hello\r\n\r\nNewLife in \"2025\"", all[1][1]);
        }
    }

    // ===================== 新增覆盖测试 =====================

    private sealed class CsvFileEx : CsvFile
    {
        public CsvFileEx(Stream stream, Boolean leaveOpen = true) : base(stream, leaveOpen) { }
        public String Build(IEnumerable<Object?> line) => base.BuildLine(line);
    }

    [Fact]
    public void BuildLine_AllBranches()
    {
        using var ms = new MemoryStream();
        using var csv = new CsvFileEx(ms, true) { Separator = ',', Encoding = Encoding.UTF8 };

        var dt = new DateTime(2025, 1, 2, 3, 4, 5, DateTimeKind.Local);
        var line = csv.Build(new Object?[] {
            dt,
            true,
            "1234567890",
            "He said \"Hi\"",
            "A,B",
            "Plain"
        });

        Assert.StartsWith(dt.ToFullString(), line);
        Assert.Contains(",1,", line);
        Assert.Contains(",\t1234567890,", line);
        // 双引号字段应整体加双引号，内部双引号成对
        Assert.Contains("\"He said \"\"Hi\"\"\"", line);
        Assert.Contains(",\"A,B\",", line);
        Assert.EndsWith(",Plain", line);
    }

    [Fact]
    public void BuildLine_EscapeQuotes_Rfc4180()
    {
        using var ms = new MemoryStream();
        using var csv = new CsvFileEx(ms, true) { Separator = ',', Encoding = Encoding.UTF8 };
        var field = "Path C:\\Temp\\\"File\".txt"; // 包含反斜杠与引号
        var line = csv.Build(new Object?[] { field });
        // 仅当含引号/分隔符/换行才整体加引号；内部引号重复
        var expected = "\"" + field.Replace("\"", "\"\"") + "\"";
        Assert.Equal(expected, line);
    }

    [Fact]
    public void BuildLine_NewlineNeedsQuote()
    {
        using var ms = new MemoryStream();
        using var csv = new CsvFileEx(ms, true) { Separator = ',', Encoding = Encoding.UTF8 };
        var field = "Line1\nLine2"; // 仅含换行，无引号
        var line = csv.Build(new Object?[] { field });
        Assert.Equal("\"" + field + "\"", line);
    }

    [Fact]
    public void ReadRecord_QuotedCommaAndEscapedQuote()
    {
        var raw = "a,\"b\"\"c,d\" ,e," + Environment.NewLine + Environment.NewLine; // 第二行空行
        using var ms = new MemoryStream(raw.GetBytes());
        using var csv = new CsvFile(ms, true) { Separator = ',' };

        var r1 = csv.ReadLine();
        Assert.NotNull(r1);
        Assert.Equal(4, r1.Length);
        Assert.Equal("a", r1[0]);
        Assert.Equal("b\"c,d ", r1[1]);
        Assert.Equal("e", r1[2]);
        Assert.Equal(String.Empty, r1[3]);

        var r2 = csv.ReadLine();
        Assert.NotNull(r2);
        Assert.Single(r2);
        Assert.Equal(String.Empty, r2[0]);

        var r3 = csv.ReadLine();
        Assert.Null(r3);
    }

    [Fact]
    public void ReadRecord_TrailingComma()
    {
        var raw = "x,y,z,";
        using var ms = new MemoryStream(raw.GetBytes());
        using var csv = new CsvFile(ms, true) { Separator = ',' };

        var r = csv.ReadLine();
        Assert.Equal(4, r!.Length);
        Assert.Equal(String.Empty, r[3]);

        Assert.Null(csv.ReadLine());
    }

    [Fact]
    public void ReadRecord_CR_LF_CRLF()
    {
        var raw = "a,b\ra,2\na,3\r\n";
        using var ms = new MemoryStream(raw.GetBytes());
        using var csv = new CsvFile(ms, true) { Separator = ',' };

        var r1 = csv.ReadLine();
        var r2 = csv.ReadLine();
        var r3 = csv.ReadLine();
        var r4 = csv.ReadLine();

        Assert.NotNull(r1); Assert.NotNull(r2); Assert.NotNull(r3);
        Assert.Null(r4);
        Assert.Equal(new[] { "a", "b" }, r1);
        Assert.Equal(new[] { "a", "2" }, r2);
        Assert.Equal(new[] { "a", "3" }, r3);
    }

    [Fact]
    public void EmptyFile()
    {
        using var ms = new MemoryStream();
        using var csv = new CsvFile(ms, true);
        Assert.Null(csv.ReadLine());
    }

    [Fact]
    public void LeaveOpen_Dispose()
    {
        var ms = new MemoryStream();
        using (var csv = new CsvFile(ms, true))
        {
            csv.WriteLine(new Object?[] { "A" });
        }
        Assert.True(ms.CanWrite);
        Assert.NotEqual(0, ms.Length);

        var ms2 = new MemoryStream();
        using (var csv2 = new CsvFile(ms2))
        {
            csv2.WriteLine(new Object?[] { "B" });
        }
        Assert.Throws<ObjectDisposedException>(() => ms2.WriteByte(1));
    }

#if NET5_0_OR_GREATER || NETSTANDARD2_1_OR_GREATER
    [Fact]
    public async Task WriteLineAsync_And_DisposeAsync()
    {
        var ms = new MemoryStream();
        var csv = new CsvFile(ms, true) { Separator = ',', Encoding = Encoding.UTF8 };

        await csv.WriteLineAsync(new Object[] { "A", "B" });
        await csv.WriteLineAsync(new Object[] { "1", "2" });
        await csv.DisposeAsync();

        ms.Position = 0;
        using var csv2 = new CsvFile(ms, true);
        var h = csv2.ReadLine();
        var d = csv2.ReadLine();
        Assert.Equal(new[] { "A", "B" }, h);
        Assert.Equal(new[] { "1", "2" }, d);
    }
#endif
}
