using System;
using System.Collections.Generic;
using System.Text;
using XCode;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 管理员接口
    /// </summary>
    partial interface IAdministrator : IEntity
    {
        #region 属性
        /// <summary>
        /// 编号
        /// </summary>
        Int32 ID { get; set; }

        /// <summary>
        /// 名称
        /// </summary>
        String Name { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        String Password { get; set; }

        /// <summary>
        /// 显示名
        /// </summary>
        String DisplayName { get; set; }

        /// <summary>
        /// 角色
        /// </summary>
        Int32 RoleID { get; set; }

        /// <summary>
        /// 登录次数
        /// </summary>
        Int32 Logins { get; set; }

        /// <summary>
        /// 最后登录
        /// </summary>
        DateTime LastLogin { get; set; }

        /// <summary>
        /// 最后登陆IP
        /// </summary>
        String LastLoginIP { get; set; }

        /// <summary>
        /// 登录用户编号
        /// </summary>
        Int32 SSOUserID { get; set; }

        /// <summary>
        /// 是否使用
        /// </summary>
        Boolean IsEnable { get; set; }
        #endregion
    }
}