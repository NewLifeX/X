using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace NewLife.Serialization
{
    /// <summary>
    /// 二进制读取器
    /// </summary>
    public class BinaryReaderX : ReaderBase
    {
        #region 属性
        private BinaryReader _Reader;
        /// <summary>读取器</summary>
        public BinaryReader Reader
        {
            get { return _Reader; }
            set { _Reader = value; }
        }

        private Boolean _IsLittleEndian = true;
        /// <summary>
        /// 是否小端字节序。
        /// </summary>
        /// <remarks>
        /// 网络协议都是Big-Endian；
        /// Java编译的都是Big-Endian；
        /// Motorola的PowerPC是Big-Endian；
        /// x86系列则采用Little-Endian方式存储数据；
        /// ARM同时支持 big和little，实际应用中通常使用Little-Endian。
        /// </remarks>
        public Boolean IsLittleEndian
        {
            get { return _IsLittleEndian; }
            set { _IsLittleEndian = value; }
        }

        private Boolean _EncodeInt;
        /// <summary>编码整数</summary>
        public Boolean EncodeInt
        {
            get { return _EncodeInt; }
            set { _EncodeInt = value; }
        }
        #endregion

        #region 已重载
        /// <summary>
        /// 读取字节
        /// </summary>
        /// <returns></returns>
        public override byte ReadByte()
        {
            return Reader.ReadByte();
        }

        /// <summary>
        /// 判断字节顺序
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        protected override byte[] ReadIntBytes(int count)
        {
            Byte[] buffer = base.ReadIntBytes(count);

            // 如果不是小端字节顺序，则倒序
            if (!IsLittleEndian) Array.Reverse(buffer);

            return buffer;
        }
        #endregion

        #region 整数
        /// <summary>
        /// 从当前流中读取 2 字节有符号整数，并使流的当前位置提升 2 个字节。
        /// </summary>
        /// <returns></returns>
        public override short ReadInt16()
        {
            if (EncodeInt)
                return ReadEncodedInt16();
            else
                return base.ReadInt16();
        }

        /// <summary>
        /// 从当前流中读取 4 字节有符号整数，并使流的当前位置提升 4 个字节。
        /// </summary>
        /// <returns></returns>
        public override int ReadInt32()
        {
            if (EncodeInt)
                return ReadEncodedInt32();
            else
                return base.ReadInt32();
        }

        /// <summary>
        /// 从当前流中读取 8 字节有符号整数，并使流的当前位置向前移动 8 个字节。
        /// </summary>
        /// <returns></returns>
        public override long ReadInt64()
        {
            if (EncodeInt)
                return ReadEncodedInt64();
            else
                return base.ReadInt64();
        }
        #endregion

        #region 7位压缩编码整数
        /// <summary>
        /// 以压缩格式读取16位整数
        /// </summary>
        /// <returns></returns>
        public Int16 ReadEncodedInt16()
        {
            Byte b;
            Int16 rs = 0;
            Byte n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int16，否则可能溢出
                rs += (Int16)((b & 0x7f) << n);
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 16) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }

        /// <summary>
        /// 以压缩格式读取32位整数
        /// </summary>
        /// <returns></returns>
        public Int32 ReadEncodedInt32()
        {
            Byte b;
            Int32 rs = 0;
            Byte n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int32，否则可能溢出
                rs += (Int32)((b & 0x7f) << n);
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 32) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }

        /// <summary>
        /// 以压缩格式读取64位整数
        /// </summary>
        /// <returns></returns>
        public Int64 ReadEncodedInt64()
        {
            Byte b;
            Int64 rs = 0;
            Byte n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int64，否则可能溢出
                rs += (Int64)(b & 0x7f) << n;
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 64) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }
        #endregion

        #region 获取成员
        /// <summary>
        /// 已重载。序列化字段
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected override MemberInfo[] OnGetMembers(Type type)
        {
            return FilterMembers(FindFields(type), typeof(NonSerializedAttribute));
        }
        #endregion
    }
}