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
}
