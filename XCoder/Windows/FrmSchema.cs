using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using System.Windows.Forms;
using NewLife.Log;
using XCode.DataAccessLayer;

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

            Icon = Source.GetIcon();
        }

        public static FrmSchema Create(IDatabase db)
        {
            var frm = new FrmSchema
            {
                Db = db ?? throw new ArgumentNullException("db")
            };

            return frm;
        }

        private void FrmSchema_Load(Object sender, EventArgs e)
        {
            Task.Factory.StartNew(() =>
            {
                var tables = Db.CreateMetaData().GetTables();
                this.Invoke(SetList, cbTables, tables);
            }).LogException();
            Task.Factory.StartNew(() =>
            {
                var list = Db.CreateMetaData().MetaDataCollections;
                this.Invoke(SetList, cbSchemas, list);
            }).LogException();
        }
        #endregion

        #region 加载
        void SetList(ComboBox cb, IEnumerable data)
        {
            if (cb == null || data == null) return;

            try
            {
                if (!(data is IList))
                {
                    var list = new List<Object>();
                    foreach (var item in data)
                    {
                        list.Add(item);
                    }
                    data = list;
                }
                cb.DataSource = data;
                //cb.DisplayMember = "value";
                cb.Update();
            }
            catch (Exception ex)
            {
                XTrace.WriteException(ex);
            }
        }
        #endregion

        private void cbTables_SelectedIndexChanged(Object sender, EventArgs e)
        {
            var cb = sender as ComboBox;
            if (cb == null) return;

            var obj = cb.SelectedItem;
            if (obj == null) return;

            try
            {
                var ss = Db.CreateSession();
                if (obj is IDataTable)
                {
                    var sql = "select * from " + (obj as IDataTable).TableName;
                    DataTable dt = null;
                    try
                    {
                        using (var cmd = ss.CreateCommand(sql))
                        using (var reader = cmd.ExecuteReader(CommandBehavior.KeyInfo | CommandBehavior.SchemaOnly))
                        {
                            dt = reader.GetSchemaTable();
                        }
                    }
                    finally
                    {
                        ss.AutoClose();
                    }
                    obj = dt;
                }
                else if (obj is String)
                {
                    obj = ss.GetSchema((String)obj, null);
                }
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