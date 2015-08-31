//using System;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.ComponentModel.Design;
//using System.Drawing;
//using System.Reflection;
//using System.Web.UI;
//using System.Web.UI.Design;
//using System.Web.UI.Design.WebControls;
//using System.Web.UI.WebControls;

//namespace XControl
//{
//    /// <summary>
//    /// 重写GridView
//    /// </summary>
//    [ToolboxItem(false)]
//    [DefaultProperty("Text")]
//    [ToolboxData("<{0}:XGridView runat=server></{0}:XGridView>")]
//    [Designer(typeof(XGridViewDesigner))]
//    [ToolboxBitmap(typeof(GridView))]
//    public class XGridView : GridView
//    {
//        private Boolean _SetEntry = false;
//        /// <summary>
//        /// 设置实体字段
//        /// </summary>
//        [Category(" 专用属性"), DefaultValue(false), Description("设置实体字段"), DesignOnly(true)]
//        public Boolean SetEntry
//        {
//            get
//            {
//                return _SetEntry;
//            }
//            set
//            {
//                try
//                {
//                    CreateEntryColumns();
//                }
//                catch (Exception ex)
//                {
//                    ViewHelper.MsgBox<XGridView>(ex.Message);
//                }
//                _SetEntry = value;
//            }
//        }

//        /// <summary>
//        /// 建立实体列字段集。把已经建立好的列转为实体列。
//        /// 主要修改各列的HeaderText为中文，同时调整各列顺序为对应的实体属性的顺序。
//        /// </summary>
//        private void CreateEntryColumns()
//        {
//            Type t = ViewHelper.GetEntryType<XGridView>(Site);
//            if (t == null) return;
//            List<FieldItem> list = ViewHelper.AllFields(t);
//            if (list == null) return;
//            XGridView gv = this;
//            if (gv == null) return;

//            // 思路
//            // 把所有属于实体类的列字段从cs中删除，添加到一个临时列表tcs中
//            // 从头开始遍历实体类的属性，判断每个属性是否存在于tcs中，存在则修改HeaderText为中文，并添加到ncs中

//            Dictionary<String, DataControlField> tcs = new Dictionary<String, DataControlField>();
//            foreach (DataControlField dcf in gv.Columns)
//            {
//                foreach (FieldItem fi in list)
//                {
//                    if (fi.Name == dcf.HeaderText)
//                    {
//                        // 因在遍历cs的过程中不能修改cs，所以这里暂时不从cs中删除dcf，遍历完成后删除
//                        tcs.Add(fi.Name, dcf);
//                        break;
//                    }
//                }
//            }
//            // 删除属于实体类的列
//            foreach (DataControlField dcf in tcs.Values)
//            {
//                gv.Columns.Remove(dcf);
//            }
//            String keyname = (gv.DataKeyNames != null && gv.DataKeyNames.Length > 0) ? gv.DataKeyNames[0] : null;
//            // 按顺序添加实体类的调整后的列
//            foreach (FieldItem fi in list)
//            {
//                if (tcs.ContainsKey(fi.Name) && fi.DataObjectField.Length <= 255)
//                {
//                    DataControlField dcf = tcs[fi.Name];
//                    //如果设置了主键字段，或是标识字段，则作为模版列绑定Select功能
//                    if (!String.IsNullOrEmpty(keyname) && fi.Name == keyname || String.IsNullOrEmpty(keyname) && fi.DataObjectField.IsIdentity)
//                    {
//                        TemplateField tf = new TemplateField();
//                        String str = String.Format("<asp:LinkButton id=\"{0}LinkButton1\" runat=\"server\"  CausesValidation=\"{1}\" Text='<%# Eval(\"{0}\") %>' CommandName=\"Select\"></asp:LinkButton>", fi.Name, Boolean.FalseString);
//                        tf.ItemTemplate = ControlParser.ParseTemplate((IDesignerHost)Site.GetService(typeof(IDesignerHost)), str);
//                        dcf = tf as DataControlField;
//                    }

//                    //指定排序表达式
//                    dcf.SortExpression = fi.Name;

//                    //指定时间格式字符串
//                    if (fi.Info.PropertyType == typeof(DateTime))
//                    {
//                        BoundField bc = dcf as BoundField;
//                        if (bc != null) bc.DataFormatString = "{0:yyyy-MM-dd HH:mm:ss}";
//                    }

