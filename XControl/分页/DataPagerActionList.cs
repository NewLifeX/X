using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel.Design;
using System.Windows.Forms;
using System.Web.UI.Design;
using System.Web.UI.Design.WebControls;
using System.ComponentModel;

namespace XControl
{
    class DataPagerActionList : DesignerActionList
    {
        #region 构造
        public DataPagerActionList(DataPagerDesigner control)
            : base(control.Component)
        {
            Designer = control;
        }
        #endregion

        #region 属性
        private DataPagerDesigner _Designer;
        /// <summary>控件</summary>
        public DataPagerDesigner Designer
        {
            get { return _Designer; }
            set { _Designer = value; }
        }

        /// <summary>自动显示</summary>
        public override bool AutoShow
        {
            get { return true; }
            set { }
        }

        //[TypeConverter(typeof(DataSourceIDConverter))]
        //public string DataSourceID
        //{
        //    get
        //    {
        //        return Designer.DataSourceID;
        //    }
        //    set
        //    {
        //        //ControlDesigner.InvokeTransactedChange(this._controlDesigner.Component, new TransactedChangeCallback(this.SetDataSourceIDCallback), value, SR.GetString("DataBoundControlActionList_SetDataSourceIDTransaction"));

        //        Designer.DataSourceID = value;
        //    }
        //}
        #endregion

        #region 方法
        public void Test()
        {
            MessageBox.Show("这是一个测试方法！");
        }

        //private bool SetDataSourceIDCallback(object context)
        //{
        //    Designer.DataSourceID = (string)context;

        //    return true;
        //}
        #endregion

        #region 重载
        public override DesignerActionItemCollection GetSortedActionItems()
        {
            DesignerActionItemCollection items = new DesignerActionItemCollection();

            //PropertyDescriptor descriptor = TypeDescriptor.GetProperties(Designer.Component)["DataSourceID"];
            //if ((descriptor != null) && descriptor.IsBrowsable)
            //{
            //    DesignerActionPropertyItem dpitem = new DesignerActionPropertyItem("DataSourceID", SR.GetString("BaseDataBoundControl_ConfigureDataVerb"), SR.GetString("BaseDataBoundControl_DataActionGroup"), SR.GetString("BaseDataBoundControl_ConfigureDataVerbDesc"));
            //    items.Add(dpitem);

            //    ControlDesigner designer = Designer.DataSourceDesigner as ControlDesigner;
            //    if (designer != null)
            //    {
            //        ((DesignerActionPropertyItem)items[0]).RelatedComponent = designer.Component;
            //    }
            //}

            items.Add(new DesignerActionHeaderItem("方法栏"));
            items.Add(new DesignerActionMethodItem(this, "Test", "测试方法"));

            items.Add(new DesignerActionHeaderItem("属性栏"));
            //items.Add(new DesignerActionPropertyItem("DataSourceID", SR.GetString("BaseDataBoundControl_ConfigureDataVerb"), SR.GetString("BaseDataBoundControl_DataActionGroup"), SR.GetString("BaseDataBoundControl_ConfigureDataVerbDesc")));

            return items;
        }
        #endregion
    }
}
