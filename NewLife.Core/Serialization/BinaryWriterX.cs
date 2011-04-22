using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace NewLife.Serialization
{
    /// <summary>
    /// 二进制写入器
    /// </summary>
    public class BinaryWriterX : WriterBase
    {
        #region 属性
        private BinaryWriter _Writer;
        /// <summary>写入器</summary>
        public BinaryWriter Writer
        {
            get { return _Writer; }
            set { _Writer = value; }
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
        /// 写入字节
        /// </summary>
        /// <param name="value"></param>
        public override void Write(byte value)
        {
            Writer.Write(value);
        }

        /// <summary>
        /// 判断字节顺序
        /// </summary>
        /// <param name="buffer"></param>
        protected override void WriteIntBytes(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 1) return;

            // 如果不是小端字节顺序，则倒序
            if (!IsLittleEndian) Array.Reverse(buffer);

            base.WriteIntBytes(buffer);
        }
        #endregion

        #region 整数
        /// <summary>
        /// 将 2 字节有符号整数写入当前流，并将流的位置提升 2 个字节。
        /// </summary>
        /// <param name="value">要写入的 2 字节有符号整数。</param>
        public override void Write(short value)
        {
            if (EncodeInt)
                WriteEncoded(value);
            else
                base.Write(value);
        }

        /// <summary>
        /// 将 4 字节有符号整数写入当前流，并将流的位置提升 4 个字节。
        /// </summary>
        /// <param name="value">要写入的 4 字节有符号整数。</param>
        public override void Write(int value)
        {
            if (EncodeInt)
                WriteEncoded(value);
            else
                base.Write(value);
        }

        /// <summary>
        /// 将 8 字节有符号整数写入当前流，并将流的位置提升 8 个字节。
        /// </summary>
        /// <param name="value">要写入的 8 字节有符号整数。</param>
        public override void Write(long value)
        {
            if (EncodeInt)
                WriteEncoded(value);
            else
                base.Write(value);
        }
        #endregion

        #region 7位压缩编码整数
        /// <summary>
        /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value"></param>
        /// <returns>实际写入字节数</returns>
        public Int32 WriteEncoded(Int16 value)
        {
            Int32 count = 1;
            UInt16 num = (UInt16)value;
            while (num >= 0x80)
            {
                this.Write((byte)(num | 0x80));
                num = (UInt16)(num >> 7);

                count++;
            }
            this.Write((byte)num);

            return count;
        }

        /// <summary>
        /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value"></param>
        /// <returns>实际写入字节数</returns>
        public Int32 WriteEncoded(Int32 value)
        {
            Int32 count = 1;
            UInt32 num = (UInt32)value;
            while (num >= 0x80)
            {
                this.Write((byte)(num | 0x80));
                num = num >> 7;

                count++;
            }
            this.Write((byte)num);

            return count;
        }

        /// <summary>
        /// 以7位压缩格式写入64位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value"></param>
        /// <returns>实际写入字节数</returns>
        public Int32 WriteEncoded(Int64 value)
        {
            Int32 count = 1;
            UInt64 num = (UInt64)value;
            while (num >= 0x80)
            {
                this.Write((byte)(num | 0x80));
                num = num >> 7;

                count++;
            }
            this.Write((byte)num);

            return count;
        }
        #endregion

        #region 写入对象
        /// <summary>
        /// 写对象成员
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        public override bool WriteMembers(object value, Type type, WriteObjectCallback callback)
        {
            Boolean allowNull = Depth > 1;

            // 值类型不会为null，只有引用类型才需要写标识
            if (!type.IsValueType)
            {
                // 允许空时，增加一个字节表示对象是否为空
                if (value == null)
                {
                    if (allowNull) Write(false);
                    return true;
                }
                if (allowNull) Write(true);
            }

            return base.WriteMembers(value, type, callback);
        }
        #endregion

        #region 枚举
        /// <summary>
        /// 写入枚举数据，复杂类型使用委托方法进行处理
        /// </summary>
        /// <param name="value">对象</param>
        /// <param name="type">类型</param>
        /// <param name="callback">使用指定委托方法处理复杂数据</param>
        /// <returns>是否写入成功</returns>
        public override bool WriteEnumerable(System.Collections.IEnumerable value, Type type, WriteObjectCallback callback)
        {
            if (value == null)
            {
                // 允许空，写入0字节
                if (Depth > 1) Write(0);
                return true;
            }

            #region 初始化数据
            Int32 count = 0;
            Type elementType = null;
            List<Object> list = new List<Object>();

            if (type.IsArray)
            {
                Array arr = value as Array;
                count = arr.Length;
                elementType = type.GetElementType();
            }
            //else if (typeof(ICollection).IsAssignableFrom(type))
            //{
            //    count = (value as ICollection).Count;
            //}
            else
            {
                foreach (Object item in value)
                {
                    // 加入集合，防止value进行第二次遍历
                    list.Add(item);

                    if (item == null) continue;

                    // 找到枚举的元素类型
                    Type t = item.GetType();
                    if (elementType == null)
                        elementType = t;
                    else if (elementType != item.GetType())
                    {
                        if (elementType.IsAssignableFrom(t))
                        {
                            // t继承自elementType
                        }
                        else if (t.IsAssignableFrom(elementType))
                        {
                            // elementType继承自t
                            elementType = t;
                        }
                        else
                        {
                            // 可能是Object类型，无法支持
                            return false;
                        }
                    }
                }
                count = list.Count;
                value = list;
            }
            if (count == 0)
            {
                Write(0);
                return true;
            }

            // 可能是Object类型，无法支持
            if (elementType == null) return false;

            //TODO 如果不是基本类型和特殊类型，必须有委托方法
            //if (!Support(elementType) && callback == null) return false;
            #endregion

            // 写入长度
            Write(count);

            return base.WriteEnumerable(value, type, callback);
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

            return ObjectInfo.GetMembers(type, value, true);
        }
        #endregion

        #region 设置
        ///// <summary>
        ///// 创建配置
        ///// </summary>
        ///// <returns></returns>
        //protected override ReaderWriterConfig CreateConfig()
        //{
        //    BinaryReaderWriterConfig config = new BinaryReaderWriterConfig();
        //    config.EncodeInt = EncodeInt;
        //    return config;
        //}
        #endregion
    }
}