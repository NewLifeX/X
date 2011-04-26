using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using System.Collections;

namespace NewLife.Serialization
{
    /// <summary>
    /// 二进制写入器
    /// </summary>
    public class BinaryWriterX : WriterBase<BinarySettings>
    {
        #region 属性
        private BinaryWriter _Writer;
        /// <summary>写入器</summary>
        public BinaryWriter Writer
        {
            get { return _Writer ?? (_Writer = new BinaryWriter(Stream, Settings.Encoding)); }
            set
            {
                _Writer = value;
                if (Stream != _Writer.BaseStream) Stream = _Writer.BaseStream;
            }
        }

        /// <summary>
        /// 数据流。更改数据流后，重置Writer为空，以使用新的数据流
        /// </summary>
        public override Stream Stream
        {
            get
            {
                return base.Stream;
            }
            set
            {
                if (base.Stream != value) _Writer = null;
                base.Stream = value;
            }
        }

        //private BinarySettings _Settings;
        ///// <summary>设置</summary>
        //public BinarySettings Settings
        //{
        //    get { return _Settings ?? (_Settings = new BinarySettings()); }
        //    set { _Settings = value; }
        //}
        #endregion

        #region 基础元数据
        /// <summary>
        /// 写入字节
        /// </summary>
        /// <param name="value"></param>
        public override void Write(byte value)
        {
            Writer.Write(value);
        }

        /// <summary>
        /// 将字节数组部分写入当前流。
        /// </summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="index">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        public override void Write(byte[] buffer, int index, int count)
        {
            if (buffer == null || buffer.Length < 1 || count <= 0 || index >= buffer.Length) return;

            Writer.Write(buffer, index, count);
        }

        /// <summary>
        /// 判断字节顺序
        /// </summary>
        /// <param name="buffer"></param>
        protected override void WriteIntBytes(byte[] buffer)
        {
            if (buffer == null || buffer.Length < 1) return;

            // 如果不是小端字节顺序，则倒序
            if (!Settings.IsLittleEndian) Array.Reverse(buffer);

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
            if (Settings.EncodeInt)
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
            if (Settings.EncodeInt)
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
            if (Settings.EncodeInt)
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
            List<Byte> list = new List<Byte>();

            Int32 count = 1;
            UInt16 num = (UInt16)value;
            while (num >= 0x80)
            {
                list.Add((byte)(num | 0x80));
                num = (UInt16)(num >> 7);

                count++;
            }
            list.Add((byte)num);

            Write(list.ToArray());

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
            List<Byte> list = new List<Byte>();

            Int32 count = 1;
            UInt32 num = (UInt32)value;
            while (num >= 0x80)
            {
                list.Add((byte)(num | 0x80));
                num = num >> 7;

                count++;
            }
            list.Add((byte)num);

            Write(list.ToArray());

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
            List<Byte> list = new List<Byte>();

            Int32 count = 1;
            UInt64 num = (UInt64)value;
            while (num >= 0x80)
            {
                list.Add((byte)(num | 0x80));
                num = num >> 7;

                count++;
            }
            list.Add((byte)num);

            Write(list.ToArray());

            return count;
        }
        #endregion

        #region 写入对象
        /// <summary>
        /// 写对象
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="type">要写入的对象类型</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        protected override bool WriteRefObject(object value, Type type, WriteObjectCallback callback)
        {
            if (value != null) type = value.GetType();
            if (!Settings.IgnoreType && type != null) Write(type);

            return base.WriteRefObject(value, type, callback);
        }

        List<Object> objRefs = new List<Object>();

        /// <summary>
        /// 写入对象引用。
        /// </summary>
        /// <param name="value">对象</param>
        /// <returns>是否写入成功。对象为空时写入0，否则写入对象的引用计数</returns>
        public override Boolean WriteObjRef(object value)
        {
            if (value == null)
            {
                // 顶级不需要
                if (Depth > 1) Write(0);
                return true;
            }

            // 在对象引用集合中找该对象
            Int32 index = objRefs.IndexOf(value);

            // 如果没找到，添加，返回false，通知上层继续处理
            if (index < 0)
            {
                objRefs.Add(value);

                // 写入引用计数
                if (Depth > 1) Write(objRefs.Count);

                return false;
            }

            // 如果找到，写入对象引用计数，返回true，通知上层不要再处理该对象，避免重写写入对象
            Write(index + 1);

            return true;
        }
        #endregion

        #region 自定义对象
        /// <summary>
        /// 写入成员
        /// </summary>
        /// <param name="value">要写入的对象</param>
        /// <param name="member">成员</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        protected override bool WriteMember(object value, IObjectMemberInfo member, int index, WriteObjectCallback callback)
        {
            if (!Settings.IgnoreName) Write(member.Name);

            return base.WriteMember(value, member, index, callback);
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
        public override bool WriteEnumerable(IEnumerable value, Type type, WriteObjectCallback callback)
        {
            if (value == null)
            {
                // 写入0长度。至此，枚举类型前面就会有两个字节用于标识，一个是是否为空，或者是对象引用，第二个是长度，注意长度为0的枚举类型
                Write(0);
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
                        // 争取找到最顶级的类型
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
            #endregion

            if (count == 0)
            {
                Write(0);
                return true;
            }

            // 可能是Object类型，无法支持
            if (elementType == null) return false;

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

            return ObjectInfo.GetMembers(type, value, true, true);
        }
        #endregion

        #region 方法
        /// <summary>
        /// 刷新缓存中的数据
        /// </summary>
        public override void Flush()
        {
            Writer.Flush();

            base.Flush();
        }

        /// <summary>
        /// 重置
        /// </summary>
        public override void Reset()
        {
            objRefs.Clear();

            base.Reset();
        }
        #endregion
    }
}