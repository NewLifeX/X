using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.Serialization;
using System.Web.Script.Serialization;
using System.Xml.Serialization;

namespace XCode.Membership
{
    /// <summary>菜单</summary>
    public partial interface IMenu
    {
        #region 属性
        /// <summary>编号</summary>
        Int32 ID { get; set; }

        /// <summary>名称</summary>
        String Name { get; set; }

        /// <summary>显示名</summary>
        String DisplayName { get; set; }

        /// <summary>全名</summary>
        String FullName { get; set; }

        /// <summary>父编号</summary>
        Int32 ParentID { get; set; }

        /// <summary>链接</summary>
        String Url { get; set; }

        /// <summary>排序</summary>
        Int32 Sort { get; set; }

        /// <summary>图标</summary>
        String Icon { get; set; }

        /// <summary>可见</summary>
        Boolean Visible { get; set; }

        /// <summary>必要。必要的菜单，必须至少有角色拥有这些权限，如果没有则自动授权给系统角色</summary>
        Boolean Necessary { get; set; }

        /// <summary>权限子项。逗号分隔，每个权限子项名值竖线分隔</summary>
        String Permission { get; set; }

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

        /// <summary>创建者</summary>
        String CreateUser { get; set; }

        /// <summary>创建用户</summary>
        Int32 CreateUserID { get; set; }

        /// <summary>创建地址</summary>
        String CreateIP { get; set; }

        /// <summary>创建时间</summary>
        DateTime CreateTime { get; set; }

        /// <summary>更新者</summary>
        String UpdateUser { get; set; }

        /// <summary>更新用户</summary>
        Int32 UpdateUserID { get; set; }

        /// <summary>更新地址</summary>
        String UpdateIP { get; set; }

        /// <summary>更新时间</summary>
        DateTime UpdateTime { get; set; }

        /// <summary>备注</summary>
        String Remark { get; set; }
        #endregion
    }
}