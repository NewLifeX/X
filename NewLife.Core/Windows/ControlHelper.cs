using System.Drawing;
using System.Runtime.InteropServices;
using System.Text.RegularExpressions;
using NewLife.Reflection;
using NewLife.Threading;

namespace System.Windows.Forms
{
    /// <summary>控件助手</summary>
    public static class ControlHelper
    {
        #region 在UI线程上执行委托
        /// <summary>执行无参委托</summary>
        /// <param name="control"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static void Invoke(this Control control, Func method)
        {
            if (control.IsDisposed) return;

            control.Invoke(method);
        }

        /// <summary>执行仅返回值委托</summary>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="control"></param>
        /// <param name="method"></param>
        /// <returns></returns>
        public static TResult Invoke<TResult>(this Control control, Func<TResult> method)
        {
            if (control.IsDisposed) return default(TResult);

            return (TResult)control.Invoke(method);
        }

        /// <summary>执行单一参数无返回值的委托</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="control"></param>
        /// <param name="method"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static void Invoke<T>(this Control control, Action<T> method, T arg)
        {
            if (control.IsDisposed) return;

            control.Invoke(method, arg);
        }

        /// <summary>执行单一参数和返回值的委托</summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="control"></param>
        /// <param name="method"></param>
        /// <param name="arg"></param>
        /// <returns></returns>
        public static TResult Invoke<T, TResult>(this Control control, Func<T, TResult> method, T arg)
        {
            if (control.IsDisposed) return default(TResult);

            return (TResult)control.Invoke(method, arg);
        }

        /// <summary>执行二参数无返回值的委托</summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <param name="control"></param>
        /// <param name="method"></param>
        /// <param name="arg"></param>
        /// <param name="arg2"></param>
        public static void Invoke<T, T2>(this Control control, Action<T, T2> method, T arg, T2 arg2)
        {
            if (control.IsDisposed) return;

            control.Invoke(method, arg, arg2);
        }

        /// <summary>执行二参数和返回值的委托</summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="T2"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="control"></param>
        /// <param name="method"></param>
        /// <param name="arg"></param>
        /// <param name="arg2"></param>
        /// <returns></returns>
        public static TResult Invoke<T, T2, TResult>(this Control control, Func<T, T2, TResult> method, T arg, T2 arg2)
        {
            return (TResult)control.Invoke(method, arg, arg2);
        }
        #endregion

