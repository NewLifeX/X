using System;
using System.Windows.Forms;

namespace XCoder
{
    public partial class FrmText : Form
    {
        #region 界面初始化
        public FrmText()
        {
            InitializeComponent();

            this.Icon = FileSource.GetIcon();
        }

        public static FrmText Create(String title, String content)
        {
            var frm = new FrmText();
            if (!String.IsNullOrEmpty(title)) frm.Text = title;
            frm.Content = content;

            return frm;
        }
        #endregion

        /// <summary>内容</summary>
        public String Content { get { return richTextBox1.Text; } set { richTextBox1.Text = ("" + value).Replace("\t", "    "); } }
    }
}