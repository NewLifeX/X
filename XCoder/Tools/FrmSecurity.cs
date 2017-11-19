using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace XCoder.Tools
{
    [DisplayName("加密解密")]
    public partial class FrmSecurity : Form, IXForm
    {
        public FrmSecurity()
        {
            InitializeComponent();
        }

        #region 辅助
        #endregion

        private void btnHex_Click(Object sender, EventArgs e)
        {
            var v = rtSource.Text;
            var rs = v.GetBytes();
            v = rs.ToHex(" ", 32);
            rtResult.Text = v;
        }

        private void btnHex2_Click(Object sender, EventArgs e)
        {

        }

        private void btnExchange_Click(Object sender, EventArgs e)
        {
            var v = rtSource.Text;
            rtSource.Text = rtResult.Text;
            rtResult.Text = v;
        }
    }
}
