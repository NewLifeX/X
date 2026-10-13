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

    /// <summary>把任意换行统一为 CRLF，供 HTTP 报文等要求 CRLF 分隔的测试数据使用</summary>
    /// <remarks>仓库不同文件的源码行尾不一（部分按 LF 存储），原始字符串字面量直接 GetBytes
    /// 会随检出平台产生不同报文；HTTP 协议要求 CRLF 分隔，先归一再用</remarks>
    /// <param name="text">待归一文本</param>
    /// <returns>CRLF 换行的文本，null 视为空串</returns>
    public static String Crlf(this String? text) => (text?.Replace("\r\n", "\n") ?? String.Empty).Replace("\n", "\r\n");
}
