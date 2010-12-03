using System;
using System.Collections;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.IO;
using System.Web.UI;
using System.Web.UI.Design;
using System.Web.UI.Design.WebControls;
using System.Web.UI.WebControls;
using System.Windows.Forms;
using AttributeCollection = System.ComponentModel.AttributeCollection;
using Control = System.Web.UI.Control;

namespace XControl
{
    /// <summary>
    /// 分页控件设计器
    /// </summary>
    public class DataPagerDesigner : CompositeControlDesigner
    {
        #region 属性
        /// <summary>
        /// 文档编辑器中的控件，可以更改持久化属性
        /// </summary>
        DataPager control { get { return base.Component as DataPager; } }

        ///// <summary>
        ///// 数据源
        ///// </summary>
        //public string DataSourceID
        //{
        //    get
        //    {
        //        return control.DataSourceID;
        //    }
        //    set
        //    {
        //        if (value != DataSourceID)
        //        {
        //            if (value == SR.GetString("DataSourceIDChromeConverter_NewDataSource"))
        //            {
        //                //this.CreateDataSource();
        //                value = string.Empty;
        //            }
        //            else
        //            {
        //                if (value == SR.GetString("DataSourceIDChromeConverter_NoDataSource"))
        //                {
        //                    value = string.Empty;
        //                }
        //                control.DataSourceID = value;
        //                //this.OnDataSourceChanged();
        //                //this.ExecuteChooseDataSourcePostSteps();

        //                UpdateDesignTimeHtml();
        //            }
        //        }
        //    }
        //}

        /// <summary>
        /// 模版组
        /// </summary>
        public override TemplateGroupCollection TemplateGroups
        {
            get
            {
                TemplateGroupCollection templateGroups = base.TemplateGroups;

                String name = "PagerTemplate";
                TemplateGroup group = new TemplateGroup(name);
                Style style = new Style();
                style.CopyFrom(control.ControlStyle);
                style.CopyFrom(control.PagerStyle);
                TemplateDefinition definition = new TemplateDefinition(this, name, base.Component, name, style);
                definition.SupportsDataBinding = true;
                group.AddTemplateDefinition(definition);
                templateGroups.Add(group);

                return templateGroups;
            }
        }

        /// <summary>
        /// 是否预览控件
        /// </summary>
        protected override bool UsePreviewControl { get { return true; } }

        DataPagerActionList _actionLists;
        /// <summary>
        /// 动作列表集合
        /// </summary>
        public override DesignerActionListCollection ActionLists
        {
            get
            {
                DesignerActionListCollection lists = new DesignerActionListCollection();
                lists.AddRange(base.ActionLists);

                //DataPagerActionList _actionLists = new DataPagerActionList(this, DataSourceDesigner);
                if (_actionLists == null) _actionLists = new DataPagerActionList(this);
                lists.Add(_actionLists);
                return lists;
            }
        }
        #endregion

        #region 构造
        //private static Int32 tid = 0;
        //public Int32 TID = ++tid;

        //public DataPagerDesigner()
        //{
        //    XTrace.WriteLine("DataPagerDesigner {0}", TID);
        //    XTrace.DebugStack();
        //}
        #endregion

        #region 设计时Html
        /// <summary>
        /// 取得设计时Html
        /// </summary>
        /// <returns></returns>
        public override string GetDesignTimeHtml()
        {
            DataPager view = base.ViewControl as DataPager;

            // 设计时返回一个随机数
            Random rnd = new Random((Int32)DateTime.Now.Ticks);
            view.TotalRowCount = rnd.Next(100, 1000);
            view.PageIndex = rnd.Next(0, view.PageCount);
            //Pager.DataBind();

            StringWriter sw = new StringWriter();
            HtmlTextWriter writer = new HtmlTextWriter(sw);
            view.RenderControl(writer);
            String str = sw.ToString();
            //MessageBox.Show(str);
            if (!String.IsNullOrEmpty(str)) return str;

            //return String.Format("[{0}]", Pager.ID);

            //return CreatePlaceHolderDesignTimeHtml(String.Format("请选择分页样式或设置分页模版！Compent={0} View={1}", control.TID, view.TID));
            return CreatePlaceHolderDesignTimeHtml("请选择分页样式或设置分页模版！");
        }

        /// <summary>
        /// 取得空Html
        /// </summary>
        /// <returns></returns>
        protected override string GetEmptyDesignTimeHtml()
        {
            //return base.CreatePlaceHolderDesignTimeHtml("AtlasWebDesign.DataPager_NoFieldsDefined");
            return "没有设置分页模版";
        }

