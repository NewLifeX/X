using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using NewLife.Log;

namespace XCoder.FolderInfo
{
    [DisplayName("文件夹大小统计")]
    public partial class FrmMain : Form, IXForm
    {
        /// <summary>业务日志输出</summary>
        ILog BizLog;

        #region 初始化
        public FrmMain()
        {
            InitializeComponent();

            // 动态调节宽度高度，兼容高DPI
            this.FixDpi();

            Icon = IcoHelper.GetIcon("文件");
        }

        private void Form1_Load(Object sender, EventArgs e)
        {
            var log = TextFileLog.Create(null, "Folder_{0:yyyy_MM_dd}.log");
            BizLog = txtLog.Combine(log);
            txtLog.UseWinFormControl();

            foreach (var item in DriveInfo.GetDrives())
            {
                if (item.DriveType == DriveType.Fixed)
                {
                    var str = String.Format("{0,-10} ({1})", item.Name, FormatSize(item.TotalSize));
                    var tn = treeView1.Nodes.Add(str);
                    tn.Tag = item.RootDirectory.ToString();
                    tn.Nodes.Add("no");
                }
            }
        }
        #endregion

        #region 构造目录树
        void MakeTree(String path, TreeNode Node)
        {
            BizLog.Info("展开目录 {0}", path);

            Node.Nodes.Clear();

            ////修正大小
            //if (Node.Text.EndsWith("-1 Byte"))
            //{
            //    Node.Text = Node.Text.Substring(0, Node.Text.Length - 8) + FormatSize(FolderSize(Node.Tag.ToString()));
            //}

            var di = new DirectoryInfo(path);
            var dis = di.GetDirectories();
            var fis = di.GetFiles();

            var list = new List<FileSystemInfo>();
            list.AddRange(dis);
            list.AddRange(fis);

            var max = 0;
            foreach (var item in list)
            {
                max = Math.Max(max, StrLen(item.Name));
            }
            max++;
            foreach (var item in list)
            {
                var len = max;
                len -= (StrLen(item.Name) - item.Name.Length);
                Int64 size = 0;

                if (item is FileInfo)
                    size = (item as FileInfo).Length;
                else//默认不统计大小，加快显示速度
                    size = -1;

                var str = String.Format("{0,-" + len.ToString() + "} {1,10}", item.Name, FormatSize(size));
                var tn = Node.Nodes.Add(str);
                tn.Tag = item;
                tn.BackColor = GetColor(size);
                tn.ContextMenuStrip = contextMenuStrip1;
                if (item is DirectoryInfo)
                {
                    tn.Nodes.Add("no");
                    //使用后台线程统计大小信息
                    Task.Factory.StartNew(() => TongJi(tn), TaskCreationOptions.LongRunning).LogException();
                }
            }
        }

        String FormatSize(Int64 size)
        {
            if (size < 1024) return size.ToString() + " Byte";
            var ds = (Double)size / 1024;
            if (ds < 1024) return ds.ToString("N2") + " K";
            ds = ds / 1024;
            if (ds < 1024) return ds.ToString("N2") + " M";
            ds = ds / 1024;
            if (ds < 1024) return ds.ToString("N2") + " G";
            ds = ds / 1024;
            if (ds < 1024) return ds.ToString("N2") + " T";
            throw new Exception("Too Large");
        }

        Int32 StrLen(String str)
        {
            return Encoding.UTF8.GetBytes(str).Length;
        }
        #endregion

        #region 统计文件夹大小
        //long FolderSize(String path)
        //{
        //    return FolderSize(new DirectoryInfo(path));
        //}

        //long FolderSize(FileSystemInfo si)
        //{
        //    return FolderSize(si as DirectoryInfo);
        //}

        Dictionary<String, Int64> cache = new Dictionary<String, Int64>();
        Int64 FolderSize(DirectoryInfo di)
        {
            Int64 size = 0;
            if (cache.ContainsKey(di.FullName)) return cache[di.FullName];
            lock (di.FullName)
            {
                if (cache.ContainsKey(di.FullName)) return cache[di.FullName];
                try
                {
                    foreach (var item in di.GetFiles())
                    {
                        size += item.Length;
                    }
                    foreach (var item in di.GetDirectories())
                    {
                        size += FolderSize(item);
                    }
                }
                catch { }
                if (!cache.ContainsKey(di.FullName)) cache.Add(di.FullName, size);
            }
            return size;
        }
        #endregion

