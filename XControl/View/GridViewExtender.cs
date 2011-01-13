using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Reflection;

namespace XControl
{
    /// <summary>
    /// GridView扩展控件
    /// </summary>
    [Description("GridView扩展控件")]
    [ToolboxData("<{0}:GridViewExtender runat=server></{0}:GridViewExtender>")]
    //[TargetControlType(typeof(GridView))]
    [Designer(typeof(GridViewExtenderDesigner))]
    public class GridViewExtender : ExtenderControl<GridView>
    {
        #region 属性
        /// <summary>选中项背景颜色</summary>
        [Description("选中项背景颜色")]
        public Color SelectedRowBackColor
        {
            get { return GetPropertyValue<Color>("SelectedRowBackColor", Color.Empty); }
            set { SetPropertyValue<Color>("SelectedRowBackColor", value); }
        }

        /// <summary>请求字符串中作为键值的参数</summary>
        [Description("启用")]
        [DefaultValue("请求字符串中作为键值的参数")]
        public String RequestKeyName
        {
            get { return GetPropertyValue<String>("RequestKeyName", "ID"); }
            set { SetPropertyValue<String>("RequestKeyName", value); }
        }

        /// <summary>客户端单击行时执行脚本，{rowindex}代表索引，{datakey}代表键值，{cell0}代表单元格值</summary>
        [Description("客户端单击行时执行脚本，{rowindex}代表索引，{datakey}代表键值，{cell0}代表单元格值")]
        public String OnRowClientClick
        {
            get { return GetPropertyValue<String>("OnRowClientClick", String.Empty); }
            set { SetPropertyValue<String>("OnRowClientClick", value); }
        }

        /// <summary>客户端双击行时执行脚本，{rowindex}代表索引，{datakey}代表键值，{cell0}代表单元格值</summary>
        [Description("客户端双击行时执行脚本，{rowindex}代表索引，{datakey}代表键值，{cell0}代表单元格值")]
        public String OnRowDoubleClientClick
        {
            get { return GetPropertyValue<String>("OnRowDoubleClientClick", String.Empty); }
            set { SetPropertyValue<String>("OnRowDoubleClientClick", value); }
        }

        ///// <summary>是否启用多选</summary>
        //[Description("是否启用多选")]
        //[DefaultValue(false)]
        //public Boolean EnableMultiSelect
        //{
        //    get { return GetPropertyValue<Boolean>("EnableMultiSelect", false); }
        //    set { SetPropertyValue<Boolean>("EnableMultiSelect", value); }
        //}

        /// <summary>选择框位置，需要自己创建CheckBox模版列，这里只是指定而已</summary>
        [Description("选择框位置，需要自己创建CheckBox模版列，这里只是指定而已")]
        [DefaultValue(0)]
        public Int32 CheckBoxIndex
        {
            get { return GetPropertyValue<Int32>("CheckBoxIndex", 0); }
            set { SetPropertyValue<Int32>("CheckBoxIndex", value); }
        }
        #endregion

        #region 扩展属性
        //private Int32 _TotalCount;
        /// <summary>总记录数</summary>
        [Browsable(false)]
        public Int32 TotalCount
        {
            get
            {
                return PropertyInfoX.GetValue<DataSourceSelectArguments>(TargetControl, "SelectArguments").TotalRowCount;
            }
            //set { _TotalCount = value; }
        }

        /// <summary>被选择行的索引</summary>
        [Browsable(false)]
        public Int32[] SelectedIndexes
        {
            get
            {
                GridView gv = TargetControl;
                if (gv == null || gv.Rows == null || gv.Rows.Count < 1 || CheckBoxIndex >= gv.Columns.Count) return null;

                Int32 index = CheckBoxIndex;
                List<Int32> list = new List<Int32>();
                foreach (GridViewRow row in gv.Rows)
                {
                    if (row.RowType != DataControlRowType.DataRow) continue;

                    TableCell cell = row.Cells[index];
                    if (cell == null || cell.Controls.Count < 1) continue;

                    CheckBox cb = FindControl<CheckBox>(cell);
                    if (cb == null) continue;

                    if (cb.Checked) list.Add(row.RowIndex);
                }

                return list.Count > 0 ? list.ToArray() : null;
            }
        }

