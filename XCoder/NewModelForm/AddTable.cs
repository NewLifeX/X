using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Text;
using System.Windows.Forms;

using XCode;
using XCode.DataAccessLayer;

namespace XCoder
{
    public partial class AddTable : UserControl
    {
        private IDataTable CurrentTable;

        private BindingList<IDataColumn> list;

        public AddTable()
        {
            InitializeComponent();
        }
        public AddTable(IDataTable table)
        {
            InitializeComponent();
            combDbType.DataSource = BindComboxEnumType<DatabaseType>.BindTyps;
            //combDbType.DisplayMember = "Name";
            //combDbType.ValueMember = "Type";

            CurrentTable = table;
            //绑定Table信息到文本框
            txtTableName.Text = CurrentTable.TableName;
            txtTableRemark.Text = CurrentTable.Description;
            combDbType.SelectedValue = CurrentTable.DbType;

            list = new BindingList<IDataColumn>();

            //绑定字段到表格
            if (CurrentTable.Columns.Count > 0) dgvColumns.DataSource = CurrentTable.Columns;

            BandingDGV();
        }

        private void BandingDGV()
        {
            if (CurrentTable.Columns.Count > 0)
            {
                list.Clear();
                foreach (var item in CurrentTable.Columns)
                {
                    list.Add(item);
                }
                dgvColumns.DataSource = null;
                dgvColumns.DataSource = list;
            }
            else dgvColumns.DataSource = null;
        }

        public static BaseForm CreateForm(IDataTable table)
        {
            var frm = new AddTable(table);
            frm.Dock = DockStyle.Fill;
            return WinFormHelper.CreateForm(frm, "添加表");
        }

        private void toolAddColumns_Click(Object sender, EventArgs e)
        {
            var dc = CurrentTable.CreateColumn();
            //CurrentTable.Columns.Add(dc);
            var id = CurrentTable.Columns.Count + 1;
            dc.ColumnName = "Column" + id;
            dc.Description = "字段" + id;
            var dr = AddField.CreateForm(dc, true).ShowDialog();
            if (dr != DialogResult.Cancel)
            {
                CurrentTable.Columns.Add(dc);
                BandingDGV();
            }
        }

        private void toolEidtColumn_Click(Object sender, EventArgs e)
        {

            var row = dgvColumns.Rows[dgvColumns.CurrentCell.RowIndex];
            if (row == null) return;

            AddField.CreateForm((IDataColumn)row.DataBoundItem, false).ShowDialog();

            BandingDGV();
        }

        private void toolStripButton1_Click(Object sender, EventArgs e)
        {
            if (MessageBox.Show("是否需要保存数据?", "提示", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                toolSave_Click(sender, e);
            }
            else
            {
                ParentForm.Close();
            }
        }

        private void toolSave_Click(Object sender, EventArgs e)
        {
            CurrentTable.TableName = txtTableName.Text.Trim();
            CurrentTable.Description = txtTableRemark.Text.Trim();
            CurrentTable.DbType = (DatabaseType)Enum.Parse(typeof(DatabaseType), combDbType.SelectedValue.ToString());

            BandingDGV();
        }

        private void toolDelete_Click(Object sender, EventArgs e)
        {
            CurrentTable.Columns.RemoveAt(dgvColumns.CurrentCell.RowIndex);
            BandingDGV();
        }
    }
}
