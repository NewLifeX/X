//using System;
//using System.Collections;
//using System.Collections.Generic;
//using System.ComponentModel;
//using System.ComponentModel.Design;
////using System.Windows.Forms;

//using System.Drawing;
//using System.Text;
//using System.Web.UI;
//using System.Web.UI.Design;
//using System.Web.UI.Design.WebControls;
//using System.Web.UI.WebControls;
////using XCode.Configuration;
////using XCode.Attributes;

//namespace XControl
//{
//    /// <summary>
//    /// 重写FormView
//    /// </summary>
//    [ToolboxItem(false)]
//    [DefaultProperty("Text")]
//    [ToolboxData("<{0}:XFormView runat=server></{0}:XFormView>")]
//    [Designer(typeof(XFormViewDesigner))]
//    [ToolboxBitmap(typeof(FormView))]
//    public class XFormView : FormView
//    {
//        #region 每行字段个数
//        /// <summary>
//        /// 每行字段个数。设置该属性后，自动生成模板时将根据该属性来调整。
//        /// </summary>
//        [Category(" 专用属性"), DefaultValue(2), Description("每行字段个数。设置该属性后，自动生成模板时将根据该属性来调整。")]
//        public Int32 ColumnSize
//        {
//            get
//            {
//                return ViewState["ColumnSize"] == null ? 2 : Int32.Parse(ViewState["ColumnSize"].ToString());
//            }
//            set
//            {
//                ViewState["ColumnSize"] = value;
//            }
//        }
//        #endregion

//        #region 自动刷新对应的XGridView
//        /// <summary>
//        /// 自动刷新对应的XGridView
//        /// </summary>
//        [Category(" 专用属性"), DefaultValue(false), Description("自动刷新对应的XGridView")]
//        public Boolean AutoRefreshXGridView
//        {
//            get
//            {
//                return ViewState["AutoRefreshXGridView"] == null ? false : (Boolean)ViewState["AutoRefreshXGridView"];
//            }
//            set
//            {
//                ViewState["AutoRefreshXGridView"] = value;
//            }
//        }

//        /// <summary>
//        /// 已重载。
//        /// </summary>
//        /// <param name="e"></param>
//        protected override void OnItemInserted(FormViewInsertedEventArgs e)
//        {
//            base.OnItemInserted(e);
//            RefreshXGridView();
//        }

//        /// <summary>
//        /// 已重载。
//        /// </summary>
//        /// <param name="e"></param>
//        protected override void OnItemUpdated(FormViewUpdatedEventArgs e)
//        {
//            base.OnItemUpdated(e);
//            RefreshXGridView();
//        }

//        /// <summary>
//        /// 已重载。
//        /// </summary>
//        /// <param name="e"></param>
//        protected override void OnItemDeleted(FormViewDeletedEventArgs e)
//        {
//            base.OnItemDeleted(e);
//            RefreshXGridView();
//        }

//        /// <summary>
//        /// 设置关联的XGridView重新绑定数据
//        /// </summary>
//        private void RefreshXGridView()
//        {
//            if (!AutoRefreshXGridView) return;
//            GridView xgv = FindGridView();
//            if (xgv == null) return;
//            xgv.DataBind();
//        }
//        #endregion

//        #region 取消选择
//        /// <summary>
//        /// 已重载。
//        /// </summary>
//        /// <param name="e"></param>
//        protected override void OnItemCommand(FormViewCommandEventArgs e)
//        {
//            //取消选择
//            if (e.CommandName == "CancelSelect")
//            {
//                GridView xgv = FindGridView();
//                if (xgv == null) return;
//                xgv.SelectedIndex = -1;
//            }
//            else
//                base.OnItemCommand(e);
//        }
//        #endregion

//        /// <summary>
//        /// 找到GridView
//        /// </summary>
//        /// <returns></returns>
//        private GridView FindGridView()
//        {
//            //找到对应的ObjectDataSource
//            if (String.IsNullOrEmpty(DataSourceID)) return null;
//            ObjectDataSource ods = ViewHelper.Find(Page, DataSourceID) as ObjectDataSource;
//            if (ods == null) return null;
//            if (ods.SelectParameters.Count != 1) return null;
//            ControlParameter para = ods.SelectParameters[0] as ControlParameter;
//            if (para == null || String.IsNullOrEmpty(para.ControlID)) return null;
//            return ViewHelper.Find(Page, para.ControlID) as GridView;
//        }
//    }

