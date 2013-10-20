using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Forms;
using NewLife.Exceptions;

namespace NewLife.Log
{
    /// <summary>文本控件输出日志</summary>
    public class TextControlLog : Logger
    {
        private Control _Control;
        /// <summary>文本控件</summary>
        public Control Control { get { return _Control; } set { _Control = value; } }

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
        }

        /// <summary>在WinForm控件上输出日志，主要考虑非UI线程操作</summary>
        /// <remarks>不是常用功能，为了避免干扰常用功能，保持UseWinForm开头</remarks>
        /// <param name="control">要绑定日志输出的WinForm控件</param>
        /// <param name="msg">日志</param>
        /// <param name="maxLines">最大行数</param>
        public static void UseWinFormWriteLog(Control control, String msg, Int32 maxLines = 1000)
        {
            if (control == null) return;

            var txt = control as TextBoxBase;
            if (txt == null) throw new XException("不支持的控件类型{0}！", control.GetType());

            var func = new Action<String>(m =>
            {
                try
                {
                    if (txt.Lines.Length >= maxLines) txt.Clear();

                    //// 如果不是第一行，加上空行
                    //if (txt.TextLength > 0) txt.AppendText(Environment.NewLine);
                    // 输出日志
                    if (m != null) txt.AppendText(m);

                    // 取得最后一行首字符索引
                    var p = txt.GetFirstCharIndexFromLine(txt.Lines.Length - 1);
                    if (p >= 0)
                    {
                        // 滚动到最后一行第一个字符
                        txt.Select(p, 0);
                        txt.ScrollToCaret();
                    }
                }
                catch { }
            });

            txt.Invoke(func, msg);
        }
    }
}