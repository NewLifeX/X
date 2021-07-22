﻿using System;
using System.Collections.Generic;
using NewLife.Model;

namespace XCode.Model
{
    /// <summary>实体动作</summary>
    public enum EntityActions
    {
        /// <summary>保存</summary>
        Save = 0,

        /// <summary>插入</summary>
        Insert = 1,

        /// <summary>更新</summary>
        Update = 2,

        /// <summary>插入或更新</summary>
        Upsert = 3,

        /// <summary>删除</summary>
        Delete = 4,
    }

    /// <summary>实体延迟队列。缓冲合并对象，批量处理</summary>
    public class EntityDeferredQueue : DeferredQueue
    {
        #region 属性
        /// <summary>实体动作。默认Save保存</summary>
        public EntityActions Action { get; set; } = EntityActions.Save;

        /// <summary>数据会话，分表分库时使用</summary>
        public IEntitySession Session { get; set; }

        /// <summary>最大单行保存大小。大于该值时才采用批量保存，默认2</summary>
        public Int32 MaxSingle { get; set; } = 2;
        #endregion

        #region 方法
        /// <summary>处理一批</summary>
        /// <param name="list"></param>
        public override Int32 Process(IList<Object> list)
        {
            if (list.Count == 0) return 0;

            var rs = 0;

            // 数量少时，直接保存
            if (list.Count <= MaxSingle)
            {
                foreach (IEntity item in list)
                {
                    switch (Action)
                    {
                        case EntityActions.Save:
                            // 来自数据库，更新
                            if (item.IsFromDatabase)
                                rs += item.Update();
                            else
                                rs += item.Upsert(null, null, null, Session);
                            break;
                        case EntityActions.Insert:
                            rs += item.Insert();
                            break;
                        case EntityActions.Update:
                            rs += item.Update();
                            break;
                        case EntityActions.Upsert:
                            rs += item.Upsert(null, null, null, Session);
                            break;
                        case EntityActions.Delete:
                            rs += item.Delete();
                            break;
                    }
                }

                return rs;
            }

            // 区分Update和Upsert
            var us = new List<IEntity>();
            var ns = new List<IEntity>();
            var ps = new List<IEntity>();
            var ds = new List<IEntity>();
            foreach (IEntity item in list)
            {
                switch (Action)
                {
                    case EntityActions.Save:
                        {
                            // 来自数据库，更新
                            if (item.IsFromDatabase)
                                us.Add(item);
                            //// 空主键，插入
                            //else if (item.IsNullKey)
                            //    ns.Add(item);
                            // 其它 Upsert
                            else
                                ps.Add(item);
                        }
                        break;
                    case EntityActions.Insert:
                        ns.Add(item);
                        break;
                    case EntityActions.Update:
                        us.Add(item);
                        break;
                    case EntityActions.Upsert:
                        ps.Add(item);
                        break;
                    case EntityActions.Delete:
                        ds.Add(item);
                        break;
                }
            }

            if (us.Count > 0) rs += us.Update(null, Session);
            if (ns.Count > 0) rs += ns.Insert(null, Session);
            if (ps.Count > 0) rs += ps.Valid(true).Upsert(null, null, null, Session);
            if (ds.Count > 0) rs += ds.Delete(null, Session);

            return rs;
        }
        #endregion
    }
}