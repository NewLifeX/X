﻿﻿using System;
using System.Text;
using System.Web.UI;
using NewLife.Collections;
using NewLife.CommonEntity;
using NewLife.Web;
using XCode;
using XControl;

#if NET4
using System.Collections.Generic;
#else
using NewLife.Collections;
#endif

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

    protected override void OnPreLoad(EventArgs e)
    {
        base.OnPreLoad(e);

        AutoAddParamForAdd();
    }

    /// <summary>自动为添加按钮附加参数，Request中，只要是当前实体成员的参数，否附加上去</summary>
    void AutoAddParamForAdd()
    {
        if (Request.QueryString == null || Request.QueryString.Count < 1) return;
        if (EntityType == null) return;

        LinkBox lb = ControlHelper.FindControlByField<LinkBox>(Page, "lbAdd");
        if (lb == null) return;

        StringBuilder sb = new StringBuilder();
        IEntityOperate op = EntityFactory.CreateOperate(EntityType);
        HashSet<String> hs = new HashSet<string>(op.FieldNames, StringComparer.OrdinalIgnoreCase);
        foreach (String item in Request.QueryString.Keys)
        {
            // 仅接受实体类成员
            if (!hs.Contains(item)) continue;

            if (sb.Length > 0) sb.Append("&");
            sb.AppendFormat("{0}={1}", item, Request.QueryString[item]);
        }
        if (sb.Length > 0)
        {
            if (!lb.Url.Contains("?"))
                lb.Url += "?" + sb;
            else
                lb.Url += "&" + sb;
        }
    }
}

/// <summary>实体列表页面基类</summary>
public class MyEntityList<TEntity> : MyEntityList where TEntity : Entity<TEntity>, new()
{
    /// <summary>实体类</summary>
    public override Type EntityType { get { return base.EntityType ?? (base.EntityType = typeof(TEntity)); } set { base.EntityType = value; } }
}