//    /// <summary>
//    /// 在可视化设计器中为 XControl.XFormView 控件提供设计时支持。
//    /// </summary>
//    public class XFormViewDesigner : FormViewDesigner
//    {
//        /// <summary>
//        /// 当关联控件的数据源架构更改时，将调用它。
//        /// </summary>
//        protected override void OnSchemaRefreshed()
//        {
//            base.OnSchemaRefreshed();
//            //先生成原来的，再生成新的
//            if (!InTemplateMode)
//            {
//#if !DEBUG
//                try
//#endif
//                {
//                    AddTemplates();
//                }
//#if !DEBUG
//                catch (Exception ex)
//                {
//                    ViewHelper.MsgBox<XFormView>(ex.Message);
//                }
//#endif
//            }
//        }

//        private ISite Site
//        {
//            get
//            {
//                return base.Component.Site;
//            }
//        }

//        //前缀
//        static String perfix = "XCL";

//        private void AddTemplates()
//        {
//            //取得实体类
//            Type t = ViewHelper.GetEntryType<XFormView>(Site);
//            if (t == null) return;
//            List<FieldItem> list = ViewHelper.AllFields(t);
//            if (list == null) return;

//            //思路
//            //遍历实体类成员，生成三种模版

//            IDesignerHost service = (IDesignerHost)Site.GetService(typeof(IDesignerHost));
//            if (service == null) return;

//            XFormView fv = Site.Component as XFormView;

//            Table Item = new Table();
//            Table Edit = new Table();
//            Table Inst = new Table();

//            Item.ID = fv.ClientID + "_Item";
//            Edit.ID = fv.ClientID + "_Edit";
//            Inst.ID = fv.ClientID + "_Inst";

//            #region 循环处理模版
//            int count = fv.ColumnSize;
//            if (count < 1) count = 1;
//            int index = 0;

//            //当前行
//            Row ItemRow = new Row();
//            Row EditRow = new Row();
//            Row InstRow = new Row();
//            Item.Rows.Add(ItemRow);
//            Edit.Rows.Add(EditRow);
//            Inst.Rows.Add(InstRow);

//            foreach (FieldItem fi in list)
//            {
//                #region 预处理
//                //是否换行
//                Boolean IsWrap = index % count == 0;
//                //第一行不换行
//                if (index == 0) IsWrap = false;
//                index++;

//                String name = fi.Name;
//                //处理得到一个名字，只含有字母数字和下划线，其它字符转为下划线
//                char[] chArray = new char[name.Length];
//                for (int i = 0; i < name.Length; i++)
//                {
//                    char c = name[i];
//                    if (char.IsLetterOrDigit(c) || (c == '_'))
//                    {
//                        chArray[i] = c;
//                    }
//                    else
//                    {
//                        chArray[i] = '_';
//                    }
//                }
//                String controlID = new String(chArray);
//                String strEval = "Eval(\"" + name + "\")";
//                String strBind = "Bind(\"" + name + "\")";
//                if (fi.Info.PropertyType == typeof(DateTime))
//                {
//                    strEval = "Eval(\"" + name + "\", \"{0:yyyy-MM-dd HH:mm:ss}\")";
//                    strBind = "Bind(\"" + name + "\", \"{0:yyyy-MM-dd HH:mm:ss}\")";
//                }

//                //重新指定为中文名
//                name = (String.IsNullOrEmpty(fi.Description)) ? fi.Name : fi.Description;

//                String strEdit = "";
//                String strItem = "";
//                String strInst = "";
//                #endregion

//                #region 识别成不同的控件
//                if (fi.DataObjectField.IsIdentity)
//                {
//                    strEdit = MakeLabel(controlID, strEval);
//                    strItem = MakeLabel(controlID, strEval);

