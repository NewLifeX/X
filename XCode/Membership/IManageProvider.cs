using System;
using System.Text;
using System.Web;
using NewLife.Configuration;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Web;
using XCode;

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
        /// <summary>用户实体类</summary>
        Type UserType { get; }

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
        /// <param name="rolename">角色名称</param>
        /// <param name="enable">是否启用</param>
        /// <returns></returns>
        IManageUser Register(String name, String password, String rolename = "注册用户", Boolean enable = false);

        /// <summary>获取服务</summary>
        /// <remarks>
        /// 其实IServiceProvider有该扩展方法，但是在FX2里面不方面使用，所以这里保留
        /// </remarks>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        TService GetService<TService>();
    }

    /// <summary>管理提供者</summary>
    public abstract class ManageProvider : IManageProvider, IErrorInfoProvider
    {
        #region 静态实例
        static ManageProvider()
        {
            ObjectContainer.Current.AutoRegister<IManageProvider, DefaultManageProvider>();
        }

        /// <summary>当前管理提供者</summary>
        public static IManageProvider Provider { get { return ObjectContainer.Current.ResolveInstance<IManageProvider>(); } }

        /// <summary>登录登录用户</summary>
        public static IManageUser User { get { return Provider.Current; } set { Provider.Current = value; } }
        #endregion

        #region IManageProvider 接口
        /// <summary>管理用户类</summary>
        public abstract Type UserType { get; }

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
        /// <param name="rolename">角色名称</param>
        /// <param name="enable">是否启用。某些系统可能需要验证审核</param>
        /// <returns></returns>
        public abstract IManageUser Register(String name, String password, String rolename, Boolean enable);

        /// <summary>获取服务</summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public TService GetService<TService>() { return (TService)GetService(typeof(TService)); }

        /// <summary>获取服务</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public virtual Object GetService(Type serviceType)
        {
            //if (serviceType == typeof(IManagePage))
            //    return GetHttpCache(typeof(IManagePage), k => CommonService.Container.Resolve<IManagePage>());
            //else if (serviceType == typeof(IEntityForm))
            //    return GetHttpCache(typeof(IEntityForm), k => CommonService.Container.Resolve<IEntityForm>());

            return ObjectContainer.Current.Resolve(serviceType);
        }
        #endregion

        #region 辅助
        /// <summary>获取Http缓存，如果不存在，则调用func去计算</summary>
        /// <param name="key"></param>
        /// <param name="func"></param>
        /// <returns></returns>
        protected Object GetHttpCache(Object key, Func<Object, Object> func)
        {
            if (HttpContext.Current.Items[key] != null) return HttpContext.Current.Items[key];

            var value = func(key);

            HttpContext.Current.Items[key] = value;

            return value;
        }
        #endregion

        #region IErrorInfoProvider 成员
        void IErrorInfoProvider.AddInfo(Exception ex, StringBuilder builder)
        {
            var user = Current;
            if (user != null)
            {
                if (user["RoleName"] != null)
                    builder.AppendFormat("登录：{0}({1})\r\n", user.Name, user["RoleName"]);
                else
                    builder.AppendFormat("登录：{0}\r\n", user.Name);
            }
        }
        #endregion

        #region 实体类扩展
        /// <summary>根据实体类接口获取实体工厂</summary>
        /// <typeparam name="TIEntity"></typeparam>
        /// <returns></returns>
        internal static IEntityOperate GetFactory<TIEntity>()
        {
            var type = ObjectContainer.Current.ResolveType<TIEntity>();
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
        public override Type UserType { get { return typeof(TUser); } }

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
        public override IManageUser Login(String name, String password, Boolean rememberme) { return User<TUser>.Login(name, password,rememberme); }

        /// <summary>注册用户</summary>
        /// <param name="name">用户名</param>
        /// <param name="password">密码</param>
        /// <param name="rolename">角色名称</param>
        /// <param name="enable">是否启用。某些系统可能需要验证审核</param>
        /// <returns></returns>
        public override IManageUser Register(String name, String password, String rolename, Boolean enable)
        {
            var user = new TUser();
            user.Name = name;
            user.Password = password;
            user.Enable = enable;

            if (!String.IsNullOrEmpty(rolename))
            {
                var fact = Get<IRole>();
                var role = fact.FindOrCreateByName(rolename);
                user.ID = role.ID;
                user.RoleName = role.Name;
            }

            user.Register();

            return user;
        }
    }

    class DefaultManageProvider : ManageProvider<User> { }
}