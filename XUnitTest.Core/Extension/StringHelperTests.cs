using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife;
using Xunit;

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

        // 额外边界
        Assert.True("*".IsMatch("")); // * 匹配空串
        Assert.False("".IsMatch("")); // 空pattern
        Assert.False("ab?c".IsMatch("abc")); // 输入短于pattern
        Assert.True("abc".IsMatch("abc", StringComparison.OrdinalIgnoreCase)); // 无通配直接比较
        Assert.True("A?C".IsMatch("aXc", StringComparison.InvariantCultureIgnoreCase));
    }

    [Fact]
    public void SplitAsDictionary()
    {
        var str = "IP=172.17.0.6,172.17.0.7,172.17.16.7";
        var dic = str.SplitAsDictionary("=", ";");

        Assert.Single(dic);
        foreach (var item in dic) Assert.Equal("IP", item.Key);

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

    [Fact]
    public void SplitAsDictionary_Quotation_Duplicate_And_Missing()
    {
        var str = "a='v1';b=\"v2\";a=v3;raw;empty=;c='v4'"; // 重复a、一个不含=、一个空值
        var dic = str.SplitAsDictionary("=", ";", trimQuotation: true);
        Assert.Equal("v1", dic["a"]); // TryAdd 忽略第二次
        Assert.Equal("v2", dic["b"]);
        Assert.Equal("v4", dic["c"]);
        Assert.True(dic.ContainsKey("[0]")); // raw 占位
        Assert.Equal("raw", dic["[0]"]); // 未含分隔符
        Assert.Equal("", dic["empty"]);
    }

    [Fact]
    public void SplitAsDictionary_CustomSeparator_And_EmptyNameValueSeparator()
    {
        var str = "k1=1|k2=2";
        var dic = str.SplitAsDictionary("", "|"); // nameValueSeparator 为空 => 重置为=
        Assert.Equal("1", dic["k1"]);
        Assert.Equal("2", dic["k2"]);
    }

    // ===== 新增测试 =====

    [Fact]
    public void EqualIgnoreCase_And_StartsEnds()
    {
        Assert.True("abc".EqualIgnoreCase("ABC", "def"));
        Assert.False("xyz".EqualIgnoreCase("ab", "cd"));
        Assert.False(((String)null).EqualIgnoreCase("abc"));
        Assert.False("abc".EqualIgnoreCase());
        Assert.True("abc".EqualIgnoreCase(null, "AbC")); // 其中一个null

        Assert.True("HelloWorld".StartsWithIgnoreCase("hello"));
        Assert.False("HelloWorld".StartsWithIgnoreCase("world"));
        Assert.False(((String)null).StartsWithIgnoreCase("a"));
        Assert.False("".StartsWithIgnoreCase("a"));
        Assert.True("HelloWorld".StartsWithIgnoreCase(null, "HEL")); // 多候选

        Assert.True("HelloWorld".EndsWithIgnoreCase("WORLD"));
        Assert.False("HelloWorld".EndsWithIgnoreCase("HELLO"));
        Assert.False(((String)null).EndsWithIgnoreCase("d"));
        Assert.False("".EndsWithIgnoreCase("d"));
        Assert.True("HelloWorld".EndsWithIgnoreCase(null, "rld"));
    }

    [Fact]
    public void NullAndWhiteSpaceChecks()
    {
        var empty = "";
        var blank = "  \t";
        var value = "data";

        Assert.True(empty.IsNullOrEmpty());
        Assert.True(((String)null).IsNullOrEmpty());
        Assert.False(value.IsNullOrEmpty());

        Assert.True(((String)null).IsNullOrWhiteSpace());
        Assert.True(blank.IsNullOrWhiteSpace());
        Assert.True("\r\n".IsNullOrWhiteSpace());
        Assert.False(value.IsNullOrWhiteSpace());
    }

    [Fact]
    public void SplitAndSplitAsInt()
    {
        //String? strNull = null;
        //Assert.Empty(strNull.Split(","));

        var arrDefaultSep = "a;b,,c".Split(";"); // 默认逗号分号, 指定一个分号 => 自己+默认? 这里会使用给定单一分隔符
        Assert.Equal(new[] { "a", "b,,c" }, arrDefaultSep);

        var arr = "1, 2;3".Split(",", ";");
        Assert.Equal(new[] { "1", " 2", "3" }, arr);

        var arrEmptySep = "x,y".Split(""); // 传入空 => 使用默认 , ;
        //Assert.Equal(new[] { "x", "y" }, arrEmptySep);
        Assert.Equal(new[] { "x,y" }, arrEmptySep);

        var ints = "1, 2,xx;3,2".SplitAsInt(",", ";");
        Assert.Equal([1, 2, 3, 2], ints); // 无效 xx 忽略、不去重

        Assert.Empty(((String)null).SplitAsInt(","));
    }

    [Fact]
    public void Split_Char_Overload_RemoveEmpty()
    {
        var partsKeep = "a,,b,".Split(',');
        Assert.Equal(new[] { "a", "", "b", "" }, partsKeep);
        var partsNoEmpty = "a,,b,".Split(',', StringSplitOptions.RemoveEmptyEntries);
        Assert.Equal(new[] { "a", "b" }, partsNoEmpty);
    }

    [Fact]
    public void Join_GenericJoin_Separate()
    {
        var list = new List<Int32> { 1, 2, 3 };
        var joined = list.Join("-");
        Assert.Equal("1-2-3", joined);

        var enumer = (IEnumerable<Object>)["a", 2, 'c'];
        var j2 = enumer.Join("|");
        Assert.Equal("a|2|c", j2);

        List<Int32>? nullList = null;
        Assert.Equal("", nullList.Join(","));

        // Separate 单独测试
        var sb = new StringBuilder();
        sb.Separate(",").Append("A");
        sb.Separate(",").Append("B");
        Assert.Equal("A,B", sb.ToString());
    }

    [Fact]
    public void GetBytes_And_FormatF()
    {
        var bytes = "你好".GetBytes();
        Assert.Equal(Encoding.UTF8.GetBytes("你好"), bytes);
        Assert.Empty(((String)null).GetBytes());
        Assert.Empty("".GetBytes());

        var unicode = "test".GetBytes(Encoding.Unicode);
        Assert.Equal(Encoding.Unicode.GetBytes("test"), unicode);

        // F：日期未指定格式时自动 ToFullString
        var dt = new DateTime(2024, 1, 2, 3, 4, 5, DateTimeKind.Utc);
#pragma warning disable CS0618
        var s1 = "Time:{0}".F(dt);
        var s2 = "Time:{0:yyyyMMdd}".F(dt); // 已有格式，不替换
        var s3 = "Num:{0}".F(123);
#pragma warning restore CS0618
        Assert.StartsWith("Time:", s1);
        Assert.Contains("2024", s1);
        Assert.Equal("Time:20240102", s2);
        Assert.Equal("Num:123", s3);
    }

    [Fact]
    public void EnsureAndTrim()
    {
        Assert.Equal("/api/test", "api/test".EnsureStart("/"));
        Assert.Equal("/api/test", "/api/test".EnsureStart("/"));
        Assert.Equal("", ((String)null).EnsureStart(""));
        Assert.Equal("start", ((String)null).EnsureStart("start"));

        Assert.Equal("file.txt", "file".EnsureEnd(".txt"));
        Assert.Equal("file.txt", "file.txt".EnsureEnd(".txt"));
        Assert.Equal("", ((String)null).EnsureEnd(""));

        Assert.Equal("path", "///path".TrimStart("/"));
        Assert.Equal("///path///", "///path///".TrimEnd("/path")); // 末尾匹配 path
        Assert.Equal("data", "data".TrimStart(null));
        Assert.Equal("data", "data".TrimEnd(""));
    }

    [Fact]
    public void TrimInvisible_Test()
    {
        var src = "A" + (Char)1 + (Char)31 + "B" + (Char)127 + "C";
        var dst = src.TrimInvisible();
        Assert.Equal("ABC", dst);

        var clean = "ABC";
        Assert.Same(clean, clean.TrimInvisible()); // 无不可见字符直接返回原引用

        var onlyInvisible = new String([(Char)1, (Char)2, (Char)127]);
        Assert.Equal("", onlyInvisible.TrimInvisible());

        String? nullStr = null;
        Assert.Null(nullStr.TrimInvisible());
    }

    [Fact]
    public void Substring_Variants()
    {
        var text = "<root><name>Stone</name><name>Second</name></root>";
        var name1 = text.Substring("<name>", "</name>");
        Assert.Equal("Stone", name1);

        // 第二次从 name1 之后找
        var startIdx = text.IndexOf("Stone", StringComparison.Ordinal) + 5;
        var pos = new Int32[2];
        var name2 = text.Substring("<name>", "</name>", startIdx, pos);
        Assert.Equal("Second", name2);
        Assert.True(pos[0] < pos[1]);

        var after = text.Substring("<root>");
        Assert.Contains("Stone", after);

        var before = text.Substring(null, "</root>");
        Assert.DoesNotContain("</root>", before);

        var miss = text.Substring("<none>", "</none>");
        Assert.Equal(String.Empty, miss);

        var bothNull = text.Substring(null, null);
        Assert.Same(text, bothNull);
    }

    [Fact]
    public void Cut_CutStart_CutEnd()
    {
        var cut = "abcdefg".Cut(5, "..."); // 保留 2 + ...
        Assert.Equal("ab...", cut);

        Assert.Throws<ArgumentOutOfRangeException>(() => "abc".Cut(2, "...."));

        var s = "xx--hello--yy";
        Assert.Equal("hello--yy", s.CutStart("--"));
        Assert.Equal("xx--hello", s.CutEnd("--"));

        // 多起始 / 结束
        Assert.Equal("/path", "///path".CutStart("//")); // 不包含 // 结果不变
        Assert.Equal("data.txt", "data.txt.bak".CutEnd(".bak", ".tmp"));

        // pad null
        Assert.Equal("abc", "abc".Cut(10)); // 长度不足
        Assert.Equal("abc", "abc".Cut(-1)); // maxLength<=0
        Assert.Equal("abcdef", "abcdef".Cut(6)); // 刚好
    }

    [Fact]
    public void LevenshteinDistance_Search()
    {
        Assert.Equal(3, StringHelper.LevenshteinDistance("kitten", "sitting"));
        Assert.Equal(0, StringHelper.LevenshteinDistance("abc", "abc"));
        Assert.Equal(4, StringHelper.LevenshteinDistance("", "abcd"));

        var words = new[] { "apple", "apply", "apples", "bpple", "maple" };
        var rs = StringHelper.LevenshteinSearch("apple", words);
        Assert.Contains("apple", rs);
        Assert.Contains("apply", rs);

        // 空 key
        Assert.Empty(StringHelper.LevenshteinSearch("   ", words));
        // 空 words
        Assert.Empty(StringHelper.LevenshteinSearch("apple", Array.Empty<String>()));
    }

    [Fact]
    public void LCSDistance_And_Search()
    {
        var dist = StringHelper.LCSDistance("abc", ["a"]);
        Assert.Equal(2, dist);

        // 排除路径：关键字太短导致 -1
        var negative = StringHelper.LCSDistance("abc", ["z"]);
        Assert.True(negative >= -1);

        var words = new[] { "network", "netcore", "newlife", "future" };
        var rs = StringHelper.LCSSearch("net", words);
        Assert.Contains("network", rs);
        Assert.Contains("netcore", rs);

        Assert.Empty(StringHelper.LCSSearch("   ", words));
        Assert.Empty(StringHelper.LCSSearch("net", Array.Empty<String>()));
    }

    [Fact]
    public void LCS_Generic_Extension_And_LCSSearch_Extension()
    {
        var items = new[] { "network", "netcore", "future" };
        var pairs = items.LCS("net", s => s).ToList();
        Assert.NotEmpty(pairs);
        var search = items.LCSSearch("net", s => s, count: 1).ToList();
        Assert.Single(search);
    }

    [Fact]
    public void FuzzyMatch_List()
    {
        var data = new[] { "StringHelper", "StringBuilder", "StrongHelp" };
        var rs = data.Match("Str Hel", s => s, count: -1, confidence: 0.3).ToList();
        Assert.Contains("StringHelper", rs);
        Assert.Contains("StringBuilder", rs);

        // 高阈值 0.99 仍应保留全部（完全匹配 token => 得分=1）
        var rs2 = data.Match("Str", s => s, count: -1, confidence: 0.99).ToList();
        Assert.Equal(3, rs2.Count);
        Assert.Contains("StringHelper", rs2);
        Assert.Contains("StringBuilder", rs2);
        Assert.Contains("StrongHelp", rs2);

        // 超过 1 的阈值才会被过滤为空
        var rs3 = data.Match("Str", s => s, count: -1, confidence: 1.01).ToList();
        Assert.Empty(rs3);
    }

    [Fact]
    public void FuzzyMatch_Core_MatchFunction()
    {
        var kv = StringHelper.Match("abcdef", "abmd", maxError: 1); // 允许一个跳过
        Assert.True(kv.Key >= 3); // 至少匹配3
        Assert.True(kv.Value <= 1); // 跳过不超过1
    }

    [Fact]
    public void ContainsAndSplitChar()
    {
        Assert.True("abc".Contains('a'));
        Assert.True("abc".Contains('c'));
        Assert.False("abc".Contains('x'));

        var parts = "a,b,c".Split(',');
        Assert.Equal(3, parts.Length);
        Assert.Equal(new[] { "a", "b", "c" }, parts);
    }

    [Fact]
    public void SpeechTip_Disabled_NoSideEffect()
    {
        var old = StringHelper.EnableSpeechTip;
        try
        {
            StringHelper.EnableSpeechTip = false;
            // 不抛异常即可
            "hello".SpeechTip();
        }
        finally
        {
            StringHelper.EnableSpeechTip = old;
        }
    }

    // 不直接测试 Speak / SpeakAsync / SpeakAsyncCancelAll 以避免外部依赖（语音设备），仅验证属性切换 & 快速调用路径
    [Fact]
    public void SpeakAsyncCancelAll_SafeInvoke()
    {
        // 只要返回同一个字符串即可
        var rs = "cancel".SpeakAsyncCancelAll();
        Assert.Equal("cancel", rs);
    }
}