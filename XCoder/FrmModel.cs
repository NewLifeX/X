using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using XCode.DataAccessLayer;
using NewLife.Reflection;
using XCode;

namespace XCoder
{
    public partial class FrmModel : Form
    {
        #region 属性
        private List<IDataTable> _Tables;
        /// <summary>表集合</summary>
        public List<IDataTable> Tables
        {
            get { return _Tables; }
            set { _Tables = value; }
        }
        #endregion

        #region 界面初始化
        public FrmModel()
        {
            InitializeComponent();
        }

        public static FrmModel Create(List<IDataTable> tables)
        {
            if (tables == null || tables.Count < 1) throw new ArgumentNullException("tables");

            FrmModel frm = new FrmModel();
            frm.Tables = tables;
            //frm.Show();

            return frm;
        }

        private void FrmModel_Load(object sender, EventArgs e)
        {
            //cbTables.DataSource = Tables;
            //cbTables.Update();

            SetTables(Tables, 0);
            SetDbTypes();

            //gv.DataSource = Tables;
        }
        #endregion

        #region 选择数据表
        IDataTable GetSelectedTable()
        {
            ComboBox cb = cbTables;
            if (cb == null || cb.SelectedItem == null) return null;

            return cb.SelectedItem as IDataTable;
        }

        private void cbTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            IDataTable table = GetSelectedTable();
            if (table == null) return;

            pgTable.SelectedObject = table;
            //gv.DataSource = Tables;

            gv.DataSource = table.Columns;
            dgvIndex.DataSource = table.Indexes;
            dgvRelation.DataSource = table.Relations;
        }

        private void gv_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex < 0) return;

            DataGridView dgv = sender as DataGridView;
            if (dgv == null) return;

            DataGridViewRow row = dgv.Rows[e.RowIndex];
            if (row == null) return;

            pgColumn.SelectedObject = row.DataBoundItem;
        }

        void SetTables(List<IDataTable> tables, Int32 index)
        {
            cbTables.Items.Clear();
            if (Tables != null && tables.Count > 0)
            {
                foreach (IDataTable item in tables)
                {
                    cbTables.Items.Add(item);
                }

                if (index < 0) index = 0;
                cbTables.SelectedIndex = index;
            }
            cbTables.Update();
        }
        #endregion

        #region 添加
        private void btnAddTable_Click(object sender, EventArgs e)
        {
            if (Tables == null || Tables.Count < 1) return;

            Type type = Tables[0].GetType();
            if (type == null) return;

            IDataTable table = TypeX.CreateInstance(type) as IDataTable;
            if (table == null) return;

            Tables.Add(table);
            table.ID = Tables.Count;
            table.Name = "NewTable" + table.ID;
            table.Description = "新建表" + table.ID;

            //cbTables.Items.Clear();
            //cbTables.DataSource = Tables;
            //cbTables.Update();
            SetTables(Tables, Tables.Count - 1);
            //cbTables.SelectedItem = table;
        }

        private void btnAddColumn_Click(object sender, EventArgs e)
        {
            IDataTable table = GetSelectedTable();
            if (table == null) return;

            IDataColumn dc = table.CreateColumn();
            table.Columns.Add(dc);
            dc.ID = table.Columns.Count;
            dc.Name = "Column" + dc.ID;
            dc.Description = "字段" + dc.ID;

            gv.DataSource = null;
            gv.DataSource = table.Columns;
            pgColumn.SelectedObject = dc;
        }

        private void btnAddIndex_Click(object sender, EventArgs e)
        {
            IDataTable table = GetSelectedTable();
            if (table == null) return;

            IDataIndex di = table.CreateIndex();
            table.Indexes.Add(di);

            dgvIndex.DataSource = null;
            dgvIndex.DataSource = table.Indexes;
            pgColumn.SelectedObject = di;
        }

        private void btnAddRelation_Click(object sender, EventArgs e)
        {
            IDataTable table = GetSelectedTable();
            if (table == null) return;

            IDataRelation dr = table.CreateRelation();
            table.Relations.Add(dr);

            dgvRelation.DataSource = null;
            dgvRelation.DataSource = table.Relations;
            pgColumn.SelectedObject = dr;
        }
        #endregion

        #region 建表语句
        void SetDbTypes()
        {
            String[] ss = Enum.GetNames(typeof(DatabaseType));
            List<String> list = new List<string>(ss);
            for (int i = list.Count - 1; i >= 0; i--)
            {
                Int32 n = (Int32)Enum.Parse(typeof(DatabaseType), list[i]);
                if (n >= 100) list.RemoveAt(i);
            }
            cbDbTypes.DataSource = list;
            cbDbTypes.Update();
        }

        private void btnCreateTableSQL_Click(object sender, EventArgs e)
        {
            if (cbDbTypes.SelectedItem == null) return;

            IDataTable table = GetSelectedTable();
            if (table == null) return;

            DatabaseType dbt = (DatabaseType)Enum.Parse(typeof(DatabaseType), (String)cbDbTypes.SelectedItem);

            IDatabase db = DbFactory.Create(dbt);
            if (db == null) return;

            IMetaData md = db.CreateMetaData();
            String sql = md.GetSchemaSQL(DDLSchema.CreateTable, table);

            FrmText.Create(table.Name + "表建表语句", sql).Show();
        }
        #endregion
    }
}