        #region 统计目录并设置大小
        void TongJi(Object obj)
        {
            var node = obj as TreeNode;
            if (node == null || node.Tag == null) return;
            var size = FolderSize(node.Tag as DirectoryInfo);
            var str = node.Text.Substring(0, node.Text.Length - 10) + String.Format("{0,10}", FormatSize(size));
            //SetNodeText(node, str, GetColor(size));
            this.Invoke(() =>
            {
                node.Text = str;
                node.BackColor = GetColor(size);
            });
        }

        Color GetColor(Int64 size)
        {
            var color = Color.White;
            if (size > 1024) color = Color.MistyRose;
            if (size > 1024 * 1024) color = Color.LightBlue;
            if (size > 100 * 1024 * 1024) color = Color.LawnGreen;
            if (size > 1024 * 1024 * 1024) color = Color.Yellow;
            return color;
        }

        //delegate void SetNodeTextDelegate(TreeNode node, String txt, Color color);
        //void SetNodeText(TreeNode node, String txt, Color color)
        //{
        //    if (treeView1.InvokeRequired)
        //    {
        //        var d = new SetNodeTextDelegate(SetNodeText);
        //        treeView1.Invoke(d, new object[] { node, txt, color });
        //    }
        //    else
        //    {
        //        node.Text = txt;
        //        node.BackColor = color;
        //    }
        //}
        #endregion

        #region 展开折叠目录树
        private void treeView1_BeforeExpand(Object sender, TreeViewCancelEventArgs e)
        {
            var node = e.Node;
            if (node.Nodes != null && node.Nodes.Count > 0 && node.Nodes[0].Text == "no")
            {
                try
                {
                    var fi = node.Tag as FileSystemInfo;
                    if (fi != null)
                        MakeTree(fi.FullName, node);
                    else if (node.Tag is String)
                        MakeTree(node.Tag + "", node);
                }
                catch { }
            }
        }

        private void treeView1_AfterCollapse(Object sender, TreeViewEventArgs e)
        {
            // 折叠后清空，使得再次展开时重新计算
            if (e.Node != null && e.Node.Nodes != null)
            {
                e.Node.Nodes.Clear();
                e.Node.Nodes.Add("no");
            }
        }
        #endregion

        #region 右键菜单
        String GetSelectedPath()
        {
            var node = treeView1.SelectedNode;
            if (node == null || node.Tag == null) return null;

            var fi = node.Tag as FileSystemInfo;
            if (fi != null) return fi.FullName;

            return node.Tag.ToString();
        }

        private void 打开目录ToolStripMenuItem_Click(Object sender, EventArgs e)
        {
            var path = GetSelectedPath();
            if (String.IsNullOrEmpty(path)) return;

            if (path.Contains(" ")) path = "\"" + path + "\"";

            try
            {
                if (File.Exists(path))
                    Process.Start("explorer.exe /select," + path);
                else
                    Process.Start("explorer.exe " + path);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "删除出错");
            }
        }

        private void 删除ToolStripMenuItem_Click(Object sender, EventArgs e)
        {
            var path = GetSelectedPath();
            if (String.IsNullOrEmpty(path)) return;

            if (MessageBox.Show("准备删除" + path + Environment.NewLine + "删除将不可恢复，是否删除？", "确认删除", MessageBoxButtons.YesNo) == DialogResult.No) return;


            if (path.Contains(" ")) path = "\"" + path + "\"";

            try
            {
                if (File.Exists(path))
                    File.Delete(path);
                else
                    //Directory.Delete(path, true);
                    DeleteRecursive(new DirectoryInfo(path));

                treeView1.SelectedNode.Remove();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "删除出错");
            }
        }

        /// <summary>递归删除</summary>
        /// <param name="di"></param>
        void DeleteRecursive(DirectoryInfo di)
        {
            // 删除本目录文件
            foreach (var item in di.GetFiles())
            {
                BizLog.Info("删除 {0}", item.FullName);
                try
                {
                    item.Delete();
                }
                catch (Exception ex)
                {
                    BizLog.Error(ex?.GetTrue().ToString());
                }
            }
            // 递归子目录
            foreach (var item in di.GetDirectories())
            {
                DeleteRecursive(item);
            }
            try
            {
                // 删除本目录
                di.Delete(true);
            }
            catch (Exception ex)
            {
                BizLog.Error(ex?.GetTrue().ToString());
            }
        }
        #endregion
    }
}