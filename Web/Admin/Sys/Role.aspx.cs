using System;
using NewLife.CommonEntity;
using NewLife.Reflection;
using NewLife.Web;
using XCode;

public partial class Pages_Role : MyEntityList
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return CommonManageProvider.Provider.RoleType; } set { base.EntityType = value; } }

    protected void Page_Load(object sender, EventArgs e)
    {
        Type type = EntityType;
        ods.TypeName = type.FullName;
        ods.DataObjectTypeName = type.FullName;
    }

    protected void Button1_Click(object sender, EventArgs e)
    {
        if (!Manager.Acquire(PermissionFlags.Insert))
        {
            WebHelper.Alert("没有添加权限！");
            return;
        }

        Label_Info.Text = "";

        if (string.IsNullOrEmpty(TextBox_Name.Text))
        {
            Label_Info.Text = "角色名不能为空";
            TextBox_Name.Focus();
            return;
        }

        try
        {
            IRole role = TypeX.CreateInstance(EntityType) as IRole;

            role.Name = TextBox_Name.Text;
            (role as IEntity).Insert();

            TextBox_Name.Text = "";


            //Label_Info.Text = "添加成功";
            WebHelper.Alert("添加成功！");

            //重新绑定数据
            gv.DataBind();
        }
        catch (Exception ex)
        {
            //异常发生时，要给出错误提示
            //Label_Info.Text = "添加失败！" + ex.Message;
            WebHelper.Alert("添加失败！" + ex.Message);
        }
    }
}