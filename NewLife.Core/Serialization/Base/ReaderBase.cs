using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;
using NewLife.Exceptions;
using NewLife.Linq;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>读取器基类</summary>
    /// <remarks>
    /// 序列化框架的核心思想：基本类型直接读取，自定义类型反射得到成员，逐层递归读取！详见<see cref="IReaderWriter"/>
    /// 
    /// 序列化框架的处理顺序为：<see cref="IAccessor" />接口 => <see cref="OnObjectReading" />事件 => 扩展类型 => <see cref="ReadValue(Type,ref Object)" />基础类型 => <see cref="ReadDictionary(Type,ref Object)" />字典 => <see cref="ReadEnumerable(Type,ref Object)" />枚举 => <see cref="ReadSerializable" />序列化接口 => <see cref="ReadCustomObject" />自定义对象 => <see cref="ReadUnKnown" />未知类型 => <see cref="OnObjectReaded" />事件
    /// 
    /// 反序列化对象时只能调用<see cref="ReadObject(Type)" />方法，其它所有方法（包括所有Read重载）仅用于内部读取或者自定义序列化时使用。
    /// </remarks>
    /// <typeparam name="TSettings">设置类</typeparam>
    public abstract class ReaderBase<TSettings> : ReaderWriterBase<TSettings>, IReader where TSettings : ReaderWriterSetting, new()
    {
        #region 基元类型
        #region 字节
        /// <summary>从当前流中读取下一个字节，并使流的当前位置提升 1 个字节。</summary>
        /// <returns></returns>
        public abstract byte ReadByte();

        /// <summary>从当前流中将 count 个字节读入字节数组，如果count小于0，则先读取字节数组长度。</summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        public virtual byte[] ReadBytes(int count)
        {
            if (count < 0) count = ReadSize();

            if (count <= 0) return null;

            Byte[] buffer = new Byte[count];
            for (int i = 0; i < count; i++)
            {
                buffer[i] = ReadByte();
            }

            return buffer;
        }

        /// <summary>从此流中读取一个有符号字节，并使流的当前位置提升 1 个字节。</summary>
        /// <returns></returns>
        //[CLSCompliant(false)]
        public virtual sbyte ReadSByte() { return (SByte)ReadByte(); }
        #endregion

        #region 有符号整数
        /// <summary>读取整数的字节数组，某些写入器（如二进制写入器）可能需要改变字节顺序</summary>
        /// <param name="count"></param>
        /// <returns></returns>
        protected virtual Byte[] ReadIntBytes(Int32 count) { return ReadBytes(count); }

        /// <summary>从当前流中读取 2 字节有符号整数，并使流的当前位置提升 2 个字节。</summary>
        /// <returns></returns>
        public virtual short ReadInt16() { return BitConverter.ToInt16(ReadIntBytes(2), 0); }

        /// <summary>从当前流中读取 4 字节有符号整数，并使流的当前位置提升 4 个字节。</summary>
        /// <returns></returns>
        public virtual int ReadInt32() { return BitConverter.ToInt32(ReadIntBytes(4), 0); }

        /// <summary>从当前流中读取 8 字节有符号整数，并使流的当前位置向前移动 8 个字节。</summary>
        /// <returns></returns>
        public virtual long ReadInt64() { return BitConverter.ToInt64(ReadIntBytes(8), 0); }
        #endregion

        #region 无符号整数
        /// <summary>使用 Little-Endian 编码从当前流中读取 2 字节无符号整数，并将流的位置提升 2 个字节。</summary>
        /// <returns></returns>
        //[CLSCompliant(false)]
        public virtual ushort ReadUInt16() { return (UInt16)ReadInt16(); }

        /// <summary>从当前流中读取 4 字节无符号整数并使流的当前位置提升 4 个字节。</summary>
        /// <returns></returns>
        //[CLSCompliant(false)]
        public virtual uint ReadUInt32() { return (UInt32)ReadInt32(); }

        /// <summary>从当前流中读取 8 字节无符号整数并使流的当前位置提升 8 个字节。</summary>
        /// <returns></returns>
        //[CLSCompliant(false)]
        public virtual ulong ReadUInt64() { return (UInt64)ReadInt64(); }
        #endregion

        #region 浮点数
        /// <summary>从当前流中读取 4 字节浮点值，并使流的当前位置提升 4 个字节。</summary>
        /// <returns></returns>
        public virtual float ReadSingle() { return BitConverter.ToSingle(ReadBytes(4), 0); }

        /// <summary>从当前流中读取 8 字节浮点值，并使流的当前位置提升 8 个字节。</summary>
        /// <returns></returns>
        public virtual double ReadDouble() { return BitConverter.ToDouble(ReadBytes(8), 0); }
        #endregion

        #region 字符串
        /// <summary>从当前流中读取下一个字符，并根据所使用的 Encoding 和从流中读取的特定字符，提升流的当前位置。</summary>
        /// <returns></returns>
        public virtual char ReadChar() { return Convert.ToChar(ReadByte()); }

        /// <summary>从当前流中读取 count 个字符，以字符数组的形式返回数据，并根据所使用的 Encoding 和从流中读取的特定字符，提升当前位置。</summary>
        /// <param name="count">要读取的字符数。</param>
        /// <returns></returns>
        public virtual char[] ReadChars(int count)
        {
            if (count < 0) count = ReadSize();

            // count个字符可能的最大字节数
            Int32 max = Settings.Encoding.GetMaxByteCount(count);

            // 首先按最小值读取
            Byte[] data = ReadBytes(count);

            // 相同，最简单的一种
            if (max == count) return Settings.Encoding.GetChars(data);

            // 按最大值准备一个字节数组
            Byte[] buffer = new Byte[max];
            // 复制过去
            Buffer.BlockCopy(data, 0, buffer, 0, data.Length);

            // 遍历，以下算法性能较差，将来可以考虑优化
            Int32 i = 0;
            for (i = count; i < max; i++)
            {
                Int32 n = Settings.Encoding.GetCharCount(buffer, 0, i);
                if (n >= count) break;

                buffer[i] = ReadByte();
            }

            return Settings.Encoding.GetChars(buffer, 0, i);
        }

        /// <summary>从当前流中读取一个字符串。字符串有长度前缀，一次 7 位地被编码为整数。</summary>
        /// <returns></returns>
        public virtual string ReadString()
        {
            // 先读长度
            Int32 n = ReadSize();
            if (n < 0) return null;
            if (n == 0) return String.Empty;

            Byte[] buffer = ReadBytes(n);

            return Settings.Encoding.GetString(buffer);
        }
        #endregion

        #region 其它
        /// <summary>从当前流中读取 Boolean 值，并使该流的当前位置提升 1 个字节。</summary>
        /// <returns></returns>
        public virtual bool ReadBoolean() { return ReadByte() != 0; }

        /// <summary>从当前流中读取十进制数值，并将该流的当前位置提升十六个字节。</summary>
        /// <returns></returns>
        public virtual decimal ReadDecimal()
        {
            Int32[] data = new Int32[4];
            for (int i = 0; i < data.Length; i++)
            {
                data[i] = ReadInt32();
            }
            return new Decimal(data);
        }

        /// <summary>读取一个时间日期</summary>
        /// <returns></returns>
        public virtual DateTime ReadDateTime() { return Settings.ConvertInt64ToDateTime(ReadInt64()); }
        #endregion
        #endregion

        #region 值类型
        /// <summary>读取值类型数据</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public Object ReadValue(Type type)
        {
            Object value = null;
            return ReadValue(type, ref value) ? value : null;
        }

        /// <summary>尝试读取值类型数据，返回是否读取成功</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <returns></returns>
        public virtual Boolean ReadValue(Type type, ref Object value)
        {
            if (type == null)
            {
                if (value == null) return false;
                type = value.GetType();
            }

            TypeCode code = Type.GetTypeCode(type);
            switch (code)
            {
                case TypeCode.Boolean:
                    value = ReadBoolean();
                    return true;
                case TypeCode.Byte:
                    value = ReadByte();
                    return true;
                case TypeCode.Char:
                    value = ReadChar();
                    return true;
                case TypeCode.DBNull:
                    value = ReadByte();
                    return true;
                case TypeCode.DateTime:
                    value = ReadDateTime();
                    return true;
                case TypeCode.Decimal:
                    value = ReadDecimal();
                    return true;
                case TypeCode.Double:
                    value = ReadDouble();
                    return true;
                case TypeCode.Empty:
                    value = ReadByte();
                    return true;
                case TypeCode.Int16:
                    value = ReadInt16();
                    return true;
                case TypeCode.Int32:
                    value = ReadInt32();
                    return true;
                case TypeCode.Int64:
                    value = ReadInt64();
                    return true;
                case TypeCode.Object:
                    break;
                case TypeCode.SByte:
                    value = ReadSByte();
                    return true;
                case TypeCode.Single:
                    value = ReadSingle();
                    return true;
                case TypeCode.String:
                    value = ReadString();
                    return true;
                case TypeCode.UInt16:
                    value = ReadUInt16();
                    return true;
                case TypeCode.UInt32:
                    value = ReadUInt32();
                    return true;
                case TypeCode.UInt64:
                    value = ReadUInt64();
                    return true;
                default:
                    break;
            }
            return false;
        }
        #endregion

        #region 字典
        /// <summary>尝试读取字典类型对象</summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <returns>是否读取成功</returns>
        public virtual Boolean ReadDictionary(Type type, ref Object value)
        {
            return ReadDictionary(type, ref value, ReadMember);
        }

        /// <summary>尝试读取字典类型对象</summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public virtual Boolean ReadDictionary(Type type, ref Object value, ReadObjectCallback callback)
        {
            //if (type == null)
            //{
            //    if (value == null) throw new ArgumentNullException("type");
            //    type = value.GetType();
            //}
            //type = CheckAndReadType("ReadDictionaryType", type, value);

            if (!typeof(IDictionary).IsAssignableFrom(type)) return false;

            // 计算元素类型
            Type keyType = null;
            Type valueType = null;

            // 无法取得键值类型
            //if (!GetDictionaryEntryType(type, ref keyType, ref valueType)) return false;
            GetDictionaryEntryType(type, ref keyType, ref valueType);

            // 提前创建对象，因为对象引用可能用到
            Int32 index = objRefIndex;
            if (index > 0 && Settings.UseObjRef && value == null)
            {
                value = TypeX.CreateInstance(type);
                if (value != null) AddObjRef(index, value);
            }

            // 读取键值对集合
            IEnumerable<DictionaryEntry> list = ReadDictionary(keyType, valueType, ReadSize(), callback);
            if (list == null)
            {
                value = null;

                // 结果为空，重新把对象引用设为空。不用担心里面里面已经引用了有对象的对象引用，因为既然列表返回空，表明里面没有元素
                if (index > 0 && Settings.UseObjRef) AddObjRef(index, value);

                return true;
            }

            if (value == null) value = TypeX.CreateInstance(type);

            IDictionary dic = value as IDictionary;
            foreach (DictionaryEntry item in list)
            {
                dic.Add(item.Key, item.Value);
            }

            return true;
        }

        /// <summary>读取字典项集合，以读取键值失败作为读完字典项的标识，子类可以重载实现以字典项数量来读取</summary>
        /// <param name="keyType">键类型</param>
        /// <param name="valueType">值类型</param>
        /// <param name="count">元素个数</param>
        /// <param name="callback">处理元素的方法</param>
        /// <returns>字典项集合</returns>
        protected virtual IEnumerable<DictionaryEntry> ReadDictionary(Type keyType, Type valueType, Int32 count, ReadObjectCallback callback)
        {
            List<DictionaryEntry> list = new List<DictionaryEntry>();

            // 元素个数小于0，可能是因为不支持元素个数，直接设为最大值
            if (count < 0) count = Int32.MaxValue;
            for (int i = 0; i < count; i++)
            {
                // 一旦有一个元素读不到，就中断
                DictionaryEntry obj;
                Depth++;
                if (!ReadDictionaryEntry(keyType, valueType, ref obj, i, callback))
                {
                    Depth--;
                    break;
                }
                Depth--;
                list.Add(obj);
            }

            return list;
        }

        /// <summary>读取字典项</summary>
        /// <param name="keyType">键类型</param>
        /// <param name="valueType">值类型</param>
        /// <param name="value">字典项</param>
        /// <param name="index">元素序号</param>
        /// <param name="callback">处理元素的方法</param>
        /// <returns>是否读取成功</returns>
        protected Boolean ReadDictionaryEntry(Type keyType, Type valueType, ref DictionaryEntry value, Int32 index, ReadObjectCallback callback)
        {
            // 读取成员前
            ReadDictionaryEventArgs e = null;
            if (OnDictionaryReading != null)
            {
                e = new ReadDictionaryEventArgs(value, keyType, valueType, index, callback);

                OnDictionaryReading(this, e);

                // 事件里面有可能改变了参数
                value = e.Value;
                keyType = e.KeyType;
                valueType = e.ValueType;
                index = e.Index;
                callback = e.Callback;

                // 事件处理器可能已经成功读取对象
                if (e.Success) return true;
            }

            Boolean rs = OnReadDictionaryEntry(keyType, valueType, ref value, index, callback);

            // 读取成员后
            if (OnDictionaryReaded != null)
            {
                if (e == null) e = new ReadDictionaryEventArgs(value, keyType, valueType, index, callback);
                e.Value = value;
                e.Success = rs;

                OnDictionaryReaded(this, e);

                // 事件处理器可以影响结果
                value = e.Value;
                rs = e.Success;
            }

            return rs;
        }

        /// <summary>读取字典项</summary>
        /// <param name="keyType">键类型</param>
        /// <param name="valueType">值类型</param>
        /// <param name="value">字典项</param>
        /// <param name="index">元素序号</param>
        /// <param name="callback">处理元素的方法</param>
        /// <returns>是否读取成功</returns>
        protected virtual Boolean OnReadDictionaryEntry(Type keyType, Type valueType, ref DictionaryEntry value, Int32 index, ReadObjectCallback callback)
        {
            Object key = null;
            Object val = null;

            // 如果无法取得字典项类型，则每个键值都单独写入类型
            //if (keyType == null)
            //{
            //    WriteLog("ReadKeyType");
            //    keyType = ReadType();
            //    WriteLog("ReadKeyType", keyType.Name);
            //}
            // keyType = CheckAndReadType("ReadKeyType", keyType, value.Key);

            if (!ReadObject(keyType, ref key)) return false;

            //if (valueType == null)
            //{
            //    WriteLog("ReadValueType");
            //    valueType = ReadType();
            //    WriteLog("ReadValueType", valueType.Name);
            //}
            //  valueType = CheckAndReadType("ReadValueType", valueType, value.Value);

            if (!ReadObject(valueType, ref val)) return false;

            value.Key = key;
            value.Value = val;

            return true;
        }

        /// <summary>取得字典的键值类型，默认只支持获取两个泛型参数的字典的键值类型</summary>
        /// <param name="type">字典类型</param>
        /// <param name="keyType">键类型</param>
        /// <param name="valueType">值类型</param>
        /// <returns>是否获取成功，如果失败，则字典读取失败</returns>
        protected virtual Boolean GetDictionaryEntryType(Type type, ref Type keyType, ref Type valueType)
        {
            // 两个泛型参数的泛型
            if (type.IsGenericType)
            {
                Type[] ts = type.GetGenericArguments();
                if (ts != null && ts.Length == 2)
                {
                    keyType = ts[0];
                    valueType = ts[1];

                    return true;
                }
            }

            return false;
        }
        #endregion

        #region 枚举
        /// <summary>尝试读取枚举</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <returns>是否读取成功</returns>
        public virtual Boolean ReadEnumerable(Type type, ref Object value)
        {
            return ReadEnumerable(type, ref value, ReadMember);
        }

        /// <summary>尝试读取枚举</summary>
        /// <remarks>重点和难点在于如果得知枚举元素类型，这里假设所有元素类型一致，否则实在无法处理</remarks>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public virtual Boolean ReadEnumerable(Type type, ref Object value, ReadObjectCallback callback)
        {
            //if (type == null)
            //{
            //    if (value == null) return false;
            //    type = value.GetType();
            //}
            String lengths = null;
            if (type.IsArray && type.GetArrayRank() > 1) lengths = ReadLengths();//lengths放在前面读取，主要是xml序列化时，lengths是写在父节点内的属性

            type = CheckAndReadType("ReadEnumerableType", type, value);

            if (!typeof(IEnumerable).IsAssignableFrom(type)) return false;

            // 计算元素类型，如果无法计算，这里不能处理，否则能写不能读（因为不知道元素类型）
            Type elementType = TypeX.GetElementType(type);

            // 找不到元素类型
            if (elementType == null) throw new SerializationException("无法找到" + type.FullName + "的元素类型！");

            if (!ReadEnumerable(type, elementType, ref value, callback)) return false;

            if (type.IsArray && type.GetArrayRank() > 1)
            {
                if (String.IsNullOrEmpty(lengths)) return false;
                String[] strs = lengths.Split(',');
                Int32[] param = new Int32[strs.Length];
                for (int i = 0; i < strs.Length; i++)
                {
                    param[i] = Convert.ToInt32(strs[i]);
                }

                Array array = Array.CreateInstance(type.GetElementType(), param);

                Array sub = value as Array;
                foreach (Object item in sub)
                {
                    ArrEnum(array, ix => array.SetValue(item, ix), item);
                }
                if (array.Length > 0) value = array;
            }

            return true;
        }

        /// <summary>尝试读取枚举</summary>
        /// <remarks>重点和难点在于如果得知枚举元素类型，这里假设所有元素类型一致，否则实在无法处理</remarks>
        /// <param name="type">类型</param>
        /// <param name="elementType">元素类型数组</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public virtual Boolean ReadEnumerable(Type type, Type elementType, ref Object value, ReadObjectCallback callback)
        {
            if (!typeof(IEnumerable).IsAssignableFrom(type)) return false;

            Int32 count = ReadSize();

            // 提前创建对象，因为对象引用可能用到
            Int32 index = objRefIndex;
            if (index > 0 && Settings.UseObjRef && value == null)
            {
                if (type.IsArray && type.HasElementType && type.GetElementType() == elementType)
                {
                    //TODO 如果是数组，在不知道元素个数时，不处理
                    if (count > 0)
                    {
                        Array arr = TypeX.CreateInstance(type, count) as Array;
                        value = arr;
                        if (value != null) AddObjRef(index, value);
                    }
                }
                else
                {
                    value = TypeX.CreateInstance(type);
                    if (value != null) AddObjRef(index, value);
                }
            }

            IList list = ReadItems(type, elementType, count, callback);
            if (list == null)
            {
                value = null;

                // 结果为空，重新把对象引用设为空。不用担心里面里面已经引用了有对象的对象引用，因为既然列表返回空，表明里面没有元素
                if (index > 0 && Settings.UseObjRef) AddObjRef(index, value);

                return true;
            }

            if (ProcessItems(type, elementType, ref value, list)) return true;

            // 无法处理
            //WriteLog("ReadEnumerable", String.Format("已完成{1}元素列表的读取，但无法写入到{0}的枚举对象中", type, elementType));
            XTrace.WriteLine("已完成{1}元素列表的读取，但无法写入到{0}的枚举对象中", type, elementType);

            return false;
        }

        /// <summary>读取元素集合</summary>
        /// <param name="type"></param>
        /// <param name="elementType"></param>
        /// <param name="count">元素个数</param>
        /// <param name="callback">处理元素的方法</param>
        /// <returns></returns>
        protected virtual IList ReadItems(Type type, Type elementType, Int32 count, ReadObjectCallback callback)
        {
            //ArrayList list = new ArrayList();
            Type listType = typeof(List<>).MakeGenericType(elementType);
            IList list = TypeX.CreateInstance(listType) as IList;

            // 元素个数小于0，可能是因为不支持元素个数，直接设为最大值
            if (count < 0) count = Int32.MaxValue;
            for (int i = 0; i < count; i++)
            {
                // 一旦有一个元素读不到，就中断
                Object obj = null;
                Depth++;
                if (!ReadItem(elementType, ref obj, i, callback))
                {
                    Depth--;
                    break;
                }
                Depth--;
                list.Add(obj);
            }

            return list;
        }

        /// <summary>读取项</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="index">元素序号</param>
        /// <param name="callback">处理元素的方法</param>
        /// <returns></returns>
        protected Boolean ReadItem(Type type, ref Object value, Int32 index, ReadObjectCallback callback)
        {
            // 读取成员前
            ReadItemEventArgs e = null;
            if (OnItemReading != null)
            {
                e = new ReadItemEventArgs(value, type, index, callback);

                OnItemReading(this, e);

                // 事件里面有可能改变了参数
                value = e.Value;
                type = e.Type;
                index = e.Index;
                callback = e.Callback;

                // 事件处理器可能已经成功读取对象
                if (e.Success) return true;
            }

            Boolean rs = OnReadItem(type, ref value, index, callback);

            // 读取成员后
            if (OnItemReaded != null)
            {
                if (e == null) e = new ReadItemEventArgs(value, type, index, callback);
                e.Value = value;
                e.Success = rs;

                OnItemReaded(this, e);

                // 事件处理器可以影响结果
                value = e.Value;
                rs = e.Success;
            }

            return rs;
        }

        /// <summary>读取项</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <param name="index">元素序号</param>
        /// <param name="callback">处理元素的方法</param>
        /// <returns></returns>
        protected virtual Boolean OnReadItem(Type type, ref Object value, Int32 index, ReadObjectCallback callback)
        {
            // 如果无法取得元素类型，则每个元素都单独写入类型
            //if (type == null || type == typeof(object))
            //{
            //    WriteLog("ReadItemType");
            //    type = ReadType();
            //    WriteLog("ReadItemType", type.Name);
            //}
            // type = CheckAndReadType("ReadItemType", type, value);

            return ReadObject(type, ref value, callback);
        }

        /// <summary>处理结果集</summary>
        /// <param name="type"></param>
        /// <param name="elementType"></param>
        /// <param name="value"></param>
        /// <param name="items"></param>
        /// <returns></returns>
        protected Boolean ProcessItems(Type type, Type elementType, ref Object value, IList items)
        {
            if (type == items.GetType())
            {
                value = items;
                return true;
            }

            // 添加方法
            MethodInfoX method = null;

            #region 如果源对象不为空，则尽量使用源对象
            if (value != null)
            {
                if (type.IsArray)
                {
                    Array arr = value as Array;
                    if (arr != null)
                    {
                        //if (XTrace.Debug && arr.Length != items.Count) throw new XSerializationException(null, "数组元素个数不匹配！");

                        for (int i = 0; i < arr.Length && i < items.Count; i++)
                        {
                            arr.SetValue(items[i], i);
                        }
                        return true;
                    }
                }

                if (typeof(IList).IsAssignableFrom(type))
                {
                    IList list = value as IList;
                    if (list != null)
                    {
                        foreach (Object item in items)
                        {
                            list.Add(item);
                        }
                        return true;
                    }
                }

                //method = MethodInfoX.Create(type, "Add", new Type[] { elementType });
                //if (method != null)
                //{
                //    foreach (Object item in items)
                //    {
                //        method.Invoke(value, item);
                //    }
                //    return true;
                //}
            }
            #endregion

            #region 数组
            if (type.IsArray && type.HasElementType && type.GetElementType() == elementType)
            {
                Array arr = TypeX.CreateInstance(type, items.Count) as Array;
                items.CopyTo(arr, 0);
                value = arr;
                return true;
            }
            #endregion

            #region 检查类型是否有指定类型的构造函数，如果有，直接创建类型，并把数组作为构造函数传入
            ConstructorInfoX ci = ConstructorInfoX.Create(type, new Type[] { typeof(IEnumerable) });
            if (ci != null)
            {
                value = ci.CreateInstance(items);
                return true;
            }
            #endregion

            #region 检查是否实现IEnumerable<>接口，如果不是，转为该接口，后面的构造函数需要用
            Type enumType = typeof(IEnumerable<>).MakeGenericType(elementType);
            // 如果数据不是IEnumerable<>类型，则需要转换
            if (!enumType.IsAssignableFrom(items.GetType()))
            {
                // 用List<>来转换
                Type listType = typeof(List<>).MakeGenericType(elementType);
                IList list = TypeX.CreateInstance(listType) as IList;
                if (list != null)
                {
                    foreach (Object item in items)
                    {
                        list.Add(item);
                    }

                    if (type == listType)
                    {
                        value = list;
                        return true;
                    }

                    items = list;
                }
            }
            #endregion

            #region 泛型枚举接口IEnumerable<>的构造函数
            ci = ConstructorInfoX.Create(type, new Type[] { enumType });
            if (ci != null)
            {
                value = ci.CreateInstance(items);
                return true;
            }
            #endregion

            #region 是否具有Add方法
            if (method == null) method = MethodInfoX.Create(type, "Add", new Type[] { elementType });
            if (method != null)
            {
                if (value == null) value = TypeX.CreateInstance(type);
                foreach (Object item in items)
                {
                    method.Invoke(value, item);
                }
                return true;
            }

            method = MethodInfoX.Create(type, "AddRange", new Type[] { typeof(ICollection) });
            if (method == null) method = MethodInfoX.Create(type, "AddRange", new Type[] { typeof(IList) });
            if (method != null)
            {
                method.Invoke(value, items);

                return true;
            }
            #endregion

            return false;
        }
        #endregion

        #region 序列化接口
        /// <summary>读取实现了可序列化接口的对象</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public virtual Boolean ReadSerializable(Type type, ref Object value, ReadObjectCallback callback)
        {
            if (!typeof(ISerializable).IsAssignableFrom(type)) return false;

            WriteLog("ReadSerializable", type.Name);

            IObjectMemberInfo[] mis = GetMembers(type, value);
            if (mis == null || mis.Length < 1) return true;

            // 调试输出成员列表
            if (Debug) ShowMembers("ReadSerializable", mis);

            for (int i = 0; i < mis.Length; i++)
            {
                Depth++;

                IObjectMemberInfo member = GetMemberBeforeRead(type, value, mis, i);
                // 没有可读成员
                if (member == null) continue;

                WriteLog("ReadMember", member.Name, member.Type.Name);

                if (!ReadMember(member.Type, ref value, member, i, callback)) return false;
                Depth--;
            }

            // 如果为空，实例化并赋值。
            if (value == null)
            {
                SerializationInfo info = new SerializationInfo(type, new FormatterConverter());
                foreach (IObjectMemberInfo item in mis)
                {
                    info.AddValue(item.Name, item[value], item.Type);
                }

                value = TypeX.CreateInstance(type, info, ObjectInfo.DefaultStreamingContext);

                if (value != null) AddObjRef(objRefIndex, value);
            }

            return true;
        }
        #endregion

        #region 未知对象
        /// <summary>读取未知对象（其它所有方法都无法识别的对象），采用BinaryFormatter或者XmlSerialization</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public virtual Boolean ReadUnKnown(Type type, ref Object value, ReadObjectCallback callback)
        {
            WriteLog("ReadBinaryFormatter", type.Name);

            // 调用.Net的二进制序列化来解决剩下的事情
            BinaryFormatter bf = new BinaryFormatter();
            MemoryStream ms = new MemoryStream(ReadBytes(-1));
            value = bf.Deserialize(ms);

            return true;
        }
        #endregion

        #region 扩展处理类型
        /// <summary>扩展读取，反射查找合适的读取方法</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <returns></returns>
        public Boolean ReadX(Type type, ref Object value)
        {
            if (type == typeof(Byte[]))
            {
                value = ReadBytes(-1);
                return true;
            }
            if (type == typeof(Char[]))
            {
                value = ReadChars(-1);
                return true;
            }

            if (type == typeof(Guid))
            {
                value = OnReadGuid();
                return true;
            }
            if (type == typeof(IPAddress))
            {
                value = OnReadIPAddress();
                return true;
            }
            if (type == typeof(IPEndPoint))
            {
                value = OnReadIPEndPoint();
                return true;
            }
            if (typeof(Type).IsAssignableFrom(type))
            {
                value = OnReadType();
                return true;
            }

            return false;
        }

        /// <summary>读取Guid</summary>
        /// <returns></returns>
        public virtual Guid ReadGuid() { return ReadObjRef<Guid>(OnReadGuid); }

        /// <summary>读取Guid</summary>
        /// <returns></returns>
        protected virtual Guid OnReadGuid() { return new Guid(ReadBytes(16)); }

        /// <summary>读取IPAddress</summary>
        /// <returns></returns>
        public virtual IPAddress ReadIPAddress() { return ReadObjRef<IPAddress>(OnReadIPAddress); }

        /// <summary>读取IPAddress</summary>
        /// <returns></returns>
        protected virtual IPAddress OnReadIPAddress() { return new IPAddress(ReadBytes(-1)); }

        /// <summary>读取IPEndPoint</summary>
        /// <returns></returns>
        public virtual IPEndPoint ReadIPEndPoint() { return ReadObjRef<IPEndPoint>(OnReadIPEndPoint); }

        /// <summary>读取IPEndPoint</summary>
        /// <returns></returns>
        protected virtual IPEndPoint OnReadIPEndPoint()
        {
            IPEndPoint ep = new IPEndPoint(IPAddress.Any, 0);
            //! 直接调用OnWrite，不写对象引用，将来可能得考虑写对象引用
            ep.Address = OnReadIPAddress();
            ep.Port = ReadInt32();
            return ep;
        }

        /// <summary>读取Type</summary>
        /// <returns></returns>
        public Type ReadType()
        {
            Depth++;
            // 分离出去，便于重载，而又能有效利用对象引用
            Type type = ReadObjRef<Type>(OnReadType);

            if (type != null) WriteLog("ReadType", type.FullName);

            Depth--;
            return type;
        }

        /// <summary>读取Type</summary>
        /// <returns></returns>
        protected virtual Type OnReadType()
        {
            String typeName = ReadString();
            if (String.IsNullOrEmpty(typeName)) return null;

            Type type = TypeX.GetType(typeName, true);
            if (type != null) return type;

            throw new XException("无法找到名为{0}的类型！", typeName);
        }

        /// <summary>检查对象类型与指定写入类型是否一致，若不一致，则先写入类型，以保证读取的时候能够以正确的类型读取。同时返回对象实际类型。</summary>
        /// <param name="action"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        protected Type CheckAndReadType(String action, Type type, Object value)
        {
            if (!IsExactType(type))
            {
                WriteLog(action);
                Type t = ReadObjectType();
                //TODO 可以在Xml和Json测试猜测类型，写入后，删除Type部分，再尝试读取
                if (t == null && type != null) t = GuessType(type);
                type = t;
                WriteLog(action, type.Name);
            }

            return type;
        }

        /// <summary>猜测类型。对于无法读取到对象类型的类型，并且是接口之类的，可以猜测</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        static Type GuessType(Type type)
        {
            if (type.IsInterface || type.IsAbstract)
            {
                // IEnumerable
                if (typeof(IEnumerable).IsAssignableFrom(type))
                {
                    if (!type.IsGenericType)
                    {
                        // IDictionary
                        if (type == typeof(IDictionary)) return typeof(Hashtable);

                        // IList
                        if (type == typeof(IList)) return typeof(ArrayList);

                        // IEnumerable
                        if (type == typeof(ICollection) || type == typeof(IEnumerable)) return typeof(ArrayList);
                    }
                    else
                    {
                        // 处理泛型
                        Type gt = type.GetGenericTypeDefinition();
                        if (gt != null)
                        {
                            // 处理泛型参数
                            Type[] gs = type.GetGenericArguments();

                            // IDictionary<,>
                            if (type == typeof(IDictionary<,>)) return typeof(Dictionary<,>).MakeGenericType(gs);

                            // IList<>
                            if (type == typeof(IList<>)) return typeof(List<>).MakeGenericType(gs);

                            // IEnumerable<>
                            if (type == typeof(ICollection<>) || type == typeof(IEnumerable<>)) typeof(List<>).MakeGenericType(gs);
                        }
                    }
                }
                return FindFirstExactType(type);
            }

            return type;
        }

        static Type FindFirstExactType(Type type)
        {
            if (IsExactType(type)) return type;

            // 找到所有实现了该接口的类型，并返回第一个精确类型
            //Type[] ts = TypeResolver.ResolveAll(type);
            //if (ts != null && ts.Length > 0)
            //{
            //    foreach (Type item in ts)
            //    {
            //        if (IsExactType(item)) return item;
            //    }
            //}
            //return null;
            return AssemblyX.FindAllPlugins(type).FirstOrDefault(t => IsExactType(t));
        }

        /// <summary>读对象类型</summary>
        /// <returns></returns>
        protected virtual Type ReadObjectType() { return ReadType(); }
        #endregion

        #region 复杂对象
        /// <summary>主要入口方法。从数据流中读取指定类型的对象</summary>
        /// <param name="type">类型</param>
        /// <returns>对象</returns>
        public Object ReadObject(Type type)
        {
            Object value = null;
            try
            {
                //return ReadObject(type, ref value, null) ? value : null;
                // 尽管读取对象出错，但是可能已经读取部分，还是需要准确返回
                ReadObject(type, ref value, null);
                return value;
            }
            catch (XSerializationException ex)
            {
                // 如果本身就是序列化异常，砍断内部的异常链，太长没有意义
                var se = new XSerializationException(ex.Member, "读取对象出错，可能已读取部分，请查看Value属性！" + ex.Message);
                se.Value = value;
                throw se;
            }
            catch (Exception ex)
            {
                // 如果不是序列化异常，则包括内部异常链
                var se = new XSerializationException(null, "读取对象出错，可能已读取部分，请查看Value属性！", ex);
                se.Value = value;
                throw se;
            }
        }

        /// <summary>主要入口方法。从数据流中读取指定类型的对象</summary>
        /// <returns>对象</returns>
        public T ReadObject<T>() { return (T)ReadObject(typeof(T)); }

        /// <summary>主要入口方法。尝试读取目标对象指定成员的值，通过委托方法递归处理成员</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <returns>是否读取成功</returns>
        public Boolean ReadObject(Type type, ref Object value) { return ReadObject(type, ref value, ReadMember); }

        /// <summary>尝试读取目标对象指定成员的值，处理基础类型、特殊类型、基础类型数组、特殊类型数组，通过委托方法处理成员</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public Boolean ReadObject(Type type, ref Object value, ReadObjectCallback callback)
        {
            // 检查数据流是否结束
            if (EndOfStream) return false;

            if (type == null && value != null) type = value.GetType();
            if (callback == null) callback = ReadMember;

            Object old = CurrentObject;
            try
            {
                // 检查IAcessor接口
                IAccessor accessor = null;
                if (typeof(IAccessor).IsAssignableFrom(type))
                {
                    // 如果为空，实例化并赋值。
                    if (value == null)
                    {
                        CurrentObject = value = TypeX.CreateInstance(type);

                        if (value != null) AddObjRef(objRefIndex, value);
                    }
                    accessor = value as IAccessor;
                    if (accessor != null && accessor.Read(this)) return true;
                }

                Boolean rs = ReadObjectWithEvent(type, ref value, callback);

                // 检查IAcessor接口
                if (accessor != null) rs = accessor.ReadComplete(this, rs);

                return rs;
            }
            finally { CurrentObject = old; }
        }

        Boolean ReadObjectWithEvent(Type type, ref Object value, ReadObjectCallback callback)
        {
            // 事件
            ReadObjectEventArgs e = null;
            if (OnObjectReading != null)
            {
                e = new ReadObjectEventArgs(value, type, callback);

                OnObjectReading(this, e);

                // 事件里面有可能改变了参数
                value = e.Value;
                type = e.Type;
                callback = e.Callback;

                // 事件处理器可能已经成功读取对象
                if (e.Success) return true;
            }

            Boolean rs = OnReadObject(type, ref value, callback);

            // 事件
            if (OnObjectReaded != null)
            {
                if (e == null) e = new ReadObjectEventArgs(value, type, callback);
                e.Value = value;
                e.Success = rs;

                OnObjectReaded(this, e);

                // 事件处理器可以影响结果
                value = e.Value;
                rs = e.Success;
            }

            return rs;
        }

        /// <summary>尝试读取目标对象指定成员的值，处理基础类型、特殊类型、基础类型数组、特殊类型数组，通过委托方法处理成员</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        protected virtual Boolean OnReadObject(Type type, ref Object value, ReadObjectCallback callback)
        {
            if (callback == null) callback = ReadMember;

            //! 2011-05-27 17:33
            //! 精确类型，直接写入值
            //! 未知类型，写对象引用，写类型，写对象

            if (IsExactType(type))
            {
                // 基本类型
                if (ReadValue(type, ref value)) return true;

                // 读取对象引用
                Int32 index = 0;
                if (ReadObjRef(type, ref value, out index)) return true;

                objRefIndex = index;

                // 特殊类型
                if (ReadX(type, ref value)) return true;

                // 读取引用对象
                if (!ReadRefObject(type, ref value, callback)) return false;

                if (value != null) AddObjRef(index, value);
            }
            else
            {
                // 读取对象引用
                Int32 index = 0;
                if (ReadObjRef(type, ref value, out index)) return true;

                // 写对象类型时增加缩进，避免写顶级对象类型的对象引用时无法写入（Depth=1的对象是不写对象引用的）
                Depth++;
                type = CheckAndReadType("ReadObjectType", type, value);
                Depth--;

                // 基本类型
                if (ReadValue(type, ref value)) return true;

                // 特殊类型
                if (ReadX(type, ref value)) return true;

                // 读取引用对象
                objRefIndex = index;
                if (!ReadRefObject(type, ref value, callback)) return false;

                if (value != null) AddObjRef(index, value);
            }

            return true;
        }

        /// <summary>尝试读取引用对象</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        protected virtual Boolean ReadRefObject(Type type, ref Object value, ReadObjectCallback callback)
        {
            // 字典
            if (typeof(IDictionary).IsAssignableFrom(type))
            {
                WriteLog("ReadDictionary", type.Name);

                if (ReadDictionary(type, ref value, callback)) return true;
            }

            // 枚举
            if (typeof(IEnumerable).IsAssignableFrom(type))
            {
                WriteLog("ReadEnumerable", type.Name);

                if (ReadEnumerable(type, ref value, callback)) return true;
            }

            // 可序列化接口
            if (ReadSerializable(type, ref value, callback)) return true;

            // 复杂类型，处理对象成员
            if (ReadCustomObject(type, ref value, callback)) return true;

            // 检查数据流是否结束
            if (EndOfStream) return false;

            return ReadUnKnown(type, ref value, callback);
        }

        #region 对象引用
        /// <summary>读取引用对象的包装，能自动从引用对象集合里面读取，如果不存在，则调用委托读取对象，并加入引用对象集合</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="func"></param>
        /// <returns></returns>
        T ReadObjRef<T>(Func<T> func)
        {
            Object obj = null;
            Int32 index = 0;
            if (ReadObjRef(typeof(T), ref obj, out index)) return (T)obj;

            if (func != null)
            {
                obj = func();

                if (obj != null) AddObjRef(index, obj);

                return (T)obj;
            }

            return default(T);
        }

        List<Object> objRefs = new List<Object>();
        Int32 objRefIndex = 0;

        /// <summary>读取对象引用。</summary>
        /// <param name="type">类型</param>
        /// <param name="value">对象</param>
        /// <param name="index">引用计数</param>
        /// <returns>是否读取成功</returns>
        public Boolean ReadObjRef(Type type, ref Object value, out Int32 index)
        {
            index = 0;
            if (!Settings.UseObjRef) return false;

            // 顶级特殊处理
            if (Depth <= 1)
                index = 1;
            else
                index = OnReadObjRefIndex();

            if (index < 0) return false;

            if (index == 0)
            {
                WriteLog("ReadObjRef", "null", type.Name);

                value = null;
                return true;
            }

            //// 如果引用计数刚好是下一个引用对象，说明这是该对象的第一次引用，返回false
            //if (index == objRefs.Count + 1) return false;

            //if (index > objRefs.Count) throw new XException("对象引用错误，无法找到引用计数为" + index + "的对象！");

            // 引用计数等于索引加一
            if (index > objRefs.Count)
            {
                WriteLog("ReadObjRef", index, type == null ? null : type.Name);

                return false;
            }

            value = objRefs[index - 1];

            if (value != null)
                WriteLog("ReadObjRef", index, value.ToString(), value.GetType().Name);
            else
                WriteLog("ReadObjRef", index, "", type == null ? "" : type.Name);

            return true;
        }

        /// <summary>读取对象引用计数</summary>
        /// <returns></returns>
        protected virtual Int32 OnReadObjRefIndex() { return ReadInt32(); }

        /// <summary>添加对象引用</summary>
        /// <param name="index">引用计数</param>
        /// <param name="value">对象</param>
        protected virtual void AddObjRef(Int32 index, Object value)
        {
            if (!Settings.UseObjRef || index < 1) return;
            //if (value == null) return;

            while (index > objRefs.Count) objRefs.Add(null);

            objRefs[index - 1] = value;
        }
        #endregion
        #endregion

        #region 自定义对象
        /// <summary>尝试读取自定义对象</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public virtual Boolean ReadCustomObject(Type type, ref Object value, ReadObjectCallback callback)
        {
            Object old = CurrentObject;
            CurrentObject = value;

            try
            {
                IObjectMemberInfo[] mis = GetMembers(type, value);
                if (mis == null || mis.Length < 1) return true;
                if (callback == null) callback = ReadMember;

                // 如果为空，实例化并赋值。
                if (value == null)
                {
                    CurrentObject = value = TypeX.CreateInstance(type);

                    if (value != null) AddObjRef(objRefIndex, value);
                }

                // 调试输出成员列表
                if (Debug) ShowMembers("ReadCustomObject", mis);

                for (int i = 0; i < mis.Length; i++)
                {
                    Depth++;

                    IObjectMemberInfo member = GetMemberBeforeRead(type, value, mis, i);
                    // 没有可读成员
                    if (member == null) continue;

                    // 基础类型输出日志时，同时输出值，更直观
                    if (Type.GetTypeCode(mis[i].Type) == TypeCode.Object)
                        WriteLog("ReadMember", member.Name, member.Type.Name);

                    if (!ReadMember(member.Type, ref value, member, i, callback)) return false;

                    // 基础类型输出日志时，同时输出值，更直观
                    if (Type.GetTypeCode(mis[i].Type) != TypeCode.Object)
                        WriteLog("ReadMember", member.Name, member.Type.Name, mis[i][value]);

                    Depth--;
                }

                return true;
            }
            finally { CurrentObject = old; }
        }

        /// <summary>读取成员之前获取要读取的成员，默认是index处的成员，实现者可以重载，改变当前要读取的成员，如果当前成员不在数组里面，则实现者自己跳到下一个可读成员。</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="members">可匹配成员数组</param>
        /// <param name="index">索引</param>
        /// <returns></returns>
        protected virtual IObjectMemberInfo GetMemberBeforeRead(Type type, Object value, IObjectMemberInfo[] members, Int32 index) { return members[index]; }

        /// <summary>根据名称，从成员数组中查找成员</summary>
        /// <param name="members">可匹配成员数组</param>
        /// <param name="name">名称</param>
        /// <returns></returns>
        protected IObjectMemberInfo GetMemberByName(IObjectMemberInfo[] members, String name)
        {
            foreach (IObjectMemberInfo item in members)
            {
                if (item.Name == name) return item;
            }

            return null;
        }

        /// <summary>读取对象成员</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="member">成员</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        public Boolean ReadMember(Type type, ref Object value, IObjectMemberInfo member, Int32 index, ReadObjectCallback callback)
        {
            if (callback == null) callback = ReadMember;

            IObjectMemberInfo old = CurrentMember;
            CurrentMember = member;

#if !DEBUG
            try
#endif
            {
                // 读取成员前
                ReadMemberEventArgs e = null;
                if (OnMemberReading != null)
                {
                    e = new ReadMemberEventArgs(value, type, member, index, callback);

                    OnMemberReading(this, e);

                    // 事件里面有可能改变了参数
                    value = e.Value;
                    type = e.Type;
                    member = e.Member;
                    index = e.Index;
                    callback = e.Callback;

                    // 事件处理器可能已经成功读取对象
                    if (e.Success)
                    {
                        CurrentMember = old;
                        return true;
                    }
                }

                Object obj = null;
                Boolean rs = OnReadMember(type, ref obj, member, index, callback);

                // 读取成员后
                if (OnMemberReaded != null)
                {
                    if (e == null) e = new ReadMemberEventArgs(value, type, member, index, callback);
                    e.Value = obj;
                    e.Success = rs;

                    OnMemberReaded(this, e);

                    // 事件处理器可以影响结果
                    obj = e.Value;
                    rs = e.Success;
                }

                // 设置成员的值
                member[value] = obj;

                CurrentMember = old;

                return rs;
            }
#if !DEBUG
            catch (XException) { throw; }
            catch (Exception ex)
            {
                throw new XSerializationException(member, ex);
            }
#endif
        }

        /// <summary>读取对象成员</summary>
        /// <param name="type">要读取的对象类型</param>
        /// <param name="value">要读取的对象</param>
        /// <param name="member">成员</param>
        /// <param name="index">成员索引</param>
        /// <param name="callback">处理成员的方法</param>
        /// <returns>是否读取成功</returns>
        protected virtual Boolean OnReadMember(Type type, ref Object value, IObjectMemberInfo member, Int32 index, ReadObjectCallback callback)
        {
            //type = CheckAndReadType("ReadMemberType", type, value);

            //if (type == typeof(Object))
            //{
            //    WriteLog("ReadMemberType");
            //    type = ReadType();
            //    WriteLog("ReadMemberType", type.Name);
            //}
            return callback(this, type, ref value, callback);
        }

        static Boolean ReadMember(IReader reader, Type type, ref Object value, ReadObjectCallback callback) { return reader.ReadObject(type, ref value, callback); }
        #endregion

        #region 事件
        /// <summary>读对象前触发。</summary>
        public event EventHandler<ReadObjectEventArgs> OnObjectReading;

        /// <summary>读对象后触发。</summary>
        public event EventHandler<ReadObjectEventArgs> OnObjectReaded;

        /// <summary>读成员前触发。</summary>
        public event EventHandler<ReadMemberEventArgs> OnMemberReading;

        /// <summary>读成员后触发。</summary>
        public event EventHandler<ReadMemberEventArgs> OnMemberReaded;

        /// <summary>读字典项前触发。</summary>
        public event EventHandler<ReadDictionaryEventArgs> OnDictionaryReading;

        /// <summary>读字典项后触发。</summary>
        public event EventHandler<ReadDictionaryEventArgs> OnDictionaryReaded;

        /// <summary>读枚举项前触发。</summary>
        public event EventHandler<ReadItemEventArgs> OnItemReading;

        /// <summary>读枚举项后触发。</summary>
        public event EventHandler<ReadItemEventArgs> OnItemReaded;
        #endregion

        #region 方法
        /// <summary>读取大小</summary>
        /// <returns></returns>
        public Int32 ReadSize()
        {
            if (!UseSize) return -1;

            Int32 size = OnReadSize();

            WriteLog("ReadSize", size);

            return size;
        }

        /// <summary>读取大小</summary>
        /// <returns></returns>
        protected virtual Int32 OnReadSize()
        {
            switch (Settings.SizeFormat)
            {
                case TypeCode.Int16:
                case TypeCode.UInt16:
                    return ReadInt16();
                case TypeCode.Int32:
                case TypeCode.UInt32:
                case TypeCode.Int64:
                case TypeCode.UInt64:
                default:
                    return ReadInt32();
            }
        }

        /// <summary>读取多维数组相关参数</summary>
        /// <returns></returns>
        protected virtual String ReadLengths()
        {
            String lengths = ReadString();
            WriteLog("ReadLengths", lengths);
            return lengths;
        }
        #endregion

        #region 辅助方法
        /// <summary>给多维数组赋值</summary>
        /// <param name="arr"></param>
        /// <param name="func"></param>
        /// <param name="value"></param>
        protected void ArrEnum(Array arr, Action<Int32[]> func, Object value)
        {
            Int32[] ix = new Int32[arr.Rank];
            Int32 rank = 0;

            for (int i = 0; i < arr.Length; i++)
            {
                // 当前层以下都清零
                for (int j = rank + 1; j < arr.Rank; j++)
                {
                    ix[j] = 0;
                }

                // 设置为最底层
                rank = arr.Rank - 1;

                //do something
                //arr.SetValue(i, ix);
                Object val = arr.GetValue(ix);
                if (val == null || (val.Equals(0) && val != value))
                {
                    func(ix);
                    return;
                }

                // 当前层递加
                ix[rank]++;

                // 如果超过上限，则减少层次
                while (ix[rank] >= arr.GetLength(rank))
                {
                    rank--;
                    if (rank < 0) break;
                    ix[rank]++;
                }
            }
        }
        #endregion

        #region 辅助属性
        /// <summary>获取一个值，该值表示当前的流位置是否在流的末尾。</summary>
        /// <returns>如果当前的流位置在流的末尾，则为 true；否则为 false。</returns>
        public virtual Boolean EndOfStream
        {
            get
            {
                var s = Stream;
                if (s == null) return false;
                return s.Position == s.Length;
            }
        }
        #endregion
    }
}