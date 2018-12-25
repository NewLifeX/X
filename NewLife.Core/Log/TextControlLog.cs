using System;
using System.Windows.Forms;

namespace NewLife.Log
{
    /// <summary>文本控件输出日志</summary>
    public class TextControlLog : Logger
    {
        /// <summary>文本控件</summary>
        public Control Control { get; set; }

        /// <summary>最大行数，超过该行数讲清空文本控件。默认1000行</summary>
        public Int32 MaxLines { get; set; } = 1000;

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
            WriteLog(Control, Format(format, args) + Environment.NewLine, MaxLines);
        }

        /// <summary>在WinForm控件上输出日志，主要考虑非UI线程操作</summary>
        /// <remarks>不是常用功能，为了避免干扰常用功能，保持UseWinForm开头</remarks>
        /// <param name="control">要绑定日志输出的WinForm控件</param>
        /// <param name="msg">日志</param>
        /// <param name="maxLines">最大行数</param>
        public static void WriteLog(Control control, String msg, Int32 maxLines = 1000)
        {
            if (control == null) return;

            var txt = control as TextBoxBase;
            if (txt == null) throw new XException("不支持的控件类型{0}！", control.GetType());

            txt.Append(msg, maxLines);
        }
    }
}