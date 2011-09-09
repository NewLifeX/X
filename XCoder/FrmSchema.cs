using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Text;
using System.Windows.Forms;
using XCode.DataAccessLayer;
using NewLife.Reflection;
using System.Threading;
using NewLife.Log;
using System.Collections;

namespace XCoder
{
    public partial class FrmSchema : Form
    {
        #region 属性
        private String _ConnName;
        /// <summary>连接名</summary>
        public String ConnName
        {
            get { return _ConnName; }
            set { _ConnName = value; }
        }

        private DAL _Dal;
        /// <summary>属性说明</summary>
        public DAL Dal
        {
            get { return _Dal; }
            set { _Dal = value; }
        }
        #endregion

        #region 初始化界面
        public FrmSchema()
        {
            InitializeComponent();
        }

        private void FrmSchema_Load(object sender, EventArgs e)
        {
            Dal = DAL.Create(ConnName);

            ThreadPool.QueueUserWorkItem(SetTables);
            ThreadPool.QueueUserWorkItem(SetSchemas);
        }
        #endregion

        #region 加载
        void SetTables(Object data)
        {
            try
            {
                List<IDataTable> tables = Dal.Tables;
                this.Invoke(new Func<ComboBox, IEnumerable, Boolean>(SetList), cbTables, tables);
            }
            catch (Exception ex)
            {
                XTrace.WriteLine(ex.ToString());
            }
        }

        void SetSchemas(Object data)
        {
            try
            {
                ICollection<String> list = Dal.Db.CreateMetaData().MetaDataCollections;
                this.Invoke(new Func<ComboBox, IEnumerable, Boolean>(SetList), cbSchemas, list);
            }
            catch (Exception ex)
            {
                XTrace.WriteLine(ex.ToString());
            }
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
                XTrace.WriteLine(ex.ToString());
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
                    obj = (obj as IDataTable).Columns;
                else if (obj is String)
                    obj = Dal.Db.CreateSession().GetSchema((String)obj, null);
                gv.DataSource = obj;
                gv.Update();
            }
            catch (Exception ex)
            {
                XTrace.WriteLine(ex.ToString());
            }
        }
    }
}
