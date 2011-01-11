using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;
using System.Drawing;
using System.Web.UI;
using System.Web;
using System.ComponentModel;
using System.ComponentModel.Design;
using System.Web.UI.Design;
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
        #endregion

        #region 扩展属性
        private Int32 _TotalCount;
        /// <summary>总记录数</summary>
        public Int32 TotalCount
        {
            get { return _TotalCount; }
            set { _TotalCount = value; }
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

            GridView gv = TargetControl;
            if (gv == null) return;

            // 挂接ObjectDataSource的时间，记录总记录数
            if (!String.IsNullOrEmpty(gv.DataSourceID))
            {
                ObjectDataSource ds = FindControl(gv.DataSourceID) as ObjectDataSource;
                ds.Selected += new ObjectDataSourceStatusEventHandler(ds_Selected);
            }

            // 挂接分页模版
            if (gv.AllowPaging && gv.PagerTemplate == null)
            {
                //TemplateBuilder tb = new TemplateBuilder2();
                //tb.Text = pagerTemplate.Replace("TotalCountStr", String.Format("{0}.TotalCount.ToString(\"n0\")", ID));
                gv.PagerTemplate = new CompiledTemplateBuilder(BuilderPagerTemplate);

                if (!DesignMode && Page.EnableEventValidation) Page.EnableEventValidation = false;
            }
        }

        void ds_Selected(object sender, ObjectDataSourceStatusEventArgs e)
        {
            if (e.ReturnValue is Int32) TotalCount = (Int32)e.ReturnValue;
        }

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

            foreach (GridViewRow item in gv.Rows)
            {
                if (item.RowType != DataControlRowType.DataRow) continue;

                String onclick = OnRowClientClick;
                String ondblclick = OnRowDoubleClientClick;

                Object keyValue = gv.DataKeys[item.RowIndex].Value;

                if (SelectedRowBackColor != Color.Empty)
                {
                    String js = String.Format("style.backgroundColor=!style.backgroundColor?'{0}':'';", (new WebColorConverter().ConvertToString(SelectedRowBackColor)));
                    onclick = js + onclick;

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

            if (!value.EndsWith(";")) value = value + ";";
            row.Attributes[att] = value + row.Attributes[att];
        }
        #endregion

        #region 分页模版
        static String _pagerTemplate;
        /// <summary>
        /// 分页模版
        /// </summary>
        static String pagerTemplate
        {
            get
            {
                if (!String.IsNullOrEmpty(_pagerTemplate)) return _pagerTemplate;

                StringBuilder sb = new StringBuilder();
                sb.AppendLine("共<asp:Label ID=\"lbTotalCount\" runat=\"server\" Text=\"<%# TotalCountStr %>\"></asp:Label>条");
                sb.AppendLine("每页<asp:Label ID=\"lbPageSize\" runat=\"server\" Text=\"<%# ((GridView)Container.NamingContainer).PageSize %>\"></asp:Label>条");
                sb.Append("当前第<asp:Label ID=\"lbCurrentPage\" runat=\"server\" Text=\"<%# ((GridView)Container.NamingContainer).PageIndex + 1 %>\"></asp:Label>页/共");
                sb.AppendLine("<asp:Label ID=\"lbPageCount\" runat=\"server\" Text=\"<%# ((GridView)Container.NamingContainer).PageCount %>\"></asp:Label>页");
                sb.AppendLine("<asp:LinkButton ID=\"LinkButtonFirstPage\" runat=\"server\" CommandArgument=\"First\" CommandName=\"Page\" Visible='<%#((GridView)Container.NamingContainer).PageIndex != 0 %>'>首页</asp:LinkButton>");
                sb.AppendLine("<asp:LinkButton ID=\"LinkButtonPreviousPage\" runat=\"server\" CommandArgument=\"Prev\" CommandName=\"Page\" Visible='<%# ((GridView)Container.NamingContainer).PageIndex != 0 %>'>上一页</asp:LinkButton>");
                sb.AppendLine("<asp:LinkButton ID=\"LinkButtonNextPage\" runat=\"server\" CommandArgument=\"Next\" CommandName=\"Page\" Visible='<%# ((GridView)Container.NamingContainer).PageIndex != ((GridView)Container.NamingContainer).PageCount - 1 %>'>下一页</asp:LinkButton>");
                sb.AppendLine("<asp:LinkButton ID=\"LinkButtonLastPage\" runat=\"server\" CommandArgument=\"Last\" CommandName=\"Page\" Visible='<%# ((GridView)Container.NamingContainer).PageIndex != ((GridView)Container.NamingContainer).PageCount - 1 %>'>尾页</asp:LinkButton>");
                sb.AppendLine("转到第<input type=\"textbox\" id=\"txtNewPageIndex\" style=\"width: 40px;\" value='<%# ((GridView)Container.Parent.Parent).PageIndex + 1 %>' />页");
                sb.AppendLine("<input type=\"button\" id=\"btnGo\" value=\"GO\" onclick=\"javascript:__doPostBack('<%# ((GridView)Container.NamingContainer).UniqueID %>','Page$'+document.getElementById('txtNewPageIndex').value)\" />");
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
    }

    /// <summary>
    /// GridView扩展控件设计时
    /// </summary>
    public class GridViewExtenderDesigner : ExtenderControlDesigner<GridViewExtender, GridView>
    {
        #region 智能标记
        /// <summary>
        /// 已重载。
        /// </summary>
        public override DesignerActionListCollection ActionLists
        {
            get
            {
                DesignerActionListCollection lists = new DesignerActionListCollection();
                lists.AddRange(base.ActionLists);
                lists.Add(new GridViewExtenderActionList(this));
                return lists;
            }
        }

        class GridViewExtenderActionList : DesignerActionList
        {
            private GridViewExtenderDesigner _parent;

            public GridViewExtenderActionList(GridViewExtenderDesigner parent)
                : base(parent.Component)
            {
                _parent = parent;
            }

            /// <summary>
            /// 已重载。
            /// </summary>
            /// <returns></returns>
            public override DesignerActionItemCollection GetSortedActionItems()
            {
                DesignerActionItemCollection items = new DesignerActionItemCollection();
                //if (_parent.CanConfigure)
                {
                    DesignerActionMethodItem item = new DesignerActionMethodItem(this, "Configure", SR.GetString("DataSourceDesigner_ConfigureDataSourceVerb"), SR.GetString("DataSourceDesigner_DataActionGroup"), SR.GetString("DataSourceDesigner_ConfigureDataSourceVerbDesc"), true);
                    item.AllowAssociate = true;
                    items.Add(item);
                }
                return items;
            }

            /// <summary>
            /// 是否自动显示
            /// </summary>
            public override bool AutoShow { get { return true; } set { } }
        }
        #endregion
    }
}