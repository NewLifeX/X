using System;
using System.Web;
using NewLife.Reflection;

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
        IManageUser Current { get; }

        /// <summary>
        /// 根据用户编号查找
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        IManageUser FindByID(Object userid);

        /// <summary>
        /// 根据用户帐号查找
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        IManageUser FindByAccount(String account);

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        IManageUser Login(String account, String password);

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        TService GetService<TService>();

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        Object GetService(Type serviceType);
    }

    /// <summary>管理提供者</summary>
    public class ManageProvider : IManageProvider
    {
        #region 静态实例
        /// <summary>当前提供者</summary>
        public static IManageProvider Provider { get { return CommonService.Resolve<IManageProvider>(); } }
        #endregion

        #region IManageProvider 接口
        /// <summary>管理用户类</summary>
        public virtual Type ManageUserType { get { return typeof(User); } }

        /// <summary>当前用户</summary>
        public virtual IManageUser Current { get { return User.Current; } }

        /// <summary>
        /// 根据用户编号查找
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public virtual IManageUser FindByID(Object userid)
        {
            return User.FindByID((Int32)userid);
        }

        /// <summary>
        /// 根据用户帐号查找
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public virtual IManageUser FindByAccount(String account)
        {
            return User.FindByAccount(account);
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public virtual IManageUser Login(String account, String password)
        {
            return User.Login(account, password);
        }

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        public TService GetService<TService>() { return (TService)GetService(typeof(TService)); }

        /// <summary>
        /// 获取服务
        /// </summary>
        /// <param name="serviceType"></param>
        /// <returns></returns>
        public virtual Object GetService(Type serviceType)
        {
            if (serviceType == typeof(IManagePage))
                return GetHttpCache(typeof(IManagePage), k => CommonService.Resolve<IManagePage>());
            else if (serviceType == typeof(IEntityForm))
                return GetHttpCache(typeof(IEntityForm), k => CommonService.Resolve<IEntityForm>());

            return CommonService.Resolve(serviceType);
        }
        #endregion

        #region 辅助
        /// <summary>
        /// 获取Http缓存，如果不存在，则调用func去计算
        /// </summary>
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
    }
}