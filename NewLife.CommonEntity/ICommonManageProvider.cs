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

        #region 实例
        /// <summary>当前用户</summary>
        new IAdministrator Current { get; }
        #endregion
    }

    /// <summary>通用实体类管理提供者</summary>
    public class CommonManageProvider : ICommonManageProvider
    {
        #region 静态实例
        /// <summary>当前提供者</summary>
        public static IManageProvider Provider { get { return CommonService.Resolve<IManageProvider>(); } }
        #endregion

        #region 静态构造
        static CommonManageProvider()
        {
            // 不覆盖注册，谁先被调用，就以它为准
            CommonService.Register<IManageProvider, CommonManageProvider>(null, false);
        }
        #endregion

        #region IManageProvider 接口
        /// <summary>管理用户类</summary>
        public virtual Type ManageUserType { get { return typeof(Administrator); } }

        /// <summary>当前用户</summary>
        IManageUser IManageProvider.Current { get { return Administrator.Current; } }

        /// <summary>
        /// 根据用户编号查找
        /// </summary>
        /// <param name="userid"></param>
        /// <returns></returns>
        public virtual IManageUser FindByID(Object userid)
        {
            return Administrator.FindByID((Int32)userid);
        }

        /// <summary>
        /// 根据用户帐号查找
        /// </summary>
        /// <param name="account"></param>
        /// <returns></returns>
        public virtual IManageUser FindByAccount(String account)
        {
            return Administrator.FindByName(account);
        }

        /// <summary>
        /// 登录
        /// </summary>
        /// <param name="account"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public virtual IManageUser Login(String account, String password)
        {
            return Administrator.Login(account, password);
        }
        #endregion

        #region 类型
        /// <summary>管理员类</summary>
        public virtual Type AdminstratorType { get { return typeof(Administrator); } }

        /// <summary>日志类</summary>
        public virtual Type LogType { get { return typeof(Log); } }

        /// <summary>菜单类</summary>
        public virtual Type MenuType { get { return typeof(Menu); } }

        /// <summary>角色类</summary>
        public virtual Type RoleType { get { return typeof(Role); } }

        /// <summary>权限类</summary>
        public virtual Type RoleMenuType { get { return typeof(RoleMenu); } }
        #endregion

        #region 实例
        /// <summary>当前用户</summary>
        public virtual IAdministrator Current { get { return Administrator.Current; } }
        #endregion
    }
}