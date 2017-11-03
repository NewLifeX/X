using NewLife;
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
    [DisplayName("小工具")]
    public partial class FrmMain : Form, IXForm
    {
        public FrmMain()
        {
            InitializeComponent();
        }
        private void FrmMain_Load(object sender, EventArgs e)
        {
            var frm = new FrmGPS();
            ShowForm(frm);
        }
        private void btn_Include_Click(object sender, EventArgs e)
        {
#if !NET4
            var frm = new FrmInclude();
            ShowForm(frm);
#endif
        }
        private void btn_gps_Click(object sender, EventArgs e)
        {
            var frm = new FrmGPS();
            ShowForm(frm);
        }
        public void ShowForm(Form frm)
        {
            frm.TopLevel = false;
            frm.Dock = DockStyle.Fill;
            frm.FormBorderStyle = FormBorderStyle.None;

            frm.Parent = this;

            var sps = panel2.Controls;
            foreach (var item in sps)
            {
                try
                {
                    var mm = item as Form;
                    if (mm != null) mm.Close();
                }
                catch { }
                item.TryDispose();
            }
            sps.Clear();
            sps.Add(frm);
            frm.Show();
        }


    }
}
