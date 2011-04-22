using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读写服务
    /// </summary>
    public static class RWServices
    {
        /// <summary>
        /// 创建反射成员信息
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static IObjectMemberInfo CreateObjectMemberInfo(MemberInfo member)
        {
            return new ReflectMemberInfo(member);
        }

        /// <summary>
        /// 创建简单成员信息
        /// </summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static IObjectMemberInfo CreateObjectMemberInfo(String name, Type type, Object value)
        {
            return new SimpleMemberInfo(name, type, value);
        }
    }
}