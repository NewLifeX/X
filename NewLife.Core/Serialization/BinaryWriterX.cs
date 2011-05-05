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
        #endregion

        #region 字节
        /// <summary>
        /// 写入字节
        /// </summary>
        /// <param name="value"></param>
        public override void Write(byte value)
        {
            SetDebugIndent();

            Writer.Write(value);

            AutoFlush();
        }

        ///// <summary>
        ///// 将字节数组写入，先写入字节数组的长度
        ///// </summary>
        ///// <param name="buffer">包含要写入的数据的字节数组。</param>
        //public override void Write(byte[] buffer)
        //{
        //    if (buffer == null)
        //    {
        //        Write(0);
        //        return;
        //    }

        //    Write(buffer.Length);
        //    Write(buffer, 0, buffer.Length);
        //}

        /// <summary>
        /// 将字节数组部分写入当前流。
        /// </summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="index">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        public override void Write(byte[] buffer, int index, int count)
        {
            if (buffer == null || buffer.Length < 1 || count <= 0 || index >= buffer.Length) return;

            SetDebugIndent();

            Writer.Write(buffer, index, count);

            AutoFlush();
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

            Write(list.ToArray(), 0, list.Count);

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

            Write(list.ToArray(), 0, list.Count);

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

            Write(list.ToArray(), 0, list.Count);

            return count;
        }
        #endregion

        #region 扩展处理类型
        /// <summary>
        /// 写入Type
        /// </summary>
        /// <param name="value"></param>
        protected override void OnWriteType(Type value)
        {
            if (Settings.SplitComplexType)
            {
                if (value.IsArray)
                {
                    // 数组类型
                    Write((Byte)BinarySettings.TypeKinds.Array);
                    // 数组维数
                    Write(value.GetArrayRank());
                    // 数据元素类型
                    Write(value.GetElementType());
                    return;
                }

                if (value.IsNested)
                {
                    // 内嵌类型
                    Write((Byte)BinarySettings.TypeKinds.Nested);
                    // 声明类
                    Write(value.DeclaringType);
                    // 本类类名
                    Write(value.Name);
                    return;
                }

                // 特殊处理泛型，把泛型类型和泛型参数拆分开来，充分利用对象引用以及FullName
                if (value.IsGenericType && !value.IsGenericTypeDefinition)
                {
                    // 泛型类型
                    Write((Byte)BinarySettings.TypeKinds.Generic);

                    Write(value.GetGenericTypeDefinition());
                    Type[] ts = value.GetGenericArguments();
                    if (ts != null && ts.Length > 0)
                    {
                        // 不需要泛型参数个数，因为读取时能从GetGenericArguments得知个数
                        //Write(ts.Length);
                        foreach (Type type in ts)
                        {
                            Write(type);
                        }
                    }
                    //else
                    //{
                    //    Write(0);
                    //}
                    return;
                }

                // 普通类型
                Write((Byte)BinarySettings.TypeKinds.Normal);
            }

            base.OnWriteType(value);
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
            // WriteType必须增加深度，否则写对象引用时将会受到影响，顶级对象不写对象引用
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
                WriteLog("WriteObjRef", "null");

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

                WriteLog("AddObjRef", objRefs.Count, value.ToString(), value.GetType().Name);

                // 写入引用计数
                if (Depth > 1) Write(objRefs.Count);

                return false;
            }

            WriteLog("WriteObjRef", index + 1, value.ToString(), value.GetType().Name);

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
        /// <param name="type">要写入的对象类型</param>
        /// <param name="member">成员</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否写入成功</returns>
        protected override bool OnWriteMember(object value, Type type, IObjectMemberInfo member, int index, WriteObjectCallback callback)
        {
            if (!Settings.IgnoreName) Write(member.Name);

            return base.OnWriteMember(value, type, member, index, callback);
        }
        #endregion

        //#region 字典
        ///// <summary>
        ///// 写入字典类型数据
        ///// </summary>
        ///// <param name="value">字典数据</param>
        ///// <param name="type">要写入的对象类型</param>
        ///// <param name="callback">处理成员的方法</param>
        ///// <returns>是否写入成功</returns>
        //public override bool WriteDictionary(IDictionary value, Type type, WriteObjectCallback callback)
        //{
        //    if (value == null) return true;

        //    WriteSize(value.Count);
        //    if (value.Count == 0) return true;

        //    return base.WriteDictionary(value, type, callback);
        //}
        //#endregion

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
            Int32 count = 0;

            if (type.IsArray)
            {
                Array arr = value as Array;
                count = arr.Length;
            }
            else
            {
                List<Object> list = new List<Object>();
                foreach (Object item in value)
                {
                    // 加入集合，防止value进行第二次遍历
                    list.Add(item);
                }
                count = list.Count;
                value = list;
            }

            if (count == 0)
            {
                // 写入0长度。至此，枚举类型前面就会有两个字节用于标识，一个是是否为空，或者是对象引用，第二个是长度，注意长度为0的枚举类型
                WriteSize(0);
                return true;
            }

            // 写入长度
            WriteSize(count);

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

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            Byte[] buffer = ToArray();
            if (buffer == null || buffer.Length < 1) return base.ToString();

            return BitConverter.ToString(buffer);
        }
        #endregion
    }
}