using System;
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

        private Int32 _MaxLines = 1000;
        /// <summary>最大行数，超过该行数讲清空文本控件。默认1000行</summary>
        public Int32 MaxLines { get { return _MaxLines; } set { _MaxLines = value; } }

        //private Boolean _Timestamp;
        ///// <summary>是否输出时间戳</summary>
        //public Boolean Timestamp { get { return _Timestamp; } set { _Timestamp = value; } }

        /// <summary>写日志</summary>
        /// <param name="level"></param>
        /// <param name="format"></param>
        /// <param name="args"></param>
        protected override void OnWrite(LogLevel level, String format, params Object[] args)
        {
            //var e = WriteLogEventArgs.Current.Set(level, Format(format, args), null, true);
            //WriteLog(Control, e.ToString(), MaxLines);

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

            var func = new Action<String>(m =>
            {
                try
                {
                    if (txt.Lines.Length >= maxLines) txt.Clear();

                    //// 如果不是第一行，加上空行
                    //if (txt.TextLength > 0) txt.AppendText(Environment.NewLine);

                    //// 提前取得光标所在行索引，决定后面要不要滚动
                    //var cur = txt.GetFirstCharIndexOfCurrentLine();
                    //var line = txt.GetLineFromCharIndex(cur);
                    // AppendText本身就会让文本滚动到最后，不需要额外的滚动代码

                    // 记录原原则
                    var selstart = txt.SelectionStart;
                    var sellen = txt.SelectionLength;

                    // 输出日志
                    if (m != null)
                    {
                        //txt.AppendText(m);
                        // 需要考虑处理特殊符号
                        ProcessBackspace(txt, ref m);

                        if (String.IsNullOrEmpty(m)) return;
                        txt.AppendText(m);
                    }

                    // 如果有选择，则不要滚动
                    if (sellen > 0)
                    {
                        // 恢复选择
                        if (selstart < txt.TextLength)
                        {
                            sellen = Math.Min(sellen, txt.TextLength - selstart - 1);
                            txt.Select(selstart, sellen);
                            txt.ScrollToCaret();
                        }

                        return;
                    }

                    // 5行内滚动
                    //if (line + 5 > txt.Lines.Length && txt.SelectionLength <= 0)
                    //{
                    // 取得最后一行首字符索引
                    var lines = txt.Lines.Length;
                    var last = lines <= 1 ? 0 : txt.GetFirstCharIndexFromLine(lines - 1);
                    if (last >= 0)
                    {
                        // 滚动到最后一行第一个字符
                        txt.Select(last, 0);
                        txt.ScrollToCaret();
                    }
                    //}
                }
                catch { }
            });

            txt.Invoke(func, msg);
            //var ar = txt.BeginInvoke(func, msg);
            //ar.AsyncWaitHandle.WaitOne(100);
            //if (!ar.AsyncWaitHandle.WaitOne(10))
            //    txt.EndInvoke(ar);
        }

        static void ProcessBackspace(TextBoxBase txt, ref String m)
        {
            var size = m.Length;
            var p = m.IndexOf('\b');
            while (p >= 0)
            {
                // 计算一共有多少个字符
                var count = 1;
                while (p + count < m.Length && m[p + count] == '\b') count++;

                // 前面的字符不足，消去前面历史字符
                if (p < count)
                {
                    count -= p;
                    if (count < 21)
                    {
                        size = size - count;
                    }
                    // 选中最后字符，然后干掉它
                    if (txt.TextLength > count)
                    {
                        txt.Select(txt.TextLength - count, count);
                        txt.SelectedText = null;
                    }
                    else
                        txt.Clear();
                }
                else if (p > count)
                {
                    // 少输出一个
                    txt.AppendText(m.Substring(0, p - count));
                }

                if (p == m.Length - count)
                {
                    m = null;
                    break;
                }
                m = m.Substring(p + count);
                p = m.IndexOf('\b');
            }
        }
    }
}