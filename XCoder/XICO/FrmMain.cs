using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using NewLife.IO;
using XCoder;

namespace XICO
{
    [DisplayName("图标水印处理工具")]
    public partial class FrmMain : Form, IXForm
    {
        #region 窗口初始化
        public FrmMain()
        {
            InitializeComponent();

            // 动态调节宽度高度，兼容高DPI
            this.FixDpi();

            //AllowDrop = true;
            picSrc.AllowDrop = true;

            Icon = IcoHelper.GetIcon("图标");
        }

        private void FrmMain_Shown(Object sender, EventArgs e)
        {
            //sfd.InitialDirectory = AppDomain.CurrentDomain.BaseDirectory;

            var ms = FileSource.GetFileResource(null, "XCoder.XICO.leaf.png");
            if (ms != null) picSrc.Image = new Bitmap(ms);

            var ft = lbFont.Font;
            lbFont.Tag = new Font(ft.Name, 96, ft.Style);

            MakeWater();
        }
        #endregion

        #region 水印
        private void label3_Click(Object sender, EventArgs e)
        {
            fontDialog1.Font = (Font)lbFont.Tag;
            if (fontDialog1.ShowDialog() != DialogResult.OK) return;

            var ft = fontDialog1.Font;
            lbFont.Tag = ft;
            lbFont.Font = new Font(ft.Name, lbFont.Font.Size, ft.Style);

            MakeWater();
        }

        private void label4_Click(Object sender, EventArgs e)
        {
            colorDialog1.Color = lbFont.ForeColor;
            if (colorDialog1.ShowDialog() != DialogResult.OK) return;

            lbFont.ForeColor = colorDialog1.Color;
            label4.ForeColor = colorDialog1.Color;

            MakeWater();
        }

        private void btnWater_Click(Object sender, EventArgs e)
        {
        }

        private void txt_TextChanged(Object sender, EventArgs e)
        {
            //var str = txt.Text;
            //if (String.IsNullOrEmpty(str)) str = "字体样板";
            //lbFont.Text = str;

            MakeWater();
        }

        private void numX_ValueChanged(Object sender, EventArgs e)
        {
            MakeWater();
        }

        void MakeWater()
        {
            var bmp = MakeWater(true);
            picDes.Image = bmp;
            picDes.Refresh();
        }

        Image MakeWater(Boolean fitSize)
        {
            var brush = new SolidBrush(lbFont.ForeColor);

            var bmp = picSrc.Image;
            if (fitSize && bmp.Width > picDes.Width)
                bmp = new Bitmap(bmp, picDes.Width, picDes.Height);
            else
                bmp = new Bitmap(bmp);

            if (!String.IsNullOrEmpty(txt.Text))
            {
                var ft = (Font)lbFont.Tag;

                var g = Graphics.FromImage(bmp);
                g.DrawString(txt.Text, ft, brush, (Int32)numX.Value, (Int32)numY.Value);
                g.Dispose();
            }

            return bmp;
        }

        private void btnSave_Click(Object sender, EventArgs e)
        {
            var bmp = MakeWater(true);

            //sfd.DefaultExt = "png";
            sfd.Filter = "PNG图片(*.png)|*.png";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                bmp.Save(sfd.FileName, picSrc.Image.RawFormat);
            }
        }
        #endregion

        #region 图标
        private void btnMakeICO_Click(Object sender, EventArgs e)
        {
            var list = new List<Int32>();
            foreach (var item in groupBox2.Controls)
            {
                var chk = item as CheckBox;
                if (chk != null && chk.Checked) list.Add(chk.Name.Substring(3).ToInt());
            }
            list.Sort();

            if (list.Count < 1)
            {
                MessageBox.Show("请选择大小！");
                return;
            }

            var bmp = MakeWater(true);

            var ms = new MemoryStream();
            //IconFile.Convert(bmp, ms, list.ToArray(), new Int32[] { 8, 32 });
            IconFile.Convert(bmp, ms, list.ToArray(), new Int32[] { 32 });

            //sfd.DefaultExt = "ico";
            sfd.Filter = "ICO图标(*.ico)|*.ico";
            if (sfd.ShowDialog() == DialogResult.OK)
            {
                File.WriteAllBytes(sfd.FileName, ms.ToArray());
            }
        }
        #endregion

        #region 图片加载
        private void picSrc_DragDrop(Object sender, DragEventArgs e)
        {
            var fs = (String[])e.Data.GetData(DataFormats.FileDrop);
            if (fs != null && fs.Length > 0)
            {
                var fi = fs[0];
                sfd.FileName = fi;

                // 如果是图标，读取信息
                if (fi.EndsWithIgnoreCase(".ico"))
                {
                    var ico = new IconFile(fi);
                    //ico.Sort();
                    var sb = new StringBuilder();
                    foreach (var item in ico.Items)
                    {
                        if (sb.Length > 0) sb.AppendLine();
                        sb.AppendFormat("{0}*{1}*{2}", item.Width, item.Height, item.BitCount);
                    }
                    MessageBox.Show(sb.ToString());
                }
                picSrc.Load(fi);
            }
        }

        private void picSrc_DragEnter(Object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }
        #endregion
    }
}