using System.Runtime.InteropServices;

namespace NewLife.Windows;

/// <summary>
/// 控制台帮助类，用于控制控制台的快速编辑、关闭按钮。
/// </summary>
public class ConsoleHelper
{
    #region 关闭控制台 快速编辑模式、插入模式

    private const Int32 STD_INPUT_HANDLE = -10;
    private const UInt32 ENABLE_QUICK_EDIT_MODE = 0x0040;
    private const UInt32 ENABLE_INSERT_MODE = 0x0020;

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern IntPtr GetStdHandle(Int32 hConsoleHandle);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern Boolean GetConsoleMode(IntPtr hConsoleHandle, out UInt32 mode);

    [DllImport("kernel32.dll", SetLastError = true)]
    internal static extern Boolean SetConsoleMode(IntPtr hConsoleHandle, UInt32 mode);

    /// <summary>
    /// 退出编辑模式
    /// </summary>
    public static void DisableQuickEditMode()
    {
        var hStdin = GetStdHandle(STD_INPUT_HANDLE);
        GetConsoleMode(hStdin, out var mode);
        mode &= ~ENABLE_QUICK_EDIT_MODE;//移除快速编辑模式
        mode &= ~ENABLE_INSERT_MODE;      //移除插入模式
        SetConsoleMode(hStdin, mode);
    }

    #endregion 关闭控制台 快速编辑模式、插入模式

    #region 设置控制台标题 禁用关闭按钮

    [DllImport("user32.dll", EntryPoint = "FindWindow")]
    private static extern IntPtr FindWindow(String? lpClassName, String lpWindowName);

    [DllImport("user32.dll", EntryPoint = "GetSystemMenu")]
    private static extern IntPtr GetSystemMenu(IntPtr hWnd, IntPtr bRevert);

    [DllImport("user32.dll", EntryPoint = "RemoveMenu")]
    private static extern IntPtr RemoveMenu(IntPtr hMenu, UInt32 uPosition, UInt32 uFlags);

    /// <summary>
    /// 禁用关闭按钮
    /// </summary>
    /// <param name="cmdTitle">控制台标题，程序名称</param>
    public static void DisableCloseButton(String cmdTitle)
    {
        var windowHandle = FindWindow(null, cmdTitle);
        var closeMenu = GetSystemMenu(windowHandle, IntPtr.Zero);
        UInt32 SC_CLOSE = 0xF060;
        RemoveMenu(closeMenu, SC_CLOSE, 0x0);
    }

    /// <summary>
    /// 禁用关闭按钮
    /// </summary>
    /// <param name="windowHandle">窗口句柄</param>
    public static void DisableCloseButton(IntPtr windowHandle)
    {
        var closeMenu = GetSystemMenu(windowHandle, IntPtr.Zero);
        UInt32 SC_CLOSE = 0xF060;
        RemoveMenu(closeMenu, SC_CLOSE, 0x0);
    }

    /// <summary>
    /// 关闭控制台
    /// </summary>
    /// <param name="sender"></param>
    /// <param name="e"></param>
    protected static void CloseConsole(Object sender, ConsoleCancelEventArgs e)
    {
        Environment.Exit(0);
    }

    #endregion 设置控制台标题 禁用关闭按钮
}