//                    dcf.HeaderText = (String.IsNullOrEmpty(fi.Description)) ? fi.Name : fi.Description;

//                    if (fi.Info.PropertyType == typeof(Int32))
//                    {
//                        //dcf.HeaderStyle.Width = Unit.Pixel(40);
//                        //dcf.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
//                        dcf.HeaderStyle.CssClass = "IntHead";
//                        dcf.ItemStyle.CssClass = "IntItem";
//                    }
//                    else if (fi.Info.PropertyType == typeof(DateTime) || fi.Info.PropertyType == typeof(Nullable<DateTime>))
//                    {
//                        //dcf.HeaderStyle.Width = Unit.Pixel(140);
//                        //dcf.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
//                        dcf.HeaderStyle.CssClass = "DateHead";
//                        dcf.ItemStyle.CssClass = "DateItem";
//                    }
//                    else
//                    {
//                        //dcf.HeaderStyle.Width = Unit.Pixel(150);
//                        dcf.HeaderStyle.CssClass = "TextHead";
//                        dcf.ItemStyle.CssClass = "TextItem";
//                    }
//                    gv.Columns.Add(dcf);
//                }
//            }

//            //额外工作，设置ObjectDataSource的SelectCountMethod
//            String datasourceid = gv.DataSourceID;
//            if (String.IsNullOrEmpty(datasourceid)) return;
//            // 找到数据绑定控件ObjectDataSource
//            ObjectDataSource obj = gv.Page.FindControl(datasourceid) as ObjectDataSource;
//            if (obj == null) return;
//            //指定分页方法
//            Boolean canpaging = true;
//            if (String.IsNullOrEmpty(obj.SelectCountMethod)) obj.SelectCountMethod = "FindCount";
//            //如果不是默认的FindAll，则使用新构造方法
//            if (!obj.SelectMethod.Equals("FindAll", StringComparison.OrdinalIgnoreCase))
//                obj.SelectCountMethod = obj.SelectMethod + "Count";
//            //指定分页参数
//            if (obj.SelectParameters["startRowIndex"] == null) 
//                canpaging = false;
//            else if (String.IsNullOrEmpty(obj.SelectParameters["startRowIndex"].DefaultValue))
//                obj.SelectParameters["startRowIndex"].DefaultValue = "0";

//            if (obj.SelectParameters["maximumRows"] == null)
//                canpaging = false;
//            else if (String.IsNullOrEmpty(obj.SelectParameters["maximumRows"].DefaultValue))
//                obj.SelectParameters["maximumRows"].DefaultValue = Int32.MaxValue.ToString();

//            //打开分页
//            if (canpaging)
//            {
//                obj.EnablePaging = true;
//                gv.AllowPaging = true;
//            }
//            else
//                ViewHelper.MsgBox<XGridView>(String.Format("当前的查询方法{0}不支持分页，请选择带有分页参数的查询方法！", obj.SelectMethod));

//            //指定排序参数
//            Boolean cansort = false;//是否可以排序
//            if (obj.SelectParameters != null)
//            {
//                //遍历所有Select参数，找到第一个带有order字样的参数做为排序参数
//                foreach (Parameter item in obj.SelectParameters)
//                {
//                    if (item.Name.ToLower().IndexOf("order") >= 0)
//                    {
//                        obj.SortParameterName = item.Name;
//                        cansort = true;
//                        break;
//                    }
//                }
//            }
//            if (cansort)
//                gv.AllowSorting = true;
//            else
//                ViewHelper.MsgBox<XGridView>(String.Format("当前的查询方法{0}不支持排序，请选择带有排序参数的查询方法！", obj.SelectMethod));
//        }

