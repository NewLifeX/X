using System;
using System.Text;
using System.Web;
using NewLife.Configuration;
using NewLife.Model;
using NewLife.Reflection;
using NewLife.Web;
using XCode;

namespace NewLife.CommonEntity
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
        /// <remarks>
        /// 其实IServiceProvider有该扩展方法，但是在FX2里面不方面使用，所以这里保留
        /// </remarks>
        /// <typeparam name="TService"></typeparam>
        /// <returns></returns>
        TService GetService<TService>();

        ///// <summary>获取服务</summary>
        ///// <param name="serviceType"></param>
        ///// <returns></returns>
        //Object GetService(Type serviceType);

        /// <summary>写日志</summary>
        /// <param name="type">类型</param>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        void WriteLog(Type type, String action, String remark);
    }

    ///// <summary>管理提供者</summary>
    //public class ManageProvider : ManageProvider<User> { }

    /// <summary>管理提供者</summary>
    public abstract class ManageProvider : IManageProvider, IErrorInfoProvider
    {
        #region 静态实例
        static ManageProvider()
        {
            // 为了引发CommonService的静态构造函数，从而实现自动注册
            var container = CommonService.Container;
        }

        /// <summary>当前提供者</summary>
        public static IManageProvider Provider { get { return CommonService.Container.ResolveInstance<IManageProvider>(); } }
        #endregion

        #region IManageProvider 接口
        /// <summary>管理用户类</summary>
        public abstract Type ManageUserType { get; }

        /// <summary>当前用户</summary>
        public abstract IManageUser Current { get; set; }

        /// <summary>根据用户编号查找</summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public abstract IManageUser FindByID(Object userid);

        /// <summary>根据用户帐号查找</summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public abstract IManageUser FindByAccount(String account);

        /// <summary>登录</summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public abstract IManageUser Login(String account, String password);

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
            if (serviceType == typeof(IManagePage))
                return GetHttpCache(typeof(IManagePage), k => CommonService.Container.Resolve<IManagePage>());
            else if (serviceType == typeof(IEntityForm))
                return GetHttpCache(typeof(IEntityForm), k => CommonService.Container.Resolve<IEntityForm>());

            return CommonService.Container.Resolve(serviceType);
        }

        /// <summary>写日志</summary>
        /// <param name="type">类型</param>
        /// <param name="action">操作</param>
        /// <param name="remark">备注</param>
        public virtual void WriteLog(Type type, String action, String remark)
        {
            if (!Config.GetConfig<Boolean>("NewLife.CommonEntity.WriteEntityLog", true)) return;

            if (type == null) type = this.GetType();

            var user = Current;

            var factory = ManageProvider.Get<ILog>();
            var log = factory.Create(type, action);

            if (user != null)
            {
                log.UserID = user.ID;
                log.UserName = user.ToString();
            }

            log.Remark = remark;
            log.Save();
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
                    builder.AppendFormat("登录：{0}({1})\r\n", user.Account, user["RoleName"]);
                else
                    builder.AppendFormat("登录：{0}\r\n", user.Account);
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
}