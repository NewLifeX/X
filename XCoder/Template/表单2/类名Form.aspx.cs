using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Web;
using XControl;

public partial class <#=ClassName#>Form : System.Web.UI.Page
{
    /// <summary>编号</summary>
    public Int32 EntityID { get { return WebHelper.RequestInt("ID"); } }

    private <#=ClassName#> _Entity;
    /// <summary><#=ClassDescription#></summary>
    public <#=ClassName#> Entity
    {
        get { return _Entity ?? (_Entity = <#=ClassName#>.FindByKeyForEdit(EntityID)); }
        set { _Entity = value; }
    }

    protected void Page_Load(object sender, EventArgs e)
    {
        if (!Page.IsPostBack)
        {
            DataBind();
        }
    }

    protected void UpdateButton_Click(object sender, EventArgs e)
    {<# 
        foreach(XField Field in Table.Fields) { 
            String pname = GetPropertyName(Field);
            if(Field.PrimaryKey) continue;
            String frmName = "frm" + pname;
        #>
        //if (!WebHelper.CheckEmptyAndFocus(<#=frmName#>, null)) return;<#}#>
        <# 
        foreach(XField Field in Table.Fields) { 
            if(Field.PrimaryKey) continue;
            String pname = GetPropertyName(Field);
            String frmName = "frm" + pname;
            TypeCode code = Type.GetTypeCode(Field.DataType);
        if(code == TypeCode.String){#>
        Entity.<#=pname#> = <#=frmName#>.Text;<#
        }else if(code == TypeCode.Int32 || code == TypeCode.Double || code == TypeCode.DateTime || code == TypeCode.Decimal){#>
        Entity.<#=pname#> = <#=frmName#>.Value;<#
        }else if(code == TypeCode.Boolean){#>
        Entity.<#=pname#> = <#=frmName#>.Checked;<#
        }}#>

        try
        {
            Entity.Save();
            //WebHelper.AlertAndRedirect("成功！", "<#=ClassName#>.aspx");
            ClientScript.RegisterStartupScript(this.GetType(), "alert", "alert('成功！');parent.Dialog.CloseAndRefresh(frameElement);", true);
        }
        catch (Exception ex)
        {
            WebHelper.Alert("失败！" + ex.Message);
        }
    }
}