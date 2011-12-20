using System;
using System.IO;
using System.Reflection;
using NewLife.Exceptions;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>二进制读取器</summary>
    /// <remarks>
    /// 序列化框架的核心思想：基本类型直接读取，自定义类型反射得到成员，逐层递归读取！详见<see cref="IReaderWriter"/>
    /// 
    /// 二进制序列化，并不仅仅是为了序列化一个对象那么简单，它最初的目标是实现一个高度可自定义的序列化组件，后来提升为以序列化各种协议为重点。
    /// 理论上，只要用实体类实现了各种协议（文件格式），那么它就能只用一个Read/Write实现协议实体对象与二进制数据流之间的映射。
    /// </remarks>
    /// <example>
    /// 标准用法：
    /// <code>
    /// var reader = new BinaryReaderX();
    /// reader.Stream = stream;
    /// entity = reader.ReadObject&lt;TEntity&gt;();
    /// // 使用数据流填充已有对象，这是几乎所有其它序列化框架所不具有的功能
    /// // reader.ReadObject(null, ref entity);
    /// </code>
    /// </example>
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

        #region 构造
        /// <summary>实例化一个二进制读取器</summary>
        public BinaryReaderX()
        {
            // 默认的大小格式为32位压缩编码整数
            Settings.SizeFormat = TypeCode.UInt32;
            // 默认使用字段作为序列化成员
            Settings.UseField = true;
        }
        #endregion

        #region 字节
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

            //if (count < 0) count = ReadInt32();
            if (count < 0) count = ReadSize();

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

        #region 方法
        /// <summary>读取大小</summary>
        /// <returns></returns>
        protected override Int32 OnReadSize()
        {
            var member = CurrentMember as ReflectMemberInfo;
            if (member != null)
            {
                // 获取FieldSizeAttribute特性
                var att = AttributeX.GetCustomAttribute<FieldSizeAttribute>(member.Member, true);
                if (att != null)
                {
                    // 如果指定了固定大小，直接返回
                    if (att.Size > 0) return att.Size;

                    // 如果指定了引用字段，则找引用字段所表示的长度d
                    Int32 size = att.GetReferenceSize(CurrentObject, member.Member);
                    if (size >= 0) return size;
                }
            }

            switch (Settings.SizeFormat)
            {
                case TypeCode.Int16:
                    return ReadInt16();
                case TypeCode.UInt16:
                    return ReadEncodedInt16();
                case TypeCode.Int32:
                case TypeCode.Int64:
                default:
                    return ReadInt32();
                case TypeCode.UInt32:
                case TypeCode.UInt64:
                    return ReadEncodedInt32();
            }
        }

        /// <summary>探测下一个可用的字节是否预期字节，并且不提升字节的位置。</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Boolean Expect(params Byte[] values) { return Expect(ReadByte, values); }

        /// <summary>探测下一个可用的数字是否预期数字，并且不提升字节的位置。</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Boolean Expect(params Int16[] values) { return Expect(ReadInt16, values); }

        /// <summary>探测下一个可用的数字是否预期数字，并且不提升字节的位置。</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Boolean Expect(params UInt16[] values) { return Expect(ReadUInt16, values); }

        /// <summary>探测下一个可用的数字是否预期数字，并且不提升字节的位置。</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Boolean Expect(params Int32[] values) { return Expect(ReadInt32, values); }

        /// <summary>探测下一个可用的数字是否预期数字，并且不提升字节的位置。</summary>
        /// <param name="values"></param>
        /// <returns></returns>
        public Boolean Expect(params UInt32[] values) { return Expect(ReadUInt32, values); }

        /// <summary>探测下一个可用的数值是否预期数值，并且不提升字节的位置。</summary>
        /// <param name="func">读取数值的方法，比如ReadInt32等</param>
        /// <param name="values">预期数值列表</param>
        /// <returns></returns>
        public Boolean Expect<T>(Func<T> func, params T[] values)
        {
            var stream = Reader.BaseStream;
            if (!stream.CanSeek) return false;

            Int64 p = stream.Position;
            T rs = func();
            stream.Position = p;

            return Array.IndexOf<T>(values, rs) >= 0;
        }

        /// <summary>使用跟踪流</summary>
        public override void EnableTraceStream()
        {
            var stream = Stream;
            if (stream == null || stream is TraceStream) return;

            var ts = new TraceStream(stream);
            ts.IsLittleEndian = Settings.IsLittleEndian;
            Stream = ts;
        }
        #endregion
    }
}