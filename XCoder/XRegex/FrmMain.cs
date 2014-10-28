using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Windows.Forms;
using XCoder;
using XCoder.XRegex;

namespace NewLife.XRegex
{
    public partial class FrmMain : Form
    {
        #region 窗体初始化
        public FrmMain()
        {
            InitializeComponent();

            this.Icon = IcoHelper.GetIcon("正则");

            FileResource.CheckTemplate();
        }

        private void FrmMain_Shown(object sender, EventArgs e)
        {
            //Text += " V" + FileVersion;

            GetOption();
        }

        private void FrmMain_FormClosing(object sender, FormClosingEventArgs e)
        {
            // 关闭前保存正则和内容
            Save(txtPattern.Text, "Pattern");
            Save(txtContent.Text, "Sample");
        }

        void Save(String content, String name)
        {
            if (String.IsNullOrEmpty(content)) return;

            String p = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, name);
            p = Path.Combine(p, "最近");

            #region 预处理
            if (Directory.Exists(p))
            {
                // 拿出所有文件
                String[] files = Directory.GetFiles(p, "*.txt", SearchOption.TopDirectoryOnly);
                if (files != null && files.Length > 0)
                {
                    // 内容比对
                    List<String> list = new List<String>();
                    foreach (String item in files)
                    {
                        String content2 = File.ReadAllText(item);
                        if (content2 == content)
                            File.Delete(item);
                        else
                            list.Add(item);
                    }

                    // 是否超标
                    if (list.Count >= 10)
                    {
                        // 文件排序
                        list.Sort();

                        // 删除最后的
                        for (int i = list.Count - 1; i >= 10; i--)
                        {
                            File.Delete(list[i]);
                        }
                    }
                }
            }
            else
                Directory.CreateDirectory(p);
            #endregion

            // 写入
            String file = String.Format("{0:yyyy-MM-dd_HHmmss}.txt", DateTime.Now);
            file = Path.Combine(p, file);
            File.WriteAllText(file, content);
        }
        #endregion

        #region 正则匹配
        private void radioButton1_CheckedChanged(object sender, EventArgs e)
        {
            RadioButton rb = sender as RadioButton;
            if (rb.Checked)
            {
                button2.Text = "正则匹配";
                groupBox3.Text = "匹配结果";
                splitContainer3.Visible = true;
                rtReplace.Visible = false;
                panel1.Visible = false;
            }
            else
            {
                button2.Text = "正则替换";
                groupBox3.Text = "替换内容";
                splitContainer3.Visible = false;
                rtReplace.Visible = true;
                panel1.Visible = true;
            }
        }

        void GetReg(out String pattern, out RegexOptions options)
        {
            pattern = txtPattern.SelectedText;
            options = RegexOptions.None;

            if (String.IsNullOrEmpty(txtPattern.Text)) return;
            if (String.IsNullOrEmpty(pattern)) pattern = txtPattern.Text;

            if (chkIgnoreCase.Checked) options |= RegexOptions.IgnoreCase;
            if (chkMultiline.Checked) options |= RegexOptions.Multiline;
            if (chkIgnorePatternWhitespace.Checked) options |= RegexOptions.IgnorePatternWhitespace;
            if (chkSingleline.Checked) options |= RegexOptions.Singleline;
        }

