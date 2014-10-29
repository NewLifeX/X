using System;
using System.Drawing;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using NewLife;
using NewLife.Log;
using XCoder;

namespace XCom
{
    public partial class FrmMain : Form
    {
        #region 窗体
        public FrmMain()
        {
            InitializeComponent();

            //var asmx = AssemblyX.Entry;
            //this.Text = asmx.Title;

            this.Icon = IcoHelper.GetIcon("串口");
        }

        private void FrmMain_Load(object sender, EventArgs e)
        {
            gbReceive.Tag = gbReceive.Text;
            gbSend.Tag = gbSend.Text;

            spList.ReceivedString += OnReceived;

            var menu = spList.Menu;
            txtReceive.ContextMenuStrip = menu;

            // 添加清空
            menu.Items.Insert(0, new ToolStripSeparator());
            //var ti = menu.Items.Add("清空");
            var ti = new ToolStripMenuItem("清空");
            menu.Items.Insert(0, ti);
            ti.Click += mi清空_Click;

            ti = new ToolStripMenuItem("字体");
            menu.Items.Add(ti);
            ti.Click += mi字体_Click;

            ti = new ToolStripMenuItem("前景色");
            menu.Items.Add(ti);
            ti.Click += mi前景色_Click;

            ti = new ToolStripMenuItem("背景色");
            menu.Items.Add(ti);
            ti.Click += mi背景色_Click;

            // 加载保存的颜色
            var ui = UIConfig.Current;
            if (ui != null)
            {
                try
                {
                    txtReceive.Font = ui.Font;
                    txtReceive.BackColor = ui.BackColor;
                    txtReceive.ForeColor = ui.ForeColor;
                }
                catch
                {
                    ui.Font = txtReceive.Font;
                    ui.BackColor = txtReceive.BackColor;
                    ui.ForeColor = txtReceive.ForeColor;
                }
            }
        }
        #endregion

        #region 收发数据
        void Connect()
        {
            spList.Connect();
            var st = spList.Port;
            st.FrameSize = 8;

            // 需要考虑UI线程
            st.Disconnected += (s, e) => this.Invoke(Disconnect);

            btnConnect.Text = "关闭";
        }

        void Disconnect()
        {
            spList.Port.Disconnected -= (s, e) => this.Invoke(Disconnect);
            spList.Disconnect();

            btnConnect.Text = "打开";
        }

        private void btnConnect_Click(object sender, EventArgs e)
        {
            var btn = sender as Button;
            if (btn.Text == "打开")
                Connect();
            else
                Disconnect();
        }

        void OnReceived(Object sender, EventArgs<String> e)
        {
            var line = e.Arg;
            //XTrace.UseWinFormWriteLog(txtReceive, line, 100000);
            TextControlLog.WriteLog(txtReceive, line);
        }

        Int32 lastReceive = 0;
        Int32 lastSend = 0;
        private void timer1_Tick(object sender, EventArgs e)
        {
            var sp = spList.Port;
            if (sp != null)
            {
                // 检查串口是否已断开，自动关闭已断开的串口，避免内存暴涨
                if (!spList.Enabled && btnConnect.Text == "打开")
                {
                    Disconnect();
                    return;
                }

                var rcount = spList.BytesOfReceived;
                var tcount = spList.BytesOfSent;
                if (rcount != lastReceive)
                {
                    gbReceive.Text = (gbReceive.Tag + "").Replace("0", rcount + "");
                    lastReceive = rcount;
                }
                if (tcount != lastSend)
                {
                    gbSend.Text = (gbSend.Tag + "").Replace("0", tcount + "");
                    lastSend = tcount;
                }

                ChangeColor();
            }
        }

        private void btnSend_Click(object sender, EventArgs e)
        {
            var str = txtSend.Text;
            if (String.IsNullOrEmpty(str))
            {
                MessageBox.Show("发送内容不能为空！", this.Text);
                txtSend.Focus();
                return;
            }

            // 多次发送
            var count = (Int32)numMutilSend.Value;
            for (int i = 0; i < count; i++)
            {
                spList.Send(str);

                Thread.Sleep(100);
            }
        }
        #endregion

        #region 右键菜单
        private void mi清空_Click(object sender, EventArgs e)
        {
            txtReceive.Clear();
            spList.ClearReceive();
        }

        private void mi清空2_Click(object sender, EventArgs e)
        {
            txtSend.Clear();
            spList.ClearSend();
        }

        void mi字体_Click(object sender, EventArgs e)
        {
            fontDialog1.Font = txtReceive.Font;
            if (fontDialog1.ShowDialog() != DialogResult.OK) return;

            txtReceive.Font = fontDialog1.Font;

            var ui = UIConfig.Current;
            ui.Font = txtReceive.Font;
            ui.Save();
        }

        void mi前景色_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = txtReceive.ForeColor;
            if (colorDialog1.ShowDialog() != DialogResult.OK) return;

            txtReceive.ForeColor = colorDialog1.Color;

            var ui = UIConfig.Current;
            ui.ForeColor = txtReceive.ForeColor;
            ui.Save();
        }

