﻿using System;

namespace NewLife.Serialization
{
    /// <summary>成员序列化访问器。接口实现者可以在这里完全自定义序列化行为</summary>
    public interface IMemberAccessor
    {
        /// <summary>从数据流中读取消息</summary>
        /// <param name="formatter">序列化</param>
        /// <param name="context">上下文</param>
        /// <returns>是否成功</returns>
        Boolean Read(IFormatterX formatter, AccessorContext context);

        /// <summary>把消息写入到数据流中</summary>
        /// <param name="formatter">序列化</param>
        /// <param name="context">上下文</param>
        Boolean Write(IFormatterX formatter, AccessorContext context);
    }
}