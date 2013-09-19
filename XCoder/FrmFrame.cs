using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;

namespace XCoder
{
    public partial class FrmFrame : Form
    {
        public FrmFrame()
        {
            InitializeComponent();
        }

        private void FrmFrame_Load(object sender, EventArgs e)
        {
            var frm = new FrmMain();
            frm.Parent = this;

            this.Controls.Add(frm);
        }
    }
}
