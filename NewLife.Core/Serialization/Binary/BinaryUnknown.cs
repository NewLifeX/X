using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using NewLife.Collections;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>内部对象处理器。对于其它处理器无法支持的类型，一律由该处理器解决</summary>
    public class BinaryUnknown : BinaryHandlerBase
    {
        /// <summary>实例化</summary>
        public BinaryUnknown()
        {
            Priority = 0xFFFF;
        }

        /// <summary>写入对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            // 先写引用
            if (value == null)
            {
                Host.WriteSize(0);
                return true;
            }

            // 调用.Net的二进制序列化来解决剩下的事情
            var bf = new BinaryFormatter();
            var ms = new MemoryStream();
            bf.Serialize(ms, value);
            ms.Position = 0;
            var buf = ms.ToArray();

            Host.WriteSize(buf.Length);
            Host.Write(buf);

            return true;
        }

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            // 先读取引用
            var len = Host.ReadSize();
            if (len == 0) return true;

            var bf = new BinaryFormatter();
            var ms = new MemoryStream(Host.ReadBytes(len));
            value = bf.Deserialize(ms);

            return true;
        }
    }
}