//                    IsWrap = true;
//                    index = 0;
//                }
//                //布尔型，或者是Is开头且第三字母是大写字母的整型，比如IsTop
//                else if (fi.Info.PropertyType == typeof(Boolean))
//                {
//                    strItem = MakeCheckBox(controlID, strBind, false);
//                    strEdit = MakeCheckBox(controlID, strBind, true);
//                    strInst = MakeCheckBox(controlID, strBind, true);
//                }
//                else if (fi.Info.PropertyType == typeof(Int32))
//                {
//                    if (fi.Info.PropertyType == typeof(Int32) && fi.Name.Length > 2 &&
//                    fi.Name.StartsWith("Is") && fi.Name[2] >= 'A' && fi.Name[2] <= 'Z')
//                    {
//                        strItem = MakeIntCheckBox(controlID, strBind, false);
//                        strEdit = MakeIntCheckBox(controlID, strBind, true);
//                        strInst = MakeIntCheckBox(controlID, strBind, true);
//                    }
//                    else
//                    {
//                        strItem = MakeLabel(controlID, strBind);
//                        strEdit = String.Format("<{2}:NumberBox Text='<%# {1} %>' runat=\"server\" id=\"{0}NumberBox\" />", controlID, strBind, perfix);
//                        strInst = strEdit;
//                    }
//                }
//                else if (fi.Info.PropertyType == typeof(Double))
//                {
//                    strItem = MakeLabel(controlID, strBind);
//                    strEdit = String.Format("<{2}:RealBox Text='<%# {1} %>' runat=\"server\" id=\"{0}RealBox\" />", controlID, strBind, perfix);
//                    strInst = strEdit;
//                }
//                else if (fi.Info.PropertyType == typeof(DateTime))
//                {
//                    strItem = MakeLabel(controlID, strBind);
//                    strEdit = String.Format("<{2}:DateBox Text='<%# {1} %>' runat=\"server\" id=\"{0}DateBox\" />", controlID, strBind, perfix);
//                    strInst = strEdit;
//                }
//                else if (fi.Name.ToLower() == "ip")
//                {
//                    strItem = MakeLabel(controlID, strBind);
//                    strEdit = String.Format("<{2}:IPBox Text='<%# {1} %>' runat=\"server\" id=\"{0}IPBox\" />", controlID, strBind, perfix);
//                    strInst = strEdit;
//                }
//                else if (fi.Name.ToLower() == "mail" || fi.Name.ToLower() == "email")
//                {
//                    strItem = MakeLabel(controlID, strBind);
//                    strEdit = String.Format("<{2}:MailBox Text='<%# {1} %>' runat=\"server\" id=\"{0}MailBox\" />", controlID, strBind, perfix);
//                    strInst = strEdit;
//                }
//                else if (fi.Name.ToLower() == "password")
//                {
//                    strItem = MakeLabel(controlID, strBind);
//                    strEdit = String.Format("<asp:TextBox Text='<%# {1} %>' runat=\"server\" id=\"{0}TextBox\" TextMode=\"Password\" />", controlID, strBind);
//                    strInst = strEdit;
//                }
//                else if (fi.DataObjectField.Length > 255)
//                {
//                    strItem = String.Format("<asp:Label Text='<%# {1} %>' runat=\"server\" id=\"{0}Label\" Width=\"440px\" style=\"word-break: break-all; min-height: 130px\" />", controlID, strBind);
//                    strEdit = String.Format("<asp:TextBox Text='<%# {1} %>' runat=\"server\" id=\"{0}TextBox\" Height=\"130px\" TextMode=\"MultiLine\" Width=\"440px\" />", controlID, strBind);
//                    strInst = strEdit;

//                    IsWrap = true;
//                    index = 0;
//                }
//                else
//                {
//                    strItem = MakeLabel(controlID, strBind);
//                    strEdit = String.Format("<asp:TextBox Text='<%# {1} %>' runat=\"server\" id=\"{0}TextBox\" />", controlID, strBind);
//                    strInst = strEdit;
//                }
//                #endregion

//                #region 开始加模板
//                if (IsWrap)
//                {
//                    //大文本列独立一行，当前行新起一行
//                    ItemRow = new Row();
//                    EditRow = new Row();
//                    InstRow = new Row();
//                    Item.Rows.Add(ItemRow);
//                    Edit.Rows.Add(EditRow);
//                    Inst.Rows.Add(InstRow);
//                }

//                //加入到当前行
//                name = name + "：";
//                if (fi.DataObjectField.Length > 255)
//                {
//                    //大文本的标签和值分别需要独立一行
//                    ItemRow.Cells.Add(new Cell(name, null));
//                    EditRow.Cells.Add(new Cell(name, null));
//                    if (!fi.DataObjectField.IsIdentity) InstRow.Cells.Add(new Cell(name, null));

//                    ItemRow = new Row();
//                    EditRow = new Row();
//                    InstRow = new Row();
//                    Item.Rows.Add(ItemRow);
//                    Edit.Rows.Add(EditRow);
//                    Inst.Rows.Add(InstRow);

