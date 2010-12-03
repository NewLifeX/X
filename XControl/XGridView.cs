using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

using System.Windows.Forms;
using System.Diagnostics;

using System.Reflection;
using System.Web.UI.Design;
using System.ComponentModel.Design;
using System.Drawing;
using System.Drawing.Design;
using System.Web.UI.Design.WebControls;
using XCode.Configuration;

namespace XControl
{
    /// <summary>
    /// 重写GridView
    /// </summary>
    [DefaultProperty("Text")]
    [ToolboxData("<{0}:XGridView runat=server></{0}:XGridView>")]
    [Designer(typeof(XGridViewDesigner))]
    [ToolboxBitmap(typeof(GridView))]
    public class XGridView : GridView
    {
        private Boolean _SetEntry = false;
        /// <summary>
        /// 设置实体字段
        /// </summary>
        [Category(" 专用属性"), DefaultValue(false), Description("设置实体字段"), DesignOnly(true)]
        public Boolean SetEntry
        {
            get
            {
                return _SetEntry;
            }
            set
            {
                try
                {
                    CreateEntryColumns();
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                    MessageBox.Show(ex.Message, "XGridView");
                }
                _SetEntry = value;
            }
        }

        /// <summary>
        /// 建立实体列字段集。把已经建立好的列转为实体列。
        /// 主要修改各列的HeaderText为中文，同时调整各列顺序为对应的实体属性的顺序。
        /// </summary>
        private void CreateEntryColumns()
        {
            Type t = GetEntryType();
            if (t == null) return;
            List<FieldItem> list = Config.AllFields(t);
            if (list == null) return;
            XGridView gv = this;
            if (gv == null) return;

            // 思路
            // 把所有属于实体类的列字段从cs中删除，添加到一个临时列表tcs中
            // 从头开始遍历实体类的属性，判断每个属性是否存在于tcs中，存在则修改HeaderText为中文，并添加到ncs中

            Dictionary<String, DataControlField> tcs = new Dictionary<String, DataControlField>();
            foreach (DataControlField dcf in gv.Columns)
            {
                foreach (FieldItem fi in list)
                {
                    if (fi.Name == dcf.HeaderText)
                    {
                        // 因在遍历cs的过程中不能修改cs，所以这里暂时不从cs中删除dcf，遍历完成后删除
                        tcs.Add(fi.Name, dcf);
                        break;
                    }
                }
            }
            // 删除属于实体类的列
            foreach (DataControlField dcf in tcs.Values)
            {
                gv.Columns.Remove(dcf);
            }
            String keyname = (gv.DataKeyNames != null && gv.DataKeyNames.Length > 0) ? gv.DataKeyNames[0] : null;
            // 按顺序添加实体类的调整后的列
            foreach (FieldItem fi in list)
            {
                if (tcs.ContainsKey(fi.Name))
                {
                    DataControlField dcf = tcs[fi.Name];
                    //如果设置了主键字段，或是标识字段，则作为模版列绑定Select功能
                    if (!String.IsNullOrEmpty(keyname) && fi.Name == keyname || String.IsNullOrEmpty(keyname) && fi.DataObjectField.IsIdentity)
                    {
                        TemplateField tf = new TemplateField();
                        String str = String.Format("<asp:LinkButton id=\"{0}LinkButton1\" runat=\"server\"  CausesValidation=\"{1}\" Text='<%# Eval(\"{0}\") %>' CommandName=\"Select\"></asp:LinkButton>", fi.Name, Boolean.FalseString);
                        tf.ItemTemplate = ControlParser.ParseTemplate((IDesignerHost)Site.GetService(typeof(IDesignerHost)), str); ;
                        dcf = tf as DataControlField;
                    }
                    dcf.HeaderText = (String.IsNullOrEmpty(fi.CnName)) ? fi.Name : fi.CnName;

                    if (fi.Info.PropertyType == typeof(Int32))
                    {
                        dcf.HeaderStyle.Width = Unit.Pixel(40);
                        dcf.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
                    }
                    else if (fi.Info.PropertyType == typeof(DateTime) || fi.Info.PropertyType == typeof(Nullable<DateTime>))
                    {
                        dcf.HeaderStyle.Width = Unit.Pixel(140);
                        dcf.ItemStyle.HorizontalAlign = HorizontalAlign.Center;
                    }
                    else
                    {
                        dcf.HeaderStyle.Width = Unit.Pixel(150);
                    }
                    gv.Columns.Add(dcf);
                }
            }

            //额外工作，设置ObjectDataSource的SelectCountMethod
            String datasourceid = gv.DataSourceID;
            if (String.IsNullOrEmpty(datasourceid)) return;
            // 找到数据绑定控件ObjectDataSource
            ObjectDataSource obj = gv.Page.FindControl(datasourceid) as ObjectDataSource;
            if (obj == null) return;
            if (String.IsNullOrEmpty(obj.SelectCountMethod)) obj.SelectCountMethod = "SelectCount";
            if (obj.SelectParameters["startRowIndex"] != null && String.IsNullOrEmpty(obj.SelectParameters["startRowIndex"].DefaultValue))
            {
                obj.SelectParameters["startRowIndex"].DefaultValue = "0";
            }
            if (obj.SelectParameters["maximumRows"] != null && String.IsNullOrEmpty(obj.SelectParameters["maximumRows"].DefaultValue))
            {
                obj.SelectParameters["maximumRows"].DefaultValue = Int32.MaxValue.ToString();
            }
        }

