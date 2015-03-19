using System;
using NewLife.Windows;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows.Forms;
using XCoder;
using NewLife.Log;

namespace FolderInfo
{
    public partial class FrmMain : Form
    {
        #region 初始化
        public FrmMain()
        {
            InitializeComponent();

            this.Icon = IcoHelper.GetIcon("文件");
        }

        private void Form1_Load(object sender, EventArgs e)
        {
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
            XTrace.WriteLine("展开目录 {0}", path);

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

            int max = 0;
            foreach (var item in list)
            {
                max = Math.Max(max, StrLen(item.Name));
            }
            max++;
            foreach (var item in list)
            {
                Int32 len = max;
                len -= (StrLen(item.Name) - item.Name.Length);
                long size = 0;

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
                    ThreadPool.QueueUserWorkItem(TongJi, tn);
                }
            }
        }

        String FormatSize(long size)
        {
            if (size < 1024) return size.ToString() + " Byte";
            Double ds = (double)size / 1024;
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
            return Encoding.Default.GetBytes(str).Length;
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

        Dictionary<String, long> cache = new Dictionary<String, long>();
        long FolderSize(DirectoryInfo di)
        {
            long size = 0;
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
            long size = FolderSize(node.Tag as DirectoryInfo);
            var str = node.Text.Substring(0, node.Text.Length - 10) + String.Format("{0,10}", FormatSize(size));
            //SetNodeText(node, str, GetColor(size));
            this.Invoke(() =>
            {
                node.Text = str;
                node.BackColor = GetColor(size);
            });
        }

        Color GetColor(long size)
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
        private void treeView1_BeforeExpand(object sender, TreeViewCancelEventArgs e)
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

        private void treeView1_AfterCollapse(object sender, TreeViewEventArgs e)
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

        private void 打开目录ToolStripMenuItem_Click(object sender, EventArgs e)
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

        private void 删除ToolStripMenuItem_Click(object sender, EventArgs e)
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
                XTrace.WriteLine("删除 {0}", item.FullName);
                try
                {
                    item.Delete();
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                }
            }
            // 递归子目录
            foreach (var item in di.GetDirectories())
            {
                DeleteRecursive(item);
            }
            // 删除本目录
            di.Delete(true);
        }
        #endregion
    }
}