        Boolean ProcessAsync(Action<Regex> callback, Int32 timeout)
        {
            String pattern;
            RegexOptions options;
            GetReg(out pattern, out options);

            Boolean isSuccess = false;
            AutoResetEvent e = new AutoResetEvent(false);
            Thread thread = new Thread(new ThreadStart(delegate
            {
                try
                {
                    // 如果正则表达式过于复杂，创建对象时可能需要花很长时间，甚至超时
                    Regex reg = new Regex(pattern, options);
                    callback(reg);

                    isSuccess = true;
                }
                catch (ThreadAbortException) { }
                catch (Exception ex)
                {
                    ShowError(ex.Message);
                }
                finally
                {
                    e.Set();
                }
            }));

            lbStatus.Text = "准备开始！";
            thread.Start();
            //if (!are.WaitOne(5000))
            //{
            //    thread.Abort();
            //    ShowError("执行正则表达式超时！");
            //    return;
            //}
            Boolean b = false;
            for (int i = 0; i < timeout / 100; i++)
            {
                if (e.WaitOne(100))
                {
                    b = true;
                    break;
                }

                lbStatus.Text = String.Format("正在处理…… {0:n1}s/{1:n1}s", (Double)i / 10, timeout / 1000);
            }
            if (!b)
            {
                thread.Abort();
                ShowError("执行正则表达式超时！");
            }
            return isSuccess;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(txtPattern.Text)) return;
            String content = txtContent.Text;
            if (String.IsNullOrEmpty(content)) return;

            // 是否替换
            Boolean isReplace = !radioButton1.Checked;
            String replacement = rtReplace.Text;
            if (isReplace && String.IsNullOrEmpty(replacement)) return;

            Regex r = null;
            MatchCollection ms = null;
            Int32 count = 0;

            // 异步执行，防止超时
            Boolean isSuccess = ProcessAsync(delegate(Regex reg)
            {
                r = reg;
                ms = reg.Matches(content);
                if (ms != null) count = ms.Count;
                if (isReplace && count > 0) content = reg.Replace(content, replacement);
            }, 5000);

            lvMatch.Tag = r;
            lvMatch.Items.Clear();
            lvGroup.Items.Clear();
            lvCapture.Items.Clear();

            if (!isSuccess || count < 1) return;

            lbStatus.Text = String.Format("成功{0} {1} 项！", !isReplace ? "匹配" : "替换", count);

            int i = 1;
            foreach (Match match in ms)
            {
                var item = lvMatch.Items.Add(i.ToString());
                item.SubItems.Add(match.Value);
                item.SubItems.Add(String.Format("({0},{1},{2})", txtContent.GetLineFromCharIndex(match.Index), match.Index, match.Length));
                item.Tag = match;
                i++;
            }

