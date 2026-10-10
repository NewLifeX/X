using NewLife.Configuration;
using Xunit;

namespace XUnitTest.Configuration;

public class CommandParserTests
{
    [Fact]
    public void Normal()
    {
        var args = new[] { "-appid", "cube", "--secret", "abcd1234", "-allowall" };
        var cmp = new CommandParser { };
        var dic = cmp.Parse(args);

        Assert.NotNull(dic);
        Assert.Equal(3, dic.Count);
        Assert.True(dic.ContainsKey("appid"));
        Assert.True(dic.ContainsKey("secret"));
        Assert.True(dic.ContainsKey("allowall"));

        Assert.Equal("cube", dic["appid"]);
        Assert.Equal("abcd1234", dic["secret"]);
        Assert.Null(dic["allowall"]);
    }

    [Fact]
    public void IgnoreCase()
    {
        var args = new[] { "-appid", "cube", "--secret", "abcd1234", "-allowall" };
        var cmp = new CommandParser { IgnoreCase = true };
        var dic = cmp.Parse(args);

        Assert.NotNull(dic);
        Assert.Equal(3, dic.Count);
        Assert.True(dic.ContainsKey("appid"));
        Assert.True(dic.ContainsKey("secret"));
        Assert.True(dic.ContainsKey("AllowAll"));

        Assert.Equal("cube", dic["AppId"]);
        Assert.Equal("abcd1234", dic["Secret"]);
        Assert.Null(dic["allowall"]);
    }

    [Fact]
    public void TrimStart()
    {
        var args = new[] { "-appid", "cube", "--secret", "abcd1234", "-allowall" };
        var cmp = new CommandParser { IgnoreCase = true, TrimStart = false };
        var dic = cmp.Parse(args);

        Assert.NotNull(dic);
        Assert.Equal(3, dic.Count);
        Assert.True(dic.ContainsKey("-appid"));
        Assert.True(dic.ContainsKey("--secret"));
        Assert.True(dic.ContainsKey("-AllowAll"));

        Assert.Equal("cube", dic["-AppId"]);
        Assert.Equal("abcd1234", dic["--Secret"]);
        Assert.Null(dic["-allowall"]);
    }

    [Fact]
    public void TrimQuote()
    {
        var args = new[] { "-appid", "'cube'", "--secret", "\"abcd1234\"", "-allowall" };
        var cmp = new CommandParser { IgnoreCase = true, TrimStart = false };
        var dic = cmp.Parse(args);

        Assert.NotNull(dic);
        Assert.Equal(3, dic.Count);
        Assert.True(dic.ContainsKey("-appid"));
        Assert.True(dic.ContainsKey("--secret"));
        Assert.True(dic.ContainsKey("-AllowAll"));

        Assert.Equal("cube", dic["-AppId"]);
        Assert.Equal("abcd1234", dic["--Secret"]);
        Assert.Null(dic["-allowall"]);
    }

    [Fact(DisplayName = "空参数回退到当前进程命令行")]
    public void DefaultArgs()
    {
        var cmp = new CommandParser { };

        // Parse(null) 回退到 Environment.GetCommandLineArgs()
        var dic = cmp.Parse(null);
        Assert.NotNull(dic);

        // 回退结果必须与显式传入当前进程命令行完全一致。
        // 不能断言参数条数：测试宿主由 VSTest 注入 --port/--endpoint/--role/--parentprocessid/--telemetryoptedin 等选项，
        // 其集合随 VSTest 版本变化，写死数量会让本用例随运行器升级而失败
        var expected = cmp.Parse(Environment.GetCommandLineArgs());
        Assert.Equal(expected.Count, dic.Count);
        foreach (var kv in expected)
        {
            Assert.True(dic.ContainsKey(kv.Key));
            Assert.Equal(kv.Value, dic[kv.Key]);
        }

        // 用宿主必然注入的选项确认确实读到了当前进程命令行，而非空集合
        Assert.True(dic.ContainsKey("port"));
        Assert.True(dic.ContainsKey("endpoint"));
        Assert.True(dic.ContainsKey("role"));
    }

    [Fact]
    public void Split()
    {
        var str = "-appid=cube --secret=abcd1234";
        var args = CommandParser.Split(str);
        Assert.Equal(2, args.Length);
        Assert.Equal("-appid=cube", args[0]);
        Assert.Equal("--secret=abcd1234", args[1]);

        str = " -appid=cube  \"C:\\Program Files (x86)\\Internet Explorer\\iexplore.exe\" ";
        args = CommandParser.Split(str);
        Assert.Equal(2, args.Length);
        Assert.Equal("-appid=cube", args[0]);
        Assert.Equal("C:\\Program Files (x86)\\Internet Explorer\\iexplore.exe", args[1]);

        str = " -appid=cube  \"\" ";
        args = CommandParser.Split(str);
        Assert.Equal(2, args.Length);
        Assert.Equal("-appid=cube", args[0]);
        Assert.Equal("", args[1]);

        //str = " -appid=cube  F\"C:\\Program Files (x86)\\Internet Explorer\\iexplore.exe\"ddd ";
        //args = CommandParser.Split(str);
        //Assert.Equal(2, args.Length);
        //Assert.Equal("-appid=cube", args[0]);
        //Assert.Equal("F\"C:\\Program Files (x86)\\Internet Explorer\\iexplore.exe\"ddd", args[1]);
    }

    [Fact(DisplayName = "空参数不崩溃")]
    public void EmptyArgs()
    {
        var cmp = new CommandParser();
        var dic = cmp.Parse(["", "-appid", "", "-port", "1234"]);

        Assert.NotNull(dic);
        // 空参数被跳过，空值参数正常保留
        Assert.Equal(2, dic.Count);
        Assert.Equal("1234", dic["port"]);
        Assert.Null(dic["appid"]);
    }

    [Fact(DisplayName = "单字符引号不越界")]
    public void TrimQuote_SingleChar()
    {
        Assert.Equal("\"", CommandParser.TrimQuote("\""));
        Assert.Equal("'", CommandParser.TrimQuote("'"));
    }
}