        private Boolean _SetDefaultStype = false;
        /// <summary>
        /// 设置样式
        /// </summary>
        [Category(" 专用属性"), DefaultValue(false), Description("设置样式"), DesignOnly(true)]
        public Boolean SetDefaultStype
        {
            get { return _SetDefaultStype; }
            set
            {
                try
                {
                    XGridView gv = this;
                    if (gv == null) return;
                    gv.BorderColor = System.Drawing.Color.FromArgb(0x82, 0xA8, 0xCF);
                    gv.BorderStyle = System.Web.UI.WebControls.BorderStyle.Solid;
                    gv.BorderWidth = Unit.Pixel(1);
                    gv.Font.Underline = false;
                    gv.SelectedRowStyle.BackColor = System.Drawing.Color.FromArgb(0xc0, 0xff, 0xc0);
                    gv.AllowPaging = true;
                    gv.AlternatingRowStyle.BackColor = System.Drawing.Color.FromArgb(0xef, 0xe6, 0xf7);
                    gv.PagerStyle.HorizontalAlign = HorizontalAlign.Right;
                    gv.PagerStyle.Font.Size = FontUnit.Point(12);
                    gv.PagerStyle.Font.Underline = false;
                    if (gv.Columns == null || gv.Columns.Count < 1) return;
                    foreach (DataControlField dcf in gv.Columns)
                    {
                        dcf.HeaderStyle.HorizontalAlign = HorizontalAlign.Center;
                        dcf.HeaderStyle.BorderWidth = Unit.Pixel(1);
                        dcf.HeaderStyle.BorderColor = System.Drawing.Color.FromArgb(0x82, 0xA8, 0xCF);
                        dcf.HeaderStyle.BorderStyle = System.Web.UI.WebControls.BorderStyle.Solid;
                        dcf.HeaderStyle.BackColor = System.Drawing.Color.FromArgb(0xe3, 0xef, 0xff);
                        dcf.HeaderStyle.Height = Unit.Pixel(20);
                        dcf.HeaderStyle.Font.Bold = true;
                        dcf.HeaderStyle.ForeColor = System.Drawing.Color.Black;
                        //dcf.HeaderStyle.Font.Size = FontUnit.Point(11);
                        dcf.HeaderStyle.Font.Underline = false;
                        dcf.ItemStyle.BorderWidth = Unit.Pixel(1);
                        dcf.ItemStyle.BorderColor = System.Drawing.Color.FromArgb(0x82, 0xA8, 0xCF);
                        dcf.ItemStyle.BorderStyle = System.Web.UI.WebControls.BorderStyle.Solid;
                        dcf.ItemStyle.Height = Unit.Pixel(25);
                    }
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                    MsgBox(ex.Message);
                }
                _SetDefaultStype = value;
            }
        }

