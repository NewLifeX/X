using System;
using System.Reflection;
using System.Runtime.Versioning;
using System.Threading;
using System.Threading.Tasks;
#if __WIN__
using System.Windows.Forms;
#endif
using NewLife.Reflection;
using NewLife.Threading;

namespace NewLife.Log
{
    /// <summary>日志类，包含跟踪调试功能</summary>
    /// <remarks>
    /// 该静态类包括写日志、写调用栈和Dump进程内存等调试功能。
    /// 
    /// 默认写日志到文本文件，可通过修改<see cref="Log"/>属性来增加日志输出方式。
    /// 对于控制台工程，可以直接通过UseConsole方法，把日志输出重定向为控制台输出，并且可以为不同线程使用不同颜色。
    /// </remarks>
    public static partial class XTrace2
    {
        #region 拦截WinForm异常
#if __WIN__
        private static Int32 initWF = 0;
        private static Boolean _ShowErrorMessage;
        //private static String _Title;

        /// <summary>拦截WinForm异常并记录日志，可指定是否用<see cref="MessageBox"/>显示。</summary>
        /// <param name="showErrorMessage">发为捕获异常时，是否显示提示，默认显示</param>
        public static void UseWinForm(Boolean showErrorMessage = true)
        {
            Runtime.IsConsole = false;

            _ShowErrorMessage = showErrorMessage;

            if (initWF > 0 || Interlocked.CompareExchange(ref initWF, 1, 0) != 0) return;
            //if (!Application.MessageLoop) return;

            AppDomain.CurrentDomain.UnhandledException += CurrentDomain_UnhandledException2;
            Application.SetUnhandledExceptionMode(UnhandledExceptionMode.CatchException);
            Application.ThreadException += Application_ThreadException;
        }

        static void CurrentDomain_UnhandledException2(Object sender, UnhandledExceptionEventArgs e)
        {
            var show = _ShowErrorMessage && Application.MessageLoop;
            var ex = e.ExceptionObject as Exception;
            var title = e.IsTerminating ? "异常退出" : "出错";
            if (show) MessageBox.Show(ex?.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        static void Application_ThreadException(Object sender, ThreadExceptionEventArgs e)
        {
            XTrace.WriteException(e.Exception);

            var show = _ShowErrorMessage && Application.MessageLoop;
            if (show) MessageBox.Show(e.Exception == null ? "" : e.Exception.Message, "出错", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        /// <summary>在WinForm控件上输出日志，主要考虑非UI线程操作</summary>
        /// <remarks>不是常用功能，为了避免干扰常用功能，保持UseWinForm开头</remarks>
        /// <param name="control">要绑定日志输出的WinForm控件</param>
        /// <param name="useFileLog">是否同时使用文件日志，默认使用</param>
        /// <param name="maxLines">最大行数</param>
        public static void UseWinFormControl(this Control control, Boolean useFileLog = true, Int32 maxLines = 1000)
        {
            var clg = XTrace.Log as TextControlLog;
            var ftl = XTrace.Log as TextFileLog;
            if (XTrace.Log is CompositeLog cmp)
            {
                ftl = cmp.Get<TextFileLog>();
                clg = cmp.Get<TextControlLog>();
            }

            // 控制控制台日志
            if (clg == null) clg = new TextControlLog();
            clg.Control = control;
            clg.MaxLines = maxLines;

            if (!useFileLog)
            {
                XTrace.Log = clg;
                if (ftl != null) ftl.Dispose();
            }
            else
            {
                if (ftl == null) ftl = TextFileLog.Create(null);
                XTrace.Log = new CompositeLog(clg, ftl);
            }
        }

        /// <summary>控件绑定到日志，生成混合日志</summary>
        /// <param name="control"></param>
        /// <param name="log"></param>
        /// <param name="maxLines"></param>
        /// <returns></returns>
        public static ILog Combine(this Control control, ILog log, Int32 maxLines = 1000)
        {
            if (control == null || log == null) return log;

            var clg = new TextControlLog
            {
                Control = control,
                MaxLines = maxLines
            };

            return new CompositeLog(log, clg);
        }
#endif
        #endregion
    }
}