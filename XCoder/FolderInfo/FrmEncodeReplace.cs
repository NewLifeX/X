using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using XCoder;
using System.Windows.Forms;
using System.IO;

namespace XCoder.FolderInfo
{
    public partial class FrmEncodeReplace : Form
    {
        string[] EncodeType = new string[] { "UTF-8", "ASNI", "Unicode", "Default" };
        private string ChiocePath { get; set; }

        private Int32 Count { get; set; }
        public FrmEncodeReplace()
        {
            InitializeComponent();
        }

        private void FrmEncodeReplace_Load(object sender, EventArgs e)
        {
            txt_file_suffix_name.Text = ".cs,.aspx";
            cmb_file_encode_name.DataSource = EncodeType;
            cmb_file_encode_name.Text = "UTF-8";
            //cmb_tag.Text = "UTF-8";
            btn_replace.Enabled = false;
        }


        private void btn_choice_file_Click(object sender, EventArgs e)
        {
            if (fbd_choice_folder.ShowDialog() != DialogResult.OK) return;

            gv_data.Rows.Clear();
            txt_file_path.Text = fbd_choice_folder.SelectedPath;
            ChiocePath = txt_file_path.Text;
            Count = 1;
            FileFilter(new DirectoryInfo(ChiocePath));
            btn_replace.Enabled = true;
        }

        private void btn_replace_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txt_file_path.Text))
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

            foreach (DataGridViewRow item in gv_data.Rows)
            {
                if (item.Cells["序号"].Value == null)
                    continue;
                try
                {
                    ReplaceEncoding(txt_file_path.Text + item.Cells["名称"].Value.ToString());
                }
                catch (Exception ex)
                {
                    MessageBox.Show("文件[" + txt_file_path.Text + item.Cells["名称"].Value.ToString() + "]" + "转换时出错,请手动转换" + ex.Message);
                }
            }

            MessageBox.Show("转换完成");
            gv_data.Rows.Clear();
        }

        /// <summary>
        /// 替换文件编码
        /// </summary>
        /// <param name="v"></param>
        private void ReplaceEncoding(string v)
        {
            string fileInfo = "";
            using (StreamReader sr = new StreamReader(v, Encoding.Default, false))
            {
                fileInfo = sr.ReadToEnd();
            }

            using (StreamWriter sw = new StreamWriter(v, false, GetEncode()))
            {
                sw.Write(fileInfo);
            }
        }

        Encoding GetEncode()
        {
            // "UTF-8", "ASNI", "Unicode", "Default" 
            var e = cmb_file_encode_name.Text;

            var result = Encoding.Default;
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
        public void FileFilter(FileSystemInfo info)
        {
            //var info = new DirectoryInfo(ChiocePath);
            if (!info.Exists) return;

            var dir = info as DirectoryInfo;
            //不是目录
            if (dir == null) return;

            var files = dir.GetFileSystemInfos();

            for (int i = 0; i < files.Length; i++)
            {
                FileInfo file = files[i] as FileInfo;
                //是文件
                if (file != null)
                {
                    if (!file.Name.Contains(".")) continue;
                    var b = IsContainsType(file.Name);
                    if (b)
                    {
                        string fileEncoding = EncodePelaceHelper.GetEncoding(file.FullName).EncodingName;
                        if (fileEncoding.ToUpper().IndexOf("UTF".ToUpper()) < 0)
                        {
                            gv_data.Rows.Add(Count, fileEncoding, file.FullName.ToString().Substring(ChiocePath.Length));
                            Count++;
                        }
                    }
                }
                //对于子目录，进行递归调用
                else
                {
                    FileFilter(files[i]);
                }
            }
        }

        /// <summary>
        /// 判断是否包含对应类型文件
        /// </summary>
        bool IsContainsType(string filename)
        {
            var t = txt_file_suffix_name.Text;
            if (string.IsNullOrEmpty(t)) return true;

            var s = t.Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);
            string fileLastName = filename.Substring(filename.LastIndexOf(".")).ToUpper();

            foreach (var item in s)
            {
                if (fileLastName.IndexOf(item.ToUpper()) >= 0) return true;
            }

            return false;
        }


    }

    /// <summary>
    /// 文件替换辅助类
    /// </summary>
    public class EncodePelaceHelper
    {
        /// <summary>
        /// 取得一个文本文件的编码方式。如果无法在文件头部找到有效的前导符，Encoding.Default将被返回。
        /// </summary>
        /// <param name="fileName">文件名</param>
        /// 
        public static Encoding GetEncoding(string fileName)
        {
            return GetEncoding(fileName, Encoding.Default);
        }

        /// <summary>
        /// 取得一个文本文件流的编码方式。
        /// </summary>
        /// <param name="stream">文本文件流</param>
        /// <returns></returns>
        /// 
        public static Encoding GetEncoding(FileStream stream)
        {
            return GetEncoding(stream, Encoding.Default);
        }

        /// <summary>
        /// 取得一个文本文件的编码方式。
        /// </summary>
        /// <param name="fileName">文件名。</param>
        /// <param name="defaultEncoding">默认编码方式。当该方法无法从文件的头部取得有效的前导符时，将返回该编码方式。</param>
        /// <returns></returns>
        /// 
        public static Encoding GetEncoding(string fileName, Encoding defaultEncoding)
        {
            FileStream fs = new FileStream(fileName, FileMode.Open);
            Encoding targetEncoding = GetEncoding(fs, defaultEncoding);
            fs.Close();
            return targetEncoding;
        }

        /// <summary>
        /// 取得一个文本文件流的编码方式。

        /// </summary>
        /// <param name="stream">文本文件流。</param>

        /// <param name="defaultEncoding">默认编码方式。当该方法无法从文件的头部取得有效的前导符时，将返回该编码方式。</param>
        /// <returns></returns>
        /// 
        public static Encoding GetEncoding(FileStream stream, Encoding defaultEncoding)
        {
            Encoding targetEncoding = defaultEncoding;
            if (stream != null && stream.Length >= 2)
            {
                //保存文件流的前4个字节
                byte byte1 = 0;
                byte byte2 = 0;
                byte byte3 = 0;
                byte byte4 = 0;

                //保存当前Seek位置
                long origPos = stream.Seek(0, SeekOrigin.Begin);
                stream.Seek(0, SeekOrigin.Begin);
                int nByte = stream.ReadByte();
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
