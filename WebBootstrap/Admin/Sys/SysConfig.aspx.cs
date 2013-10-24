using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Xml.Serialization;
using NewLife.Common;
using NewLife.Log;
using NewLife.Reflection;
using NewLife.Web;
using XControl;

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
            foreach (PropertyInfo pi in GetProperties())
            {
                object v = Request["frm" + pi.Name];
                if (pi.PropertyType == typeof(Boolean)) v = "" + v == "on" ? "true" : "false";
                //pi.SetValue(Config, v);
                Reflect.SetValue(Config, pi, v);
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

    protected PropertyInfo[] GetProperties()
    {
        PropertyInfo[] pis = Config.GetType().GetProperties();
        List<PropertyInfo> list = new List<PropertyInfo>();
        foreach (PropertyInfo item in pis)
        {
            if (AttributeX.GetCustomAttribute<XmlIgnoreAttribute>(item, true) == null) list.Add(item);
        }
        return list.ToArray();
    }

    protected String GetDisplayName(PropertyInfo pi)
    {
        return AttributeX.GetCustomAttributeValue<DisplayNameAttribute, String>(pi);
    }

    protected String GetDescription(PropertyInfo pi)
    {
        return AttributeX.GetCustomAttributeValue<DescriptionAttribute, String>(pi);
    }
}