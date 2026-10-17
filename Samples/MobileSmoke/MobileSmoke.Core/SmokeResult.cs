using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

namespace MobileSmoke;

/// <summary>用例状态。</summary>
public enum SmokeStatus
{
    /// <summary>通过。</summary>
    Pass,

    /// <summary>失败。</summary>
    Fail,

    /// <summary>跳过（平台不支持或已知限制）。</summary>
    Skip,
}

/// <summary>单个冒烟用例结果。</summary>
public sealed class SmokeResult
{
    /// <summary>用例名称。</summary>
    public String Name { get; init; } = "";

    /// <summary>状态。</summary>
    public SmokeStatus Status { get; init; }

    /// <summary>失败或跳过原因。</summary>
    public String? Message { get; init; }

    /// <summary>耗时（毫秒）。</summary>
    public Int64 ElapsedMs { get; init; }

    /// <summary>格式化为一行输出。</summary>
    /// <returns>PASS/FAIL/SKIP 行。</returns>
    public String ToLine() => Status switch
    {
        SmokeStatus.Pass => $"[PASS] {Name} ({ElapsedMs}ms)",
        SmokeStatus.Skip => $"[SKIP] {Name} -> {Message}",
        _ => $"[FAIL] {Name} -> {Message}",
    };
}

/// <summary>冒烟报告。</summary>
public sealed class SmokeReport
{
    /// <summary>全部用例结果。</summary>
    public List<SmokeResult> Results { get; } = [];

    /// <summary>操作系统描述。</summary>
    public String Platform { get; } = RuntimeInformation.OSDescription;

    /// <summary>运行时描述。</summary>
    public String Framework { get; } = RuntimeInformation.FrameworkDescription;

    /// <summary>是否支持动态代码。iOS AOT / NativeAOT 通常为 false。</summary>
    public Boolean DynamicCode { get; } = RuntimeFeature.IsDynamicCodeSupported;

    /// <summary>通过数。</summary>
    public Int32 PassCount => Results.Count(r => r.Status == SmokeStatus.Pass);

    /// <summary>失败数。</summary>
    public Int32 FailCount => Results.Count(r => r.Status == SmokeStatus.Fail);

    /// <summary>跳过数。</summary>
    public Int32 SkipCount => Results.Count(r => r.Status == SmokeStatus.Skip);

    /// <summary>汇总行。CI 以 fail=0 作为门禁。</summary>
    public String Summary => $"SUMMARY total={Results.Count} pass={PassCount} fail={FailCount} skip={SkipCount}";

    /// <summary>生成文本报告。</summary>
    /// <returns>文本内容。</returns>
    public String ToText()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"platform={Platform}");
        sb.AppendLine($"framework={Framework}");
        sb.AppendLine($"dynamicCode={DynamicCode}");
        foreach (var item in Results) sb.AppendLine(item.ToLine());
        sb.AppendLine(Summary);
        return sb.ToString();
    }

    /// <summary>生成 JSON 报告。手写 JSON，避免报告本身依赖序列化能力。</summary>
    /// <returns>JSON 内容。</returns>
    public String ToJson()
    {
        var sb = new StringBuilder();
        sb.Append('{');
        AppendPair(sb, "platform", Platform, false);
        AppendPair(sb, "framework", Framework, true);
        sb.Append(",\"dynamicCode\":").Append(DynamicCode ? "true" : "false");
        sb.Append(",\"total\":").Append(Results.Count);
        sb.Append(",\"pass\":").Append(PassCount);
        sb.Append(",\"fail\":").Append(FailCount);
        sb.Append(",\"skip\":").Append(SkipCount);
        sb.Append(",\"results\":[");
        for (var i = 0; i < Results.Count; i++)
        {
            if (i > 0) sb.Append(',');
            var r = Results[i];
            sb.Append('{');
            AppendPair(sb, "name", r.Name, false);
            AppendPair(sb, "status", r.Status.ToString().ToUpperInvariant(), true);
            sb.Append(",\"elapsedMs\":").Append(r.ElapsedMs);
            if (r.Message != null) AppendPair(sb, "message", r.Message, true);
            sb.Append('}');
        }
        sb.Append("]}");
        return sb.ToString();
    }

    /// <summary>保存文本与 JSON 报告。</summary>
    public void Save()
    {
        File.WriteAllText(SmokeEnvironment.ReportTextPath, ToText());
        File.WriteAllText(SmokeEnvironment.ReportJsonPath, ToJson());
    }

    private static void AppendPair(StringBuilder sb, String name, String value, Boolean comma)
    {
        if (comma) sb.Append(',');
        sb.Append('"').Append(Escape(name)).Append("\":\"").Append(Escape(value)).Append('"');
    }

    private static String Escape(String value)
    {
        var sb = new StringBuilder(value.Length + 8);
        foreach (var ch in value)
        {
            switch (ch)
            {
                case '"': sb.Append("\\\""); break;
                case '\\': sb.Append("\\\\"); break;
                case '\n': sb.Append("\\n"); break;
                case '\r': sb.Append("\\r"); break;
                case '\t': sb.Append("\\t"); break;
                default:
                    if (ch < ' ') sb.Append("\\u").Append(((Int32)ch).ToString("x4"));
                    else sb.Append(ch);
                    break;
            }
        }
        return sb.ToString();
    }
}
