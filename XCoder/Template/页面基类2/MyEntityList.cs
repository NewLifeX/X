using System;
using System.Web.UI;
using NewLife.CommonEntity;

/// <summary>实体列表页面基类</summary>
public abstract class MyEntityList : Page
{
    #region 管理页控制器
    private Type _EntityType;
    /// <summary>实体类</summary>
    public virtual Type EntityType { get { return _EntityType; } set { _EntityType = value; } }

    /// <summary>管理页控制器</summary>
    protected IManagerPage Manager;

    protected override void OnPreInit(EventArgs e)
    {
        Manager = CommonManageProvider.Provider.CreatePage(this, EntityType);

        base.OnPreInit(e);
    }
    #endregion
}