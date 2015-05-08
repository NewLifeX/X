/*
 * XCoder v4.4.2011.1031
 * 作者：nnhy/NEWLIFE
 * 时间：2011-11-07 16:54:58
 * 版权：版权所有 (C) 新生命开发团队 2011
*/
﻿using System;
using System.Web.UI;
using NewLife.CommonEntity;
using XCode;
using XCode.Membership;

/// <summary>实体表单页面基类</summary>
public class MyEntityForm : Page
{
    #region 管理页控制器
    private Type _EntityType;
    /// <summary>实体类</summary>
    public virtual Type EntityType { get { return _EntityType; } set { _EntityType = value; } }

    /// <summary>管理页控制器</summary>
    protected IManagePage Manager;

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

/// <summary>实体表单页面基类</summary>
public class MyEntityForm<TEntity> : MyEntityForm where TEntity : Entity<TEntity>, new()
{
    /// <summary>实体类</summary>
    public override Type EntityType { get { return base.EntityType ?? (base.EntityType = typeof(TEntity)); } set { base.EntityType = value; } }

    /// <summary>实体</summary>
    public virtual TEntity Entity { get { return EntityForm == null ? null : EntityForm.Entity as TEntity; } set { if (EntityForm != null) EntityForm.Entity = value; } }
}