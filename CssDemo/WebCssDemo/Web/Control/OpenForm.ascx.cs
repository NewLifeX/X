using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;

public partial class Control_OpenForm : System.Web.UI.UserControl
{
    private String _BtText;
    /// <summary>按键文本</summary>
    public String BtText
    {
        get { return _BtText; }
        set { _BtText = value; }
    }

    private String _DialogWidth;
    /// <summary>窗口宽度</summary>
    public String DialogWidth
    {
        get { return _DialogWidth; }
        set { _DialogWidth = value; }
    }

    private String _DialogHeight;
    /// <summary>窗口高度</summary>
    public String DialogHeight
    {
        get { return _DialogHeight; }
        set { _DialogHeight = value; }
    }

    private String _Url;
    /// <summary>url地址</summary>
    public String Url
    {
        get { return _Url; }
        set { _Url = value; }
    }

    private String _Title;
    /// <summary>标题</summary>
    public String Title
    {
        get { return _Title; }
        set { _Title = value; }
    }

    private Boolean _IsButtonStyle = true;
    /// <summary>是否使用按钮样式</summary>
    public Boolean IsButtonStyle
    {
        get { return _IsButtonStyle; }
        set { _IsButtonStyle = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {

    }
}