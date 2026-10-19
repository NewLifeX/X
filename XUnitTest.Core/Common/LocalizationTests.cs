using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using System.Threading.Tasks;
using NewLife;
using NewLife.Log;
using Xunit;

namespace XUnitTest.Common;

/// <summary>测试集合定义：本类用例会临时改写 <see cref="Localization.Culture"/> 等进程级状态，
/// 与其它用例串行执行，避免并行用例读到被临时改写的语言</summary>
[Collection("GlobalState")]
public class LocalizationTests
{
    private const String Key = "Reset is not supported in streaming mode.";
    private const String Zh = "流式模式不支持复位";
    private static readonly CultureInfo _zh = CultureInfo.GetCultureInfo("zh-Hans");

    #region 基础翻译
    [Fact(DisplayName = "简体中文：按英文原文查到译文")]
    public void Translate_zh_Hans() => Assert.Equal(Zh, Localization.Translate(_zh, Key));

    [Theory(DisplayName = "简体变体：zh / zh-CN / zh-SG 回退到 zh-Hans")]
    [InlineData("zh")]
    [InlineData("zh-CN")]
    [InlineData("zh-SG")]
    public void Translate_zh_variants(String name) => Assert.Equal(Zh, Localization.Translate(CultureInfo.GetCultureInfo(name), Key));

    [Theory(DisplayName = "英文与无表语言：回退英文原文")]
    [InlineData("en-US")]
    [InlineData("en-GB")]
    [InlineData("sw-KE")]
    public void Translate_fallback(String name) => Assert.Equal(Key, Localization.Translate(CultureInfo.GetCultureInfo(name), Key));

    [Fact(DisplayName = "未知键：回退英文原文")]
    public void Translate_unknown_key() => Assert.Equal("Not a translated message.", Localization.Translate(_zh, "Not a translated message."));

    [Fact(DisplayName = "空值：原样返回")]
    public void Translate_null_or_empty()
    {
        Assert.Null(Localization.Translate((String)null!));
        Assert.Equal("", Localization.Translate(""));
    }

    [Fact(DisplayName = "含制表符的模板：键与译文正常查表")]
    public void Translate_tab_template()
    {
        var key = "Saved {0:n0} items\telapsed {1:n0} ms\tspeed {2:n0} tps\trounds {3:n0}\tspeed {4:n0} tps\tsucceeded {5:n0}";
        var text = Localization.Translate(_zh, key, 1, 2, 3, 4, 5, 6);

        Assert.Contains("保存", text);
        Assert.Contains("\t", text);
    }

    [Theory(DisplayName = "整句词条：状态句整体本地化，不残留英文状态词")]
    [InlineData("The configuration file {1} of {0} failed to load; overwriting with the default configuration.", "加载失败")]
    [InlineData("The configuration file {1} of {0} is missing; overwriting with the default configuration.", "不存在")]
    public void Translate_status_sentence(String key, String expected)
    {
        var text = Localization.Translate(_zh, key, "AppConfig", "app.config");

        Assert.Contains(expected, text);
        Assert.DoesNotContain("failed to load", text);
        Assert.DoesNotContain("missing", text);
    }

    [Theory(DisplayName = "整句词条：动态能力守卫消息整体本地化")]
    [InlineData("NativeAOT/trimmed environments do not support the script engine. Use static registration or an alternative implementation.", "脚本引擎")]
    [InlineData("NativeAOT/trimmed environments do not support plugin dynamic loading. Use static registration or an alternative implementation.", "插件动态加载")]
    public void Translate_dynamic_code_sentence(String key, String expected)
    {
        var text = Localization.Translate(_zh, key);

        Assert.Contains(expected, text);
        Assert.DoesNotContain("script engine", text);
        Assert.DoesNotContain("plugin dynamic loading", text);
    }
    #endregion

    #region 格式化
    [Fact(DisplayName = "格式化：保留占位符参数")]
    public void Translate_format()
    {
        Assert.Equal("消息体未读满：期望 10 字节，实读 3 字节", Localization.Translate(_zh, "Message body is incomplete; expected {0} bytes, read {1} bytes.", 10, 3));
    }

    [Fact(DisplayName = "格式化：保留数字格式说明符并跟随目标文化")]
    public void Translate_format_specifier()
    {
        var key = "Download completed. Size: {0:n0} bytes, elapsed: {1:n0} ms";
        Assert.Equal("下载完成！大小1,234字节，耗时56ms", Localization.Translate(_zh, key, 1234, 56));

        var de = CultureInfo.GetCultureInfo("de-DE");
        try
        {
            Localization.Register(de, new Dictionary<String, String>
            {
                [key] = "下载完成，大小 {0:n0} 字节，耗时 {1:n0} 毫秒",
            }, true);

            Assert.Equal("下载完成，大小 1.234 字节，耗时 56 毫秒", Localization.Translate(de, key, 1234, 56));
        }
        finally
        {
            Localization.Unregister(de);
        }
    }

