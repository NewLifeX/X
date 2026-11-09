using System;

namespace NewLife;

/// <summary>本地化翻译快捷入口</summary>
/// <remarks>仅用于库内部，缩短抛出点的消息构造代码。</remarks>
internal static class L
{
    /// <summary>翻译指定文本</summary>
    /// <param name="template">英文原文模板</param>
    /// <returns>本地化文本；缺少翻译时返回原文</returns>
    public static String T(String template) => Localization.Translate(template);

    /// <summary>翻译并格式化指定文本</summary>
    /// <param name="template">英文原文模板</param>
    /// <param name="args">格式化参数</param>
    /// <returns>本地化文本；缺少翻译或格式化失败时回退英文</returns>
    public static String T(String template, params Object?[]? args) => Localization.Translate(template, args);

    /// <summary>按日志语言翻译指定文本</summary>
    /// <param name="template">英文原文模板</param>
    /// <returns>本地化文本；缺少翻译时返回原文</returns>
    /// <remarks>日志语言优先取 <see cref="Localization.LogCulture"/>，未设置时依次回退锁定语言、<see cref="Localization.DefaultCulture"/> 与启动时的系统语言，避免随请求语言混杂。</remarks>
    public static String TLog(String template) => Localization.TranslateForLog(template);

    /// <summary>按日志语言翻译并格式化指定文本</summary>
    /// <param name="template">英文原文模板</param>
    /// <param name="args">格式化参数</param>
    /// <returns>本地化文本；缺少翻译或格式化失败时回退英文</returns>
    public static String TLog(String template, params Object?[]? args) => Localization.TranslateForLog(template, args);
}
