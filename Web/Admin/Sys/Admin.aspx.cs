using System;
using System.IO;
using NewLife.CommonEntity;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Web;
using XCode;
using XCode.Membership;

public partial class Pages_Admin : MyEntityList
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return ManageProvider.Provider.UserType; } set { base.EntityType = value; } }

    IEntityOperate RoleFactory { get { return EntityFactory.CreateOperate(ManageProvider.Provider.GetService<IRole>().GetType()); } }

    protected void Page_Load(object sender, EventArgs e)
    {
        Type type = ManageProvider.Provider.GetService<IRole>().GetType();
        odsRole.TypeName = type.FullName;
        odsRole.DataObjectTypeName = type.FullName;
    }

    protected void btnEnable_Click(object sender, EventArgs e) { EnableOrDisable(true); }

    protected void btnDisable_Click(object sender, EventArgs e) { EnableOrDisable(false); }

    void EnableOrDisable(Boolean isenable)
    {
        DoBatch(isenable ? "启用" : "禁用", delegate(IUser admin)
        {
            if (admin.Enable != isenable)
            {
                admin.Enable = isenable;
                return true;
            }
            return false;
        });
    }

    void DoBatch(String action, Func<IUser, Boolean> callback)
    {
        Int32[] vs = gvExt.SelectedIntValues;
        if (vs == null || vs.Length < 1) return;

        Int32 n = 0;
        IEntityOperate eop = EntityFactory.CreateOperate(EntityType);
        eop.BeginTransaction();
        try
        {
            foreach (Int32 item in vs)
            {
                IEntity entity = eop.FindByKey(item);
                IUser admin = entity as IUser;
                if (admin != null && callback(admin))
                {
                    entity.Save();
                    n++;
                }
            }

            eop.Commit();

            WebHelper.Alert("成功" + action + n + "个管理员！");
        }
        catch (Exception ex)
        {
            eop.Rollback();

            WebHelper.Alert("操作失败！" + ex.Message);
        }

        if (n > 0) gv.DataBind();
    }

    protected void btnUpgradeToRole_Click(object sender, EventArgs e)
    {
        DoBatch("升级", delegate(IUser admin)
        {
            if (admin.RoleName != "管理员" || admin.Name == "admin") return false;

            IRole role = FindByRoleName(admin.FriendName);
            if (role == null)
            {
                role = RoleFactory.Create(false) as IRole;
                role.Name = admin.FriendName;
                role.Save();
            }

            admin.RoleID = role.ID;

            return true;
        });
    }

    IRole FindByRoleName(String name)
    {
        return RoleFactory.FindWithCache("Name", name) as IRole;
        //return MethodInfoX.Create(ManageProvider.Provider.RoleType, "Find", new Type[] { typeof(String), typeof(Object) }).Invoke(null, "Name", name) as IRole;
    }

    protected void btnDelete_Click(object sender, EventArgs e)
    {
        DoBatch("删除", delegate(IUser admin)
        {
            if (admin.Name == "admin") return false;

            (admin as IEntity).Delete();

            return false;
        });
    }

    protected void btnChangePass_Click(object sender, EventArgs e)
    {
        String file = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, @"..\WebData\Pass.txt");
        //if (File.Exists(file)) File.Delete(file);
        File.AppendAllText(file, String.Format("{0}{0}{1:yyyy-MM-dd HH:ss:mm} 随机生成密码{0}", Environment.NewLine, DateTime.Now));

        Random rnd = new Random((Int32)DateTime.Now.Ticks);

        DoBatch("修改密码", delegate(IUser admin)
        {
            if (admin.Name == "admin") return false;

            String pass = DataHelper.Hash(admin.Name + rnd.Next(0, 10000)).Substring(0, 8).ToUpper();
            admin.Password = DataHelper.Hash(pass);
            File.AppendAllText(file, String.Format("{0}\t{1}\t{2}\r\n", admin.DisplayName, admin.Name, pass));

            return true;
        });
    }
}