//        private Boolean _SetDefaultStype = false;
//        /// <summary>
//        /// 设置样式
//        /// </summary>
//        [Category(" 专用属性"), DefaultValue(false), Description("设置样式"), DesignOnly(true)]
//        public Boolean SetDefaultStype
//        {
//            get { return _SetDefaultStype; }
//            set
//            {
//                try
//                {
//                    //XGridView gv = this;
//                    //if (gv == null) return;
//                    //gv.BorderColor = System.Drawing.Color.FromArgb(0x82, 0xA8, 0xCF);
//                    //gv.BorderStyle = System.Web.UI.WebControls.BorderStyle.Solid;
//                    //gv.BorderWidth = Unit.Pixel(1);
//                    //gv.Font.Underline = false;
//                    //gv.SelectedRowStyle.BackColor = System.Drawing.Color.FromArgb(0xc0, 0xff, 0xc0);
//                    //gv.AllowPaging = true;
//                    //gv.AlternatingRowStyle.BackColor = System.Drawing.Color.FromArgb(0xef, 0xe6, 0xf7);
//                    //gv.PagerStyle.HorizontalAlign = HorizontalAlign.Right;
//                    //gv.PagerStyle.Font.Size = FontUnit.Point(12);
//                    //gv.PagerStyle.Font.Underline = false;
//                    //if (gv.Columns == null || gv.Columns.Count < 1) return;
//                    //foreach (DataControlField dcf in gv.Columns)
//                    //{
//                    //    dcf.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
//                    //    dcf.HeaderStyle.BorderWidth = Unit.Pixel(1);
//                    //    dcf.HeaderStyle.BorderColor = System.Drawing.Color.FromArgb(0x82, 0xA8, 0xCF);
//                    //    dcf.HeaderStyle.BorderStyle = System.Web.UI.WebControls.BorderStyle.Solid;
//                    //    dcf.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(0xe3, 0xef, 0xff);
//                    //    dcf.HeaderStyle.Height = Unit.Pixel(20);
//                    //    dcf.HeaderStyle.Font.Bold = true;
//                    //    dcf.HeaderStyle.ForeColor = System.Drawing.Color.Black;
//                    //    //dcf.HeaderStyle.Font.Size = FontUnit.Point(11);
//                    //    dcf.HeaderStyle.Font.Underline = false;
//                    //    dcf.ItemStyle.BorderWidth = Unit.Pixel(1);
//                    //    dcf.ItemStyle.BorderColor = System.Drawing.Color.FromArgb(0x82, 0xA8, 0xCF);
//                    //    dcf.ItemStyle.BorderStyle = System.Web.UI.WebControls.BorderStyle.Solid;
//                    //    dcf.ItemStyle.Height = Unit.Pixel(25);
//                    //}
//                }
//                catch (Exception ex)
//                {
//                    ViewHelper.MsgBox<XGridView>(ex.Message);
//                }
//                _SetDefaultStype = value;
//            }
//        }

//        /// <summary>
//        /// 已重写。切换页时取消原选择
//        /// </summary>
//        /// <param name="e"></param>
//        protected override void OnPageIndexChanging(GridViewPageEventArgs e)
//        {
//            this.SelectedIndex = -1;
//            base.OnPageIndexChanging(e);
//        }
//    }

//    /// <summary>
//    /// 在可视化设计器中为 XControl.XGridView 控件提供设计时支持。
//    /// </summary>
//    public class XGridViewDesigner : GridViewDesigner
//    {
//        /// <summary>
//        /// 当关联控件的数据源架构更改时，将调用它。
//        /// </summary>
//        protected override void OnSchemaRefreshed()
//        {
//            base.OnSchemaRefreshed();
//            //先生成原来的，再生成新的
//            //if (!InTemplateMode && !IgnoreSchemaRefreshedEvent)
//            if (!InTemplateMode)
//            {
//                try
//                {
//                    XGridView gv = base.Component as XGridView;
//                    if (gv == null) return;
//                    DataControlFieldCollection cs = gv.Columns;
//                    if (cs == null || cs.Count < 1) return;
//                    gv.SetEntry = !gv.SetEntry;
//                    //gv.SetDefaultStype = !gv.SetDefaultStype;
//                    //gv.CreateEntryColumns();
//                    //gv.SetDefaultStype();
//                    //gv.Columns = cs;
//                    //gv.Columns.Clear();
//                    //foreach (DataControlField dcf in cs) gv.Columns.Add(dcf);
//                }
//                catch (Exception ex)
//                {
//                    ViewHelper.MsgBox<XGridView>(ex.Message);
//                }
//            }
//        }

//        private Boolean IgnoreSchemaRefreshedEvent
//        {
//            get
//            {
//                Type t = typeof(GridViewDesigner);
//                if (t == null) return false;
//                PropertyInfo pi = t.GetProperty("_ignoreSchemaRefreshedEvent", BindingFlags.Instance | BindingFlags.NonPublic);
//                if (pi == null) return false;
//                return (Boolean)pi.GetValue((this as GridViewDesigner), null);
//            }
//        }

//        private ISite Site
//        {
//            get
//            {
//                return base.Component.Site;
//            }
//        }
//    }
//}