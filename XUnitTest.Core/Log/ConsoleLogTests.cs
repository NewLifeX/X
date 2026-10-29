using System.Diagnostics;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Log;

public class ConsoleLogTests
{
    [Fact]
    public void Create_Default()
    {
        var log = new ConsoleLog();

        Assert.NotNull(log);
        Assert.True(log.UseColor);
        Assert.True(log.Enable);
        Assert.True(log.UseAnsiColor);
        Assert.Null(log.Colors);
        Assert.Null(log.AnsiColors);
    }

    [Fact]
    public void UseColor_Property()
    {
        var log = new ConsoleLog { UseColor = false };
        Assert.False(log.UseColor);

        log.UseColor = true;
        Assert.True(log.UseColor);
    }

    [Fact]
    public void Enable_Property()
    {
        var log = new ConsoleLog { Enable = false };
        Assert.False(log.Enable);
    }

    [Fact]
    public void WriteLog_NoException()
    {
        var log = new ConsoleLog();
        var ex = Record.Exception(() => log.Info("测试控制台日志"));

        Assert.Null(ex);
    }

    [Fact]
    public void WriteLog_WithFormat()
    {
        var log = new ConsoleLog();
        var ex = Record.Exception(() => log.Info("格式化 {0} {1}", "arg1", 42));

        Assert.Null(ex);
    }

    [Fact]
    public void WriteLog_AllLevels()
    {
        var log = new ConsoleLog();

        var ex = Record.Exception(() =>
        {
            log.Debug("debug");
            log.Info("info");
            log.Warn("warn");
            log.Error("error");
            log.Fatal("fatal");
        });

        Assert.Null(ex);
    }

    [Fact(DisplayName = "颜色先到先得且前N个线程不重复")]
    public void ColorIndex_FirstComeUnique()
    {
        var log = new ConsoleLog();
        var length = ConsoleColorPalette.Basic.Length;
        var used = new HashSet<Int32>();

        // 刻意使用有空洞的线程编号，取模方案会在这里撞色
        for (var i = 0; i < length; i++)
        {
            used.Add(log.GetColorIndex(100 + i * 37) % length);
        }

        Assert.Equal(length, used.Count);
    }

    [Fact(DisplayName = "同一线程颜色稳定")]
    public void ColorIndex_Stable()
    {
        var log = new ConsoleLog();
        var first = log.GetColorIndex(77);

        Assert.Equal(first, log.GetColorIndex(77));
        Assert.Equal(first, log.GetColorIndex(77));
    }

    [Fact(DisplayName = "颜色映射表有上限")]
    public void ColorIndex_Bounded()
    {
        var log = new ConsoleLog();
        for (var i = 2; i < 1100; i++)
        {
            log.GetColorIndex(i);
        }

        // 超过上限后不再登记，直接使用线程编号，保证内存有界
        Assert.Equal(50000, log.GetColorIndex(50000));
    }

    [Fact(DisplayName = "默认调色板不含不可读色与级别冲突色")]
    public void Palette_Basic_Valid()
    {
        var colors = ConsoleColorPalette.Basic;

        Assert.True(colors.Length >= 6);
        Assert.Equal(colors.Length, colors.Distinct().Count());

        // 黑、蓝、洋红在深色背景下不可读；红、黄与警告/错误级别撞色
        var forbidden = new ConsoleColor[]
        {
            ConsoleColor.Black, ConsoleColor.Blue, ConsoleColor.DarkBlue,
            ConsoleColor.Magenta, ConsoleColor.DarkMagenta,
            ConsoleColor.Red, ConsoleColor.DarkRed, ConsoleColor.Yellow, ConsoleColor.DarkGray,
        };
        Assert.DoesNotContain(colors, c => forbidden.Contains(c));
    }

    [Fact(DisplayName = "ANSI调色板合法且互异")]
    public void Palette_Ansi_Valid()
    {
        var colors = ConsoleColorPalette.Ansi;

        Assert.True(colors.Length >= 12);
        Assert.Equal(colors.Length, colors.Distinct().Count());
        Assert.DoesNotContain(colors, c => c < 16);
    }

    [Fact(DisplayName = "调色板对比度达标")]
    public void Palette_Contrast()
    {
        var background = Luminance(18, 18, 18);
        foreach (var color in ConsoleColorPalette.Ansi)
        {
            Assert.True(Contrast(color, background) >= 4.5, $"调色板 {color} 对比度不足");
        }
    }

