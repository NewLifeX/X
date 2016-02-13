using System;
using System.IO;
using System.Reflection;

namespace NewLife.Serialization
{
    /// <summary>成员序列化访问器。接口实现者可以在这里完全自定义序列化行为</summary>
    public interface IMemberAccessor
    {
        /// <summary>从数据流中读取消息</summary>
        /// <param name="fm">序列化</param>
        /// <param name="member">成员</param>
        /// <returns>是否成功</returns>
        Boolean Read(IFormatterX fm, MemberInfo member);

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="fm">序列化</param>
        /// <param name="member">成员</param>
        void Write(IFormatterX fm, MemberInfo member);
    }
}