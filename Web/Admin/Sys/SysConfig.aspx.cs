using System;
using NewLife.Common;
using XControl;
using NewLife.Reflection;

public partial class Admin_SysConfig : System.Web.UI.Page
{
    /// <summary>系统配置。如果重载，修改这里即可。</summary>
    public static SysConfig Config { get { return SysConfig.Current; } }

    protected void Page_Load(object sender, EventArgs e)
    {
        ClientScript.RegisterClientScriptResource(typeof(NumberBox), "XControl.TextBox.Validator.js");
        ClientScript.RegisterClientScriptResource(typeof(DateTimePicker), "XControl.TextBox.DateTimePicker.WdatePicker.js");
    }

    protected void btnSave_Click(object sender, EventArgs e)
    {
        foreach (PropertyInfoX pi in TypeX.Create(Config.GetType()).Properties)
        {
            object v = Request["frm" + pi.Name];
            if (pi.Type == typeof(Boolean)) v = "" + v == "on" ? "true" : "false";
            pi.SetValue(Config, v);
        }
        Config.Save();
    }
}