//                    ItemRow.Cells.Add(new Cell(null, strItem));
//                    EditRow.Cells.Add(new Cell(null, strEdit));
//                    if (!fi.DataObjectField.IsIdentity) InstRow.Cells.Add(new Cell(null, strInst));
//                }
//                else
//                {
//                    ItemRow.Cells.Add(new Cell(name, strItem));
//                    EditRow.Cells.Add(new Cell(name, strEdit));
//                    if (!fi.DataObjectField.IsIdentity) InstRow.Cells.Add(new Cell(name, strInst));
//                }

//                if (IsWrap)
//                {
//                    //标识列独立一行，当前行新起一行
//                    ItemRow = new Row();
//                    EditRow = new Row();
//                    InstRow = new Row();
//                    Item.Rows.Add(ItemRow);
//                    Edit.Rows.Add(EditRow);
//                    Inst.Rows.Add(InstRow);
//                }
//                #endregion
//            }
//            //移除空行
//            Item.RemoveEmptyRow();
//            Edit.RemoveEmptyRow();
//            Inst.RemoveEmptyRow();

//            if (DesignerView.CanUpdate || DesignerView.CanDelete || DesignerView.CanInsert)
//            {
//                Item.Foot = Foot.Item;
//                if (!DesignerView.CanUpdate) Item.Foot.Left = null;
//                if (!DesignerView.CanDelete) Item.Foot.Middle = null;
//                if (!DesignerView.CanInsert) Item.Foot.Right = null;
//            }

//            Edit.Foot = Foot.Edit;
//            Inst.Foot = Foot.Inst;
//            #endregion

//            #region 生成模板
//#if !DEBUG
//            try
//#endif
//            {
//                //XFormView fv = base.Component as XFormView;
//                if (fv != null)
//                {
//                    fv.ItemTemplate = ControlParser.ParseTemplate(service, Item.ToString());
//                    if (base.DesignerView.CanUpdate)
//                        fv.EditItemTemplate = ControlParser.ParseTemplate(service, Edit.ToString());
//                    if (base.DesignerView.CanInsert)
//                        fv.InsertItemTemplate = ControlParser.ParseTemplate(service, Inst.ToString());

//                    DescriptionAttribute[] btas = t.GetCustomAttributes(typeof(DescriptionAttribute), false) as DescriptionAttribute[];
//                    if (btas == null || btas.Length < 1 || String.IsNullOrEmpty(btas[0].Description))
//                        fv.EmptyDataTemplate = ControlParser.ParseTemplate(service, "<asp:LinkButton ID=\"LinkButton1\" runat=\"server\" CommandName=\"New\">新增</asp:LinkButton>");
//                    else
//                        fv.EmptyDataTemplate = ControlParser.ParseTemplate(service, "<asp:LinkButton ID=\"LinkButton1\" runat=\"server\" CommandName=\"New\">新增" + btas[0].Description + "</asp:LinkButton>");

//                    //额外工作，设置AutoRefreshXGridView
//                    fv.AutoRefreshXGridView = true;
//                }
//            }
//#if !DEBUG
//            catch (Exception ex)
//            {
//                ViewHelper.MsgBox<XFormView>(ex.Message);
//            }
//#endif
//            #endregion
//        }

//        #region 建立元素
//        private static String ValueDiv
//        {
//            get
//            {
//                return "<div class=\"XFormView_ItemValue\">{0}</div>";
//            }
//        }

//        private static String BlankDiv
//        {
//            get
//            {
//                return "<div style=\"width:5px; float:left\"></div>";
//            }
//        }

//        /// <summary>
//        /// 建立Label
//        /// </summary>
//        /// <param name="controlid">控件ID</param>
//        /// <param name="bindstr">绑定字符串</param>
//        /// <returns></returns>
//        private static String MakeLabel(String controlid, String bindstr)
//        {
//            return String.Format("<asp:Label Text='<%# {1} %>' runat=\"server\" id=\"{0}Label\" />", controlid, bindstr);
//        }

//        /// <summary>
//        /// 建立TextBox
//        /// </summary>
//        /// <param name="controlid">控件ID</param>
//        /// <param name="bindstr">绑定字符串</param>
//        /// <returns></returns>
//        private static String MakeTextBox(String controlid, String bindstr)
//        {
//            return String.Format("<asp:TextBox Text='<%# {1} %>' runat=\"server\" id=\"{0}TextBox\" />", controlid, bindstr);
//        }