        /// <summary>
        /// 取得实体类型
        /// </summary>
        /// <returns></returns>
        private Type GetEntryType()
        {
            if (this.Site == null || this.Site.Component == null || !(Site.Component is XGridView)) return null;
            XGridView gv = this;
            if (gv == null || gv.Page == null) return null;
            String datasourceid = gv.DataSourceID;
            if (String.IsNullOrEmpty(datasourceid)) return null;
            // 找到数据绑定控件ObjectDataSource
            ObjectDataSource obj = gv.Page.FindControl(datasourceid) as ObjectDataSource;
            if (obj == null) return null;
            // 找到实体类型
            String typeName = obj.DataObjectTypeName;
            if (String.IsNullOrEmpty(typeName)) typeName = obj.TypeName;
            Type t = Type.GetType(typeName);
            if (t == null)
            {
                t = System.Web.Compilation.BuildManager.GetType(typeName, false, true);
                if (t == null)
                {
                    Assembly[] abs = AppDomain.CurrentDomain.GetAssemblies();
                    foreach (Assembly ab in abs)
                    {
                        t = ab.GetType(typeName, false, true);
                        if (t != null) break;
                    }
                    if (t == null)
                    {
                        MsgBox("无法定位数据组件类：" + typeName + "，可能你需要编译一次数据组件类所在项目。");
                        return null;
                    }
                }
            }
            return t;
            //// 检查该实体类是否继承自XCode.Entry.Entry
            //while (t.BaseType != typeof(Object))
            //{
            //    if (t.BaseType.Name == "Entry") return t;
            //    t = t.BaseType;
            //}
            //return null;
        }

        /// <summary>
        /// 已重写。切换页时取消原选择
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPageIndexChanging(GridViewPageEventArgs e)
        {
            this.SelectedIndex = -1;
            base.OnPageIndexChanging(e);
        }

        private static void MsgBox(String msg)
        {
            XLog.Trace.Debug();
            System.Windows.Forms.MessageBox.Show(msg + "\n日志目录：" + XLog.Trace.LogDir, "XGridView");
        }
    }

    /// <summary>
    /// 在可视化设计器中为 XControl.XGridView 控件提供设计时支持。
    /// </summary>
    public class XGridViewDesigner : GridViewDesigner
    {
        /// <summary>
        /// 当关联控件的数据源架构更改时，将调用它。
        /// </summary>
        protected override void OnSchemaRefreshed()
        {
            base.OnSchemaRefreshed();
            //先生成原来的，再生成新的
            //if (!InTemplateMode && !IgnoreSchemaRefreshedEvent)
            if (!InTemplateMode)
            {
                try
                {
                    XGridView gv = base.Component as XGridView;
                    if (gv == null) return;
                    DataControlFieldCollection cs = gv.Columns;
                    if (cs == null || cs.Count < 1) return;
                    gv.SetEntry = !gv.SetEntry;
                    gv.SetDefaultStype = !gv.SetDefaultStype;
                    //gv.CreateEntryColumns();
                    //gv.SetDefaultStype();
                    //gv.Columns = cs;
                    //gv.Columns.Clear();
                    //foreach (DataControlField dcf in cs) gv.Columns.Add(dcf);
                }
                catch (Exception ex)
                {
                    Trace.WriteLine(ex.Message);
                    MessageBox.Show(ex.Message, "XGridViewDesigner");
                }
            }
        }

        private Boolean IgnoreSchemaRefreshedEvent
        {
            get
            {
                Type t = typeof(GridViewDesigner);
                if (t == null) return false;
                PropertyInfo pi = t.GetProperty("_ignoreSchemaRefreshedEvent", BindingFlags.Instance | BindingFlags.NonPublic);
                if (pi == null) return false;
                return (Boolean)pi.GetValue((this as GridViewDesigner), null);
            }
        }

        private ISite Site
        {
            get
            {
                return base.Component.Site;
            }
        }
    }
}