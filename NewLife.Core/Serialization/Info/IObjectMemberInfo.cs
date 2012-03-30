using System;

namespace NewLife.Serialization
{
    /// <summary>对象成员信息</summary>
    public interface IObjectMemberInfo
    {
        /// <summary>名称</summary>>
        String Name { get; }

        /// <summary>类型</summary>>
        Type Type { get; }

        /// <summary>对目标对象的该成员取值赋值</summary>>
        /// <param name="target">目标对象</param>
        /// <returns></returns>
        Object this[Object target] { get; set; }

        /// <summary>是否可读</summary>>
        Boolean CanRead { get; }

        /// <summary>是否可写</summary>>
        Boolean CanWrite { get; }
    }
}