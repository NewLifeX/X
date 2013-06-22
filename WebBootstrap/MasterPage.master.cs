using System;
using NewLife.Reflection;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;

public partial class MasterPage : System.Web.UI.MasterPage
{
    private String _Title;
    /// <summary></summary>
    public String Title { get { return _Title; } set { _Title = value; } }

    private String _Navigation;
    /// <summary>导航</summary>
    public String Navigation
    {
        get { return _Navigation; }
        set { _Navigation = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        FieldInfoX fix = FieldInfoX.Create(Page.GetType(), "Manager");

        if (fix != null)
        {
            IManagePage manager = fix.GetValue(Page) as IManagePage;
            if (manager != null)
            {
                Title = manager.CurrentMenu.Name;
                Navigation = manager.Navigation;
            }
        }

        bool IsShowToolBar = NewLife.Web.WebHelper.RequestBool("SetToolBar");

        if (!String.IsNullOrEmpty(Request["SetToolBar"]) && !IsShowToolBar) SetToolBar(this, IsShowToolBar);
    }



    /// <summary>
    /// 设置导航栏
    /// </summary>
    /// <param name="control"></param>
    /// <param name="isShow"></param>
    /// <returns></returns>
    public static Boolean SetToolBar(Control control, Boolean isShow)
    {
        Boolean r = false;

        if (control != null)
        {
            Control box = control.FindControl("ToolBar");
            if (box != null)
            {
                box.Visible = isShow;
                r = true;
            }
        }
        return r;
    }

    /// <summary>
    /// 设置导航栏
    /// </summary>
    /// <param name="isShow"></param>
    /// <returns></returns>
    public static Boolean SetToolBar(Boolean isShow)
    {
        Page page = (Page)HttpContext.Current.Handler;
        if (page == null || page.Master == null) return false;
        return SetToolBar(page.Master, isShow);
    }
}
