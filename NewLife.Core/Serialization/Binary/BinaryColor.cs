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
    /// <summary>颜色处理器。</summary>
    public class BinaryColor : BinaryHandlerBase
    {
        /// <summary>实例化</summary>
        public BinaryColor()
        {
            Priority = 0x50;
        }

        /// <summary>写入对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            if (type != typeof(Color)) return false;

            // 结构体不需要引用计数
            //// 写入引用
            //if (value == null)
            //{
            //    Host.WriteSize(0);
            //    return true;
            //}
            //Host.WriteSize(1);

            var color = (Color)value;
            WriteLog("WriteColor {0}", color);

            Host.Write(color.A);
            Host.Write(color.R);
            Host.Write(color.G);
            Host.Write(color.B);

            return true;
        }

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (type != typeof(Color)) return false;

            // 结构体不需要引用计数
            //// 读引用
            //var size = Host.ReadSize();
            //if (size == 0) return true;

            //if (size != 1) WriteLog("读取引用应该是1，而实际是{0}", size);

            var a = Host.ReadByte();
            var r = Host.ReadByte();
            var g = Host.ReadByte();
            var b = Host.ReadByte();
            var color = Color.FromArgb(a, r, g, b);
            WriteLog("ReadColor {0}", color);
            value = color;

            return true;
        }
    }
}