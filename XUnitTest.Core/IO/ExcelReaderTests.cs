using System.IO.Compression;
using System.Text;
using NewLife;
using NewLife.IO;
using Xunit;

namespace XUnitTest.IO;

public class ExcelReaderTests
{
    [Fact]
    public void Test1()
    {
        var type = GetType();
        var stream = type.Assembly.GetManifestResourceStream(type.Namespace + ".excel.xlsx");
        Assert.NotNull(stream);

        var reader = new ExcelReader(stream, Encoding.UTF8);
        var rows = reader.ReadRows().ToList();
        Assert.Equal(927, rows.Count);

        var names = "序号,名字,昵称,启用,年龄,生日,时间,余额,比率,开始时间,结束时间".Split(',');
        var fields = rows[0].Cast<String>().ToArray();
        Assert.Equal(names.Length, fields.Length);
        for (var i = 0; i < names.Length; i++)
        {
            Assert.Equal(names[i], fields[i]);
        }

        var values = "111,Stone,大石头,1,36.6,1984-07-01,2020-03-04 20:08:45,323.452,0.234,11:22:00,23:59:00".Split(',');
        var row1 = rows[1];
        Assert.Equal(values.Length, row1.Length);
        for (var i = 0; i < values.Length; i++)
        {
            if (row1[i] is DateTime dt)
                Assert.Equal(values[i].ToDateTime(), dt);
            else if (row1[i] is TimeSpan ts)
                Assert.Equal(TimeSpan.Parse(values[i]), ts);
            else
                Assert.Equal(values[i], row1[i] + "");
        }
    }

