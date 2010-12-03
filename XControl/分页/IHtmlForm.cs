using System;
using System.Collections.Generic;
using System.Text;
using System.Web.UI;

namespace XControl
{
    internal interface IHtmlForm
    {
        // Methods
        void RenderControl(HtmlTextWriter writer);
        void SetRenderMethodDelegate(RenderMethod renderMethod);

        // Properties
        string ClientID { get; }
        string Method { get; }
    }
}
