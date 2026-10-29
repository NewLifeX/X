using NewLife.Collections;
using NewLife.Windows;

namespace NewLife.Log;

/// <summary>控制台日志调色板与终端能力探测</summary>
/// <remarks>
/// 调色板经过实测筛选：ANSI 调色板在深色背景下对比度均不低于 5.9:1，两两色彩差异（CIE76 ΔE）不小于 25；
/// 16 色降级调色板在 Windows Terminal、传统控制台、VS Code 三套主流配色下对比度均不低于 4.0:1，
/// 并已剔除与警告/错误级别撞色的红、黄，以及深色背景下不可读的蓝、洋红。
/// </remarks>
internal static class ConsoleColorPalette
{
    #region 调色板
    /// <summary>16 色降级调色板</summary>
    public static readonly ConsoleColor[] Basic =
        [ConsoleColor.Cyan, ConsoleColor.Green, ConsoleColor.DarkYellow, ConsoleColor.DarkCyan, ConsoleColor.DarkGreen, ConsoleColor.White];

    /// <summary>ANSI 256 色调色板（xterm 索引）</summary>
    public static readonly Byte[] Ansi = [39, 216, 77, 212, 43, 177, 149, 116, 69, 143, 147, 249];

    /// <summary>主线程颜色（16 色）</summary>
    public const ConsoleColor BasicMaster = ConsoleColor.Gray;

    /// <summary>主线程颜色（ANSI）</summary>
    public const Byte AnsiMaster = 250;

    /// <summary>警告级别颜色（16 色）</summary>
    public const ConsoleColor BasicWarn = ConsoleColor.Yellow;

    /// <summary>警告级别颜色（ANSI）</summary>
    public const Byte AnsiWarn = 214;

    /// <summary>错误级别颜色（16 色）</summary>
    public const ConsoleColor BasicError = ConsoleColor.Red;

    /// <summary>错误级别颜色（ANSI）</summary>
    public const Byte AnsiError = 203;

    /// <summary>致命级别颜色（ANSI）</summary>
    public const Byte AnsiFatal = 196;
    #endregion

    #region 能力探测
    private static Int32 _colorState;
    /// <summary>终端是否支持彩色输出。输出重定向、NO_COLOR 环境变量、TERM=dumb 时关闭</summary>
    public static Boolean SupportColor
    {
        get
        {
            if (_colorState == 0) _colorState = DetectColor() ? 1 : 2;

            return _colorState == 1;
        }
    }

    private static Boolean DetectColor()
    {
        // NO_COLOR 规范 https://no-color.org
        if (!Environment.GetEnvironmentVariable("NO_COLOR").IsNullOrEmpty()) return false;

        // FORCE_COLOR 规范 https://force-color.org，可覆盖重定向判断
        if (!Environment.GetEnvironmentVariable("FORCE_COLOR").IsNullOrEmpty()) return true;

        try
        {
            // 输出到文件或管道时不能写入控制字符
            if (Console.IsOutputRedirected) return false;
        }
        catch
        {
            return false;
        }

        return !"dumb".EqualIgnoreCase(Environment.GetEnvironmentVariable("TERM"));
    }

    private static Int32 _ansiState;
    /// <summary>终端是否支持 ANSI 256 色。不支持时降级使用 16 色调色板</summary>
    public static Boolean SupportAnsiColor
    {
        get
        {
            if (_ansiState == 0) _ansiState = DetectAnsi() ? 1 : 2;

            return _ansiState == 1;
        }
    }

    private static Boolean DetectAnsi()
    {
        if (!SupportColor) return false;

        // FORCE_COLOR 规范 https://force-color.org
        if (!Environment.GetEnvironmentVariable("FORCE_COLOR").IsNullOrEmpty()) return true;

        // 非 Windows 终端天然支持 ANSI 转义序列
        if (!Runtime.Windows) return true;

        // Windows 需要显式开启虚拟终端；经典控制台即使开启，也只会把 256 色映射到最近的 16 色，改用 16 色调色板更精确
        return ConsoleHelper.TryEnableVirtualTerminal() && IsRichTerminal();
    }

    private static Boolean IsRichTerminal() =>
        !Environment.GetEnvironmentVariable("WT_SESSION").IsNullOrEmpty() ||
        !Environment.GetEnvironmentVariable("TERM_PROGRAM").IsNullOrEmpty() ||
        !Environment.GetEnvironmentVariable("COLORTERM").IsNullOrEmpty() ||
        !Environment.GetEnvironmentVariable("TERMINAL_EMULATOR").IsNullOrEmpty() ||
        "ON".EqualIgnoreCase(Environment.GetEnvironmentVariable("ConEmuANSI"));
    #endregion

    #region 方法
    /// <summary>整行着色</summary>
    /// <param name="color">256 色索引</param>
    /// <param name="text">文本</param>
    /// <returns></returns>
    public static String Wrap(Byte color, String text)
    {
        var sb = Pool.StringBuilder.Get();
        sb.Append("\u001b[38;5;").Append(color).Append('m').Append(text).Append("\u001b[0m");

        return sb.Return(true);
    }
    #endregion
}