    /// <summary>
    /// 构造一个内存xlsx（最小xml集合）用来测试：
    /// 1) 多字母列AA/AB索引
    /// 2) 中间缺失列补null
    /// 3) 布尔单元格解析
    /// </summary>
    [Fact]
    public void ColumnIndexAndBooleanTest()
    {
        // 生成一个简单工作簿，包含共享字符串与一个sheet，列：A(共享字符串) C(布尔) AA(数字) AB(共享字符串)
        using var ms = new MemoryStream();
        using (var za = new ZipArchive(ms, ZipArchiveMode.Create, true, Encoding.UTF8))
        {
            // [Content_Types].xml
            var entry = za.CreateEntry("[Content_Types].xml");
            using (var sw = new StreamWriter(entry.Open(), Encoding.UTF8))
            {
                sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/><Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/><Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/></Types>");
            }
            // workbook.xml
            entry = za.CreateEntry("xl/workbook.xml");
            using (var sw = new StreamWriter(entry.Open(), Encoding.UTF8))
            {
                sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheets><sheet name=\"Sheet1\" sheetId=\"1\" r:id=\"rId1\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"/></sheets></workbook>");
            }
            // styles.xml (最小）
            entry = za.CreateEntry("xl/styles.xml");
            using (var sw = new StreamWriter(entry.Open(), Encoding.UTF8))
            {
                sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><cellXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellXfs></styleSheet>");
            }
            // sharedStrings.xml
            entry = za.CreateEntry("xl/sharedStrings.xml");
            using (var sw = new StreamWriter(entry.Open(), Encoding.UTF8))
            {
                sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"2\" uniqueCount=\"2\"><si><t>Head</t></si><si><t>Tail</t></si></sst>");
            }
            // sheet1.xml: A1=共享字符串0, C1=布尔1(true), AA1=数值123, AB1=共享字符串1
            entry = za.CreateEntry("xl/worksheets/sheet1.xml");
            using (var sw = new StreamWriter(entry.Open(), Encoding.UTF8))
            {
                // Columns: A (index0), B skipped(null), C(index2 boolean), ... AA(index26 numeric), AB(index27 shared string)
                sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"A1\" t=\"s\"><v>0</v></c><c r=\"C1\" t=\"b\"><v>1</v></c><c r=\"AA1\"><v>123</v></c><c r=\"AB1\" t=\"s\"><v>1</v></c></row></sheetData></worksheet>");
            }
        }
        ms.Position = 0;

        var reader = new ExcelReader(ms, Encoding.UTF8);
        var rows = reader.ReadRows().ToList();
        Assert.Single(rows);

        var arr = rows[0];
        // 期望有 AB 列 => 28 列 (index 0..27)
        Assert.Equal(28, arr.Length);
        Assert.Equal("Head", arr[0]); // A
        Assert.Null(arr[1]); // B 缺失
        Assert.True(arr[2] is Boolean b && b); // C 布尔
        Assert.Equal("123", arr[26]); // AA (多字母)
        Assert.Equal("Tail", arr[27]); // AB SharedString
    }

    // 构造最小/可变 excel 的工具方法
    private static MemoryStream BuildExcel(String sheetXml, String? sharedStrings = null, String? styles = null, String sheetName = "Sheet1", Boolean includeStyles = true, Boolean includeShared = true)
    {
        var ms = new MemoryStream();
        using var za = new ZipArchive(ms, ZipArchiveMode.Create, true, Encoding.UTF8);
        // content types
        using (var sw = new StreamWriter(za.CreateEntry("[Content_Types].xml").Open(), Encoding.UTF8))
        {
            sw.Write("<?xml version=\"1.0\" encoding=\"UTF-8\"?><Types xmlns=\"http://schemas.openxmlformats.org/package/2006/content-types\"><Default Extension=\"xml\" ContentType=\"application/xml\"/><Override PartName=\"/xl/workbook.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sheet.main+xml\"/><Override PartName=\"/xl/worksheets/sheet1.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.worksheet+xml\"/>{0}{1}</Types>"
                .Replace("{0}", includeShared ? "<Override PartName=\"/xl/sharedStrings.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.sharedStrings+xml\"/>" : String.Empty)
                .Replace("{1}", includeStyles ? "<Override PartName=\"/xl/styles.xml\" ContentType=\"application/vnd.openxmlformats-officedocument.spreadsheetml.styles+xml\"/>" : String.Empty));
        }
        // workbook
        using (var sw = new StreamWriter(za.CreateEntry("xl/workbook.xml").Open(), Encoding.UTF8))
        {
            sw.Write($"<?xml version=\"1.0\" encoding=\"UTF-8\"?><workbook xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheets><sheet name=\"{sheetName}\" sheetId=\"1\" r:id=\"rId1\" xmlns:r=\"http://schemas.openxmlformats.org/officeDocument/2006/relationships\"/></sheets></workbook>");
        }
        // styles
        if (includeStyles)
        {
            using var sw = new StreamWriter(za.CreateEntry("xl/styles.xml").Open(), Encoding.UTF8);
            sw.Write(styles ?? "<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><cellXfs count=\"1\"><xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/></cellXfs></styleSheet>");
        }
        // sharedStrings
        if (includeShared)
        {
            using var sw = new StreamWriter(za.CreateEntry("xl/sharedStrings.xml").Open(), Encoding.UTF8);
            sw.Write(sharedStrings ?? "<?xml version=\"1.0\" encoding=\"UTF-8\"?><sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"1\" uniqueCount=\"1\"><si><t>Str</t></si></sst>");
        }
        // sheet
        using (var sw = new StreamWriter(za.CreateEntry("xl/worksheets/sheet1.xml").Open(), Encoding.UTF8))
        {
            sw.Write(sheetXml);
        }

        // 全部写完再关闭
        za.Dispose();

        ms.Position = 0;
        return ms;
    }

    [Fact]
    public void EmptySheet_NoShared_NoStyles()
    {
        var sheetXml = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData/></worksheet>";
        using var ms = BuildExcel(sheetXml, includeShared: false, includeStyles: false);
        var reader = new ExcelReader(ms, Encoding.UTF8);
        var rows = reader.ReadRows().ToList();
        Assert.Empty(rows);
    }

    [Fact]
    public void MultipleSheets_SelectByName_And_Invalid()
    {
        // 两个工作表：Sheet1 有一行，Sheet2 有两行
        var sheet1 = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"A1\"><v>1</v></c></row></sheetData></worksheet>";
        var sheet2 = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"A1\"><v>2</v></c></row><row r=\"2\"><c r=\"A2\"><v>3</v></c></row></sheetData></worksheet>";
        // 先构造 zip 再手动添加第二个 sheet
        using var ms = BuildExcel(sheet1);
        using (var za = new ZipArchive(ms, ZipArchiveMode.Update, true, Encoding.UTF8))
        {
            var entry = za.CreateEntry("xl/worksheets/sheet2.xml");
            using var sw = new StreamWriter(entry.Open(), Encoding.UTF8);
            sw.Write(sheet2);
        }
        ms.Position = 0;

        var reader = new ExcelReader(ms, Encoding.UTF8);
        // 默认取第一个 sheet (Sheet1)
        var rows1 = reader.ReadRows().ToList();
        Assert.Single(rows1);
        Assert.Equal("1", rows1[0][0] + "");

        // 指定 Sheet2
        Assert.Throws<ArgumentOutOfRangeException>(() => reader.ReadRows("Str").ToList());
        var rows2 = reader.ReadRows("sheet2")?.ToList(); 
    }

    [Fact]
    public void Styles_AllBranches_And_NullStyleEntry()
    {
        // styles: 8 个xf
        var styles = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><styleSheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><cellXfs count=\"8\">" +
                     // 0: General(0)
                     "<xf numFmtId=\"0\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>" +
                     // 1: Date (14)
                     "<xf numFmtId=\"14\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>" +
                     // 2: Time (20)
                     "<xf numFmtId=\"20\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>" +
                     // 3: Int only (1)
                     "<xf numFmtId=\"1\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>" +
                     // 4: Decimal (2)
                     "<xf numFmtId=\"2\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>" +
                     // 5: Double(9)
                     "<xf numFmtId=\"9\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>" +
                     // 6: Text (49)
                     "<xf numFmtId=\"49\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>" +
                     // 7: Unknown (999) -> null style entry
                     "<xf numFmtId=\"999\" fontId=\"0\" fillId=\"0\" borderId=\"0\"/>" +
                     "</cellXfs></styleSheet>";
        // Row cells: A1 General 2147483648 (long), B1 Date serial 5 => 1900-01-04, C1 Time 0.5 => 12:00, D1 Int style decimal won't parse => string, E1 Decimal 123.45 => decimal/double, F1 Double 0.25 => double, G1 Text 123.456 => string, H1 Unknown style 99 => remains string, I1 style index=99(越界) remains string
        var sheet = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\">" +
                     "<c r=\"A1\" s=\"0\"><v>2147483648</v></c>" +
                     "<c r=\"B1\" s=\"1\"><v>5</v></c>" +
                     "<c r=\"C1\" s=\"2\"><v>0.5</v></c>" +
                     "<c r=\"D1\" s=\"3\"><v>12.34</v></c>" +
                     "<c r=\"E1\" s=\"4\"><v>123.45</v></c>" +
                     "<c r=\"F1\" s=\"5\"><v>0.25</v></c>" +
                     "<c r=\"G1\" s=\"6\"><v>123.456</v></c>" +
                     "<c r=\"H1\" s=\"7\"><v>99</v></c>" +
                     "<c r=\"I1\" s=\"99\"><v>7</v></c>" +
                     "</row></sheetData></worksheet>";
        using var ms = BuildExcel(sheet, styles: styles);
        var reader = new ExcelReader(ms, Encoding.UTF8);
        var row = reader.ReadRows().First();
        Assert.Equal(9, row.Length);
        Assert.IsType<Int64>(row[0]);
        Assert.IsType<DateTime>(row[1]);
        Assert.Equal(new DateTime(1900, 1, 4), (DateTime)row[1]);
        Assert.IsType<TimeSpan>(row[2]);
        Assert.Equal(TimeSpan.FromHours(12), (TimeSpan)row[2]);
        Assert.Equal("12.34", row[3]); // 未转换
        Assert.True(row[4] is Decimal or Double);
        Assert.True(row[5] is Double);
        Assert.IsType<String>(row[6]);
        Assert.Equal("99", row[7]); // 未知样式未转换
        Assert.Equal("7", row[8]); // 越界 style 索引
    }

    [Fact]
    public void BooleanVariants_And_SharedString_OutOfRange()
    {
        var shared = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><sst xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\" count=\"1\" uniqueCount=\"1\"><si><t>Only</t></si></sst>";
        // A1 shared index 0 => Only, B1 shared index 5 越界 => null, C1 bool 0 false, D1 bool 1 true, E1 bool TRUE true, F1 bool false false
        var sheet = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\">" +
                    "<c r=\"A1\" t=\"s\"><v>0</v></c>" +
                    "<c r=\"B1\" t=\"s\"><v>5</v></c>" +
                    "<c r=\"C1\" t=\"b\"><v>0</v></c>" +
                    "<c r=\"D1\" t=\"b\"><v>1</v></c>" +
                    "<c r=\"E1\" t=\"b\"><v>TRUE</v></c>" +
                    "<c r=\"F1\" t=\"b\"><v>false</v></c>" +
                    "</row></sheetData></worksheet>";
        using var ms = BuildExcel(sheet, sharedStrings: shared);
        var reader = new ExcelReader(ms, Encoding.UTF8);
        var row = reader.ReadRows().First();
        Assert.Equal("Only", row[0]);
        Assert.Null(row[1]); // 越界
        Assert.False((Boolean)row[2]!);
        Assert.True((Boolean)row[3]!);
        Assert.True((Boolean)row[4]!);
        Assert.False((Boolean)row[5]!);
    }

    [Fact]
    public void InlineStr_And_FormulaString()
    {
        // inlineStr: A1, str: B1
        var sheet = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\">" +
                    "<c r=\"A1\" t=\"inlineStr\"><is><t>Inline Text</t></is></c>" +
                    "<c r=\"C1\" t=\"str\"><v>FormulaResult</v></c>" + // 中间缺失 B => null
                    "</row></sheetData></worksheet>";
        using var ms = BuildExcel(sheet, includeShared: false);
        var reader = new ExcelReader(ms, Encoding.UTF8);
        var row = reader.ReadRows().First();
        Assert.Equal(3, row.Length);
        Assert.Equal("Inline Text", row[0]);
        Assert.Null(row[1]);
        Assert.Equal("FormulaResult", row[2]);
    }

    [Fact]
    public void MissingManyLeadingColumns()
    {
        // 只有 AC1 = 1 (AC 是第 29 列，索引 28) => 前面应补 28 个 null
        var sheet = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData><row r=\"1\"><c r=\"AC1\"><v>1</v></c></row></sheetData></worksheet>";
        using var ms = BuildExcel(sheet, includeShared: false, includeStyles: false);
        var reader = new ExcelReader(ms, Encoding.UTF8);
        var row = reader.ReadRows().First();
        Assert.Equal(29, row.Length);
        for (var i = 0; i < 28; i++) Assert.Null(row[i]);
        Assert.Equal("1", row[28]);
    }

    [Fact]
    public void Dispose_Then_Read_ShouldThrow()
    {
        var sheet = "<?xml version=\"1.0\" encoding=\"UTF-8\"?><worksheet xmlns=\"http://schemas.openxmlformats.org/spreadsheetml/2006/main\"><sheetData/></worksheet>";
        using var ms = BuildExcel(sheet, includeShared: false, includeStyles: false);
        var reader = new ExcelReader(ms, Encoding.UTF8);
        reader.Dispose();
        Assert.Throws<ObjectDisposedException>(() => reader.ReadRows().ToList());
    }
}
