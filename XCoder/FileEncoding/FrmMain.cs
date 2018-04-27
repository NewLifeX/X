using System;
using System.ComponentModel;
using System.IO;
using System.Text;
using System.Windows.Forms;
using NewLife.IO;

namespace XCoder.FileEncoding
{
    [DisplayName("文件编码工具")]
    public partial class FrmMain : Form, IXForm
    {
        public FrmMain()
        {
            InitializeComponent();

            // 动态调节宽度高度，兼容高DPI
            this.FixDpi();
        }

        private void FrmEncodeReplace_Load(Object sender, EventArgs e)
        {
            txtSuffix.Text = "*.cs;*.aspx";
            var encs = new String[] { "UTF-8", "UTF-8 NoBOM", "ASNI", "Unicode", "Default" };
            //var encs = new Encoding[] { Encoding.UTF8, new UTF8Encoding(false), Encoding.ASCII, Encoding.UTF8 };
            ddlEncodes.DataSource = encs;
            ddlEncodes.Text = "UTF-8";
            //cmb_tag.Text = "UTF-8";
            btnReplace.Enabled = false;
        }


        private void btn_choice_file_Click(Object sender, EventArgs e)
        {
            if (!txtPath.Text.IsNullOrEmpty()) fbd_choice_folder.SelectedPath = txtPath.Text;
            if (fbd_choice_folder.ShowDialog() != DialogResult.OK) return;

            txtPath.Text = fbd_choice_folder.SelectedPath;
        }

        private void btn_replace_Click(Object sender, EventArgs e)
        {
            if (String.IsNullOrEmpty(txtPath.Text))
            {
                MessageBox.Show("请选择文件夹");
                return;
            }

            if (gv_data.Rows[0].Cells["序号"].Value == null)
            {
                MessageBox.Show("当前没有需要替换的文件");
                return;
            }

            if (MessageBox.Show("是否确定要批量修改列表中的编码", "警告", MessageBoxButtons.YesNo, MessageBoxIcon.Question) == DialogResult.No)
                return;


            var enc = Encoding.UTF8;
            switch (ddlEncodes.Text)
            {
                case "UTF-8": enc = Encoding.UTF8; break;
                case "UTF-8 NoBOM": enc = new UTF8Encoding(false); break;
                case "ASNI": enc = Encoding.ASCII; break;
                case "Unicode": enc = Encoding.Unicode; break;
            }

            var count = 0;
            foreach (DataGridViewRow item in gv_data.Rows)
            {
                if (item.Cells["序号"].Value == null) continue;
                var fileCharset = item.Cells["编码"].Value.ToString();
                if (fileCharset.EqualIgnoreCase(ddlEncodes.Text)) continue;

                try
                {
                    //ReplaceEncoding(txtPath.Text + item.Cells["名称"].Value.ToString(), fileCharset, enc);
                    var file = txtPath.Text + item.Cells["名称"].Value;
                    var txt = File.ReadAllText(file);
                    File.WriteAllText(file, txt, enc);

                    count++;
                }
                catch (Exception ex)
                {
                    MessageBox.Show("文件[" + txtPath.Text + item.Cells["名称"].Value.ToString() + "]" + "转换时出错,请手动转换" + ex.Message);
                }
            }

            MessageBox.Show("转换{0}个文件完成".F(count));
            gv_data.Rows.Clear();
        }

        /// <summary>
        /// 替换文件编码
        /// </summary>
        /// <param name="file"></param>
        private void ReplaceEncoding(String file, String charset, Encoding targetEncoding)
        {
            var fileInfo = "";
            using (var sr = new StreamReader(file, Encoding.GetEncoding(charset), false))
            {
                fileInfo = sr.ReadToEnd();
            }

            using (var sw = new StreamWriter(file, false, targetEncoding))
            {
                sw.Write(fileInfo);
            }
        }

        Encoding GetEncode()
        {
            // "UTF-8", "ASNI", "Unicode", "Default" 
            var e = ddlEncodes.Text;

            var result = Encoding.UTF8;
            switch (e)
            {
                case "UTF-8": result = Encoding.UTF8; break;
                case "ASNI": result = Encoding.ASCII; break;
                case "Unicode": result = Encoding.Unicode; break;
            }
            return result;
        }

