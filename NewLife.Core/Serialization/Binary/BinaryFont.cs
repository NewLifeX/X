using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using NewLife.Collections;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>字体处理器。</summary>
    public class BinaryFont : BinaryHandlerBase
    {
        /// <summary>实例化</summary>
        public BinaryFont()
        {
            Priority = 0x50;
        }

        /// <summary>写入对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            if (type != typeof(Font)) return false;

            // 写入引用
            if (value == null)
            {
                Host.WriteSize(0);
                return true;
            }
            Host.WriteSize(1);

            var font = value as Font;
            WriteLog("WriteFont {0}", font);

            Host.Write(font.Name);
            Host.Write(font.Size);
            Host.Write((Byte)font.Style);

            return true;
        }

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (type != typeof(Font)) return false;

            // 读引用
            var size = Host.ReadSize();
            if (size == 0) return true;

            if (size != 1) WriteLog("读取引用应该是1，而实际是{0}", size);

            var font = new Font(Host.Read<String>(), Host.Read<Single>(), (FontStyle)Host.ReadByte());
            value = font;
            WriteLog("ReadFont {0}", font);

            return true;
        }
    }
}