//        /// <summary>
//        /// 建立CheckBox
//        /// </summary>
//        /// <param name="controlid">控件ID</param>
//        /// <param name="bindstr">绑定字符串</param>
//        /// <param name="enabled">是否Enabled</param>
//        /// <returns></returns>
//        private String MakeCheckBox(String controlid, String bindstr, Boolean enabled)
//        {
//            String str = String.Format("<asp:CheckBox Checked='<%# {1} %>' runat=\"server\" id=\"{0}CheckBox\"{2} />", controlid, bindstr, enabled ? "" : " Enabled=\"false\"");
//            return String.Format(ValueDiv, str);
//        }

//        /// <summary>
//        /// 建立IntCheckBox
//        /// </summary>
//        /// <param name="controlid">控件ID</param>
//        /// <param name="bindstr">绑定字符串</param>
//        /// <param name="enabled">是否Enabled</param>
//        /// <returns></returns>
//        private String MakeIntCheckBox(String controlid, String bindstr, Boolean enabled)
//        {
//            String str = String.Format("<{3}:IntCheckBox Value='<%# {1} %>' runat=\"server\" id=\"{0}IntCheckBox\"{2} />", controlid, bindstr, enabled ? "" : " Enabled=\"false\"", perfix);
//            return String.Format(ValueDiv, str);
//        }
//        #endregion

//        #region 模板类
//        private class Base
//        {
//            private String _CssClass;
//            /// <summary>
//            /// 样式
//            /// </summary>
//            public String CssClass { get { return _CssClass; } set { _CssClass = value; } }

//            private String _ID;
//            /// <summary>
//            /// 层ID
//            /// </summary>
//            public String ID { get { return _ID; } set { _ID = value; } }

//            private String _Content;
//            /// <summary>
//            /// 内容
//            /// </summary>
//            public virtual String Content { get { return _Content; } set { _Content = value; } }

//            /// <summary>
//            /// 标签头
//            /// </summary>
//            public String Begin
//            {
//                get
//                {
//                    try
//                    {
//                        StringBuilder sb = new StringBuilder();
//                        sb.Append("<div");
//                        if (!String.IsNullOrEmpty(ID)) sb.AppendFormat(" id=\"{0}\"", ID);
//                        if (!String.IsNullOrEmpty(CssClass)) sb.AppendFormat(" class=\"{0}\"", CssClass);
//                        sb.Append(">");
//                        return sb.ToString();
//                    }
//                    catch (Exception ex)
//                    {
//                        throw ex;
//                    }
//                }
//            }

//            /// <summary>
//            /// 标签尾
//            /// </summary>
//            public String End
//            {
//                get { return "</div>"; }
//            }

//            /// <summary>
//            /// 已重载。生成内容。
//            /// </summary>
//            /// <returns></returns>
//            public override string ToString()
//            {
//                StringBuilder sb = new StringBuilder();
//                sb.Append(Begin);
//                if (!String.IsNullOrEmpty(Content)) sb.Append(Content);
//                sb.Append(End);
//                return sb.ToString();
//            }
//        }

//        private class Table : Base
//        {
//            public IList<Row> Rows = new List<Row>();
//            public Foot Foot;

//            public override string Content
//            {
//                get
//                {
//                    if (Rows == null || Rows.Count < 1) return null;
//                    StringBuilder sb = new StringBuilder();
//                    foreach (Row r in Rows)
//                    {
//                        sb.Append(r.ToString());
//                    }
//                    if (Foot != null) sb.Append(Foot.ToString());
//                    return sb.ToString();
//                }
//                set { }
//            }

//            public Table()
//            {
//                CssClass = "XFormView";
//            }

//            /// <summary>
//            /// 移除空行
//            /// </summary>
//            public void RemoveEmptyRow()
//            {
//                IList<Row> todel = new List<Row>();
//                foreach (Row r in Rows)
//                {
//                    if (r.Cells == null || r.Cells.Count < 1) todel.Add(r);
//                }
//                foreach (Row r in todel)
//                {
//                    Rows.Remove(r);
//                }
//            }
//        }

//        private class Row : Base
//        {
//            public IList<Cell> Cells = new List<Cell>();

//            public override string Content
//            {
//                get
//                {
//                    if (Cells == null || Cells.Count < 1) return null;
//                    StringBuilder sb = new StringBuilder();
//                    foreach (Cell c in Cells)
//                    {
//                        sb.Append(c.ToString());
//                    }
//                    return sb.ToString();
//                }
//                set { }
//            }

//            public Row()
//            {
//                CssClass = "Row";
//            }
//        }

