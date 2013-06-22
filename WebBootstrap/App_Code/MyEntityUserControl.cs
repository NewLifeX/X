﻿using System;
using System.Web.UI;
using NewLife.CommonEntity;
using XCode;

/// <summary>实体用户控件基类</summary>
public class MyEntityUserControl : UserControl
{
    #region 管理页控制器
    private Type _EntityType;
    /// <summary>实体类</summary>
    public virtual Type EntityType { get { return _EntityType; } set { _EntityType = value; } }

    /// <summary>表单控制器</summary>
    protected IEntityForm EntityForm;

    protected override void OnInit(EventArgs e)
    {
        EntityForm = ManageProvider.Provider.GetService<IEntityForm>().Init(this, EntityType);

        base.OnInit(e);
    }
    #endregion
}

/// <summary>实体表单页面基类</summary>
public class MyEntityUserControl<TEntity> : MyEntityUserControl where TEntity : Entity<TEntity>, new()
{
    /// <summary>实体类</summary>
    public override Type EntityType { get { return base.EntityType ?? (base.EntityType = typeof(TEntity)); } set { base.EntityType = value; } }

    /// <summary>实体</summary>
    public virtual TEntity Entity { get { return EntityForm == null ? null : EntityForm.Entity as TEntity; } set { if (EntityForm != null) EntityForm.Entity = value; } }
}