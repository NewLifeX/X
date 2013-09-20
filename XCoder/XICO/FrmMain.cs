using System;
using System.Drawing;
using System.Windows.Forms;
using NewLife.IO;

namespace XICO
{
    public partial class FrmMain : Form
    {
        public FrmMain()
        {
            InitializeComponent();

            //AllowDrop = true;
            picSrc.AllowDrop = true;
        }

        private void label3_Click(object sender, EventArgs e)
        {
            fontDialog1.Font = lbFont.Font;
            if (fontDialog1.ShowDialog() != DialogResult.OK) return;

            lbFont.Font = fontDialog1.Font;
        }

        private void label4_Click(object sender, EventArgs e)
        {
            colorDialog1.Color = lbFont.ForeColor;
            if (colorDialog1.ShowDialog() != DialogResult.OK) return;

            lbFont.ForeColor = colorDialog1.Color;
            label4.ForeColor = colorDialog1.Color;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            var brush = new SolidBrush(lbFont.ForeColor);

            var bmp = new Bitmap(picSrc.Image);
            var g = Graphics.FromImage(bmp);
            //g.DrawImage(picSrc.Image, 0, 0);
            g.DrawString(txt.Text, lbFont.Font, brush, 0, 0);
            g.Dispose();

            picDes.Image = bmp;
            picDes.Refresh();
        }

        private void button2_Click(object sender, EventArgs e)
        {

        }

        private void txt_TextChanged(object sender, EventArgs e)
        {
            var str = txt.Text;
            if (String.IsNullOrEmpty(str)) str = "字体样板";
            lbFont.Text = str;
        }

        private void picSrc_DragDrop(object sender, DragEventArgs e)
        {
            var fs = (String[])e.Data.GetData(DataFormats.FileDrop);
            if (fs != null && fs.Length > 0)
            {
                try
                {
                    picSrc.Load(fs[0]);
                }
                catch { }
            }
        }

        private void picSrc_DragEnter(object sender, DragEventArgs e)
        {
            if (e.Data.GetDataPresent(DataFormats.FileDrop))
                e.Effect = DragDropEffects.Link;
            else
                e.Effect = DragDropEffects.None;
        }

        private void FrmMain_Shown(object sender, EventArgs e)
        {
            var ms = FileSource.GetFileResource(null, "XCoder.XICO.leaf.png");
            if (ms != null) picSrc.Image = new Bitmap(ms);
        }
    }
}