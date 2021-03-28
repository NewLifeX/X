using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Data;

namespace XCode.Code
{
    /// <summary>用户模型。帐号信息</summary>
    public class ExtendUser2 : IExtend
    {
        #region 属性
        /// <summary>编号</summary>
        public Int32 ID { get; set; }

        /// <summary>名称。登录用户名</summary>
        public String Name { get; set; }

        /// <summary>密码</summary>
        public String Password { get; set; }

        /// <summary>昵称</summary>
        public String DisplayName { get; set; }

        /// <summary>性别。未知、男、女</summary>
        public XCode.Membership.SexKinds Sex { get; set; }

        /// <summary>邮件</summary>
        public String Mail { get; set; }

        /// <summary>手机</summary>
        public String Mobile { get; set; }

        /// <summary>代码。身份证、员工编号等</summary>
        public String Code { get; set; }

        /// <summary>头像</summary>
        public String Avatar { get; set; }

        /// <summary>角色。主要角色</summary>
        public Int32 RoleID { get; set; }

        /// <summary>角色组。次要角色集合</summary>
        public String RoleIds { get; set; }

        /// <summary>部门。组织机构</summary>
        public Int32 DepartmentID { get; set; }

        /// <summary>在线</summary>
        public Boolean Online { get; set; }

        /// <summary>启用</summary>
        public Boolean Enable { get; set; }

        /// <summary>登录次数</summary>
        public Int32 Logins { get; set; }

        /// <summary>最后登录</summary>
        public DateTime LastLogin { get; set; }

        /// <summary>最后登录IP</summary>
        public String LastLoginIP { get; set; }

        /// <summary>注册时间</summary>
        public DateTime RegisterTime { get; set; }

        /// <summary>注册IP</summary>
        public String RegisterIP { get; set; }

        /// <summary>扩展1</summary>
        public Int32 Ex1 { get; set; }

        /// <summary>扩展2</summary>
        public Int32 Ex2 { get; set; }

        /// <summary>扩展3</summary>
        public Double Ex3 { get; set; }

        /// <summary>扩展4</summary>
        public String Ex4 { get; set; }

        /// <summary>扩展5</summary>
        public String Ex5 { get; set; }

        /// <summary>扩展6</summary>
        public String Ex6 { get; set; }

        /// <summary>备注</summary>
        public String Remark { get; set; }
        #endregion

        #region 获取/设置 字段值
        /// <summary>获取/设置 字段值</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        public virtual Object this[String name]
        {
            get
            {
                switch (name)
                {
                    case "ID": return ID;
                    case "Name": return Name;
                    case "Password": return Password;
                    case "DisplayName": return DisplayName;
                    case "Sex": return Sex;
                    case "Mail": return Mail;
                    case "Mobile": return Mobile;
                    case "Code": return Code;
                    case "Avatar": return Avatar;
                    case "RoleID": return RoleID;
                    case "RoleIds": return RoleIds;
                    case "DepartmentID": return DepartmentID;
                    case "Online": return Online;
                    case "Enable": return Enable;
                    case "Logins": return Logins;
                    case "LastLogin": return LastLogin;
                    case "LastLoginIP": return LastLoginIP;
                    case "RegisterTime": return RegisterTime;
                    case "RegisterIP": return RegisterIP;
                    case "Ex1": return Ex1;
                    case "Ex2": return Ex2;
                    case "Ex3": return Ex3;
                    case "Ex4": return Ex4;
                    case "Ex5": return Ex5;
                    case "Ex6": return Ex6;
                    case "Remark": return Remark;
                    default: throw new KeyNotFoundException($"{name} not found");
                }
            }
            set
            {
                switch (name)
                {
                    case "ID": ID = value.ToInt(); break;
                    case "Name": Name = Convert.ToString(value); break;
                    case "Password": Password = Convert.ToString(value); break;
                    case "DisplayName": DisplayName = Convert.ToString(value); break;
                    case "Sex": Sex = (XCode.Membership.SexKinds)value.ToInt(); break;
                    case "Mail": Mail = Convert.ToString(value); break;
                    case "Mobile": Mobile = Convert.ToString(value); break;
                    case "Code": Code = Convert.ToString(value); break;
                    case "Avatar": Avatar = Convert.ToString(value); break;
                    case "RoleID": RoleID = value.ToInt(); break;
                    case "RoleIds": RoleIds = Convert.ToString(value); break;
                    case "DepartmentID": DepartmentID = value.ToInt(); break;
                    case "Online": Online = value.ToBoolean(); break;
                    case "Enable": Enable = value.ToBoolean(); break;
                    case "Logins": Logins = value.ToInt(); break;
                    case "LastLogin": LastLogin = value.ToDateTime(); break;
                    case "LastLoginIP": LastLoginIP = Convert.ToString(value); break;
                    case "RegisterTime": RegisterTime = value.ToDateTime(); break;
                    case "RegisterIP": RegisterIP = Convert.ToString(value); break;
                    case "Ex1": Ex1 = value.ToInt(); break;
                    case "Ex2": Ex2 = value.ToInt(); break;
                    case "Ex3": Ex3 = value.ToDouble(); break;
                    case "Ex4": Ex4 = Convert.ToString(value); break;
                    case "Ex5": Ex5 = Convert.ToString(value); break;
                    case "Ex6": Ex6 = Convert.ToString(value); break;
                    case "Remark": Remark = Convert.ToString(value); break;
                    default: throw new KeyNotFoundException($"{name} not found");
                }
            }
        }
        #endregion
    }
}