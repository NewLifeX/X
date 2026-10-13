namespace XUnitTest;

/// <summary>测试文本助手</summary>
internal static class TestText
{
    /// <summary>把 CRLF 归一为 LF，消除文本比对对源文件行尾与平台输出换行的依赖</summary>
    /// <remarks>仓库源码按 CRLF 检出，而 .NET 在 Unix 上的部分文本输出（XML/响应头等）使用 LF，
    /// 直接比较原始字符串会随平台假红；比对前两侧统一归一</remarks>
    /// <param name="text">待归一文本</param>
    /// <returns>归一后的文本，null 视为空串</returns>
    public static String NormalizeNewline(this String? text) => text?.Replace("\r\n", "\n") ?? String.Empty;
}
