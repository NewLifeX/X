using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.Design;
using System.Web.UI;

namespace XControl
{
    /// <summary>
    /// 扩展控件设计时
    /// </summary>
    /// <typeparam name="TControl">扩展控件</typeparam>
    /// <typeparam name="TTargetControl">目标控件</typeparam>
    public class ExtenderControlDesigner<TControl, TTargetControl> : ControlDesigner
        where TControl : ExtenderControl<TTargetControl>
        where TTargetControl : Control
    {
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string GetDesignTimeHtml()
        {
            return CreatePlaceHolderDesignTimeHtml();
        }
    }
}