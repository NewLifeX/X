using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using NewLife.Reflection;
using System.Collections;
using NewLife.Exceptions;

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
            get { return _Reader ?? (_Reader = new BinaryReader(Stream, Encoding)); }
            set
            {
                _Reader = value;
                if (Stream != _Reader.BaseStream) Stream = _Reader.BaseStream;
            }
        }

        /// <summary>
        /// 数据流。更改数据流后，重置Reader为空，以使用新的数据流
        /// </summary>
        public override Stream Stream
        {
            get
            {
                return base.Stream;
            }
            set
            {
                if (base.Stream != value) _Reader = null;
                base.Stream = value;
            }
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
        /// 从当前流中将 count 个字节读入字节数组，并使当前位置提升 count 个字节。
        /// </summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        public override byte[] ReadBytes(int count)
        {
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
            if (!IsLittleEndian) Array.Reverse(buffer);

            return buffer;
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

        #region 枚举
        /// <summary>
        /// 读取元素集合
        /// </summary>
        /// <param name="type"></param>
        /// <param name="elementTypes"></param>
        /// <param name="callback"></param>
        /// <returns></returns>
        protected override Array[] ReadItems(Type type, Type[] elementTypes, ReadObjectCallback callback)
        {
            Type elementType = null;
            Type valueType = null;
            if (elementTypes != null)
            {
                if (elementTypes.Length >= 1) elementType = elementTypes[0];
                if (elementTypes.Length >= 2) valueType = elementTypes[1];
            }

            // 先读元素个数
            Int32 count = ReadInt32();
            if (count < 0) throw new InvalidOperationException("无效元素个数" + count + "！");

            // 没有元素
            if (count == 0) return new Array[0];

            Array[] arrs = new Array[elementTypes.Length];
            for (int i = 0; i < count; i++)
            {
                for (int j = 0; j < elementTypes.Length; j++)
                {
                    if (arrs[j] == null) arrs[j] = TypeX.CreateInstance(elementTypes[j].MakeArrayType(), count) as Array;

                    Object obj = null;
                    if (!ReadItem(elementTypes[j], ref obj, callback)) return null;
                    arrs[j].SetValue(obj, i);
                }
            }
            return arrs;
        }

        ///// <summary>
        ///// 尝试读取枚举
        ///// </summary>
        ///// <remarks>重点和难点在于如果得知枚举元素类型，这里假设所有元素类型一致，否则实在无法处理</remarks>
        ///// <param name="type">类型</param>
        ///// <param name="elementTypes">元素类型数组</param>
        ///// <param name="value">要读取的对象</param>
        ///// <param name="callback">处理成员的方法</param>
        ///// <returns>是否读取成功</returns>
        //public override Boolean ReadEnumerable(Type type, Type[] elementTypes, ref Object value, ReadObjectCallback callback)
        //{
        //    Type elementType = null;
        //    Type valueType = null;
        //    if (elementTypes != null)
        //    {
        //        if (elementTypes.Length >= 1) elementType = elementTypes[0];
        //        if (elementTypes.Length >= 2) valueType = elementTypes[1];
        //    }

        //    // 先读元素个数
        //    Int32 count = ReadInt32();
        //    if (count < 0) throw new InvalidOperationException("无效元素个数" + count + "！");

        //    // 没有元素
        //    if (count == 0) return true;

        //    #region 多数组取值
        //    //Array arr = Array.CreateInstance(elementType, count);
        //    //Array arr = TypeX.CreateInstance(elementType.MakeArrayType(), count) as Array;
        //    Array[] arrs = new Array[elementTypes.Length];
        //    for (int i = 0; i < count; i++)
        //    {
        //        //if (allowNull && ReadEncodedInt32() == 0) continue;

        //        for (int j = 0; j < elementTypes.Length; j++)
        //        {
        //            if (arrs[j] == null) arrs[j] = TypeX.CreateInstance(elementTypes[j].MakeArrayType(), count) as Array;

        //            Object obj = null;
        //            //if (!ReadValue(elementTypes[j], ref obj) &&
        //            //    !TryReadX(elementTypes[j], ref obj))
        //            {
        //                //obj = CreateInstance(elementType);
        //                //Read(obj, reader, encodeInt, allowNull, member.Member.MemberType == MemberTypes.Property);

        //                //obj = TypeX.CreateInstance(elementType);
        //                if (!ReadObject(elementTypes[j], ref obj, callback)) return false;
        //            }
        //            arrs[j].SetValue(obj, i);
        //        }
        //    }
        //    #endregion

        //    #region 结果处理
        //    // 如果是数组，直接赋值
        //    if (type.IsArray)
        //    {
        //        value = arrs[0];
        //        return true;
        //    }

        //    // 一个元素类型还是两个元素类型，分开处理
        //    if (arrs.Length == 1)
        //    {
        //        // 检查类型是否有指定类型的构造函数，如果有，直接创建类型，并把数组作为构造函数传入
        //        ConstructorInfoX ci = ConstructorInfoX.Create(type, new Type[] { typeof(IEnumerable) });
        //        if (ci == null) ci = ConstructorInfoX.Create(type, new Type[] { typeof(IEnumerable<>).MakeGenericType(elementType) });
        //        if (ci != null)
        //        {
        //            //value = TypeX.CreateInstance(type, arrs[0]);
        //            value = ci.CreateInstance(arrs[0]);
        //            return true;
        //        }

        //        // 添加方法
        //        MethodInfoX method = MethodInfoX.Create(type, "Add", new Type[] { elementType });
        //        if (method != null)
        //        {
        //            value = TypeX.CreateInstance(type);
        //            for (int i = 0; i < count; i++)
        //            {
        //                method.Invoke(value, arrs[0].GetValue(i));
        //            }
        //            return true;
        //        }
        //    }
        //    else if (arrs.Length == 2)
        //    {
        //        // 检查类型是否有指定类型的构造函数，如果有，直接创建类型，并把数组作为构造函数传入
        //        ConstructorInfoX ci = ConstructorInfoX.Create(type, new Type[] { typeof(IDictionary<,>).MakeGenericType(elementType, valueType) });
        //        if (ci != null)
        //        {
        //            Type dicType = typeof(Dictionary<,>).MakeGenericType(elementType, valueType);
        //            IDictionary dic = TypeX.CreateInstance(dicType) as IDictionary;
        //            for (int i = 0; i < count; i++)
        //            {
        //                dic.Add(arrs[0].GetValue(i), arrs[1].GetValue(i));
        //            }
        //            //value = TypeX.CreateInstance(type, dic);
        //            value = ci.CreateInstance(dic);
        //            return true;
        //        }

        //        // 添加方法
        //        MethodInfoX method = MethodInfoX.Create(type, "Add", new Type[] { elementType, valueType });
        //        if (method != null)
        //        {
        //            value = TypeX.CreateInstance(type);
        //            for (int i = 0; i < count; i++)
        //            {
        //                method.Invoke(value, arrs[0].GetValue(i), arrs[1].GetValue(i));
        //            }
        //            return true;
        //        }
        //    }
        //    #endregion

        //    return base.ReadEnumerable(type, elementTypes, ref value, callback);
        //}
        #endregion

        #region 读取对象
        /// <summary>
        /// 尝试读取目标对象指定成员的值，处理基础类型、特殊类型、基础类型数组、特殊类型数组，通过委托方法处理成员
        /// </summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public override bool ReadCustomObject(Type type, ref object value, ReadObjectCallback callback)
        {
            // 引用类型允许空时，先读取一个字节判断对象是否为空
            //if (!type.IsValueType && !config.Required && !ReadBoolean()) return true;
            if (!type.IsValueType && Depth > 1 && !ReadBoolean()) return true;

            return base.ReadCustomObject(type, ref value, callback);
        }

        List<Object> objRefs = new List<Object>();

        /// <summary>
        /// 读取对象引用。
        /// </summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <param name="index">引用计数</param>
        /// <returns>是否读取成功</returns>
        public override Boolean ReadObjRef(Type type, ref object value, out Int32 index)
        {
            index = ReadInt32();

            if (index < 0) return false;

            if (index == 0)
            {
                value = null;
                return true;
            }

            //// 如果引用计数刚好是下一个引用对象，说明这是该对象的第一次引用，返回false
            //if (index == objRefs.Count + 1) return false;

            //if (index > objRefs.Count) throw new XException("对象引用错误，无法找到引用计数为" + index + "的对象！");

            // 引用计数等于索引加一
            if (index > objRefs.Count) return false;

            value = objRefs[index - 1];

            return true;
        }

        /// <summary>
        /// 添加对象引用
        /// </summary>
        /// <param name="index">引用计数</param>
        /// <param name="value">对象</param>
        protected override void AddObjRef(Int32 index, object value)
        {
            //if (value != null && !objRefs.Contains(value)) objRefs.Add(value);

            if (value == null) return;

            while (index > objRefs.Count) objRefs.Add(null);

            objRefs[index - 1] = value;
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