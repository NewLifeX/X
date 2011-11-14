using System;
using System.Web.UI;
using NewLife.CommonEntity;

/// <summary>实体表单页面基类</summary>
public class MyEntityForm : Page
{
    #region 管理页控制器
    private Type _EntityType;
    /// <summary>实体类</summary>
    public virtual Type EntityType { get { return _EntityType; } set { _EntityType = value; } }

    /// <summary>管理页控制器</summary>
    protected IManagerPage Manager;

    /// <summary>表单控制器</summary>
    protected IEntityForm EntityForm;

    protected override void OnPreInit(EventArgs e)
    {
        // 让页面管理器先注册，因为页面管理器要控制权限
        Manager = ManageProvider.Provider.GetService<IManagePage>().Init(this, EntityType);
        EntityForm = ManageProvider.Provider.GetService<IEntityForm>().Init(this, EntityType);

        base.OnPreInit(e);
    }
    #endregion
}