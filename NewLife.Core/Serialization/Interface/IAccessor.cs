using System;
using System.IO;

namespace NewLife.Serialization
{
    /// <summary>序列化访问器。接口实现者可以在这里完全自定义序列化行为</summary>
    public interface IAccessor
    {
        /// <summary>从数据流中读取消息</summary>
        /// <param name="stream">数据流</param>
        /// <param name="context">上下文</param>
        /// <returns>是否成功</returns>
        Boolean Read(Stream stream, Object context);

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="stream">数据流</param>
        /// <param name="context">上下文</param>
        void Write(Stream stream, Object context);
    }
}