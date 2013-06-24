using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Web;
using XCode;

/// <summary>实体表单基类</summary>
/// <typeparam name="TKey">主键类型</typeparam>
/// <typeparam name="TEntity">表单实体类</typeparam>
public class EntityForm<TKey, TEntity> : NewLife.CommonEntity.Web.EntityForm<TKey, TEntity> where TEntity : Entity<TEntity>, new()
{
    /// <summary>是否管理员</summary>
    public Boolean IsAdmin { get { return Current.RoleName == "管理员"; } }

    /// <summary>校验权限</summary>
    /// <returns></returns>
    public override Boolean CheckPermission()
    {
        if (base.CheckPermission()) return true;

        //Response.Redirect("../../Admin/Login.aspx");
        WebHelper.AlertAndEnd("无权访问【" + PermissionName + "】！");
        return false;
    }

    protected override void OnPreLoad(EventArgs e)
    {
        base.OnPreLoad(e);

        Control btn = SaveButton;
        if (!Page.IsPostBack)
        {
            if (btn != null)
            {
                btn.Visible = CanSave;

                if (btn is IButtonControl) (btn as IButtonControl).Text = IsNullKey ? "提交" : "提交";
            }
        }
    }

    protected void CloseWindow(String msg)
    {
        //ClientScript.RegisterStartupScript(this.GetType(), "Close", "parent.Dialog.CloseSelfDialog(frameElement);", true);
        String js = null;
        if (!String.IsNullOrEmpty(msg)) js += "alert('" + Js.Encode(msg) + "');";
        js += "parent.Dialog.CloseSelfDialog(frameElement);";
        WebHelper.WriteScript(js);
        //Response.End();
    }

    protected void CloseAndRefreshWindow(String msg)
    {
        //ClientScript.RegisterStartupScript(this.GetType(), "CloseAndRefresh", "parent.Dialog.CloseAndRefresh(frameElement);", true);
        String js = null;
        if (!String.IsNullOrEmpty(msg)) js += "alert('" + Js.Encode(msg) + "');";
        js += "parent.Dialog.CloseAndRefresh(frameElement);";
        WebHelper.WriteScript(js);
        Response.End();
    }
}