        /// <summary>
        /// 文件过滤
        /// </summary>
        /// <param name="info"></param>
        public void FileFilter(String path)
        {
            var di = path.AsDirectory();
            if (!di.Exists) return;

            var Count = 1;
            foreach (var file in di.GetAllFiles(txtSuffix.Text, true))
            {
                var enc = EncodePelaceHelper.GetEncoding(file.FullName);
                if (enc != null && !enc.WebName.EqualIgnoreCase(ddlEncodes.Text))
                {
                    gv_data.Rows.Add(Count++, enc.WebName, file.FullName.Substring(path.Length));
                }
            }
        }

        private void btnFind_Click(Object sender, EventArgs e)
        {
            gv_data.Rows.Clear();
            FileFilter(txtPath.Text);
            btnReplace.Enabled = true;
        }
    }

    /// <summary>
    /// 文件替换辅助类
    /// </summary>
    public class EncodePelaceHelper
    {
        /// <summary>
        /// 取得一个文本文件的编码方式。如果无法在文件头部找到有效的前导符，Encoding.UTF8将被返回。
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// 
        public static Encoding GetEncoding(String fileName)
        {
            return GetEncoding(fileName, Encoding.UTF8);
        }

        /// <summary>取得一个文本文件的编码方式。</summary>
        /// <param name="fileName">文件名。</param>
        /// <param name="defaultEncoding">默认编码方式。当该方法无法从文件的头部取得有效的前导符时，将返回该编码方式。</param>
        /// <returns></returns>
        /// 
        public static Encoding GetEncoding(String fileName, Encoding defaultEncoding)
        {
            using (var fs = File.OpenRead(fileName))
            {
                return fs.Detect() ?? defaultEncoding;
            }
        }

        ///// <summary>
        ///// 取得一个文本文件流的编码方式。
        ///// </summary>
        ///// <param name="stream">文本文件流</param>
        ///// <returns></returns>
        ///// 
        //public static Encoding GetEncoding(FileStream stream)
        //{
        //    return GetEncoding(stream, Encoding.UTF8);
        //}

        ///// <summary>
        ///// 取得一个文本文件的编码方式。
        ///// </summary>
        ///// <param name="fileName">文件名。</param>
        ///// <param name="defaultEncoding">默认编码方式。当该方法无法从文件的头部取得有效的前导符时，将返回该编码方式。</param>
        ///// <returns></returns>
        ///// 
        //public static Encoding GetEncoding(string fileName, Encoding defaultEncoding)
        //{
        //    FileStream fs = new FileStream(fileName, FileMode.Open);
        //    Encoding targetEncoding = GetEncoding(fs, defaultEncoding);
        //    fs.Close();
        //    return targetEncoding;
        //}

        /// <summary>
        /// 取得一个文本文件流的编码方式。

        /// </summary>
        /// <param name="stream">文本文件流。</param>

        /// <param name="defaultEncoding">默认编码方式。当该方法无法从文件的头部取得有效的前导符时，将返回该编码方式。</param>
        /// <returns></returns>
        /// 
        public static Encoding GetEncoding(FileStream stream, Encoding defaultEncoding)
        {
            var targetEncoding = defaultEncoding;
            if (stream != null && stream.Length >= 2)
            {
                //保存文件流的前4个字节
                Byte byte1 = 0;
                Byte byte2 = 0;
                Byte byte3 = 0;
                Byte byte4 = 0;

                //保存当前Seek位置
                var origPos = stream.Seek(0, SeekOrigin.Begin);
                stream.Seek(0, SeekOrigin.Begin);
                var nByte = stream.ReadByte();
                byte1 = Convert.ToByte(nByte);
                byte2 = Convert.ToByte(stream.ReadByte());

                if (stream.Length >= 3)
                {
                    byte3 = Convert.ToByte(stream.ReadByte());
                }

                if (stream.Length >= 4)
                {
                    byte4 = Convert.ToByte(stream.ReadByte());
                }
                //根据文件流的前4个字节判断Encoding
                //Unicode {0xFF, 0xFE};
                //BE-Unicode {0xFE, 0xFF};
                //UTF8 = {0xEF, 0xBB, 0xBF};

                if (byte1 == 0xFE && byte2 == 0xFF)//UnicodeBe
                {
                    targetEncoding = Encoding.BigEndianUnicode;
                }

                if (byte1 == 0xFF && byte2 == 0xFE && byte3 != 0xFF)//Unicode
                {
                    targetEncoding = Encoding.Unicode;
                }

                if (byte1 == 0xEF && byte2 == 0xBB && byte3 == 0xBF)//UTF8
                {
                    targetEncoding = Encoding.UTF8;
                }

                //恢复Seek位置
                stream.Seek(origPos, SeekOrigin.Begin);
            }
            return targetEncoding;
        }
    }
}
