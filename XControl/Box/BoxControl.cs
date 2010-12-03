using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;
using System.ComponentModel;
using System.Drawing;

namespace XControl
{
    /// <summary>
    /// 弹出窗口
    /// </summary>
    [Description("弹出窗口控件")]
    [ToolboxData("<{0}:LinkBox runat=server></{0}:LinkBox>")]
    [ToolboxBitmap(typeof(Control))]
    public class BoxControl : Control
    {
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="e"></param>
        protected override void OnPreRender(EventArgs e)
        {
            base.OnPreRender(e);

            Page.ClientScript.RegisterClientScriptResource(typeof(BoxControl), "XControl.Box.Box.js");
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="writer"></param>
        protected override void Render(HtmlTextWriter writer)
        {
            //base.Render(writer);
        }
    }
}