using System;
using System.Reflection;

namespace NewLife.Serialization
{
    /// <summary>序列化访问上下文</summary>
    public class AccessorContext
    {
        /// <summary>宿主</summary>
        public Object Host { get; set; }

        /// <summary>对象类型</summary>
        public Type Type { get; set; }

        /// <summary>目标对象</summary>
        public Object Value { get; set; }

        /// <summary>成员</summary>
        public MemberInfo Member { get; set; }
    }
}