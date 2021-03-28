using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace XCode.Code
{
    /// <summary>用户。用户帐号信息</summary>
    [Serializable]
    [DataObject]
    [Description("用户。用户帐号信息")]
    public class UserTT
    {
        #region 属性
        /// <summary>编号</summary>
        [Description("编号")]
        [DisplayName("编号")]
        public Int32 ID { get; set; }

        /// <summary>名称。登录用户名</summary>
        [Description("名称。登录用户名")]
        [DisplayName("名称")]
        public String Name { get; set; }

        /// <summary>密码</summary>
        [Description("密码")]
        [DisplayName("密码")]
        public String Password { get; set; }

        /// <summary>昵称</summary>
        [Description("昵称")]
        [DisplayName("昵称")]
        public String DisplayName { get; set; }

        /// <summary>性别。未知、男、女</summary>
        [Description("性别。未知、男、女")]
        [DisplayName("性别")]
        public XCode.Membership.SexKinds Sex { get; set; }

        /// <summary>邮件</summary>
        [Description("邮件")]
        [DisplayName("邮件")]
        public String Mail { get; set; }

        /// <summary>手机</summary>
        [Description("手机")]
        [DisplayName("手机")]
        public String Mobile { get; set; }

        /// <summary>代码。身份证、员工编号等</summary>
        [Description("代码。身份证、员工编号等")]
        [DisplayName("代码")]
        public String Code { get; set; }

        /// <summary>头像</summary>
        [Description("头像")]
        [DisplayName("头像")]
        public String Avatar { get; set; }

        /// <summary>角色。主要角色</summary>
        [Description("角色。主要角色")]
        [DisplayName("角色")]
        public Int32 RoleID { get; set; }

        /// <summary>角色组。次要角色集合</summary>
        [Description("角色组。次要角色集合")]
        [DisplayName("角色组")]
        public String RoleIds { get; set; }

        /// <summary>部门。组织机构</summary>
        [Description("部门。组织机构")]
        [DisplayName("部门")]
        public Int32 DepartmentID { get; set; }

        /// <summary>在线</summary>
        [Description("在线")]
        [DisplayName("在线")]
        public Boolean Online { get; set; }

        /// <summary>启用</summary>
        [Description("启用")]
        [DisplayName("启用")]
        public Boolean Enable { get; set; }

        /// <summary>登录次数</summary>
        [Description("登录次数")]
        [DisplayName("登录次数")]
        public Int32 Logins { get; set; }

        /// <summary>最后登录</summary>
        [Description("最后登录")]
        [DisplayName("最后登录")]
        public DateTime LastLogin { get; set; }

        /// <summary>最后登录IP</summary>
        [Description("最后登录IP")]
        [DisplayName("最后登录IP")]
        public String LastLoginIP { get; set; }

        /// <summary>注册时间</summary>
        [Description("注册时间")]
        [DisplayName("注册时间")]
        public DateTime RegisterTime { get; set; }

        /// <summary>注册IP</summary>
        [Description("注册IP")]
        [DisplayName("注册IP")]
        public String RegisterIP { get; set; }

        /// <summary>扩展1</summary>
        [Description("扩展1")]
        [DisplayName("扩展1")]
        public Int32 Ex1 { get; set; }

        /// <summary>扩展2</summary>
        [Description("扩展2")]
        [DisplayName("扩展2")]
        public Int32 Ex2 { get; set; }

        /// <summary>扩展3</summary>
        [Description("扩展3")]
        [DisplayName("扩展3")]
        public Double Ex3 { get; set; }

        /// <summary>扩展4</summary>
        [Description("扩展4")]
        [DisplayName("扩展4")]
        public String Ex4 { get; set; }

        /// <summary>扩展5</summary>
        [Description("扩展5")]
        [DisplayName("扩展5")]
        public String Ex5 { get; set; }

        /// <summary>扩展6</summary>
        [Description("扩展6")]
        [DisplayName("扩展6")]
        public String Ex6 { get; set; }

        /// <summary>更新者</summary>
        [Description("更新者")]
        [DisplayName("更新者")]
        public String UpdateUser { get; set; }

        /// <summary>更新用户</summary>
        [Description("更新用户")]
        [DisplayName("更新用户")]
        public Int32 UpdateUserID { get; set; }

        /// <summary>更新地址</summary>
        [Description("更新地址")]
        [DisplayName("更新地址")]
        public String UpdateIP { get; set; }

        /// <summary>更新时间</summary>
        [Description("更新时间")]
        [DisplayName("更新时间")]
        public DateTime UpdateTime { get; set; }

        /// <summary>备注</summary>
        [Description("备注")]
        [DisplayName("备注")]
        public String Remark { get; set; }
        #endregion
    }
}