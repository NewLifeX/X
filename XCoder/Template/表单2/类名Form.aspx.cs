using System;
using System.Collections.Generic;
using System.Web;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using NewLife.Web;
using XControl;
using <#=Config.NameSpace#>;

public partial class <#=ClassName#>Form : PageBase
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

            // 添加/编辑 按钮需要添加/编辑权限
            if (EntityID > 0)
                UpdateButton.Visible = Acquire(PermissionFlags.Update);
            else
                UpdateButton.Visible = Acquire(PermissionFlags.Insert);
        }
    }

    protected void UpdateButton_Click(object sender, EventArgs e)
    {
        // 添加/编辑 按钮需要添加/编辑权限
        if (EntityID > 0 && !Acquire(PermissionFlags.Update))
        {
            WebHelper.Alert("没有编辑权限！");
            return;
        }
        if (EntityID <= 0 && !Acquire(PermissionFlags.Insert))
        {
            WebHelper.Alert("没有添加权限！");
            return;
        }

<# 
        foreach(IDataColumn Field in Table.Columns) { 
            String pname = GetPropertyName(Field);
            if(Field.PrimaryKey) continue;
            String frmName = "frm" + pname;
        #>
        //if (!WebHelper.CheckEmptyAndFocus(<#=frmName#>, null)) return;<#}#>
        <# 
        foreach(IDataColumn Field in Table.Columns) { 
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