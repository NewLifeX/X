using System;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>二进制读写接口</summary>
    public interface IBinaryReadWrite
    {
        /// <summary>编码整数</summary>
        Boolean EncodeInt { get; }

        /// <summary>小端字节序</summary>
        Boolean IsLittleEndian { get; }

        /// <summary>文本编码</summary>
        Encoding Encoding { get; }

        /// <summary>写入一个对象</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean Write(Object value);

        /// <summary>写入字节</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean Write(Byte value);

        /// <summary>写入大小</summary>
        /// <param name="size"></param>
        /// <returns></returns>
        Boolean WriteSize(Int32 size);
    }

    /// <summary>二进制读写处理器接口</summary>
    public interface IBinaryReadWriteHandler
    {
        /// <summary>宿主读写器</summary>
        IBinaryReadWrite Host { get; set; }

        /// <summary>写入一个对象</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        Boolean Write(Object value);

        ///// <summary>读取一个对象</summary>
        ///// <param name="value"></param>
        ///// <returns></returns>
        //Boolean Read(Object value);
    }

    /// <summary>二进制读写处理器基类</summary>
    public abstract class BinaryReadWriteHandlerBase : IBinaryReadWriteHandler
    {
        private IBinaryReadWrite _Host;
        /// <summary>宿主读写器</summary>
        public IBinaryReadWrite Host { get { return _Host; } set { _Host = value; } }

        /// <summary>写入一个对象</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public abstract Boolean Write(Object value);
    }
}