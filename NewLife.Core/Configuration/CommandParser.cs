namespace NewLife.Configuration;

/// <summary>命令分析器</summary>
public class CommandParser
{
    /// <summary>不区分大小写</summary>
    public Boolean IgnoreCase { get; set; }

    /// <summary>去除前导横杠。默认true</summary>
    public Boolean TrimStart { get; set; } = true;

    /// <summary>分析参数数组，得到名值字段</summary>
    /// <param name="args"></param>
    /// <returns></returns>
    public IDictionary<String, String> Parse(String[] args)
    {
        args ??= Environment.GetCommandLineArgs();

        var dic = IgnoreCase ?
            new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase) :
            new Dictionary<String, String>();
        for (var i = 0; i < args.Length; i++)
        {
            var key = args[i];

            // 如果key以-开头，说明是参数名，下一个可能是参数值
            if (key[0] == '-')
            {
                // 有=表明是kv结构
                var p = key.IndexOf('=');
                if (p > 0)
                {
                    var value = key.Substring(p + 1);
                    key = key.Substring(0, p);
                    if (TrimStart) key = key.TrimStart('-');
                    dic[key] = TrimQuote(value);
                }
                else
                {
                    // 下一个是值
                    if (TrimStart) key = key.TrimStart('-');
                    var value = (i + 1 < args.Length && args[i + 1][0] != '-') ? args[++i] : null;
                    dic[key] = TrimQuote(value);
                }
            }
            else
            {
                // 下一个是值
                if (TrimStart) key = key.TrimStart('-');
                var value = (i + 1 < args.Length && args[i + 1][0] != '-') ? args[++i] : null;
                dic[key] = TrimQuote(value);
            }
        }

        return dic;
    }

    /// <summary>去除两头的双引号</summary>
    /// <param name="value"></param>
    /// <returns></returns>
    public static String TrimQuote(String value)
    {
        if (value.IsNullOrEmpty()) return value;

        if (value[0] == '"' && value[value.Length - 1] == '"') value = value.Substring(1, value.Length - 2);
        if (value[0] == '\'' && value[value.Length - 1] == '\'') value = value.Substring(1, value.Length - 2);

        return value;
    }
}