    [Fact(DisplayName = "译文占位符不合法：回退英文，不抛新异常")]
    public void Translate_bad_translation_falls_back()
    {
        var key = "Message body is incomplete; expected {0} bytes, read {1} bytes.";
        try
        {
            Localization.Register(_zh, new Dictionary<String, String>
            {
                [key] = "坏译文 {0} {1} {2}",
            }, true);

            Assert.Equal("Message body is incomplete; expected 5 bytes, read 3 bytes.", Localization.Translate(_zh, key, 5, 3));
        }
        finally
        {
            Localization.Unregister(_zh);
        }
    }
    #endregion

    #region 注册与覆盖
    [Fact(DisplayName = "注册覆盖表：默认不覆盖内置译文")]
    public void Register_does_not_override_by_default()
    {
        try
        {
            Localization.Register(_zh, new Dictionary<String, String> { [Key] = "覆盖译文" });

            Assert.Equal(Zh, Localization.Translate(_zh, Key));
        }
        finally
        {
            Localization.Unregister(_zh);
        }
    }

    [Fact(DisplayName = "全部内置语言：解压后都能查到译文")]
    public void AllBuiltinLanguages()
    {
        var cultures = new[]
        {
            "zh-Hans", "zh-Hant", "ja", "ko", "vi", "th", "id", "ms", "hi", "bn",
            "ar", "he", "fa", "de", "fr", "es", "pt", "it", "nl", "pl",
            "cs", "sv", "da", "nb", "fi", "el", "ro", "hu", "ru", "uk", "tr",
        };
        var saved = Localization.Culture;
        var missing = new List<String>();
        try
        {
            foreach (var name in cultures)
            {
                Localization.Culture = CultureInfo.GetCultureInfo(name);
                if (Localization.Translate(Key) == Key) missing.Add(name);
            }
        }
        finally
        {
            Localization.Culture = saved;
        }

        Assert.Empty(missing);
    }

    [Fact(DisplayName = "注册覆盖表：显式 overwrite 可改内置译文")]
    public void Register_overwrite()
    {
        try
        {
            Localization.Register(_zh, new Dictionary<String, String> { [Key] = "覆盖译文" }, true);

            Assert.Equal("覆盖译文", Localization.Translate(_zh, Key));
        }
        finally
        {
            Localization.Unregister(_zh);
        }
    }

    [Fact(DisplayName = "多次注册：默认先注册者优先，overwrite 后注册者优先")]
    public void Register_multiple_times()
    {
        var key = "Localization test {0}.";
        var culture = CultureInfo.GetCultureInfo("sw-KE");
        try
        {
            Localization.Register(culture, new Dictionary<String, String> { [key] = "第一次" });
            Localization.Register(culture, new Dictionary<String, String> { [key] = "第二次" });
            Assert.Equal("第一次", Localization.Translate(culture, key));

            Localization.Register(culture, new Dictionary<String, String> { [key] = "第三次" }, true);
            Assert.Equal("第三次", Localization.Translate(culture, key));
        }
        finally
        {
            Localization.Unregister(culture);
        }
    }

    [Fact(DisplayName = "多次注册不同键：累加合并而非清空已有内容")]
    public void Register_accumulates()
    {
        var culture = CultureInfo.GetCultureInfo("sw-KE");
        var first = "Localization accumulate first {0}.";
        var second = "Localization accumulate second {0}.";
        try
        {
            Localization.Register(culture, new Dictionary<String, String> { [first] = "第一 {0}" });
            Localization.Register(culture, new Dictionary<String, String> { [second] = "第二 {0}" });

            Assert.Equal("第一 1", Localization.Translate(culture, first, 1));
            Assert.Equal("第二 2", Localization.Translate(culture, second, 2));

            // 覆盖注册同样只影响表中出现的键，未出现的键保持不变
            Localization.Register(culture, new Dictionary<String, String> { [first] = "覆盖 {0}" }, true);
            Assert.Equal("覆盖 1", Localization.Translate(culture, first, 1));
            Assert.Equal("第二 2", Localization.Translate(culture, second, 2));
        }
        finally
        {
            Localization.Unregister(culture);
        }
    }

    [Fact(DisplayName = "注册新语言：无需编译即可新增译文")]
    public void Register_new_language()
    {
        var culture = CultureInfo.GetCultureInfo("sw-KE");
        try
        {
            Localization.Register(culture, new Dictionary<String, String> { [Key] = "Maudhui ya jaribio" });

            Assert.Equal("Maudhui ya jaribio", Localization.Translate(culture, Key));
            Assert.Equal(Key, Localization.Translate(CultureInfo.GetCultureInfo("sq-AL"), Key));
        }
        finally
        {
            Localization.Unregister(culture);
        }
    }

