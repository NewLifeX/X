using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading;
using System.Windows.Forms;
using NewLife.Log;
using NewLife.Reflection;
using XCode.DataAccessLayer;
using NewLife.Threading;

namespace XCoder
{
    public partial class FrmSchema : Form
    {
        #region 属性
        private IDatabase _Db;
        /// <summary>数据库</summary>
        public IDatabase Db
        {
            get { return _Db; }
            set { _Db = value; }
        }
        #endregion

        #region 初始化界面
        public FrmSchema()
        {
            InitializeComponent();
        }

        public static FrmSchema Create(IDatabase db)
        {
            if (db == null) throw new ArgumentNullException("db");

            FrmSchema frm = new FrmSchema();
            frm.Db = db;

            return frm;
        }

        private void FrmSchema_Load(object sender, EventArgs e)
        {
            ThreadPoolX.QueueUserWorkItem(SetTables);
            ThreadPoolX.QueueUserWorkItem(SetSchemas);
        }
        #endregion

        #region 加载
        void SetTables(Object data)
        {
            List<IDataTable> tables = Db.CreateMetaData().GetTables();
            //DataTable dt = Db.CreateSession().GetSchema("Tables", null);
            //if (dt == null || dt.Rows == null) return;

            //ICollection<String> tables = new List<String>();
            //foreach (DataRow dr in dt.Rows)
            //{
            //    tables.Add((String)dr["table_name"]);
            //}
            this.Invoke(new Func<ComboBox, IEnumerable, Boolean>(SetList), cbTables, tables);
        }

        void SetSchemas(Object data)
        {
            ICollection<String> list = Db.CreateMetaData().MetaDataCollections;
            this.Invoke(new Func<ComboBox, IEnumerable, Boolean>(SetList), cbSchemas, list);
        }

        Boolean SetList(ComboBox cb, IEnumerable data)
        {
            if (cb == null || data == null) return false;

            try
            {
                if (!(data is IList))
                {
                    List<Object> list = new List<Object>();
                    foreach (Object item in data)
                    {
                        list.Add(item);
                    }
                    data = list;
                }
                cb.DataSource = data;
                //cb.DisplayMember = "value";
                cb.Update();

                return true;
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
                return false;
            }
        }
        #endregion

        private void cbTables_SelectedIndexChanged(object sender, EventArgs e)
        {
            ComboBox cb = sender as ComboBox;
            if (cb == null) return;

            Object obj = cb.SelectedItem;
            if (obj == null) return;

            try
            {
                if (obj is IDataTable)
                {
                    //obj = (obj as IDataTable).Columns;
                    DbCommand cmd = Db.CreateSession().CreateCommand();
                    cmd.CommandText = "select * from " + (obj as IDataTable).Name;
                    DataTable dt = null;
                    try
                    {
                        using (DbDataReader reader = cmd.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly))
                        {
                            dt = reader.GetSchemaTable();
                        }
                    }
                    finally
                    {
                        Db.CreateSession().AutoClose();
                    }
                    obj = dt;
                }
                else if (obj is String)
                    obj = Db.CreateSession().GetSchema((String)obj, null);
                gv.DataSource = obj;
                gv.Update();
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }
    }
}
