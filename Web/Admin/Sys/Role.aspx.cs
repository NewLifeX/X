using System;
using NewLife.CommonEntity;
using NewLife.Reflection;
using NewLife.Web;
using XCode;
using XCode.Membership;

public partial class Pages_Role : MyEntityList
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return ManageProvider.Provider.GetService<IRole>().GetType(); } set { base.EntityType = value; } }

    IEntityOperate Factory { get { return EntityFactory.CreateOperate(EntityType); } }

    protected void Page_Load(object sender, EventArgs e)
    {
        Type type = EntityType;
        ods.TypeName = type.FullName;
        ods.DataObjectTypeName = type.FullName;
    }

    protected void btnAdd_Click(object sender, EventArgs e)
    {
        if (!Manager.Acquire(PermissionFlags.Insert))
        {
            WebHelper.Alert("没有添加权限！");
            return;
        }

        if (string.IsNullOrEmpty(txtName.Text))
        {
            WebHelper.Alert("角色名不能为空！");
            txtName.Focus();
            return;
        }

        try
        {
            IRole role = Factory.Create(false) as IRole;

            role.Name = txtName.Text;

            Int32[] vs = gvExt.SelectedIntValues;
            if (vs != null && vs.Length > 0)
            {
                IRole entity = FindByRoleID(vs[0]);
                role.Permission = entity.Permission;
            }

            role.Save();

            txtName.Text = "";

            WebHelper.Alert("添加成功！");

            //重新绑定数据
            gv.DataBind();
        }
        catch (Exception ex)
        {
            //异常发生时，要给出错误提示
            WebHelper.Alert("添加失败！" + ex.Message);
        }
    }

    IRole FindByRoleID(Int32 id)
    {
        if (id <= 0) return null;

        return Factory.FindWithCache("ID", id) as IRole;
    }
}