        void mi背景色_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = txtReceive.BackColor;
            if (colorDialog1.ShowDialog() != DialogResult.OK) return;

            txtReceive.BackColor = colorDialog1.Color;

            var ui = UIConfig.Current;
            ui.BackColor = txtReceive.BackColor;
            ui.Save();
        }
        #endregion

        #region 着色
        Int32 _pColor = 0;
        static Color _Key = Color.FromArgb(255, 170, 0);
        static Color _Num = Color.FromArgb(255, 58, 131);
        static Color _KeyName = Color.FromArgb(0, 255, 255);

        static String[] _Keys = new String[] { 
            "(", ")", "{", "}", "[", "]", "*", "->", "+", "-", "*", "/", "\\", "%", "&", "|", "!", "=", ";", ",", ">", "<", 
            "void", "new", "delete", "true", "false" 
        };

        void ChangeColor()
        {
            if (_pColor > txtReceive.TextLength) _pColor = 0;
            if (_pColor == txtReceive.TextLength) return;

            // 有选择时不着色
            if (txtReceive.SelectionLength > 0) return;

            //var color = Color.Yellow;
            //var color = Color.FromArgb(255, 170, 0);
            //ChangeColor("Send", color);
            foreach (var item in _Keys)
            {
                ChangeColor(item, _Key);
            }

            ChangeCppColor();
            ChangeKeyNameColor();
            ChangeNumColor();

            // 移到最后，避免瞬间有字符串写入，所以减去100
            _pColor = txtReceive.TextLength;
            if (_pColor < 0) _pColor = 0;
        }

        private void ChangeColor(string text, Color color)
        {
            var rtx = txtReceive;

            int s = _pColor;
            //while ((-1 + text.Length - 1) != (s = text.Length - 1 + rtx.Find(text, s, -1, RichTextBoxFinds.WholeWord)))
            while (true)
            {
                s = rtx.Find(text, s, -1, RichTextBoxFinds.WholeWord);
                if (s < 0) break;
                if (s > rtx.TextLength - 1) break;
                s++;

                rtx.SelectionColor = color;
                //rtx.SelectionFont = new Font(rtx.SelectionFont.FontFamily, rtx.SelectionFont.Size, FontStyle.Bold);
            }
            //rtx.Select(0, 0);
            rtx.SelectionLength = 0;
        }

        // 正则匹配，数字开头的词。支持0x开头的十六进制
        static Regex _reg = new Regex(@"(?i)\b(0x|[0-9])([0-9a-fA-F\-]*)(.*?)\b", RegexOptions.Compiled);
        void ChangeNumColor()
        {
            var rtx = txtReceive;

            //// 获取尾部字符串
            //var str = rtx.Text.Substring(_pColor);
            //if (str.IsNullOrWhiteSpace()) return;

            //var color = Color.Red;
            //var color = Color.FromArgb(255, 58, 131);

            var ms = _reg.Matches(rtx.Text, _pColor);
            foreach (Match item in ms)
            {
                //rtx.Select(item.Index, item.Length);
                //rtx.SelectionColor = _Num;
                rtx.Select(item.Groups[1].Index, item.Groups[1].Length);
                rtx.SelectionColor = _Num;

                rtx.Select(item.Groups[2].Index, item.Groups[2].Length);
                rtx.SelectionColor = _Num;

                rtx.Select(item.Groups[3].Index, item.Groups[3].Length);
                rtx.SelectionColor = _Key;
            }
            rtx.SelectionLength = 0;
            //rtx.Select(0, 0);
        }

        static Regex _reg2 = new Regex(@"(?i)(\b\w+\b)(\s*::\s*)(\b\w+\b)", RegexOptions.Compiled);
        /// <summary>改变C++类名方法名颜色</summary>
        void ChangeCppColor()
        {
            var rtx = txtReceive;
            var color = Color.FromArgb(30, 154, 224);
            var color3 = Color.FromArgb(85, 228, 57);

            var ms = _reg2.Matches(rtx.Text, _pColor);
            foreach (Match item in ms)
            {
                rtx.Select(item.Groups[1].Index, item.Groups[1].Length);
                rtx.SelectionColor = color;

                rtx.Select(item.Groups[2].Index, item.Groups[2].Length);
                rtx.SelectionColor = _Key;

                rtx.Select(item.Groups[3].Index, item.Groups[3].Length);
                rtx.SelectionColor = color3;
            }
            rtx.SelectionLength = 0;
        }

        static Regex _reg3 = new Regex(@"(?i)(\b\w+\b)(\s*[=:])[^:]\s*", RegexOptions.Compiled);
        void ChangeKeyNameColor()
        {
            var rtx = txtReceive;

            var ms = _reg3.Matches(rtx.Text, _pColor);
            foreach (Match item in ms)
            {
                rtx.Select(item.Groups[1].Index, item.Groups[1].Length);
                rtx.SelectionColor = _KeyName;

                rtx.Select(item.Groups[2].Index, item.Groups[2].Length);
                rtx.SelectionColor = _Key;
            }
            rtx.SelectionLength = 0;
        }
        #endregion
    }
}