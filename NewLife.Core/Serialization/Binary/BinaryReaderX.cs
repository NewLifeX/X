using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NewLife.Exceptions;

namespace NewLife.Serialization
{
    /// <summary>二进制读取器</summary>
    public class BinaryReaderX : ReaderBase<BinarySettings>
    {
        #region 属性
        private BinaryReader _Reader;
        /// <summary>读取器</summary>
        public BinaryReader Reader
        {
            get { return _Reader ?? (_Reader = new BinaryReader(Stream, Settings.Encoding)); }
            set
            {
                _Reader = value;
                if (Stream != _Reader.BaseStream) Stream = _Reader.BaseStream;
            }
        }

        /// <summary>数据流。更改数据流后，重置Reader为空，以使用新的数据流</summary>
        public override Stream Stream
        {
            get { return base.Stream; }
            set
            {
                if (base.Stream != value) _Reader = null;
                base.Stream = value;
            }
        }
        #endregion

        #region 基本元数据
        /// <summary>
        /// 读取字节
        /// </summary>
        /// <returns></returns>
        public override byte ReadByte()
        {
            SetDebugIndent();

            return Reader.ReadByte();
        }

        /// <summary>
        /// 从当前流中将 count 个字节读入字节数组，并使当前位置提升 count 个字节。
        /// </summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        public override byte[] ReadBytes(int count)
        {
            SetDebugIndent();

            if (count < 0) count = ReadInt32();

            return Reader.ReadBytes(count);
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
            if (!Settings.IsLittleEndian) Array.Reverse(buffer);

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
            if (Settings.EncodeInt)
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
            if (Settings.EncodeInt)
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
            if (Settings.EncodeInt)
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

        #region 扩展处理类型
        /// <summary>
        /// 读取Type
        /// </summary>
        /// <returns></returns>
        protected override Type OnReadType()
        {
            if (Settings.SplitComplexType)
            {
                Type type = null;
                BinarySettings.TypeKinds kind = (BinarySettings.TypeKinds)ReadByte();

                WriteLog("ReadType", kind);

                switch (kind)
                {
                    case BinarySettings.TypeKinds.Normal:
                        return base.OnReadType();

                    case BinarySettings.TypeKinds.Array:
                        Int32 rank = ReadInt32();
                        return ReadType().MakeArrayType(rank);

                    case BinarySettings.TypeKinds.Nested:
                        return ReadType().GetNestedType(ReadString(), BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static | BindingFlags.Instance);
                    //type = ReadType();
                    //type = type.GetNestedType(ReadString(), BindingFlags.Public | BindingFlags.NonPublic);
                    //return type;

                    case BinarySettings.TypeKinds.Generic:
                        type = ReadType();
                        Type[] ts = type.GetGenericArguments();
                        for (int i = 0; i < ts.Length; i++)
                        {
                            ts[i] = ReadType();
                        }
                        return type.MakeGenericType(ts);

                    default:
                        break;
                }
            }

            return base.OnReadType();
        }
        #endregion

        #region 读取对象
        /// <summary>
        /// 尝试读取引用对象
        /// </summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        protected override bool ReadRefObject(Type type, ref object value, ReadObjectCallback callback)
        {
            if (value != null) type = value.GetType();
            // ReadType必须增加深度，否则写对象引用时将会受到影响，顶级对象不写对象引用
            if (!Settings.IgnoreType) type = ReadType();

            return base.ReadRefObject(type, ref value, callback);
        }
        #endregion

        #region 自定义对象
        /// <summary>
        /// 读取成员之前获取要读取的成员，默认是index处的成员，实现者可以重载，改变当前要读取的成员，如果当前成员不在数组里面，则实现者自己跳到下一个可读成员。
        /// </summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="members">可匹配成员数组</param>
        /// <param name="index">索引</param>
        /// <returns></returns>
        protected override IObjectMemberInfo GetMemberBeforeRead(Type type, object value, IObjectMemberInfo[] members, int index)
        {
            if (!Settings.IgnoreName)
            {
                while (true)
                {
                    String name = ReadString();
                    IObjectMemberInfo member = GetMemberByName(members, name);
                    if (member != null) return member;

                    // 无法找到可匹配的成员，尝试跳过该成员
                    if (Settings.IgnoreType) throw new XException("需要跳过成员" + name + "，但是无法确定其类型！");

                    Object obj = null;
                    ReadObject(null, ref obj, null);
                }
            }

            return base.GetMemberBeforeRead(type, value, members, index);
        }
        #endregion

        #region 获取成员
        /// <summary>
        /// 获取需要序列化的成员
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <returns>需要序列化的成员</returns>
        protected override IObjectMemberInfo[] OnGetMembers(Type type, Object value)
        {
            if (type == null) throw new ArgumentNullException("type");

            return ObjectInfo.GetMembers(type, value, true, true);
        }
        #endregion
    }
}