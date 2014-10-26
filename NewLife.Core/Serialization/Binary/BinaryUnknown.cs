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
            //var ms = new MemoryStream();
            //bf.Serialize(ms, value);
            //ms.Position = 0;
            //var buf = ms.ToArray();

            //Host.WriteSize(buf.Length);
            //Host.Write(buf);

            Int32 size = 0;
            // 为了预估大小，调试进行两次序列化
            if (Host.Debug)
            {
                var ms = new MemoryStream();
                bf.Serialize(ms, value);
                size = (Int32)ms.Length;
            }

            // 先写入一个长度，待会回来覆盖
            var p = Host.Stream.Position;
            Host.WriteSize(size);
            var start = Host.Stream.Position;

            bf.Serialize(Host.Stream, value);

            // 写入长度
            var end = Host.Stream.Position;
            size = (Int32)(end - start);
            Host.Stream.Position = p;
            Host.WriteSize(size);
            Host.Stream.Position = end;

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
            //var ms = new MemoryStream(Host.ReadBytes(len));
            //value = bf.Deserialize(ms);

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