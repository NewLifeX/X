using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace XCode.Membership
{
    /// <summary>用户。用户帐号信息</summary>
    public partial interface IUser
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称。登录用户名</summary>
        String Name { get; set; }

        /// <summary>密码</summary>
        String Password { get; set; }

        /// <summary>昵称</summary>
        String DisplayName { get; set; }

        /// <summary>性别。未知、男、女</summary>
        XCode.Membership.SexKinds Sex { get; set; }

        /// <summary>邮件</summary>
        String Mail { get; set; }

        /// <summary>手机</summary>
        String Mobile { get; set; }

        /// <summary>代码。身份证、员工编号等</summary>
        String Code { get; set; }

        /// <summary>地区。省市区</summary>
        Int32 AreaId { get; set; }

        /// <summary>头像</summary>
        String Avatar { get; set; }

        /// <summary>角色。主要角色</summary>
        Int32 RoleID { get; set; }

        /// <summary>角色组。次要角色集合</summary>
        String RoleIds { get; set; }

        /// <summary>部门。组织机构</summary>
        Int32 DepartmentID { get; set; }

        /// <summary>在线</summary>
        Boolean Online { get; set; }

        /// <summary>启用</summary>
        Boolean Enable { get; set; }

        /// <summary>登录次数</summary>
        Int32 Logins { get; set; }

        /// <summary>最后登录</summary>
        DateTime LastLogin { get; set; }

        /// <summary>最后登录IP</summary>
        String LastLoginIP { get; set; }

        /// <summary>注册时间</summary>
        DateTime RegisterTime { get; set; }

        /// <summary>注册IP</summary>
        String RegisterIP { get; set; }

        /// <summary>在线时间。累计在线总时间，秒</summary>
        Int32 OnlineTime { get; set; }

        /// <summary>扩展1</summary>
        Int32 Ex1 { get; set; }

        /// <summary>扩展2</summary>
        Int32 Ex2 { get; set; }

        /// <summary>扩展3</summary>
        Double Ex3 { get; set; }

        /// <summary>扩展4</summary>
        String Ex4 { get; set; }

        /// <summary>扩展5</summary>
        String Ex5 { get; set; }

        /// <summary>扩展6</summary>
        String Ex6 { get; set; }

        /// <summary>备注</summary>
        String Remark { get; set; }
        #endregion
    }
}