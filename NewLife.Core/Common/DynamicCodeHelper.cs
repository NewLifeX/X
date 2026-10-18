using System.Runtime.CompilerServices;

namespace NewLife;

/// <summary>动态代码能力检查。用于在不支持运行时动态代码的环境（如 NativeAOT）给出明确报错</summary>
internal static class DynamicCodeHelper
{
    /// <summary>当前运行时是否支持动态代码生成</summary>
    public static Boolean IsSupported
    {
#if NET5_0_OR_GREATER
        get => RuntimeFeature.IsDynamicCodeSupported;
#else
        get => true;
#endif
    }

    /// <summary>在不支持动态代码的环境抛出明确异常</summary>
    /// <param name="message">英文原文消息。由调用方提供完整句子，避免把英文短语插入已翻译的模板</param>
    public static void ThrowIfNotSupported(String message)
    {
        if (!IsSupported) throw new PlatformNotSupportedException(L.T(message));
    }
}
