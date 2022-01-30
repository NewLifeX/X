using System;

namespace NewLife.Serialization
{
    /// <summary>成员访问特性。使用自定义逻辑序列化成员</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public class MemberAccessorAttribute : Attribute
    {
        /// <summary>处理器类型</summary>
        public Type Type { get; set; }

        /// <summary>指定成员的序列化处理器</summary>
        /// <param name="type"></param>
        public MemberAccessorAttribute(Type type) => Type = type;
    }
}