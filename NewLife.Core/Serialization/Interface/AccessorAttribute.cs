using System;
using NewLife.Serialization.Interface;

namespace NewLife.Serialization
{
    /// <summary>成员访问特性。使用自定义逻辑序列化成员</summary>
    [AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
    public abstract class AccessorAttribute : Attribute, IMemberAccessor
    {
        /// <summary>从数据流中读取消息</summary>
        /// <param name="formatter">序列化</param>
        /// <param name="context">上下文</param>
        /// <returns>是否成功</returns>
        public virtual Boolean Read(IFormatterX formatter, AccessorContext context) => false;

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="formatter">序列化</param>
        /// <param name="context">上下文</param>
        public virtual Boolean Write(IFormatterX formatter, AccessorContext context) => false;
    }
}