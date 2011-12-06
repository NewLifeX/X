using System;
using System.Web.UI;
using NewLife.CommonEntity;
using XCode;

/// <summary>实体列表页面基类</summary>
public abstract class MyEntityList : Page
{
    #region 管理页控制器
    private Type _EntityType;
    /// <summary>实体类</summary>
    public virtual Type EntityType { get { return _EntityType; } set { _EntityType = value; } }

    /// <summary>管理页控制器</summary>
    protected IManagePage Manager;

    protected override void OnPreInit(EventArgs e)
    {
        Manager = ManageProvider.Provider.GetService<IManagePage>().Init(this, EntityType);

        base.OnPreInit(e);
    }
    #endregion
}

/// <summary>实体列表页面基类</summary>
public class MyEntityList<TEntity> : MyEntityList where TEntity : Entity<TEntity>, new()
{
    /// <summary>实体类</summary>
    public override Type EntityType { get { return base.EntityType ?? (base.EntityType = typeof(TEntity)); } set { base.EntityType = value; } }
}