            if (isReplace) txtContent.Text = content;
        }

        private void button5_Click(object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(txtPattern.Text)) return;
            String replacement = rtReplace.Text;
            if (String.IsNullOrEmpty(replacement)) return;

            // 获取文件
            String[] files = GetFiles();
            lbStatus.Text = String.Format("共有符合条件的文件 {0} 个！", files.Length);

            MatchCollection ms = null;
            Int32 count = 0;

            // 异步执行，防止超时
            Boolean isSuccess = ProcessAsync(delegate(Regex reg)
            {
                foreach (String item in files)
                {
                    String content = File.ReadAllText(item);
                    ms = reg.Matches(content);
                    if (ms != null) count = ms.Count;
                    // 有匹配项才替换
                    if (count > 0)
                    {
                        String content2 = reg.Replace(content, replacement);
                        // 有改变才更新文件
                        if (content != content2) File.WriteAllText(item, content2);
                    }
                }
            }, 1000 * files.Length);

            if (!isSuccess || count < 1) return;

            lbStatus.Text = String.Format("成功替换 {0} 项！", count);

            txtContent.Text = File.ReadAllText(files[0]);
        }
        #endregion

        #region 选择匹配项
        private void lvMatch_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvMatch.SelectedItems == null || lvMatch.SelectedItems.Count < 1) return;

            // 当前选择项
            Match m = lvMatch.SelectedItems[0].Tag as Match;
            //rtContent.SelectedText = m.Value;
            txtContent.Select(m.Index, m.Length);
            txtContent.ScrollToCaret();

            // 分组的选择是否与当前一致
            Match m2 = lvGroup.Tag as Match;
            if (m2 != null && m2 == m) return;

            lvGroup.Tag = m;
            lvGroup.Items.Clear();
            lvCapture.Items.Clear();

            var reg = lvMatch.Tag as Regex;
            for (int i = 0; i < m.Groups.Count; i++)
            {
                var g = m.Groups[i];
                var item = lvGroup.Items.Add(i.ToString());
                item.SubItems.Add(reg.GroupNameFromNumber(i));
                item.SubItems.Add(g.Value);
                item.SubItems.Add(String.Format("({0},{1},{2})", txtContent.GetLineFromCharIndex(g.Index), g.Index, g.Length));
                item.Tag = g;
            }
        }

        private void lvGroup_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvGroup.SelectedItems == null || lvGroup.SelectedItems.Count < 1) return;

            // 当前选择项
            Group g = lvGroup.SelectedItems[0].Tag as Group;
            //rtContent.SelectedText = g.Value;
            txtContent.Select(g.Index, g.Length);
            txtContent.ScrollToCaret();

            // 分组的选择是否与当前一致
            Group g2 = lvCapture.Tag as Group;
            if (g2 != null && g2 == g) return;

            lvCapture.Tag = g;
            lvCapture.Items.Clear();

            for (int i = 0; i < g.Captures.Count; i++)
            {
                var c = g.Captures[i];
                var item = lvCapture.Items.Add(i.ToString());
                item.SubItems.Add(c.Value);
                item.SubItems.Add(String.Format("({0},{1},{2})", txtContent.GetLineFromCharIndex(c.Index), c.Index, c.Length));
                item.Tag = g.Captures[i];
            }
        }

        private void lvCapture_SelectedIndexChanged(object sender, EventArgs e)
        {
            if (lvCapture.SelectedItems == null || lvCapture.SelectedItems.Count < 1) return;

            // 当前选择项
            Capture c = lvCapture.SelectedItems[0].Tag as Capture;
            txtContent.SelectedText = c.Value;
            txtContent.Select(c.Index, c.Length);
            txtContent.ScrollToCaret();
        }
        #endregion

        #region 辅助函数
        void ShowError(String msg)
        {
            MessageBox.Show(msg, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
        }

        private static String _FileVersion;
        /// <summary>
        /// 文件版本
        /// </summary>
        public static String FileVersion
        {
            get
            {
                if (String.IsNullOrEmpty(_FileVersion))
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    AssemblyFileVersionAttribute av = Attribute.GetCustomAttribute(asm, typeof(AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute;
                    if (av != null) _FileVersion = av.Version;
                    if (String.IsNullOrEmpty(_FileVersion)) _FileVersion = "1.0";
                }
                return _FileVersion;
            }
        }

        ///// <summary>
        ///// 找兄弟控件
        ///// </summary>
        ///// <typeparam name="T"></typeparam>
        ///// <param name="src"></param>
        ///// <returns></returns>
        //T FindBrotherControl<T>(Control src) where T : Control
        //{

        //}
        #endregion

        #region 菜单
        private void ptMenu_Opening(object sender, CancelEventArgs e)
        {
            for (int i = ptMenu.Items.Count - 1; i >= 1; i--)
            {
                ptMenu.Items.RemoveAt(i);
            }
            LoadMenuTree(ptMenu.Items, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Pattern"));
        }

        private void txtMenu_Opening(object sender, CancelEventArgs e)
        {
            for (int i = txtMenu.Items.Count - 1; i >= 1; i--)
            {
                txtMenu.Items.RemoveAt(i);
            }
            LoadMenuTree(txtMenu.Items, Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Sample"));
        }

        /// <summary>
        /// 加载菜单树
        /// </summary>
        /// <param name="items"></param>
        /// <param name="path"></param>
        void LoadMenuTree(ToolStripItemCollection items, String path)
        {
            // 遍历目录
            var dirs = Directory.GetDirectories(path, "*", SearchOption.TopDirectoryOnly);
            if (dirs != null && dirs.Length > 0)
            {
                foreach (var item in dirs)
                {
                    var name = Path.GetFileName(item);
                    //var sep = Path.DirectorySeparatorChar + "";
                    //var p = name.LastIndexOf(sep);
                    //if (p >= 0) name = name.Substring(p + 1);

                    var menu = new ToolStripMenuItem(name);
                    menu.Click += MenuItem_Click;

                    LoadMenuTree(menu.DropDownItems, Path.Combine(path, item));

                    // 有子项目才加入菜单
                    if (menu.DropDownItems.Count > 0) items.Add(menu);
                }
            }
            // 遍历文件
            String[] files = Directory.GetFiles(path, "*.txt", SearchOption.TopDirectoryOnly);
            if (files != null && files.Length > 0)
            {
                foreach (String item in files)
                {
                    ToolStripMenuItem menu = new ToolStripMenuItem(Path.GetFileNameWithoutExtension(item));
                    menu.Click += MenuItem_Click;
                    menu.Tag = Path.Combine(path, item);

                    items.Add(menu);
                }
            }
        }

        private void MenuItem_Click(object sender, EventArgs e)
        {
            ToolStripMenuItem menu = sender as ToolStripMenuItem;
            if (menu == null) return;

            // 找文件路径
            String file = (String)menu.Tag;
            if (String.IsNullOrEmpty(file)) return;
            if (!File.Exists(file)) return;

            // 找内容区域
            ToolStripItem item = menu;
            while (item.OwnerItem != null) item = item.OwnerItem;

            ContextMenuStrip cms = item.Owner as ContextMenuStrip;
            if (cms == null) return;

            TextBoxBase rt = null;
            if (txtPattern.ContextMenuStrip == cms)
                rt = txtPattern;
            else if (txtContent.ContextMenuStrip == cms)
                rt = txtContent;

            if (rt == null) return;

            String content = File.ReadAllText(file);
            rt.SelectedText = content;
        }
        #endregion

        #region 打开目录
        private void button4_Click(object sender, EventArgs e)
        {
            Button btn = sender as Button;
            GetFolder(btn);
            if (!String.IsNullOrEmpty(folderBrowserDialog1.SelectedPath)) GetFiles();
        }

        void GetFolder(Button btn)
        {
            FolderBrowserDialog fb = folderBrowserDialog1;

            if (fb.ShowDialog() != DialogResult.OK) return;
            btn.Tag = fb.SelectedPath;
            toolTip1.SetToolTip(btn, fb.SelectedPath);

            lbStatus.Text = String.Format("选择目录：{0}", fb.SelectedPath);
        }

        String[] GetFiles()
        {
            // 获取目录，如果没有选择，则打开选择
            Button btn = button4;
            String path = btn.Tag == null ? null : (String)btn.Tag;
            if (String.IsNullOrEmpty(path))
            {
                GetFolder(btn);
                path = btn.Tag == null ? null : (String)btn.Tag;
            }
            // 如果还是没有目录，退出
            if (String.IsNullOrEmpty(path) || !Directory.Exists(path)) return null;

            // 获取文件
            String[] files = Directory.GetFiles(path, textBox2.Text, SearchOption.AllDirectories);
            if (files == null || files.Length < 1)
            {
                ShowError("没有符合条件的文件！");
                return null;
            }
            lbStatus.Text = String.Format("共有符合条件的文件 {0} 个！", files.Length);

            // 第一个文件内容进入内容窗口
            try
            {
                txtContent.Text = File.ReadAllText(files[0]);
            }
            catch { }

            return files;
        }
        #endregion

        #region 正则选项
        private void checkBox1_CheckedChanged(object sender, EventArgs e)
        {
            var chk = sender as CheckBox;
            if (chk == null) return;

            GetOption();
        }

        void GetOption()
        {
            var sb = new StringBuilder();
            foreach (var item in chkIgnoreCase.Parent.Controls)
            {
                var chk = item as CheckBox;
                if (chk != null && chk.Checked)
                {
                    if (sb.Length > 0) sb.Append(" | ");

                    var name = chk.Name;
                    if (name.StartsWith("chk")) name = name.Substring(3);

                    sb.AppendFormat("RegexOptions.{0}", name);
                }
            }

            txtOption.Text = sb.ToString();
        }
        #endregion
    }
}