        /// <summary>被选择行的索引</summary>
        [Browsable(false)]
        public String SelectedIndexesString
        {
            get
            {
                Int32[] indexes = SelectedIndexes;
                if (indexes == null || indexes.Length < 1) return null;

                StringBuilder sb = new StringBuilder();
                foreach (Int32 item in indexes)
                {
                    if (sb.Length > 0) sb.Append(",");
                    sb.Append(item);
                }
                return sb.ToString();
            }
        }

        /// <summary>被选择行的键值</summary>
        [Browsable(false)]
        public Object[] SelectedValues
        {
            get
            {
                Int32[] indexes = SelectedIndexes;
                if (indexes == null || indexes.Length < 1) return null;

                GridView gv = TargetControl;
                List<Object> list = new List<object>();
                foreach (Int32 item in indexes)
                {
                    list.Add(gv.DataKeys[item].Value);
                }

                return list.Count > 0 ? list.ToArray() : null;
            }
        }

        /// <summary>被选择行的键值</summary>
        [Browsable(false)]
        public String SelectedValuesString
        {
            get
            {
                Object[] values = SelectedValues;
                if (values == null || values.Length < 1) return null;

                StringBuilder sb = new StringBuilder();
                foreach (Object item in values)
                {
                    if (sb.Length > 0) sb.Append(",");
                    sb.Append(item);
                }
                return sb.ToString();
            }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);

            if (!Enabled) return;
            GridView gv = TargetControl;
            if (gv == null) return;

            //// 挂接ObjectDataSource的事件，记录总记录数
            //if (!String.IsNullOrEmpty(gv.DataSourceID))
            //{
            //    ObjectDataSource ds = FindControl(gv.DataSourceID) as ObjectDataSource;
            //    ds.Selected += new ObjectDataSourceStatusEventHandler(ds_Selected);
            //}

            // 挂接分页模版
            if (gv.AllowPaging && gv.PagerTemplate == null)
            {
                //TemplateBuilder tb = new TemplateBuilder2();
                //tb.Text = pagerTemplate.Replace("TotalCountStr", String.Format("{0}.TotalCount.ToString(\"n0\")", ID));
                gv.PagerTemplate = new CompiledTemplateBuilder(BuilderPagerTemplate);

                if (!DesignMode && Page.EnableEventValidation) Page.EnableEventValidation = false;
            }

            //// 多选框
            //if (EnableMultiSelect)
            //{
            //    gv.RowCreated += new GridViewRowEventHandler(gv_RowCreated);
            //}
        }

        //void ds_Selected(object sender, ObjectDataSourceStatusEventArgs e)
        //{
        //    if (e.ReturnValue is Int32) TotalCount = (Int32)e.ReturnValue;
        //}

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            if (!Enabled) return;

            GridView gv = TargetControl;
            if (gv == null || gv.Rows.Count <= 0 || !gv.Visible) return;

            RenderOnClick(gv);

