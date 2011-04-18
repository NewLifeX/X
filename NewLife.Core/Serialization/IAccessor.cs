using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// 序列化访问器
    /// </summary>
    [CLSCompliant(false)]
    public interface IAccessor
    {
        /// <summary>
        /// 从读取器中读取数据到对象
        /// </summary>
        /// <param name="reader"></param>
        void Read(IReader reader);

        /// <summary>
        /// 把对象数据写入到写入器
        /// </summary>
        /// <param name="writer"></param>
        void Write(IWriter writer);
    }
}
