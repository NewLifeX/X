using System;
using System.Collections.Generic;
using System.Windows.Forms;
using NewLife.Reflection;
using XCode.DataAccessLayer;
using System.Text;

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

            this.Icon = FileSource.GetIcon();
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
            //String[] ss = Enum.GetNames(typeof(DatabaseType));
            //List<String> list = new List<string>(ss);
            //for (int i = list.Count - 1; i >= 0; i--)
            //{
            //    Int32 n = (Int32)Enum.Parse(typeof(DatabaseType), list[i]);
            //    if (n >= 100) list.RemoveAt(i);
            //}
            //cbConn.DataSource = list;
            var list = new List<String>();
            foreach (String item in DAL.ConnStrs.Keys)
            {
                list.Add(item);
            }
            cbConn.DataSource = list;
            cbConn.Update();
        }

        private void btnCreateTableSQL_Click(object sender, EventArgs e)
        {
            if (cbConn.SelectedItem == null) return;

            IDataTable table = GetSelectedTable();
            if (table == null) return;

            var dal = DAL.Create("" + cbConn.SelectedItem);
            if (dal == null) return;

            try
            {
                IMetaData md = dal.Db.CreateMetaData();
                var sql = CreateTable(md, table);

                FrmText.Create(table.Name + "表建表语句", sql).Show();
            }
            catch (Exception ex)
            {
                FrmText.Create(table.Name + "表建表语句", "生成建表语句错误！" + Environment.NewLine + ex.ToString()).Show();
            }
        }

        static String CreateTable(IMetaData md, IDataTable table)
        {
            String sql = md.GetSchemaSQL(DDLSchema.CreateTable, table);

            var sb = new StringBuilder();
            if (!String.IsNullOrEmpty(sql)) sb.AppendLine(sql + "; ");

            // 加上表注释
            if (!String.IsNullOrEmpty(table.Description))
            {
                sql = md.GetSchemaSQL(DDLSchema.AddTableDescription, table);
                if (!String.IsNullOrEmpty(sql)) sb.AppendLine(sql + "; ");
            }

            // 加上字段注释
            foreach (IDataColumn item in table.Columns)
            {
                if (!String.IsNullOrEmpty(item.Description))
                {
                    sql = md.GetSchemaSQL(DDLSchema.AddColumnDescription, item);
                    if (!String.IsNullOrEmpty(sql)) sb.AppendLine(sql + "; ");
                }
            }

            // 加上索引
            if (table.Indexes != null)
            {
                foreach (IDataIndex item in table.Indexes)
                {
                    if (!item.PrimaryKey)
                    {
                        sql = md.GetSchemaSQL(DDLSchema.CreateIndex, item);
                        if (!String.IsNullOrEmpty(sql)) sb.AppendLine(sql + "; ");
                    }
                }
            }

            return sb.ToString();
        }

        private void btnCreateDbSQL_Click(object sender, EventArgs e)
        {
            if (cbConn.SelectedItem == null) return;

            var dal = DAL.Create("" + cbConn.SelectedItem);
            if (dal == null) return;

            try
            {
                IMetaData md = dal.Db.CreateMetaData();
                var sb = new StringBuilder();
                foreach (var table in Tables)
                {
                    var sql = CreateTable(md, table);
                    if (!String.IsNullOrEmpty(sql)) sb.AppendLine(sql);
                }

                FrmText.Create("建表语句", sb.ToString()).Show();
            }
            catch (Exception ex)
            {
                FrmText.Create("建表语句", "生成建表语句错误！" + Environment.NewLine + ex.ToString()).Show();
            }
        }

        private void btnCreateDb_Click(object sender, EventArgs e)
        {
            if (cbConn.SelectedItem == null) return;

            var dal = DAL.Create("" + cbConn.SelectedItem);
            if (dal == null) return;

            try
            {
                IMetaData md = dal.Db.CreateMetaData();
                md.SetTables(Tables.ToArray());

                MessageBox.Show("成功建立" + Tables.Count + "张数据表！", this.Text);
            }
            catch (Exception ex)
            {
                MessageBox.Show("建表失败！" + Environment.NewLine + ex.Message, this.Text);
            }
        }
        #endregion
    }
}