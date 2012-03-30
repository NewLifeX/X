using System;
using System.Collections.Generic;
using System.Windows.Forms;

namespace XCoder
{
    public partial class FrmItems : Form
    {
        #region 属性
        private Dictionary<String, String> _Dic;
        /// <summary>数据字典</summary>>
        public Dictionary<String, String> Dic
        {
            get { return _Dic; }
            set { _Dic = value; }
        }

        private XConfig _XConfig;
        /// <summary>配置信息</summary>
        public XConfig XConfig
        {
            get { return _XConfig; }
            set { _XConfig = value; }
        }

        #endregion

        #region 初始化

        public FrmItems()
        {
            InitializeComponent();

            this.Icon = FileSource.GetIcon();
        }

        /// <summary>初始化界面</summary>>
        /// <param name="dic"></param>
        /// <returns></returns>
        public static FrmItems Create(Dictionary<string, string> dic)
        {
            FrmItems item = new FrmItems();

            if (dic == null) item.CreatDic();
            else
                item.Dic = dic;

            return item;
        }

        /// <summary>初始化界面</summary>>
        /// <param name="xconfig"></param>
        /// <returns></returns>
        public static FrmItems Create(XConfig xconfig)
        {
            FrmItems item = new FrmItems();

            if (xconfig == null) throw new Exception("配置信息异常");

            item.XConfig = xconfig;

            if (xconfig.Items == null) item.CreatDic();
            else
                item.Dic = xconfig.Items;

            return item;
        }

        /// <summary>初始化字典</summary>>
        private void CreatDic()
        {
            Dictionary<string, string> dic = new Dictionary<string, string>();
            dic.Add("key", "value");
            Dic = dic;

        }

        #endregion

        #region 加载
        //加载
        void SetDic(Dictionary<string, string> dic)
        {
            DataGridViewColumnCollection columns = this.dataGridView1.Columns;
            columns.Add("key", "键");
            columns.Add("value", "值");

            foreach (var item in dic)
            {
                DataGridViewRowCollection rows = this.dataGridView1.Rows;
                rows.Add(item.Key, item.Value);
            }
        }

        //获取当前的DIC
        void LoadDic()
        {

        }
        #endregion

        #region 添加/删除数据

        void AddItems()
        {
            DataGridViewRowCollection rows = this.dataGridView1.Rows;
            DataGridViewColumnCollection column = this.dataGridView1.Columns;

            Dictionary<string, string> dic = new Dictionary<string, string>();

            for (int i = 0; i < rows.Count-1; i++)
            {
                dic.Add(rows[i].Cells[0].Value.ToString(), rows[i].Cells[1].Value.ToString());
            }

            XConfig.Items = dic;
            XConfig.Save();

        }

        void DeleteItems()
        {

        }

        #endregion

        private void FrmItems_Load(object sender, EventArgs e)
        {
            SetDic(Dic);
        }

        private void dataGridView1_RowEnter(object sender, DataGridViewCellEventArgs e)
        {
            //if(e.RowIndex<0) return ;

            //DataGridView dgv=sender as DataGridView;
            //if (dgv == null) return;

            //DataGridViewRow row = dgv.Rows[e.RowIndex];
            //if (row == null) return;

            //propertyGrid1.SelectedObject = row.Cells;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            AddItems();
        }

    }
}
