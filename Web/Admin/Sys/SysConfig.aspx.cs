using System;
using NewLife.Common;
using XControl;
using NewLife.Reflection;
using NewLife.Web;
using NewLife.Log;

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
        try
        {
            foreach (PropertyInfoX pi in TypeX.Create(Config.GetType()).Properties)
            {
                object v = Request["frm" + pi.Name];
                if (pi.Type == typeof(Boolean)) v = "" + v == "on" ? "true" : "false";
                pi.SetValue(Config, v);
            }
            Config.Save();

            WebHelper.Alert("保存成功！");
        }
        catch (Exception ex)
        {
            // 因为这里很少可能失败，所以如果有错误，要记录下来
            XTrace.WriteException(ex);
            WebHelper.Alert("失败！" + ex.Message);
        }
    }
}