using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Collections;
using NewLife.Model;
using NewLife.Threading;
using NewLife.Web;

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
            public static String CreateUserID = "CreateUserID";

            /// <summary>更新人</summary>
            public static String UpdateUserID = "UpdateUserID";
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
        public UserModule(IManageProvider provider)
        {
            //Provider = provider ?? ManageProvider.Provider;
            Provider = provider;
        }
        #endregion

        /// <summary>初始化。检查是否匹配</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        protected override Boolean OnInit(Type entityType)
        {
            var fs = GetFieldNames(entityType);
            if (fs.Contains(__.CreateUserID)) return true;
            if (fs.Contains(__.UpdateUserID)) return true;

            return false;
        }

        /// <summary>验证数据，自动加上创建和更新的信息</summary>
        /// <param name="entity"></param>
        /// <param name="isNew"></param>
        protected override Boolean OnValid(IEntity entity, Boolean isNew)
        {
            if (!isNew && entity.Dirtys.Count == 0) return true;

            var fs = GetFieldNames(entity.GetType());

            // 当前登录用户
#if !__CORE__
            var user = Provider?.Current ?? HttpContext.Current?.User?.Identity as IManageUser;
#else
            var user = Provider?.Current;
#endif
            if (user != null)
            {
                if (isNew) SetNoDirtyItem(fs, entity, __.CreateUserID, user.ID);
                SetNoDirtyItem(fs, entity, __.UpdateUserID, user.ID);
            }
            else
            {
                // 在没有当前登录用户的场合，把更新者清零
                SetNoDirtyItem(fs, entity, __.UpdateUserID, 0);
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
            public static String CreateTime = "CreateTime";

            /// <summary>更新时间</summary>
            public static String UpdateTime = "UpdateTime";
        }
        #endregion

        /// <summary>初始化。检查是否匹配</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        protected override Boolean OnInit(Type entityType)
        {
            var fs = GetFieldNames(entityType);
            if (fs.Contains(__.CreateTime)) return true;
            if (fs.Contains(__.UpdateTime)) return true;

            return false;
        }

        /// <summary>验证数据，自动加上创建和更新的信息</summary>
        /// <param name="entity"></param>
        /// <param name="isNew"></param>
        protected override Boolean OnValid(IEntity entity, Boolean isNew)
        {
            if (!isNew && entity.Dirtys.Count == 0) return true;

            var fs = GetFieldNames(entity.GetType());

            if (isNew) SetNoDirtyItem(fs, entity, __.CreateTime, TimerX.Now);

            // 不管新建还是更新，都改变更新时间
            SetNoDirtyItem(fs, entity, __.UpdateTime, TimerX.Now);

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
            public static String CreateIP = "CreateIP";

            /// <summary>更新人</summary>
            public static String UpdateIP = "UpdateIP";
        }
        #endregion

        /// <summary>初始化。检查是否匹配</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        protected override Boolean OnInit(Type entityType)
        {
            var fs = GetFieldNames(entityType);
            if (fs.Contains(__.CreateIP)) return true;
            if (fs.Contains(__.UpdateIP)) return true;

            var fs2 = GetIPFieldNames(entityType);
            return fs2 != null && fs2.Count > 0;
        }

        /// <summary>验证数据，自动加上创建和更新的信息</summary>
        /// <param name="entity"></param>
        /// <param name="isNew"></param>
        protected override Boolean OnValid(IEntity entity, Boolean isNew)
        {
            if (!isNew && entity.Dirtys.Count == 0) return true;

            var ip = WebHelper.UserHost;
            if (!ip.IsNullOrEmpty())
            {
                // 如果不是IPv6，去掉后面端口
                if (ip.Contains("://")) ip = ip.Substring("://", null);
                if (ip.Contains(":") && !ip.Contains("::")) ip = ip.Substring(null, ":");

                var fs = GetFieldNames(entity.GetType());

                if (isNew)
                {
                    SetNoDirtyItem(fs, entity, __.CreateIP, ip);

                    var fs2 = GetIPFieldNames(entity.GetType());
                    if (fs2 != null)
                    {
                        foreach (var item in fs2)
                        {
                            SetNoDirtyItem(fs2, entity, item, ip);
                        }
                    }
                }

                // 不管新建还是更新，都改变更新时间
                SetNoDirtyItem(fs, entity, __.UpdateIP, ip);
            }

            return true;
        }

        private static DictionaryCache<Type, ICollection<String>> _ipFieldNames = new DictionaryCache<Type, ICollection<String>>();
        /// <summary>获取实体类的字段名。带缓存</summary>
        /// <param name="entityType"></param>
        /// <returns></returns>
        protected static ICollection<String> GetIPFieldNames(Type entityType)
        {
            return _ipFieldNames.GetItem(entityType, t =>
            {
                var fs = GetFieldNames(t);
                if (fs == null || fs.Count == 0) return null;

                return new HashSet<String>(fs.Where(e => e.EndsWith("IP")));
            });
        }
    }
}