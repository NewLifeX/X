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

    [Fact]
    public void DefaultArgs()
    {
        var cmp = new CommandParser { };
        var dic = cmp.Parse(null);

        Assert.NotNull(dic);
        Assert.Equal(6, dic.Count);
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
}