        #region 文本控件扩展
        /// <summary>附加文本到文本控件末尾。主要解决非UI线程以及滚动控件等问题</summary>
        /// <param name="txt">控件</param>
        /// <param name="msg">消息</param>
        /// <param name="maxLines">最大行数。超过该行数讲清空控件</param>
        /// <returns></returns>
        public static TextBoxBase Append(this TextBoxBase txt, String msg, Int32 maxLines = 1000)
        {
            if (txt.IsDisposed) return txt;

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
                        ProcessBell(ref m);
                        ProcessBackspace(txt, ref m);
                        ProcessReturn(txt, ref m);

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

                    //// 取得最后一行首字符索引
                    //var lines = txt.Lines.Length;
                    //var last = lines <= 1 ? 0 : txt.GetFirstCharIndexFromLine(lines - 1);
                    //if (last >= 0)
                    //{
                    //    // 滚动到最后一行第一个字符
                    //    txt.Select(last, 0);
                    //    txt.ScrollToCaret();
                    //}

                    txt.Scroll();
                }
                catch { }
            });

            //txt.Invoke(func, msg);
            var ar = txt.BeginInvoke(func, msg);
            //ar.AsyncWaitHandle.WaitOne(100);
            //if (!ar.AsyncWaitHandle.WaitOne(10))
            //    txt.EndInvoke(ar);

            return txt;
        }

        /// <summary>滚动控件的滚动条</summary>
        /// <param name="txt">指定控件</param>
        /// <param name="bottom">是否底端，或者顶端</param>
        /// <returns></returns>
        public static TextBoxBase Scroll(this TextBoxBase txt, Boolean bottom = true)
        {
            if (txt.IsDisposed) return txt;

            SendMessage(txt.Handle, WM_VSCROLL, bottom ? SB_BOTTOM : SB_TOP, 0);

            return txt;
        }

        static void ProcessBackspace(TextBoxBase txt, ref String m)
        {
            while (true)
            {
                var p = m.IndexOf('\b');
                if (p < 0) break;

                // 计算一共有多少个字符
                var count = 1;
                while (p + count < m.Length && m[p + count] == '\b') count++;

                // 前面的字符不足，消去前面历史字符
                if (p < count)
                {
                    count -= p;
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
            }
        }

        /// <summary>处理回车，移到行首</summary>
        /// <param name="txt"></param>
        /// <param name="m"></param>
        static void ProcessReturn(TextBoxBase txt, ref String m)
        {
            while (true)
            {
                var p = m.IndexOf('\r');
                if (p < 0) break;

                // 后一个是
                if (p + 1 < m.Length && m[p + 1] == '\n')
                {
                    txt.AppendText(m.Substring(0, p + 2));
                    m = m.Substring(p + 2);
                    continue;
                }

                // 后一个不是\n，移到行首
                if (p >= 0)
                {
                    // 取得最后一行首字符索引
                    var lines = txt.Lines.Length;
                    var last = lines <= 1 ? 0 : txt.GetFirstCharIndexFromLine(lines - 1);
                    if (last >= 0)
                    {
                        // 最后一行第一个字符，删掉整行
                        txt.Select(last, txt.TextLength - last);
                        txt.SelectedText = null;
                    }
                }

                if (p + 1 == m.Length)
                {
                    m = null;
                    break;
                }
                m = m.Substring(p + 1);
            }
        }

        static void ProcessBell(ref String m)
        {
            var ch = (Char)7;
            var p = 0;
            while (true)
            {
                p = m.IndexOf(ch, p);
                if (p < 0) break;

                if (p > 0)
                {
                    var str = m.Substring(0, p);
                    if (p + 1 < m.Length) str += m.Substring(p + 1);
                    m = str;
                }

                //Console.Beep();
                // 用定时器来控制Beep，避免被堵塞
                if (_timer == null) _timer = new TimerX(Bell, null, 100, 100);
                _Beep = true;
                //SystemSounds.Beep.Play();
                p++;
            }
        }

        static TimerX _timer;
        static Boolean _Beep;
        static void Bell(Object state)
        {
            if (_Beep)
            {
                _Beep = false;
                Console.Beep();
            }
        }

        [DllImport("user32.dll")]
        static extern int SendMessage(IntPtr hwnd, int wMsg, int wParam, int lParam);
        private const int SB_TOP = 6;
        private const int SB_BOTTOM = 7;
        private const int WM_VSCROLL = 0x115;
        #endregion

        #region 设置控件样式
        /// <summary>设置默认样式，包括字体、前景色、背景色</summary>
        /// <param name="control">控件</param>
        /// <param name="size">字体大小</param>
        /// <returns></returns>
        public static Control SetDefaultStyle(this Control control, Int32 size = 10)
        {
            control.SetFontSize(size);
            control.ForeColor = Color.White;
            control.BackColor = Color.FromArgb(42, 33, 28);
            return control;
        }

        /// <summary>设置字体大小</summary>
        /// <param name="control"></param>
        /// <param name="size"></param>
        /// <returns></returns>
        public static Control SetFontSize(this Control control, Int32 size = 10)
        {
            control.Font = new Font(control.Font.FontFamily, size, GraphicsUnit.Point);
            return control;
        }
        #endregion

        #region 文本控件着色
        //Int32 _pColor = 0;
        static Color _Key = Color.FromArgb(255, 170, 0);
        static Color _Num = Color.FromArgb(255, 58, 131);
        static Color _KeyName = Color.FromArgb(0, 255, 255);

        static String[] _Keys = new String[] { 
            "(", ")", "{", "}", "[", "]", "*", "->", "+", "-", "*", "/", "\\", "%", "&", "|", "!", "=", ";", ",", ">", "<", 
            "void", "new", "delete", "true", "false" 
        };

        /// <summary>采用默认着色方案进行着色</summary>
        /// <param name="rtb">文本控件</param>
        /// <param name="start">开始位置</param>
        public static Int32 ColourDefault(this RichTextBox rtb, Int32 start)
        {
            if (start > rtb.TextLength) start = 0;
            if (start == rtb.TextLength) return start;

            // 有选择时不着色
            if (rtb.SelectionLength > 0) return start;

            //var color = Color.Yellow;
            //var color = Color.FromArgb(255, 170, 0);
            //ChangeColor("Send", color);
            foreach (var item in _Keys)
            {
                ChangeColor(rtb, start, item, _Key);
            }

            ChangeCppColor(rtb, start);
            ChangeKeyNameColor(rtb, start);
            ChangeNumColor(rtb, start);

            // 移到最后，避免瞬间有字符串写入，所以减去100
            start = rtb.TextLength;
            if (start < 0) start = 0;

            return start;
        }

        static void ChangeColor(RichTextBox rtb, Int32 start, string text, Color color)
        {
            int s = start;
            //while ((-1 + text.Length - 1) != (s = text.Length - 1 + rtx.Find(text, s, -1, RichTextBoxFinds.WholeWord)))
            while (true)
            {
                if (s >= rtb.TextLength) break;
                s = rtb.Find(text, s, -1, RichTextBoxFinds.WholeWord);
                if (s < 0) break;
                if (s > rtb.TextLength - 1) break;
                s++;

                rtb.SelectionColor = color;
                //rtx.SelectionFont = new Font(rtx.SelectionFont.FontFamily, rtx.SelectionFont.Size, FontStyle.Bold);
            }
            //rtx.Select(0, 0);
            rtb.SelectionLength = 0;
        }

        // 正则匹配，数字开头的词。支持0x开头的十六进制
        static Regex _reg = new Regex(@"(?i)\b(0x|[0-9])([0-9a-fA-F\-]*)(.*?)\b", RegexOptions.Compiled);
        static void ChangeNumColor(RichTextBox rtb, Int32 start)
        {
            //var ms = _reg.Matches(rtb.Text, start);
            //foreach (Match item in ms)
            //{
            //    rtb.Select(item.Groups[1].Index, item.Groups[1].Length);
            //    rtb.SelectionColor = _Num;

            //    rtb.Select(item.Groups[2].Index, item.Groups[2].Length);
            //    rtb.SelectionColor = _Num;

            //    rtb.Select(item.Groups[3].Index, item.Groups[3].Length);
            //    rtb.SelectionColor = _Key;
            //}
            //rtb.SelectionLength = 0;

            rtb.Colour(_reg, start, _Num, _Num, _Key);
        }

        static Regex _reg2 = new Regex(@"(?i)(\b\w+\b)(\s*::\s*)(\b\w+\b)", RegexOptions.Compiled);
        /// <summary>改变C++类名方法名颜色</summary>
        static void ChangeCppColor(RichTextBox rtb, Int32 start)
        {
            var color = Color.FromArgb(30, 154, 224);
            var color3 = Color.FromArgb(85, 228, 57);

            //var ms = _reg2.Matches(rtx.Text, start);
            //foreach (Match item in ms)
            //{
            //    rtx.Select(item.Groups[1].Index, item.Groups[1].Length);
            //    rtx.SelectionColor = color;

            //    rtx.Select(item.Groups[2].Index, item.Groups[2].Length);
            //    rtx.SelectionColor = _Key;

            //    rtx.Select(item.Groups[3].Index, item.Groups[3].Length);
            //    rtx.SelectionColor = color3;
            //}
            //rtx.SelectionLength = 0;

            rtb.Colour(_reg2, start, color, _Key, color3);
        }

        static Regex _reg3 = new Regex(@"(?i)(\b\w+\b)(\s*[=:])[^:]\s*", RegexOptions.Compiled);
        static void ChangeKeyNameColor(RichTextBox rtb, Int32 start)
        {
            //var ms = _reg3.Matches(rtx.Text, _pColor);
            //foreach (Match item in ms)
            //{
            //    rtx.Select(item.Groups[1].Index, item.Groups[1].Length);
            //    rtx.SelectionColor = _KeyName;

            //    rtx.Select(item.Groups[2].Index, item.Groups[2].Length);
            //    rtx.SelectionColor = _Key;
            //}
            //rtx.SelectionLength = 0;

            rtb.Colour(_reg3, start, _KeyName, _Key);
        }

        /// <summary>着色文本控件的内容</summary>
        /// <param name="rtb">文本控件</param>
        /// <param name="reg">正则表达式</param>
        /// <param name="start">开始位置</param>
        /// <param name="colors">颜色数组</param>
        /// <returns></returns>
        public static RichTextBox Colour(this RichTextBox rtb, Regex reg, Int32 start, params Color[] colors)
        {
            var ms = reg.Matches(rtb.Text, start);
            foreach (Match item in ms)
            {
                //rtx.Select(item.Groups[1].Index, item.Groups[1].Length);
                //rtx.SelectionColor = _KeyName;

                //rtx.Select(item.Groups[2].Index, item.Groups[2].Length);
                //rtx.SelectionColor = _Key;

                // 如果没有匹配组，说明作为整体着色
                if (item.Groups.Count <= 1)
                {
                    rtb.Select(item.Groups[0].Index, item.Groups[0].Length);
                    rtb.SelectionColor = colors[0];
                }
                else
                {
                    // 遍历匹配组，注意0号代表整体
                    for (int i = 1; i < item.Groups.Count; i++)
                    {
                        rtb.Select(item.Groups[i].Index, item.Groups[i].Length);
                        rtb.SelectionColor = colors[i - 1];
                    }
                }
            }
            rtb.SelectionLength = 0;

            return rtb;
        }

        /// <summary>着色文本控件的内容</summary>
        /// <param name="rtb">文本控件</param>
        /// <param name="reg">正则表达式</param>
        /// <param name="start">开始位置</param>
        /// <param name="colors">颜色数组</param>
        /// <returns></returns>
        public static RichTextBox Colour(this RichTextBox rtb, String reg, Int32 start, params Color[] colors)
        {
            var r = new Regex(reg, RegexOptions.Compiled);
            return Colour(rtb, r, start, colors);
        }
        #endregion
    }
}