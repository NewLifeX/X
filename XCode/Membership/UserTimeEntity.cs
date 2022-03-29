﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using NewLife;
using NewLife.Threading;
using XCode.Configuration;

namespace XCode.Membership
{
    /// <summary>用户模型</summary>
    public class UserModule : EntityModule
    {
        #region 静态引用
        /// <summary>字段名</summary>
        public class __
        {
            /// <summary>创建人</summary>
            public static String CreateUserID = nameof(CreateUserID);

            /// <summary>创建人</summary>
            public static String CreateUser = nameof(CreateUser);

            /// <summary>更新人</summary>
            public static String UpdateUserID = nameof(UpdateUserID);

            /// <summary>更新人</summary>
            public static String UpdateUser = nameof(UpdateUser);
        }
        #endregion

        #region 提供者
        /// <summary>当前用户提供者</summary>
        public IManageProvider Provider { get; set; }
        #endregion

        #region 构造函数
        /// <summary>实例化</summary>
        public UserModule() : this(null) { }

        /// <summary>实例化</summary>
        /// <param name="provider"></param>
        public UserModule(IManageProvider provider) => Provider = provider;
        #endregion

        /// <summary>初始化。检查是否匹配</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        protected override Boolean OnInit(Type entityType)
        {
            var fs = GetFields(entityType);
            foreach (var fi in fs)
            {
                if (fi.Type == typeof(Int32) || fi.Type == typeof(Int64))
                {
                    if (fi.Name.EqualIgnoreCase(__.CreateUserID, __.UpdateUserID)) return true;
                }
                else if (fi.Type == typeof(String))
                {
                    if (fi.Name.EqualIgnoreCase(__.CreateUser, __.UpdateUser)) return true;
                }
            }

            return false;
        }

        /// <summary>验证数据，自动加上创建和更新的信息</summary>
        /// <param name="entity"></param>
        /// <param name="isNew"></param>
        protected override Boolean OnValid(IEntity entity, Boolean isNew)
        {
            if (!isNew && !entity.HasDirty) return true;

            var fs = GetFields(entity.GetType());

            // 当前登录用户
            var prv = Provider ?? ManageProvider.Provider;
            //var user = prv?.Current ?? HttpContext.Current?.User?.Identity as IManageUser;
            var user = prv?.Current;
            if (user != null)
            {
                if (isNew)
                {
                    SetItem(fs, entity, __.CreateUserID, user.ID);
                    SetItem(fs, entity, __.CreateUser, user + "");
                    SetItem(fs, entity, __.UpdateUserID, user.ID);
                    SetItem(fs, entity, __.UpdateUser, user + "");
                }
                else
                {
                    SetNoDirtyItem(fs, entity, __.UpdateUserID, user.ID);
                    SetNoDirtyItem(fs, entity, __.UpdateUser, user + "");
                }
            }
            else
            {
                // 在没有当前登录用户的场合，把更新者清零
                SetNoDirtyItem(fs, entity, __.UpdateUserID, 0);
                SetNoDirtyItem(fs, entity, __.UpdateUser, "");
            }

            return true;
        }
    }

    /// <summary>时间模型</summary>
    public class TimeModule : EntityModule
    {
        #region 静态引用
        /// <summary>字段名</summary>
        public class __
        {
            /// <summary>创建时间</summary>
            public static String CreateTime = nameof(CreateTime);

            /// <summary>更新时间</summary>
            public static String UpdateTime = nameof(UpdateTime);
        }
        #endregion

        /// <summary>初始化。检查是否匹配</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        protected override Boolean OnInit(Type entityType)
        {
            var fs = GetFields(entityType);
            foreach (var fi in fs)
            {
                if (fi.Type == typeof(DateTime))
                {
                    if (fi.Name.EqualIgnoreCase(__.CreateTime, __.UpdateTime)) return true;
                }
            }

            return false;
        }

        /// <summary>验证数据，自动加上创建和更新的信息</summary>
        /// <param name="entity"></param>
        /// <param name="isNew"></param>
        protected override Boolean OnValid(IEntity entity, Boolean isNew)
        {
            if (!isNew && !entity.HasDirty) return true;

            var fs = GetFields(entity.GetType());

            if (isNew)
            {
                SetItem(fs, entity, __.CreateTime, TimerX.Now);
                SetItem(fs, entity, __.UpdateTime, TimerX.Now);
            }
            else
            {
                // 不管新建还是更新，都改变更新时间
                SetNoDirtyItem(fs, entity, __.UpdateTime, TimerX.Now);
            }

            return true;
        }
    }

    /// <summary>IP地址模型</summary>
    public class IPModule : EntityModule
    {
        #region 静态引用
        /// <summary>字段名</summary>
        public class __
        {
            /// <summary>创建人</summary>
            public static String CreateIP = nameof(CreateIP);

            /// <summary>更新人</summary>
            public static String UpdateIP = nameof(UpdateIP);
        }
        #endregion

        /// <summary>初始化。检查是否匹配</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        protected override Boolean OnInit(Type entityType)
        {
            var fs = GetFields(entityType);
            foreach (var fi in fs)
            {
                if (fi.Type == typeof(String))
                {
                    if (fi.Name.EqualIgnoreCase(__.CreateIP, __.UpdateIP)) return true;
                }
            }

            var fs2 = GetIPFieldNames(entityType);
            return fs2 != null && fs2.Length > 0;
        }

        /// <summary>验证数据，自动加上创建和更新的信息</summary>
        /// <param name="entity"></param>
        /// <param name="isNew"></param>
        protected override Boolean OnValid(IEntity entity, Boolean isNew)
        {
            if (!isNew && !entity.HasDirty) return true;

            var ip = ManageProvider.UserHost;
            if (!ip.IsNullOrEmpty())
            {
                // 如果不是IPv6，去掉后面端口
                if (ip.Contains("://")) ip = ip.Substring("://", null);
                //if (ip.Contains(":") && !ip.Contains("::")) ip = ip.Substring(null, ":");

                var fs = GetFields(entity.GetType());

                if (isNew)
                {
                    //SetItem(fs, entity, __.CreateIP, ip);

                    var fs2 = GetIPFieldNames(entity.GetType());
                    if (fs2 != null)
                    {
                        foreach (var item in fs2)
                        {
                            SetItem(fs2, entity, item, ip);
                        }
                    }

                    //SetItem(fs, entity, __.UpdateIP, ip);
                }
                else
                {
                    // 不管新建还是更新，都改变更新
                    SetNoDirtyItem(fs, entity, __.UpdateIP, ip);
                }
            }

            return true;
        }

        private static readonly ConcurrentDictionary<Type, FieldItem[]> _ipFieldNames = new();
        /// <summary>获取实体类的字段名。带缓存</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        protected static FieldItem[] GetIPFieldNames(Type entityType)
        {
            return _ipFieldNames.GetOrAdd(entityType, t => GetFields(t)?.Where(e => e.Name.EndsWith("IP")).ToArray());
        }
    }
}