            //if (EnableMultiSelect) CreateMutliSelect(TargetControl);
            SetSelectAll(gv);
        }

        ///// <summary>
        ///// 已重载。
        ///// </summary>
        ///// <param name="writer"></param>
        //protected override void Render(HtmlTextWriter writer)
        //{
        //    if (EnableMultiSelect) CreateMutliSelect(TargetControl);

        //    base.Render(writer);
        //}
        #endregion

        #region 点击
        void RenderOnClick(GridView gv)
        {
            foreach (GridViewRow item in gv.Rows)
            {
                if (item.RowType != DataControlRowType.DataRow) continue;

                String onclick = OnRowClientClick;
                String ondblclick = OnRowDoubleClientClick;

                Object keyValue = gv.DataKeys[item.RowIndex].Value;

                if (SelectedRowBackColor != Color.Empty)
                {
                    //String name = String.Format("{0}_RowClick", ID);
                    String name = "rowClick";
                    String color = (new WebColorConverter().ConvertToString(SelectedRowBackColor));
                    String js = "function " + name + "(elm){ elm.style.backgroundColor=!elm.style.backgroundColor?'" + color + "':''; }";

                    Page.ClientScript.RegisterClientScriptBlock(this.GetType(), name, js, true);
                    onclick = name + "(this);" + onclick;

                    if (HttpContext.Current != null && HttpContext.Current.Request != null)
                    {
                        if (keyValue != null && String.Equals(keyValue.ToString(), HttpContext.Current.Request[RequestKeyName]))
                        {
                            //item.Style[HtmlTextWriterStyle.BackgroundColor] = SelectedRowBackColor.ToString();
                            item.BackColor = SelectedRowBackColor;
                        }
                    }
                }
                Format(item, "onclick", onclick);
                Format(item, "ondblclick", ondblclick);
            }
        }

        private void Format(GridViewRow row, string att, string value)
        {
            if (row == null || String.IsNullOrEmpty(value)) return;

            GridView gv = row.NamingContainer as GridView;

            value = value.Replace("{rowindex}", row.RowIndex.ToString());

            object keyValue = gv.DataKeys[row.RowIndex].Value;
            if (keyValue != null)
                value = value.Replace("{datakey}", keyValue.ToString());
            else
                value = value.Replace("{datakey}", null);

            for (int i = 0; i < row.Cells.Count; i++)
            {
                value = value.Replace("{cell" + i + "}", row.Cells[i].Text);
            }

            if (String.IsNullOrEmpty(value)) return;

            if (!value.EndsWith(";")) value = value + ";";
            row.Attributes[att] = value + row.Attributes[att];
        }
        #endregion

        #region 分页模版
        static String _pagerTemplate;
        /// <summary>
        /// 分页模版
        /// </summary>
        public static String PagerTemplateString
        {
            get
            {
                if (!String.IsNullOrEmpty(_pagerTemplate)) return _pagerTemplate;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("共<asp:Label runat=\"server\" Text='<%# NewLife.Reflection.PropertyInfoX.GetValue<DataSourceSelectArguments>(Container.NamingContainer, \"SelectArguments\").TotalRowCount.ToString(\"n0\") %>'></asp:Label>条");
                sb.AppendLine("每页<asp:Label runat=\"server\" Text=\"<%# ((GridView)Container.NamingContainer).PageSize %>\"></asp:Label>条");
                sb.Append("当前第<asp:Label runat=\"server\" Text=\"<%# ((GridView)Container.NamingContainer).PageIndex + 1 %>\"></asp:Label>页/共");
                sb.AppendLine("<asp:Label runat=\"server\" Text=\"<%# ((GridView)Container.NamingContainer).PageCount %>\"></asp:Label>页");
                sb.AppendLine("<asp:LinkButton runat=\"server\" CommandArgument=\"First\" CommandName=\"Page\" Visible='<%#((GridView)Container.NamingContainer).PageIndex != 0 %>'>首页</asp:LinkButton>");
                sb.AppendLine("<asp:LinkButton runat=\"server\" CommandArgument=\"Prev\" CommandName=\"Page\" Visible='<%# ((GridView)Container.NamingContainer).PageIndex != 0 %>'>上一页</asp:LinkButton>");
                sb.AppendLine("<asp:LinkButton runat=\"server\" CommandArgument=\"Next\" CommandName=\"Page\" Visible='<%# ((GridView)Container.NamingContainer).PageIndex != ((GridView)Container.NamingContainer).PageCount - 1 %>'>下一页</asp:LinkButton>");
                sb.AppendLine("<asp:LinkButton runat=\"server\" CommandArgument=\"Last\" CommandName=\"Page\" Visible='<%# ((GridView)Container.NamingContainer).PageIndex != ((GridView)Container.NamingContainer).PageCount - 1 %>'>尾页</asp:LinkButton>");
                sb.AppendLine("转到第<input type=\"textbox\" style=\"width: 40px; text-align: right;\" value='<%# ((GridView)Container.Parent.Parent).PageIndex + 1 %>' />页");
                sb.AppendLine("<input type=\"button\" value=\"GO\" onclick=\"javascript:__doPostBack('<%# ((GridView)Container.NamingContainer).UniqueID %>','Page$'+previousSibling.previousSibling.value)\" />");
                _pagerTemplate = sb.ToString();

                return _pagerTemplate;
            }
        }

        void BuilderPagerTemplate(Control ctl)
        {
            ParserHelper page = new ParserHelper(ctl);

            page.Add("共").AddLabel("lbTotalCount", delegate { return TotalCount; }, "n0").Add("条")
                .Add("&nbsp;每页").AddLabel("lbPageSize", delegate { return TargetControl.PageSize; }).Add("条")
                .Add("&nbsp;当前第").AddLabel("lbCurrentPage", delegate { return TargetControl.PageIndex + 1; })
                .Add("页/共").AddLabel("lbPageCount", delegate { return TargetControl.PageCount; }).Add("页")
                .Add("&nbsp;")
                .AddLinkButton("btnFirst", "首页", "First", delegate { return TargetControl.PageIndex != 0; })
                .AddLinkButton("btnPrev", "上一页", "Prev", delegate { return TargetControl.PageIndex != 0; })
                .AddLinkButton("btnNext", "下一页", "Next", delegate { return TargetControl.PageIndex != TargetControl.PageCount - 1; })
                .AddLinkButton("btnLast", "尾页", "Last", delegate { return TargetControl.PageIndex != TargetControl.PageCount - 1; })
                .Add("转到第").AddTextBox("txtNewPageIndex", delegate { return TargetControl.PageIndex + 1; }).Add("页")
                .AddButton("btnGo", "GO", delegate { return String.Format("javascript:__doPostBack('{0}','Page$'+previousSibling.previousSibling.value)", TargetControl.UniqueID); });

            WebControl wc = ctl as WebControl;
            if (wc != null && String.IsNullOrEmpty(wc.CssClass)) wc.CssClass = "page";
        }

        class ParserHelper
        {
            IParserAccessor parser;
            Page page;

            public ParserHelper(Control ctl) { parser = ctl; page = ctl.Page; }

            void Init(Control ctl)
            {
                if (page != null)
                {
                    ctl.TemplateControl = page;
                    ctl.ApplyStyleSheetSkin(page);
                }
            }

            public ParserHelper Add(String text)
            {
                parser.AddParsedSubObject(new LiteralControl(text));
                return this;
            }

            public ParserHelper AddLabel(String id, Func<Int32> handler)
            {
                return AddLabel(id, handler, null);
            }

            public ParserHelper AddLabel(String id, Func<Int32> handler, String format)
            {
                Label lb = new Label();
                //lb.ID = id;
                Init(lb);

                if (handler != null)
                    lb.DataBinding += delegate(Object sender, EventArgs e)
                    {
                        (sender as Label).Text = String.IsNullOrEmpty(format) ? handler().ToString() : handler().ToString(format);
                    };
                parser.AddParsedSubObject(lb);
                return this;
            }

            public ParserHelper AddLinkButton(String id, String text, String arg, Func<Boolean> handler)
            {
                LinkButton btn = new LinkButton();
                //btn.ID = id;
                Init(btn);

                btn.Text = text;
                btn.CommandName = "Page";
                btn.CommandArgument = arg;
                if (handler != null)
                {
                    btn.DataBinding += delegate(Object sender, EventArgs e)
                    {
                        (sender as LinkButton).Visible = handler();

                        // 呈现时在后面加一个空格
                        GridView gv = (parser as Control).NamingContainer.NamingContainer as GridView;
                        if (gv != null) gv.DataBound += delegate(Object sender2, EventArgs e2)
                           {
                               LinkButton btn2 = sender as LinkButton;
                               if (btn2.Visible)
                               {
                                   LiteralControl lc = new LiteralControl();
                                   lc.Text = "&nbsp;";
                                   btn2.Parent.Controls.AddAt(btn2.Parent.Controls.IndexOf(btn2) + 1, lc);
                               }
                           };
                    };
                }
                parser.AddParsedSubObject(btn);
                return this;
            }

            public ParserHelper AddTextBox(String id, Func<Int32> handler)
            {
                TextBox box = new TextBox();
                //box.ID = id;
                Init(box);

                box.Width = 40;
                box.Style[HtmlTextWriterStyle.TextAlign] = "right";
                if (handler != null)
                    box.DataBinding += delegate(Object sender, EventArgs e)
                    {
                        (sender as TextBox).Text = handler().ToString();
                    };
                parser.AddParsedSubObject(box);
                return this;
            }

            public ParserHelper AddButton(String id, String text, Func<String> handler)
            {
                Button btn = new Button();
                //btn.ID = id;
                Init(btn);

                btn.Text = text;
                btn.UseSubmitBehavior = false;
                if (handler != null)
                    btn.DataBinding += delegate(Object sender, EventArgs e)
                    {
                        (sender as Button).OnClientClick = handler();
                    };
                parser.AddParsedSubObject(btn);
                return this;
            }
        }
        #endregion

        #region 多选
        void SetSelectAll(GridView gv)
        {
            if (gv == null || gv.HeaderRow == null || gv.HeaderRow.Cells.Count <= CheckBoxIndex) return;

            Int32 index = CheckBoxIndex;
            TableCell cell = gv.HeaderRow.Cells[index];
            if (cell == null) return;

            CheckBox header = FindControl<CheckBox>(cell);
            if (header == null)
            {
                header = new CheckBox();
                header.ToolTip = "全选/取消";
                cell.Controls.Add(header);
            }
            if (header == null) return;

            // 列出该列的CheckBox
            List<CheckBox> list = new List<CheckBox>();
            foreach (GridViewRow row in gv.Rows)
            {
                if (row.RowType != DataControlRowType.DataRow) continue;

                cell = row.Cells[index];
                if (cell == null || cell.Controls.Count < 1) continue;

                CheckBox cb = FindControl<CheckBox>(cell);
                if (cb == null) continue;

                list.Add(cb);
            }

            // 构造赋值语句
            StringBuilder sb = new StringBuilder();
            foreach (CheckBox cb in list)
            {
                sb.AppendFormat("{0}.checked=", cb.UniqueID);
            }

            header.Attributes["onclick"] = String.Format("{1}{0}.checked;", header.UniqueID, sb.ToString());
        }

        //void gv_RowCreated(object sender, GridViewRowEventArgs e)
        //{
        //    //if (EnableMultiSelect) CreateMutliSelect(e.Row);
        //}

        //void CreateMutliSelect(GridView gv)
        //{
        //    //if (!EnableMultiSelect) return;

        //    foreach (GridViewRow row in gv.Rows)
        //    {
        //        CreateMutliSelect(row);
        //    }
        //}

        //void CreateMutliSelect(GridViewRow row)
        //{
        //    TableCell cell = null;
        //    CheckBox cb = null;
        //    switch (row.RowType)
        //    {
        //        case DataControlRowType.DataRow:
        //            cell = new TableCell();
        //            cb = new CheckBox();
        //            cb.ID = "s";
        //            cb.ToolTip = TargetControl.DataKeys[row.RowIndex].Value.ToString();
        //            cell.Controls.Add(cb);
        //            row.Cells.AddAt(0, cell);
        //            break;
        //        case DataControlRowType.Footer:
        //        case DataControlRowType.Header:
        //            cell = new TableCell();
        //            if (row.RowType == DataControlRowType.Header) cell.Width = 40;
        //            cb = new CheckBox();
        //            cb.ID = "s";
        //            cb.ToolTip = "全选";
        //            cell.Controls.Add(cb);
        //            row.Cells.AddAt(0, cell);
        //            break;
        //        default:
        //            cell = new TableCell();
        //            row.Cells.AddAt(0, cell);
        //            break;
        //    }
        //}
        #endregion
    }
}