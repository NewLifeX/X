using System;
using System.Collections.Generic;
using System.IO;
using System.Windows.Forms;
using NewLife.Model;
using XCode.DataAccessLayer;

namespace XCoder
{
    public partial class NewModel : UserControl
    {
        private List<IDataTable> _tables;

        public List<IDataTable> Tables
        {
            get { return _tables; }
            set { _tables = value; }
        }
        

        public NewModel()
        {
            InitializeComponent();
            Tables = new List<IDataTable>();
        }

        public static BaseForm CreateForm()
        {
            NewModel frm = new NewModel();
            frm.Dock = DockStyle.Fill;

            return WinFormHelper.CreateForm(frm, "添加模型");
        }

        private void toolAddTable_Click(object sender, EventArgs e)
        {
            //为了触发XCodeService的静态构造函数
            var temp = ModelResolver.Current;
            if (temp == null) return;

            IDataTable current = ObjectContainer.Current.Resolve <IDataTable >();
            Tables.Add(current);
            current.ID = Tables.Count;
            current.TableName = "NewTable" + current.ID;
            current.Description = "新建表" + current.ID;
            current.DbType = DatabaseType.SqlServer;
            current.Description = "默认说明";

            AddTable.CreateForm(current).ShowDialog() ;

            dgvTables.DataSource = null;
            dgvTables.DataSource = Tables;
        }

        private void toolEidtTable_Click(object sender, EventArgs e)
        {
            DataGridViewRow row = dgvTables.Rows[dgvTables.CurrentCell.RowIndex];
            if (row == null) return;

            AddTable.CreateForm((IDataTable)row.DataBoundItem).ShowDialog();
        }

        private void toolClose_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("是否需要保存?", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                toolStripButton1_Click(sender, e);
            }
            else
            {
                this.ParentForm.Close();
            }            
        }

        //保存模型
        private void toolStripButton1_Click(object sender, EventArgs e)
        {
            if (Tables == null || Tables.Count < 1)
            {
                MessageBox.Show(this.Text, "数据库架构为空！", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }            
            if (saveFileDialog1.ShowDialog() != DialogResult.OK || String.IsNullOrEmpty(saveFileDialog1.FileName)) return;
            try
            {
                String xml = DAL.Export(Tables);
                File.WriteAllText(saveFileDialog1.FileName, xml);

                MessageBox.Show("保存模型成功！", "保存模型", MessageBoxButtons.OK);
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, this.Text, MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }
        }

        private void toolDeleteTable_Click(object sender, EventArgs e)
        {
            Tables.RemoveAt(dgvTables.CurrentCell.RowIndex);

            dgvTables.DataSource = null;
            dgvTables.DataSource = Tables;
        }
    }
}
