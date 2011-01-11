using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;
using System.Drawing;
using System.Web.UI;
using System.Web;
using System.ComponentModel;

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

        /// <summary>客户端单击行时执行脚本，{datakey}代表键值，{cell0}代表单元格值</summary>
        [Description("客户端单击行时执行脚本，{datakey}代表键值，{cell0}代表单元格值")]
        public String OnRowClientClick
        {
            get { return GetPropertyValue<String>("OnRowClientClick", String.Empty); }
            set { SetPropertyValue<String>("OnRowClientClick", value); }
        }

        /// <summary>客户端双击行时执行脚本，{datakey}代表键值，{cell0}代表单元格值</summary>
        [Description("客户端双击行时执行脚本，{datakey}代表键值，{cell0}代表单元格值")]
        public String OnRowDoubleClientClick
        {
            get { return GetPropertyValue<String>("OnRowDoubleClientClick", String.Empty); }
            set { SetPropertyValue<String>("OnRowDoubleClientClick", value); }
        }
        #endregion

        #region 扩展属性

        #endregion

        #region 方法
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
                    String js = String.Format("style.backgroundColor=!style.backgroundColor?'#ffcccc':'';", SelectedRowBackColor);
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
    }

    /// <summary>
    /// GridView扩展控件设计时
    /// </summary>
    public class GridViewExtenderDesigner : ExtenderControlDesigner<GridViewExtender, GridView>
    {
    }
}