        /// <summary>
        /// 取得异常发生时的Html
        /// </summary>
        /// <param name="e"></param>
        /// <returns></returns>
        protected override string GetErrorDesignTimeHtml(Exception e)
        {
            //return base.GetErrorDesignTimeHtml(e);
            return String.Format("创建控件出错！" + e.Message);
        }
        #endregion

        #region 初始化
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="component"></param>
        public override void Initialize(IComponent component)
        {
            //ControlDesigner.VerifyInitializeArgument(component, typeof(DataPager));
            base.Initialize(component);
            base.SetViewFlags(ViewFlags.TemplateEditing, true);
            //base.SetViewFlags(ViewFlags.DesignTimeHtmlRequiresLoadComplete, true);
            //if (base.RootDesigner != null)
            //{
            //    if (base.RootDesigner.IsLoading)
            //    {
            //        base.RootDesigner.LoadComplete += new EventHandler(OnDesignerLoadComplete);
            //    }
            //    else
            //    {
            //        OnDesignerLoadComplete(null, EventArgs.Empty);
            //    }
            //}
        }

        //private void OnDesignerLoadComplete(object sender, EventArgs e)
        //{
        //    UpdateDesignTimeHtml();
        //}
        #endregion

        #region 过滤属性
        ///// <summary>
        ///// 预先过滤属性
        ///// </summary>
        ///// <param name="properties"></param>
        //protected override void PreFilterProperties(IDictionary properties)
        //{
        //    PropertyDescriptor pd = (PropertyDescriptor)properties["DataSourceID"];
        //    pd = TypeDescriptor.CreateProperty(base.GetType(), pd, new Attribute[] { new TypeConverterAttribute(typeof(DataSourceIDConverter)) });
        //    properties["DataSourceID"] = pd;
        //}

        #endregion

        #region 数据源相关
        //private IDataSourceDesigner _DataSourceDesigner;
        ///// <summary>数据源设计器</summary>
        //public IDataSourceDesigner DataSourceDesigner
        //{
        //    get { return _DataSourceDesigner ?? (_DataSourceDesigner = GetDataSourceDesigner()); }
        //}

        //private IDataSourceDesigner GetDataSourceDesigner()
        //{
        //    IDataSourceDesigner designer = null;
        //    string dataSourceID = control.DataSourceID;
        //    if (!string.IsNullOrEmpty(dataSourceID))
        //    {
        //        Control component = FindControl(base.Component.Site, (Control)base.Component, dataSourceID);
        //        if ((component != null) && (component.Site != null))
        //        {
        //            IDesignerHost service = (IDesignerHost)component.Site.GetService(typeof(IDesignerHost));
        //            if (service != null)
        //            {
        //                designer = service.GetDesigner(component) as IDataSourceDesigner;
        //            }
        //        }
        //        if (designer == null) MessageBox.Show("无法找到数据源设计器：" + dataSourceID);
        //    }
        //    //if (designer == null) MessageBox.Show("无法找到数据源设计器：" + dataSourceID);
        //    return designer;
        //}

        //static Control FindControl(IServiceProvider serviceProvider, Control control, string controlIdToFind)
        //{
        //    if (string.IsNullOrEmpty(controlIdToFind)) throw new ArgumentNullException("controlIdToFind");

        //    while (control != null)
        //    {
        //        if (control.Site == null || control.Site.Container == null) return null;

        //        IComponent component = control.Site.Container.Components[controlIdToFind];
        //        if (component != null) return (component as Control);

        //        IDesignerHost service = (IDesignerHost)control.Site.GetService(typeof(IDesignerHost));
        //        if (service == null) return null;

        //        ControlDesigner designer = service.GetDesigner(control) as ControlDesigner;
        //        //if (((designer == null) || (designer.View == null)) || (designer.View.NamingContainerDesigner == null)) return null;

        //        //control = designer.View.NamingContainerDesigner.Component as Control;
        //    }
        //    if (serviceProvider != null)
        //    {
        //        IDesignerHost host2 = (IDesignerHost)serviceProvider.GetService(typeof(IDesignerHost));
        //        if (host2 != null)
        //        {
        //            IContainer container = host2.Container;
        //            if (container != null)
        //            {
        //                return (container.Components[controlIdToFind] as Control);
        //            }
        //        }
        //    }
        //    return null;
        //}
        #endregion
    }
}
