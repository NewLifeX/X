using System;
using System.Collections.Generic;
using System.Text;
using Xunit;
using NewLife;
using System.Linq;

namespace XUnitTest.Extension;

public class StringHelperTests
{
    [Fact]
    public void IsMatch()
    {
        var rs = "".IsMatch("Stone");
        Assert.False(rs);

        rs = "*.zip".IsMatch(null);
        Assert.False(rs);

        // 常量
        rs = ".zip".IsMatch(".zip");
        Assert.True(rs);
        rs = ".zip".IsMatch("7788.Zip");
        Assert.False(rs);
        rs = ".zip".IsMatch(".Zip", StringComparison.OrdinalIgnoreCase);
        Assert.True(rs);
        rs = "/".IsMatch("/");
        Assert.True(rs);
        rs = "/".IsMatch("/api");
        Assert.False(rs);
        rs = "/".IsMatch("/api/");
        Assert.False(rs);

        // 头部
        rs = "*.zip".IsMatch("7788.zip");
        Assert.True(rs);
        rs = "*.zip".IsMatch("7788.zipxx");
        Assert.False(rs);

        // 大小写
        rs = "*.zip".IsMatch("4455.Zip");
        Assert.False(rs);
        rs = "*.zip".IsMatch("4455.Zip", StringComparison.OrdinalIgnoreCase);
        Assert.True(rs);

        // 中间
        rs = "build*.zip".IsMatch("build7788.zip");
        Assert.True(rs);
        rs = "build*.zip".IsMatch("mybuild7788.zip");
        Assert.False(rs);
        rs = "build*.zip".IsMatch("build7788.zipxxx");
        Assert.False(rs);

        // 尾部
        rs = "build.*".IsMatch("build.zip");
        Assert.True(rs);
        rs = "build.*".IsMatch("mybuild.zip");
        Assert.False(rs);
        rs = "build.*".IsMatch("build.zipxxx");
        Assert.True(rs);

        // 多个
        rs = "build*.*".IsMatch("build7788.zip");
        Assert.True(rs);
        rs = "*build*.*".IsMatch("mybuild7788.zip");
        Assert.True(rs);
        rs = "build**.*".IsMatch("build7788.zip");
        Assert.True(rs);

        // 其它
        rs = "aa*aa".IsMatch("aaa");
        Assert.False(rs);
        rs = "aa*aa".IsMatch("aaaa");
        Assert.True(rs);
        rs = "aa*aa".IsMatch("aaaaa");
        Assert.True(rs);
    }

    [Fact]
    public void IsMatch_WithQuestionMark()
    {
        // 单个?
        Assert.True("?".IsMatch("a"));
        Assert.False("?".IsMatch(""));

        // 固定长度匹配
        Assert.True("a?c".IsMatch("abc"));
        Assert.True("a?c".IsMatch("aXc"));
        Assert.False("a?c".IsMatch("ac"));
        Assert.True("??".IsMatch("ab"));
        Assert.False("??".IsMatch("a"));

        // 文件名
        Assert.True("file?.txt".IsMatch("file1.txt"));
        Assert.False("file?.txt".IsMatch("file12.txt"));

        // 与*组合
        Assert.True("a?c*".IsMatch("aXc"));
        Assert.True("a?c*".IsMatch("aXcd"));
        Assert.True("*?.zip".IsMatch("abc.zip"));
        Assert.True("*?.zip".IsMatch("a.zip")); // * 可匹配空串，? 匹配一个字符

        // 大小写
        Assert.True("a?c".IsMatch("aXc", StringComparison.OrdinalIgnoreCase));
        Assert.True("A?C".IsMatch("aXc", StringComparison.OrdinalIgnoreCase));
        Assert.False("A?C".IsMatch("aXc", StringComparison.Ordinal));

        // 边界
        Assert.False("??".IsMatch(""));
        Assert.False("?".IsMatch(null));
    }

    [Fact]
    public void SplitAsDictionary()
    {
        var str = "IP=172.17.0.6,172.17.0.7,172.17.16.7";
        var dic = str.SplitAsDictionary("=", ";");

        Assert.Single(dic);
        foreach (var item in dic)
        {
            Assert.Equal("IP", item.Key);
        }

        Assert.True(dic.ContainsKey("IP"));
        Assert.True(dic.ContainsKey("Ip"));
        Assert.True(dic.ContainsKey("ip"));
        Assert.True(dic.ContainsKey("iP"));

        var rules = dic.ToDictionary(e => e.Key, e => e.Value.Split(","));

        Assert.True(rules.ContainsKey("IP"));
        Assert.False(rules.ContainsKey("Ip"));
        Assert.False(rules.ContainsKey("ip"));
        Assert.False(rules.ContainsKey("iP"));
    }

    [Fact]
    public void SplitAsDictionary2()
    {
        var str = "TAGS\u0001Tag1\u0002KEYS\u0001Key1\u0002DELAY\u00012\u0002WAIT\u0001False\u0002";
        var dic = str.SplitAsDictionary("\u0001", "\u0002");

        Assert.Equal(4, dic.Count);
        Assert.Equal("Tag1", dic["TAGS"]);
        Assert.Equal("Key1", dic["Keys"]);
        Assert.Equal("2", dic["DELAY"]);
        Assert.Equal("False", dic["WAIT"]);
    }
}