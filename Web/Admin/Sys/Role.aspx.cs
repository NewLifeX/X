using System;
using NewLife.CommonEntity;
using NewLife.Reflection;
using NewLife.Web;
using XCode;

public partial class Pages_Role : MyEntityList
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return CommonManageProvider.Provider.RoleType; } set { base.EntityType = value; } }

    IEntityOperate Factory { get { return EntityFactory.CreateOperate(EntityType); } }

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

    protected void btnCopyRole_Click(object sender, EventArgs e)
    {
        IRole tmp = FindByRoleName(txtRoleTemplate.Text);
        if (tmp == null)
        {
            WebHelper.Alert("未指定模版角色！");
            txtRoleTemplate.Focus();
            return;
        }

        DoBatch("复制权限", delegate(IRole role)
        {
            if (role.ID == tmp.ID || role.Name == "管理员") return false;

            role.CopyRoleMenuFrom(tmp);

            return true;
        });
    }

    void DoBatch(String action, Func<IRole, Boolean> callback)
    {
        Int32[] vs = gvExt.SelectedIntValues;
        if (vs == null || vs.Length < 1) return;

        Int32 n = 0;
        IEntityOperate eop = Factory;
        eop.BeginTransaction();
        try
        {
            foreach (Int32 item in vs)
            {
                IRole entity = FindByRoleID(item);
                if (entity != null && callback(entity))
                {
                    entity.Save();
                    n++;
                }
            }

            eop.Commit();

            WebHelper.Alert("成功为" + n + "个部门" + action + "！");
        }
        catch (Exception ex)
        {
            eop.Rollback();

            WebHelper.Alert("操作失败！" + ex.Message);
        }

        if (n > 0) gv.DataBind();
    }

    IRole FindByRoleID(Int32 id)
    {
        if (id <= 0) return null;

        return Factory.FindWithCache("ID", id) as IRole;
    }

    IRole FindByRoleName(String name)
    {
        if (String.IsNullOrEmpty(name)) return null;

        return Factory.FindWithCache("Name", name) as IRole;
    }
}