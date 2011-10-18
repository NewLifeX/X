using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.CommonEntity
{
    /// <summary>通用实体类管理提供者接口</summary>
    public interface ICommonManageProvider : IManageProvider
    {
        #region 类型
        /// <summary>管理员类</summary>
        Type AdminstratorType { get; }

        /// <summary>日志类</summary>
        Type LogType { get; }

        /// <summary>菜单类</summary>
        Type MenuType { get; }

        /// <summary>角色类</summary>
        Type RoleType { get; }

        /// <summary>权限类</summary>
        Type RoleMenuType { get; }
        #endregion

        #region 接口
        /// <summary>当前用户</summary>
        new IAdministrator Current { get; }

        /// <summary>
        /// 根据用户编号查找
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        new IAdministrator FindByID(Object userid);

        /// <summary>
        /// 根据用户帐号查找
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        new IAdministrator FindByAccount(String account);

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        new IAdministrator Login(String account, String password);
        #endregion
    }

    /// <summary>通用实体类管理提供者</summary>
    public class CommonManageProvider : CommonManageProvider<Administrator>
    {
    }

    /// <summary>通用实体类管理提供者</summary>
    /// <typeparam name="TAdministrator">管理员类</typeparam>
    public class CommonManageProvider<TAdministrator> : ICommonManageProvider where TAdministrator : Administrator<TAdministrator>, new()
    {
        #region 静态实例
        /// <summary>当前提供者</summary>
        public static ICommonManageProvider Provider { get { return CommonService.Resolve<ICommonManageProvider>(); } }
        #endregion

        #region 静态构造
        //static CommonManageProvider()
        //{
        //    // 不覆盖注册，谁先被调用，就以它为准
        //    CommonService.Register<ICommonManageProvider, CommonManageProvider>(null, false);
        //}
        #endregion

        #region IManageProvider 接口
        /// <summary>管理用户类</summary>
        Type IManageProvider.ManageUserType { get { return AdminstratorType; } }

        /// <summary>当前用户</summary>
        IManageUser IManageProvider.Current { get { return Administrator<TAdministrator>.Current; } }

        /// <summary>
        /// 根据用户编号查找
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        IManageUser IManageProvider.FindByID(Object userid) { return Administrator<TAdministrator>.FindByID((Int32)userid); }

        /// <summary>
        /// 根据用户帐号查找
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        IManageUser IManageProvider.FindByAccount(String account) { return Administrator<TAdministrator>.FindByName(account); }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        IManageUser IManageProvider.Login(String account, String password) { return Administrator<TAdministrator>.Login(account, password); }
        #endregion

        #region 类型
        /// <summary>管理员类</summary>
        public virtual Type AdminstratorType { get { return typeof(IAdministrator); } }

        /// <summary>日志类</summary>
        public virtual Type LogType { get { return typeof(Log); } }

        /// <summary>菜单类</summary>
        public virtual Type MenuType { get { return typeof(Menu); } }

        /// <summary>角色类</summary>
        public virtual Type RoleType { get { return typeof(Role); } }

        /// <summary>权限类</summary>
        public virtual Type RoleMenuType { get { return typeof(RoleMenu); } }
        #endregion

        #region 接口
        /// <summary>当前用户</summary>
        public virtual IAdministrator Current { get { return Administrator<TAdministrator>.Current; } }

        /// <summary>
        /// 根据用户编号查找
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public virtual IAdministrator FindByID(Object userid) { return Administrator<TAdministrator>.FindByID((Int32)userid); }

        /// <summary>
        /// 根据用户帐号查找
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public virtual IAdministrator FindByAccount(String account) { return Administrator<TAdministrator>.FindByName(account); }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public virtual IAdministrator Login(String account, String password) { return Administrator<TAdministrator>.Login(account, password); }
        #endregion
    }
}