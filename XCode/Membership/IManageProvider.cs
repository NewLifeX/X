using System;
using System.Collections.Generic;
using System.Security.Principal;
using System.Text;
using System.Web;
using NewLife.Model;
using NewLife.Web;
using XCode.Model;

namespace XCode.Membership
{
    /// <summary>管理提供者接口</summary>
    /// <remarks>
    /// 管理提供者接口主要提供（或统一规范）用户提供者定位、用户查找登录等功能。
    /// 只需要一个实现IManageUser接口的用户类即可实现IManageProvider接口。
    /// IManageProvider足够精简，使得大多数用户可以自定义实现；
    /// 也因为其简单稳定，大多数需要涉及用户与权限功能的操作，均可以直接使用该接口。
    /// </remarks>
    public interface IManageProvider : IServiceProvider
    {
        /// <summary>当前登录用户，设为空则注销登录</summary>
        IManageUser Current { get; set; }

        /// <summary>根据用户编号查找</summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        IManageUser FindByID(Object userid);

        /// <summary>根据用户帐号查找</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        IManageUser FindByName(String name);

        /// <summary>登录</summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <param name="rememberme">是否记住密码</param>
        /// <returns></returns>
        IManageUser Login(String name, String password, Boolean rememberme = false);

        /// <summary>注销</summary>
        void Logout();

        /// <summary>注册用户</summary>
        /// <param name="name">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="roleid">角色</param>
        /// <param name="enable">是否启用</param>
        /// <returns></returns>
        IManageUser Register(String name, String password, Int32 roleid = 0, Boolean enable = false);

        /// <summary>获取服务</summary>
        /// <remarks>
        /// 其实IServiceProvider有该扩展方法，但是在FX2里面不方面使用，所以这里保留
        /// </remarks>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        TService GetService<TService>();
    }

    /// <summary>管理提供者</summary>
#if !__CORE__
    public abstract class ManageProvider : IManageProvider, IErrorInfoProvider
#else
    public abstract class ManageProvider : IManageProvider
#endif
    {
        #region 静态实例
        static ManageProvider()
        {
            var ioc = ObjectContainer.Current;
            // 外部管理提供者需要手工覆盖
            ioc.Register<IManageProvider, DefaultManageProvider>();

            ioc.AutoRegister<IRole, Role>()
                .AutoRegister<IMenu, Menu>()
                .AutoRegister<ILog, Log>()
                .AutoRegister<IUser, UserX>();
        }

        /// <summary>当前管理提供者</summary>
        public static IManageProvider Provider { get { return ObjectContainer.Current.ResolveInstance<IManageProvider>(); } }

        /// <summary>当前登录用户</summary>
        public static IUser User { get { return Provider.Current as IUser; } set { Provider.Current = value as IManageUser; } }

        /// <summary>菜单工厂</summary>
        public static IMenuFactory Menu { get { return GetFactory<IMenu>() as IMenuFactory; } }
        #endregion

        #region IManageProvider 接口
        /// <summary>当前用户</summary>
        public abstract IManageUser Current { get; set; }

        /// <summary>根据用户编号查找</summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public abstract IManageUser FindByID(Object userid);

        /// <summary>根据用户帐号查找</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public abstract IManageUser FindByName(String name);

        /// <summary>登录</summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <param name="rememberme">是否记住密码</param>
        /// <returns></returns>
        public abstract IManageUser Login(String name, String password, Boolean rememberme);

        /// <summary>注销</summary>
        public virtual void Logout() { Current = null; }

        /// <summary>注册用户</summary>
        /// <param name="name">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="roleid">角色</param>
        /// <param name="enable">是否启用。某些系统可能需要验证审核</param>
        /// <returns></returns>
        public abstract IManageUser Register(String name, String password, Int32 roleid, Boolean enable);

        /// <summary>获取服务</summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public TService GetService<TService>() { return (TService)GetService(typeof(TService)); }

        /// <summary>获取服务</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public virtual Object GetService(Type serviceType)
        {
            var container = XCodeService.Container;
            return container.Resolve(serviceType);
        }
        #endregion

#if !__CORE__
        #region IErrorInfoProvider 成员
        void IErrorInfoProvider.AddInfo(Exception ex, StringBuilder builder)
        {
            var user = Current;
            if (user != null)
            {
                var e = user as IEntity;
                if (e["RoleName"] != null)
                    builder.AppendFormat("登录：{0}({1})\r\n", user.Name, e["RoleName"]);
                else
                    builder.AppendFormat("登录：{0}\r\n", user.Name);
            }
        }
        #endregion
#endif

        #region 实体类扩展
        /// <summary>根据实体类接口获取实体工厂</summary>
        /// <typeparam name="TIEntity"></typeparam>
        /// <returns></returns>
        internal static IEntityOperate GetFactory<TIEntity>()
        {
            var container = XCodeService.Container;
            var type = container.ResolveType<TIEntity>();
            if (type == null) return null;

            return EntityFactory.CreateOperate(type);
        }

        internal static T Get<T>()
        {
            var eop = GetFactory<T>();
            if (eop == null) return default(T);

            return (T)eop.Default;
        }
        #endregion
    }

    /// <summary>基于User实体类的管理提供者</summary>
    /// <typeparam name="TUser"></typeparam>
    public class ManageProvider<TUser> : ManageProvider where TUser : User<TUser>, new()
    {
        /// <summary>当前用户</summary>
        public override IManageUser Current { get { return User<TUser>.Current; } set { User<TUser>.Current = (TUser)value; } }

        /// <summary>根据用户编号查找</summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public override IManageUser FindByID(Object userid) { return User<TUser>.FindByID((Int32)userid); }

        /// <summary>根据用户帐号查找</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public override IManageUser FindByName(String name) { return User<TUser>.FindByName(name); }

        /// <summary>登录</summary>
        /// <param name="name"></param>
        /// <param name="password"></param>
        /// <param name="rememberme">是否记住密码</param>
        /// <returns></returns>
        public override IManageUser Login(String name, String password, Boolean rememberme) { return User<TUser>.Login(name, password, rememberme); }

        /// <summary>注册用户</summary>
        /// <param name="name">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="roleid">角色</param>
        /// <param name="enable">是否启用。某些系统可能需要验证审核</param>
        /// <returns></returns>
        public override IManageUser Register(String name, String password, Int32 roleid, Boolean enable)
        {
            var user = new TUser();
            user.Name = name;
            user.Password = password;
            user.Enable = enable;
            user.RoleID = roleid;

            user.Register();

            return user;
        }
    }

    class DefaultManageProvider : ManageProvider<UserX> { }

#if !__CORE__
    /// <summary>管理提供者助手</summary>
    public static class ManagerProviderHelper
    {
        /// <summary>设置当前用户</summary>
        /// <param name="provider">提供者</param>
        public static void SetPrincipal(this IManageProvider provider)
        {
            var ctx = HttpContext.Current;
            if (ctx == null) return;

            var user = provider.Current;
            if (user == null) return;

            var id = user as IIdentity;
            if (id == null) return;

            // 角色列表
            var roles = new List<String>();
            if (user is IUser user2) roles.Add(user2.RoleName);

            ctx.User = new GenericPrincipal(id, roles.ToArray());
        }
    }
#endif
}