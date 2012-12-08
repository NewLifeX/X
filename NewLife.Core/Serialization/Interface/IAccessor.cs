using System;

namespace NewLife.Serialization
{
    /// <summary>序列化访问器。接口实现者可以在这里完全自定义行为（返回true），也可以通过设置事件来影响行为（返回false）</summary>
    /// <example>
    /// 显式实现接口默认代码：
    /// <code>
    /// Boolean IAccessor.Read(IReader reader) { return false; }
    /// 
    /// Boolean IAccessor.ReadComplete(IReader reader, Boolean success) { return success; }
    /// 
    /// Boolean IAccessor.Write(IWriter writer) { return false; }
    /// 
    /// Boolean IAccessor.WriteComplete(IWriter writer, Boolean success) { return success; }
    /// </code>
    /// </example>
    public interface IAccessor
    {
        /// <summary>从读取器中读取数据到对象。接口实现者可以在这里完全自定义行为（返回true），也可以通过设置事件来影响行为（返回false）</summary>
        /// <param name="reader">读取器</param>
        /// <returns>是否读取成功，若返回成功读取器将不再读取该对象</returns>
        Boolean Read(IReader reader);

        /// <summary>从读取器中读取数据到对象后执行。接口实现者可以在这里取消Read阶段设置的事件</summary>
        /// <param name="reader">读取器</param>
        /// <param name="success">是否读取成功</param>
        /// <returns>是否读取成功</returns>
        Boolean ReadComplete(IReader reader, Boolean success);

        /// <summary>把对象数据写入到写入器。接口实现者可以在这里完全自定义行为（返回true），也可以通过设置事件来影响行为（返回false）</summary>
        /// <param name="writer">写入器</param>
        /// <returns>是否写入成功，若返回成功写入器将不再读写入对象</returns>
        Boolean Write(IWriter writer);

        /// <summary>把对象数据写入到写入器后执行。接口实现者可以在这里取消Write阶段设置的事件</summary>
        /// <param name="writer">写入器</param>
        /// <param name="success">是否写入成功</param>
        /// <returns>是否写入成功</returns>
        Boolean WriteComplete(IWriter writer, Boolean success);
    }
}