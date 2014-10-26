using System;
using System.Text;
using System.Collections.Generic;

namespace NewLife.Serialization
{
    /// <summary>二进制序列化接口</summary>
    public interface IBinary : IFormatterX
    {
        #region 属性
        /// <summary>编码整数</summary>
        Boolean EncodeInt { get; }

        /// <summary>小端字节序</summary>
        Boolean IsLittleEndian { get; }

        /// <summary>文本编码</summary>
        Encoding Encoding { get; }

        /// <summary>使用指定大小的FieldSizeAttribute特性，默认false</summary>
        Boolean UseFieldSize { get; }

        /// <summary>处理器列表</summary>
        List<IBinaryHandler> Handlers { get; }
        #endregion

        #region 写入
        /// <summary>写入字节</summary>
        /// <param name="value"></param>
        void Write(Byte value);

        /// <summary>将字节数组部分写入当前流，不写入数组长度。</summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="offset">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        void Write(Byte[] buffer, Int32 offset = 0, Int32 count = -1);

        /// <summary>写入大小</summary>
        /// <param name="size">要写入的大小值</param>
        /// <returns>返回特性指定的固定长度，如果没有则返回-1</returns>
        Int32 WriteSize(Int32 size);
        #endregion

        #region 读取
        /// <summary>读取字节</summary>
        /// <returns></returns>
        Byte ReadByte();

        /// <summary>从当前流中将 count 个字节读入字节数组</summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        Byte[] ReadBytes(Int32 count);

        /// <summary>读取大小</summary>
        /// <returns></returns>
        Int32 ReadSize();
        #endregion

        #region 调试日志
        /// <summary>是否启用调试</summary>
        Boolean Debug { get; set; }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        void WriteLog(String format, params Object[] args);
        #endregion
    }

    /// <summary>二进制读写处理器接口</summary>
    public interface IBinaryHandler : IComparable<IBinaryHandler>
    {
        /// <summary>宿主读写器</summary>
        IBinary Host { get; set; }

        /// <summary>优先级</summary>
        Int32 Priority { get; set; }

        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        Boolean Write(Object value, Type type);

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean TryRead(Type type, ref Object value);
    }

    /// <summary>二进制读写处理器基类</summary>
    public abstract class BinaryHandlerBase : IBinaryHandler
    {
        private IBinary _Host;
        /// <summary>宿主读写器</summary>
        public IBinary Host { get { return _Host; } set { _Host = value; } }

        private Int32 _Priority;
        /// <summary>优先级</summary>
        public Int32 Priority { get { return _Priority; } set { _Priority = value; } }

        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public abstract Boolean Write(Object value, Type type);

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract Boolean TryRead(Type type, ref Object value);

        Int32 IComparable<IBinaryHandler>.CompareTo(IBinaryHandler other)
        {
            // 优先级较大在前面
            return this.Priority.CompareTo(other.Priority);
        }

        /// <summary>输出日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Host.WriteLog(format, args);
        }
    }
}