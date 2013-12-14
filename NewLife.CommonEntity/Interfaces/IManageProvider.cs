using System;
using System.Text;
using System.Web;
using NewLife.Reflection;
using NewLife.Web;

namespace NewLife.CommonEntity
{
    /// <summary>管理提供者接口</summary>
    /// <remarks>
    /// 管理提供者接口主要提供（或统一规范）用户提供者定位、用户查找登录等功能。
    /// 只需要一个实现IManageUser接口的用户类即可实现IManageProvider接口。
    /// IManageProvider足够精简，使得大多数用户可以自定义实现；
    /// 也因为其简单稳定，大多数需要涉及用户与权限功能的操作，均可以直接使用该接口。
    /// </remarks>
    public interface IManageProvider
    {
        /// <summary>管理用户类</summary>
        Type ManageUserType { get; }

        /// <summary>当前用户</summary>
        IManageUser Current { get; set; }

        /// <summary>根据用户编号查找</summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        IManageUser FindByID(Object userid);

        /// <summary>根据用户帐号查找</summary>
        /// <param name="account"></param>
        /// <returns></returns>
        IManageUser FindByAccount(String account);

        /// <summary>登录</summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        IManageUser Login(String account, String password);

        /// <summary>注销</summary>
        /// <param name="user"></param>
        void Logout(IManageUser user);

        /// <summary>获取服务</summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        TService GetService<TService>();

        /// <summary>获取服务</summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        Object GetService(Type serviceType);
    }

    /// <summary>管理提供者</summary>
    public class ManageProvider : ManageProvider<User> { }

    /// <summary>管理提供者</summary>
    public class ManageProvider<TUser> : IManageProvider, IErrorInfoProvider where TUser : User<TUser>, new()
    {
        #region 静态实例
        /// <summary>当前提供者</summary>
        public static IManageProvider Provider { get { return CommonService.Container.ResolveInstance<IManageProvider>(); } }
        #endregion

        #region IManageProvider 接口
        /// <summary>管理用户类</summary>
        public virtual Type ManageUserType { get { return typeof(TUser); } }

        /// <summary>当前用户</summary>
        public virtual IManageUser Current { get { return User<TUser>.Current; } set { User<TUser>.Current = value as TUser; } }

        /// <summary>根据用户编号查找</summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public virtual IManageUser FindByID(Object userid)
        {
            return User<TUser>.FindByID((Int32)userid);
        }

        /// <summary>根据用户帐号查找</summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public virtual IManageUser FindByAccount(String account)
        {
            return User<TUser>.FindByAccount(account);
        }

        /// <summary>登录</summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public virtual IManageUser Login(String account, String password)
        {
            return User<TUser>.Login(account, password);
        }

        /// <summary>注销</summary>
        /// <param name="user"></param>
        public virtual void Logout(IManageUser user) { Current = null; }

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
            //    return CommonService.Resolve<IManagePage>();
            //else if (serviceType == typeof(IEntityForm))
            //    return CommonService.Resolve<IEntityForm>();
            if (serviceType == typeof(IManagePage))
                return GetHttpCache(typeof(IManagePage), k => CommonService.Container.Resolve<IManagePage>());
            else if (serviceType == typeof(IEntityForm))
                return GetHttpCache(typeof(IEntityForm), k => CommonService.Container.Resolve<IEntityForm>());

            return CommonService.Container.Resolve(serviceType);
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

            Object value = func(key);

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
                if (user.Properties.ContainsKey("RoleName"))
                    builder.AppendFormat("登录：{0}({1})\r\n", user.Account, user.Properties["RoleName"]);
                else
                    builder.AppendFormat("登录：{0}\r\n", user.Account);
            }
        }
        #endregion
    }
}