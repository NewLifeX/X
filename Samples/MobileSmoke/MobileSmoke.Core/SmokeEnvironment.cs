namespace MobileSmoke;

/// <summary>移动端可写路径与报告位置。</summary>
public static class SmokeEnvironment
{
    /// <summary>平台诊断输出回调。Android 宿主将其转发到 logcat。</summary>
    public static Action<String>? DiagnosticLog { get; set; }

    /// <summary>可写根目录。Android/iOS 使用应用私有目录，桌面使用 LocalApplicationData。</summary>
    public static String WritableRoot { get; } = CreateWritableRoot();

    /// <summary>输出诊断信息。</summary>
    /// <param name="line">诊断行。</param>
    public static void WriteDiagnostic(String line)
    {
        Console.WriteLine(line);
        DiagnosticLog?.Invoke(line);
    }

    /// <summary>文本报告路径。</summary>
    public static String ReportTextPath => Path.Combine(WritableRoot, "smoke.txt");

    /// <summary>JSON 报告路径。</summary>
    public static String ReportJsonPath => Path.Combine(WritableRoot, "smoke.json");

    /// <summary>创建一次性临时目录。</summary>
    /// <param name="name">目录名前缀。</param>
    /// <returns>已创建的目录路径。</returns>
    public static String NewTempDirectory(String name)
    {
        var dir = Path.Combine(WritableRoot, name + "-" + Guid.NewGuid().ToString("N")[..8]);
        Directory.CreateDirectory(dir);
        return dir;
    }

    private static String CreateWritableRoot()
    {
        var root = "";
        try
        {
            root = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        catch
        {
            // 某些受限平台可能不支持该特殊目录，回退到临时目录
        }

        if (String.IsNullOrEmpty(root)) root = Path.GetTempPath();

        root = Path.Combine(root, "MobileSmoke");
        Directory.CreateDirectory(root);
        return root;
    }
}
