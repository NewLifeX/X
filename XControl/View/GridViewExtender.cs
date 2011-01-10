using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.WebControls;
using System.Drawing;
using System.Web.UI;
using System.Web;

namespace XControl.View
{
    /// <summary>
    /// GridView控件扩展
    /// </summary>
    [TargetControlType(typeof(GridView))]
    public class GridViewExtender : ExtenderControl<GridView>
    {
        #region 属性
        /// <summary>选中项背景颜色</summary>
        public Color SelectedRowBackColor
        {
            get { return GetPropertyValue<Color>("SelectedRowBackColor", Color.Empty); }
            set { SetPropertyValue<Color>("SelectedRowBackColor", value); }
        }

        /// <summary>请求字符串中作为键值的参数</summary>
        public String RequestKeyName
        {
            get { return GetPropertyValue<String>("RequestKeyName", "ID"); }
            set { SetPropertyValue<String>("RequestKeyName", value); }
        }

        /// <summary>客户端单击行时执行脚本</summary>
        public String OnRowClientClick
        {
            get { return GetPropertyValue<String>("OnRowClientClick", String.Empty); }
            set { SetPropertyValue<String>("OnRowClientClick", value); }
        }

        /// <summary>客户端双击行时执行脚本</summary>
        public String OnRowDoubleClientClick
        {
            get { return GetPropertyValue<String>("OnRowDoubleClientClick", String.Empty); }
            set { SetPropertyValue<String>("OnRowDoubleClientClick", value); }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            GridView gv = TargetControl;
            if (gv == null) return;

            if (Enabled && gv.Visible)
            {
                if (gv.Rows.Count > 0)
                {
                    foreach (GridViewRow item in gv.Rows)
                    {
                        if (item.RowType != DataControlRowType.DataRow) continue;

                        Object keyValue = gv.DataKeys[item.RowIndex].Value;

                        if (SelectedRowBackColor != Color.Empty)
                        {
                            item.Attributes["onclick"] = String.Format("style.backgroundColor=!style.backgroundColor?'#ffcccc':''", SelectedRowBackColor.ToString());

                            if (HttpContext.Current != null && HttpContext.Current.Request != null)
                            {
                                if (keyValue != null && String.Equals(keyValue.ToString(), HttpContext.Current.Request[RequestKeyName]))
                                {
                                    //item.Style[HtmlTextWriterStyle.BackgroundColor] = SelectedRowBackColor.ToString();
                                    item.BackColor = SelectedRowBackColor;
                                }
                            }
                        }

                        //String ret = String.Format("{0}|||{1}", id, e.Row.Cells[1].Text);
                        //item.Attributes["ondblclick"] = String.Format("window.returnValue='{0}';window.close();", ret.Replace("'", "\\'"));
                    }
                }
            }
        }
        #endregion
    }
}