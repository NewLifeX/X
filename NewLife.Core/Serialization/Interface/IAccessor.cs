using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// 序列化访问器
    /// </summary>
    public interface IAccessor
    {
        /// <summary>
        /// 从读取器中读取数据到对象
        /// </summary>
        /// <param name="reader">读取器</param>
        /// <returns>是否读取成功，若返回成功读取器将不再读取该对象</returns>
        Boolean Read(IReader reader);

        /// <summary>
        /// 把对象数据写入到写入器
        /// </summary>
        /// <param name="writer">写入器</param>
        /// <returns>是否写入成功，若返回成功写入器将不再读写入对象</returns>
        Boolean Write(IWriter writer);
    }
}