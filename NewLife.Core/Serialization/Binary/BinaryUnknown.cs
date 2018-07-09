using System;
using System.IO;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using NewLife.Collections;

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
            // 需要检查序列化标记
            if (type.GetCustomAttribute<SerializableAttribute>() == null) return false;

            // 先写引用
            if (value == null)
            {
                Host.WriteSize(0);
                return true;
            }

            // 调用.Net的二进制序列化来解决剩下的事情
            var bf = new BinaryFormatter();

            var ms = Pool.MemoryStream.Get();
            bf.Serialize(ms, value);

            Host.WriteSize((Int32)ms.Length);
            ms.CopyTo(Host.Stream);
            ms.Put();

            return true;
        }

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            // 需要检查序列化标记
            if (type.GetCustomAttribute<SerializableAttribute>() == null) return false;

            // 先读取引用
            var len = Host.ReadSize();
            if (len == 0) return true;

            var bf = new BinaryFormatter();

            var p = Host.Stream.Position;
            value = bf.Deserialize(Host.Stream);
            // 检查数据大小是否相符
            var size = Host.Stream.Position - p;
            if (size != len)
            {
                WriteLog("实际使用数据{0}字节，不同于声明的{1}字节，可能存在错误", size, len);
            }

            return true;
        }
    }
}