//        private class Foot : Base
//        {
//            public String Left;
//            public String Middle;
//            public String Right;
//            public String Ext;
//            public String Blank = "<div style=\"width:5px; float:left\"></div>";

//            public override string Content
//            {
//                get
//                {
//                    StringBuilder sb = new StringBuilder();
//                    bool hasitem = false;
//                    if (!String.IsNullOrEmpty(Left))
//                    {
//                        sb.Append("<div style=\"float: left\">");
//                        sb.Append(Left);
//                        sb.Append("</div>");
//                        hasitem = true;
//                    }
//                    if (!String.IsNullOrEmpty(Middle))
//                    {
//                        if (hasitem) sb.Append(Blank);
//                        sb.Append("<div style=\"float: left\">");
//                        sb.Append(Middle);
//                        sb.Append("</div>");
//                        hasitem = true;
//                    }
//                    if (!String.IsNullOrEmpty(Right))
//                    {
//                        if (hasitem) sb.Append(Blank);
//                        sb.Append("<div style=\"float: left\">");
//                        sb.Append(Right);
//                        sb.Append("</div>");
//                        hasitem = true;
//                    }
//                    if (!String.IsNullOrEmpty(Ext))
//                    {
//                        if (hasitem) sb.Append(Blank);
//                        sb.Append("<div style=\"float: left\">");
//                        sb.Append(Ext);
//                        sb.Append("</div>");
//                        hasitem = true;
//                    }
//                    return sb.ToString();
//                }
//                set { }
//            }

//            public Foot()
//            {
//                CssClass = "Foot";
//            }

//            public static Foot Item;
//            public static Foot Edit;
//            public static Foot Inst;

//            static Foot()
//            {
//                Item = new Foot();
//                Item.Left = "<asp:LinkButton runat=\"server\" Text=\"编辑\" CommandName=\"Edit\" id=\"EditButton\" CausesValidation=\"false\" />";
//                Item.Middle = "<asp:LinkButton runat=\"server\" Text=\"删除\" CommandName=\"Delete\" id=\"DeleteButton\" CausesValidation=\"false\" />";
//                Item.Right = "<asp:LinkButton runat=\"server\" Text=\"新建\" CommandName=\"New\" id=\"NewButton\" CausesValidation=\"false\" />";
//                Item.Ext = "<asp:LinkButton runat=\"server\" Text=\"取消\" CommandName=\"CancelSelect\" id=\"CancelSelectButton\" CausesValidation=\"false\" />";

//                Edit = new Foot();
//                Edit.Left = "<asp:LinkButton runat=\"server\" Text=\"更新\" CommandName=\"Update\" id=\"UpdateButton\" CausesValidation=\"true\" />";
//                Edit.Middle = "<asp:LinkButton runat=\"server\" Text=\"取消\" CommandName=\"Cancel\" id=\"UpdateCancelButton\" CausesValidation=\"false\" />";

//                Inst = new Foot();
//                Inst.Left = "<asp:LinkButton runat=\"server\" Text=\"插入\" CommandName=\"Insert\" id=\"InsertButton\" CausesValidation=\"true\" />";
//                Inst.Middle = "<asp:LinkButton runat=\"server\" Text=\"取消\" CommandName=\"Cancel\" id=\"InsertCancelButton\" CausesValidation=\"false\" />";
//            }
//        }

//        private class Cell : Base
//        {
//            public CellName ItemName;
//            public CellValue ItemValue;

//            public override string Content
//            {
//                get
//                {
//                    if (ItemName == null && ItemValue == null) return null;
//                    StringBuilder sb = new StringBuilder();
//                    if (ItemName != null && !String.IsNullOrEmpty(ItemName.Content)) sb.Append(ItemName.ToString());
//                    if (ItemValue != null && !String.IsNullOrEmpty(ItemValue.Content)) sb.Append(ItemValue.ToString());
//                    return sb.ToString();
//                }
//                set { }
//            }

//            public Cell()
//            {
//                CssClass = "Item";
//            }

//            public Cell(String name, String val)
//            {
//                ItemName = new CellName(name);
//                ItemValue = new CellValue(val);
//                CssClass = "Item";
//            }
//        }

//        private class CellName : Base
//        {
//            public CellName(String content)
//            {
//                Content = content;
//                CssClass = "ItemName";
//            }
//        }

//        private class CellValue : Base
//        {
//            public CellValue(String content)
//            {
//                Content = content;
//                CssClass = "ItemValue";
//            }
//        }
//        #endregion
//    }
//}