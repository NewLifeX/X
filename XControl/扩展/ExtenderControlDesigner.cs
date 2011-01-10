using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI.Design;

namespace XControl
{
    /// <summary>
    /// 扩展控件设计时
    /// </summary>
    /// <typeparam name="TControl">扩展控件</typeparam>
    public class ExtenderControlDesigner<TControl> : ControlDesigner where TControl : ExtenderControl
    {
    }
}