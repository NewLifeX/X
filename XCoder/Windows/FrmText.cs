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
            FrmText frm = new FrmText();
            if (!String.IsNullOrEmpty(title)) frm.Text = title;
            frm.richTextBox1.Text = content;

            return frm;
        }
        #endregion
    }
}