    [Fact(DisplayName = "繁体变体：zh-TW / zh-HK 命中 zh-Hant 注册表")]
    public void Register_zh_Hant_mapping()
    {
        try
        {
            Localization.Register(CultureInfo.GetCultureInfo("zh-Hant"), new Dictionary<String, String> { [Key] = "繁體譯文" }, true);

            Assert.Equal("繁體譯文", Localization.Translate(CultureInfo.GetCultureInfo("zh-TW"), Key));
            Assert.Equal("繁體譯文", Localization.Translate(CultureInfo.GetCultureInfo("zh-HK"), Key));
        }
        finally
        {
            Localization.Unregister(CultureInfo.GetCultureInfo("zh-Hant"));
        }
    }

    [Fact(DisplayName = "注册参数校验：null 语言抛 ArgumentNullException，空表忽略")]
    public void Register_arguments()
    {
        Assert.Throws<ArgumentNullException>(() => Localization.Register(null!, new Dictionary<String, String>()));
        Assert.False(Localization.Unregister(null!));

        Localization.Register(_zh, new Dictionary<String, String>());
        Assert.Equal(Zh, Localization.Translate(_zh, Key));
    }

    [Fact(DisplayName = "注销未知语言：返回 false")]
    public void Unregister_unknown() => Assert.False(Localization.Unregister(CultureInfo.GetCultureInfo("ja-JP")));
    #endregion

    #region 异常与并发
    [Fact(DisplayName = "XException：默认英文环境下输出英文原文")]
    public void XException_english()
    {
        Assert.Equal(Key, new XException(Key).Message);
        Assert.Equal("Message body is incomplete; expected 5 bytes, read 3 bytes.", new XException("Message body is incomplete; expected {0} bytes, read {1} bytes.", 5, 3).Message);
    }

    [Fact(DisplayName = "并发读取：翻译结果稳定")]
    public void Translate_concurrent()
    {
        var errors = 0;
        Parallel.For(0, 2000, _ =>
        {
            if (Localization.Translate(_zh, Key) != Zh) Interlocked.Increment(ref errors);
        });

        Assert.Equal(0, errors);
    }

    [Fact(DisplayName = "XException：强制 Culture 后输出对应语言")]
    public void XException_uses_culture()
    {
        var saved = Localization.Culture;
        try
        {
            Localization.Culture = _zh;
            Assert.Equal(Zh, new XException(Key).Message);
        }
        finally
        {
            Localization.Culture = saved;
        }
    }

    [Fact(DisplayName = "Logger.Format：日志按 LogCulture 渲染")]
    public void Logger_format_uses_log_culture()
    {
        var saved = Localization.LogCulture;
        try
        {
            Localization.LogCulture = _zh;
            var log = new ProbeLogger();
            Assert.Equal(Zh, log.Probe(Key));
            Assert.Equal("消息体未读满：期望 5 字节，实读 3 字节", log.Probe("Message body is incomplete; expected {0} bytes, read {1} bytes.", 5, 3));
        }
        finally
        {
            Localization.LogCulture = saved;
        }
    }

    [Fact(DisplayName = "Logger.FormatMessage：Logger 路径保留异常特殊处理")]
    public void Logger_format_message_uses_logger_format()
    {
        var saved = Localization.LogCulture;
        try
        {
            Localization.LogCulture = _zh;
            var log = new ProbeLogger();
            Assert.Equal("消息体未读满：期望 5 字节，实读 3 字节", Logger.FormatMessage(log, "Message body is incomplete; expected {0} bytes, read {1} bytes.", [5, 3]));

            var ex = new InvalidOperationException("boom");
            Assert.Equal(ex.GetMessage(), Logger.FormatMessage(log, "{0}", [ex]));
        }
        finally
        {
            Localization.LogCulture = saved;
        }
    }

    [Fact(DisplayName = "Logger.FormatMessage：非 Logger 日志器回退 TranslateForLog")]
    public void Logger_format_message_falls_back()
    {
        var saved = Localization.LogCulture;
        try
        {
            Localization.LogCulture = _zh;
            var log = new PlainLog();
            Assert.Equal("消息体未读满：期望 5 字节，实读 3 字节", Logger.FormatMessage(log, "Message body is incomplete; expected {0} bytes, read {1} bytes.", [5, 3]));
        }
        finally
        {
            Localization.LogCulture = saved;
        }
    }

    #endregion

    private sealed class ProbeLogger : Logger
    {
        public String Probe(String format, params Object?[] args) => Format(format, args);

        protected override void OnWrite(LogLevel level, String format, params Object?[] args) { }
    }

    private sealed class PlainLog : ILog
    {
        public Boolean Enable { get; set; } = true;

        public LogLevel Level { get; set; } = LogLevel.Info;

        public void Write(LogLevel level, String format, params Object?[] args) { }

        public void Debug(String format, params Object?[] args) { }

        public void Info(String format, params Object?[] args) { }

        public void Warn(String format, params Object?[] args) { }

        public void Error(String format, params Object?[] args) { }

        public void Fatal(String format, params Object?[] args) { }
    }
}