    [Fact(DisplayName = "ANSI着色序列格式正确")]
    public void Palette_Wrap()
    {
        Assert.Equal("\u001b[38;5;39mline\u001b[0m", ConsoleColorPalette.Wrap(39, "line"));
        Assert.Equal("\u001b[38;5;250m\u001b[0m", ConsoleColorPalette.Wrap(250, ""));
    }

    [Fact(DisplayName = "整行按线程着色")]
    public void Write_Ansi_WholeLine()
    {
        var writer = new SafeWriter();
        var log = new TestLog(writer) { UseAnsi = true };

        log.Info("hello world");

        var text = writer.WaitFor("hello world");
        // 颜色包裹整行，行首即为颜色控制符
        Assert.StartsWith("\u001b[38;5;", text);
        Assert.Contains("hello world", text);
        // 正文之后才重置，整行同色
        var message = text.IndexOf("hello world", StringComparison.Ordinal);
        Assert.True(text.LastIndexOf("\u001b[0m", StringComparison.Ordinal) > message);
    }

    [Fact(DisplayName = "警告整行使用级别色")]
    public void Write_Ansi_WarnWholeLine()
    {
        var writer = new SafeWriter();
        var log = new TestLog(writer) { UseAnsi = true };

        log.Warn("careful");

        var text = writer.WaitFor("careful");
        Assert.StartsWith("\u001b[38;5;214m", text);
        Assert.Contains("careful", text);
    }

    [Fact(DisplayName = "并发写入不丢日志")]
    public void Write_Concurrent_NoLoss()
    {
        var writer = new SafeWriter();
        var log = new TestLog(writer) { UseAnsi = true };
        const Int32 threads = 4;
        const Int32 count = 25;

        var list = new List<Thread>();
        for (var i = 0; i < threads; i++)
        {
            var idx = i;
            var t = new Thread(() =>
            {
                for (var j = 0; j < count; j++) log.Info($"t{idx}-{j}");
            });
            list.Add(t);
        }
        list.ForEach(t => t.Start());
        list.ForEach(t => t.Join());

        var text = writer.WaitFor($"t{threads - 1}-{count - 1}");
        for (var i = 0; i < threads; i++)
        {
            for (var j = 0; j < count; j++)
            {
                Assert.Contains($"t{i}-{j}", text);
            }
        }
    }

    [Fact(DisplayName = "无颜色能力时输出纯文本")]
    public void Write_Plain()
    {
        var writer = new SafeWriter();
        var log = new TestLog(writer) { UseAnsi = false, UseColor = false };

        log.Info("plain text");

        var text = writer.WaitFor("plain text");
        Assert.DoesNotContain('\u001b', text);
        Assert.Contains("plain text", text);
    }

    private static Double Luminance(Byte r, Byte g, Byte b)
    {
        static Double F(Byte v)
        {
            var s = v / 255.0;
            return s <= 0.04045 ? s / 12.92 : Math.Pow((s + 0.055) / 1.055, 2.4);
        }

        return 0.2126 * F(r) + 0.7152 * F(g) + 0.0722 * F(b);
    }

    private static Double Contrast(Byte index, Double background)
    {
        var (r, g, b) = Xterm(index);
        var lum = Luminance(r, g, b);
        var high = Math.Max(lum, background);
        var low = Math.Min(lum, background);

        return (high + 0.05) / (low + 0.05);
    }

    private static (Byte R, Byte G, Byte B) Xterm(Byte index)
    {
        if (index < 232)
        {
            Byte[] levels = [0, 95, 135, 175, 215, 255];
            var i = index - 16;
            return (levels[i / 36], levels[i % 36 / 6], levels[i % 6]);
        }

        var v = (Byte)(8 + (index - 232) * 10);
        return (v, v, v);
    }

    private sealed class TestLog : ConsoleLog
    {
        public TestLog(TextWriter writer) => Output = writer;

        public Boolean UseAnsi { get; set; }

        internal override Boolean SupportColor => true;

        internal override Boolean SupportAnsiColor => UseAnsi;
    }

    private sealed class SafeWriter : StringWriter
    {
        public override void Write(String? value) { lock (this) base.Write(value); }

        public override void Write(Char value) { lock (this) base.Write(value); }

        public override void WriteLine(String? value) { lock (this) base.WriteLine(value); }

        public override void WriteLine() { lock (this) base.WriteLine(); }

        public override String ToString() { lock (this) return base.ToString(); }

        public String WaitFor(String value, Int32 timeout = 5000)
        {
            var sw = Stopwatch.StartNew();
            while (sw.ElapsedMilliseconds < timeout)
            {
                var text = ToString();
                if (text.Contains(value)) return text;
                Thread.Sleep(10);
            }

            return ToString();
        }
    }
}
