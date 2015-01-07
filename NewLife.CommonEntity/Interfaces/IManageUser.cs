using System;
using System.Collections.Generic;

namespace NewLife.CommonEntity
{
    /// <summary>用户接口</summary>
    public interface IManageUser
    {
        /// <summary>唯一编号</summary>
        Object Uid { get; }

        /// <summary>数字编号。如果唯一编号不是数字，请抛出异常</summary>
        Int32 ID { get; }

        /// <summary>账号</summary>
        String Account { get; set; }

        /// <summary>密码</summary>
        String Password { get; set; }

        /// <summary>是否管理员</summary>
        Boolean IsAdmin { get; set; }

        /// <summary>是否启用</summary>
        Boolean IsEnable { get; set; }

        ///// <summary>属性集合</summary>
        //IDictionary<String, Object> Properties { get; }
        
        /// <summary>获取/设置 字段值。</summary>
        /// <param name="name">字段名</param>
        /// <returns></returns>
        Object this[String name] { get; set; }
    }
}