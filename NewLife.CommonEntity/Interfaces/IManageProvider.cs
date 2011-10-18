using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Model;

namespace NewLife.CommonEntity
{
    /// <summary>管理提供者接口</summary>
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
    }

    /// <summary>管理提供者</summary>
    public class ManageProvider : IManageProvider
    {
        #region 静态实例
        /// <summary>当前提供者</summary>
        public static IManageProvider Provider { get { return CommonService.Resolve<IManageProvider>(); } }
        #endregion

        #region 静态构造
        //static ManageProvider()
        //{
        //    // 不覆盖注册，谁先被调用，就以它为准
        //    CommonService.Register<IManageProvider, ManageProvider>(null, false);
        //}
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
        #endregion
    }
}