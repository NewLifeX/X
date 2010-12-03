using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using XLog;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 协议序列化
    /// </summary>
    /// <remarks>
    /// 协议序列化分为两大类：
    /// 1，纯数据不包含头部的协议类型序列化；
    /// 2，包含头部信息的增强型序列化。
    /// 而不管是哪一类序列化，都将以最小序列化结果作为目标。
    /// </remarks>
    public class ProtocolFormatter : IFormatter
    {
        #region 属性
        private ProtocolFormatterHead _Head;
        /// <summary>头部</summary>
        public ProtocolFormatterHead Head
        {
            get
            {
                if (_Head == null) _Head = new ProtocolFormatterHead();
                return _Head;
            }
            set { _Head = value; }
        }
        #endregion

        #region 扩展属性
        private Dictionary<Object, Int32> _RefObjects;
        /// <summary>引用对象，用于检测循环引用</summary>
        public Dictionary<Object, Int32> RefObjects
        {
            get { return _RefObjects; }
            set { _RefObjects = value; }
        }
        #endregion

        #region 序列化属性
        private SerializationBinder _Binder;
        /// <summary>绑定器</summary>
        public SerializationBinder Binder
        {
            get { return _Binder; }
            set { _Binder = value; }
        }

        private StreamingContext _Context;
        /// <summary>上下文</summary>
        public StreamingContext Context
        {
            get { return _Context; }
            set { _Context = value; }
        }

        private ISurrogateSelector _SurrogateSelector;
        /// <summary></summary>
        public ISurrogateSelector SurrogateSelector
        {
            get { return _SurrogateSelector; }
            set { _SurrogateSelector = value; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 构造
        /// </summary>
        public ProtocolFormatter() { }

        /// <summary>
        /// 构造
        /// </summary>
        /// <param name="type"></param>
        public ProtocolFormatter(Type type)
        {
            Head.AssemblyName = type.Assembly.FullName;
            Head.TypeName = type.FullName;
        }
        #endregion

        #region 写入数据
        /// <summary>
        /// 写入对象
        /// </summary>
        /// <param name="context"></param>
        public void WriteMember(WriteContext context)
        {
            WriteLog(context.Node.ToString());

            if (OnSerializing(context))
            {
                WriteMemberInternal(context);
                OnSerialized(context);
            }
        }

        private void WriteMemberInternal(WriteContext context)
        {
            // 空数据时，写个0字节占位，如果配置为非空，则抛出异常
            if (context.Data == null)
            {
                if (context.Config.NotNull) throw new InvalidOperationException("在非空设置下遇到空节点！" + context.Node.Path);
                context.Writer.Write((Byte)0);
                return;
            }

            #region 基础类型
            Type memberType = context.Type;
            TypeCode code = Type.GetTypeCode(memberType);

            switch (code)
            {
                case TypeCode.Boolean:
                    context.Writer.Write(Convert.ToBoolean(context.Data, CultureInfo.InvariantCulture));
                    return;
                case TypeCode.Byte:
                    context.Writer.Write(Convert.ToByte(context.Data, CultureInfo.InvariantCulture));
                    return;
                case TypeCode.Char:
                    context.Writer.Write(Convert.ToChar(context.Data, CultureInfo.InvariantCulture));
                    return;
                case TypeCode.DBNull:
                    return;
                case TypeCode.DateTime:
                    context.Writer.Write(Convert.ToDateTime(context.Data, CultureInfo.InvariantCulture).Ticks);
                    return;
                case TypeCode.Decimal:
                    context.Writer.Write(Convert.ToDecimal(context.Data, CultureInfo.InvariantCulture));
                    return;
                case TypeCode.Double:
                    context.Writer.Write(Convert.ToDouble(context.Data, CultureInfo.InvariantCulture));
                    return;
                case TypeCode.Empty:
                    context.Writer.Write((Byte)0);
                    return;
                case TypeCode.Int16:
                    context.Writer.Write(Convert.ToInt16(context.Data, CultureInfo.InvariantCulture));
                    return;
                case TypeCode.Int32:
                    if (!context.Config.EncodeInt)
                        context.Writer.Write(Convert.ToInt32(context.Data, CultureInfo.InvariantCulture));
                    else
                        context.Writer.WriteEncodeInt32(Convert.ToInt32(context.Data, CultureInfo.InvariantCulture));
                    return;
                case TypeCode.Int64:
                    if (!context.Config.EncodeInt)
                        context.Writer.Write(Convert.ToInt64(context.Data, CultureInfo.InvariantCulture));
                    else
                        context.Writer.WriteEncodeInt64(Convert.ToInt64(context.Data, CultureInfo.InvariantCulture));
                    return;
                case TypeCode.Object:
                    break;
                case TypeCode.SByte:
                    context.Writer.Write(Convert.ToSByte(context.Data, CultureInfo.InvariantCulture));
                    return;
                case TypeCode.Single:
                    context.Writer.Write(Convert.ToSingle(context.Data, CultureInfo.InvariantCulture));
                    return;
                case TypeCode.String:
                    context.Writer.Write(Convert.ToString(context.Data, CultureInfo.InvariantCulture));
                    return;
                case TypeCode.UInt16:
                    context.Writer.Write(Convert.ToUInt16(context.Data, CultureInfo.InvariantCulture));
                    return;
                case TypeCode.UInt32:
                    context.Writer.Write(Convert.ToUInt32(context.Data, CultureInfo.InvariantCulture));
                    return;
                case TypeCode.UInt64:
                    context.Writer.Write(Convert.ToUInt64(context.Data, CultureInfo.InvariantCulture));
                    return;
                default:
                    break;
            }
            #endregion

            #region 数组、枚举、值类型、对象
            //// 对于非基本类型，先写入对象引用计数
            //// 计数0表示是空对象
            //if (context.Node.Depth > 1 && !context.Config.NotNull)
            //{
            //    if (RefObjects == null) RefObjects = new Dictionary<Object, Int32>();
            //    if (RefObjects.ContainsKey(context.Data))
            //    {
            //        // 该对象已经写入，这里写入引用计数即可
            //        context.Writer.WriteEncodeInt32(RefObjects[context.Data]);
            //        return;
            //    }

            //    RefObjects.Add(context.Data, RefObjects.Count + 1);
            //    // 对象引用计数
            //    context.Writer.WriteEncodeInt32(RefObjects[context.Data]);
            //}
            // 以上功能暂时不使用，以后再说

            if (memberType.IsArray)
            {
                WriteArray(context);
            }
            else if (context.Data is IEnumerable)
            {
                WriteEnumerable(context);
            }
            else if (memberType.IsValueType)
            {
                if (context.Type == typeof(Guid))
                {
                    context.Writer.Write(((Guid)context.Data).ToByteArray());
                    return;
                }

                WriteObjectRef(context);
            }
            else
            {
                if (context.Type == typeof(IPAddress))
                {
                    IPAddress ip = context.Data as IPAddress;
                    Int32 p = ip.GetHashCode();
                    if (!context.Config.EncodeInt)
                        context.Writer.Write(p);
                    else
                        context.Writer.WriteEncodeInt32(p);
                    return;
                }

                WriteObjectRef(context);
            }
            #endregion
        }

        void WriteObjectRef(WriteContext context)
        {
            // 检测循环引用
            if (RefObjects == null) RefObjects = new Dictionary<Object, Int32>();

            if (RefObjects.ContainsKey(context.Data))
                throw new InvalidOperationException("检测到循环引用！" + context.Node);

            RefObjects.Add(context.Data, RefObjects.Count + 1);

            // 写入一个字节表示当前对象不为空
            // 实际上，还可以建立一个只能容纳255个类型的数组，这个字节用于表示类型
            if (context.Node.Depth > 1 && !context.Config.NotNull) context.Writer.Write((Byte)1);

            Type type = context.Type;
            // 对于对象引用型的字段，需要合并上类上的设置
            context.Config = context.Config.CloneAndMerge(type);

            // 反射得到所有需要序列化的字段或属性
            MemberInfo[] mis = FindAllSerialized(type, context.Config.SerialProperty);
            if (mis != null && mis.Length > 0)
            {
                // 先获取类特性，后获取成员特性，所以，成员特性优先于类特性
                foreach (MemberInfo item in mis)
                {
                    if (item is FieldInfo)
                    {
                        FieldInfo fi = item as FieldInfo;
                        Object obj = fi.GetValue(context.Data);
                        type = obj == null ? fi.FieldType : obj.GetType();
                        WriteMember(context.Clone(obj, type, item) as WriteContext);
                    }
                    else
                    {
                        PropertyInfo pi = item as PropertyInfo;
                        Object obj = pi.GetValue(context.Data, null);
                        type = obj == null ? pi.PropertyType : obj.GetType();
                        WriteMember(context.Clone(obj, type, item) as WriteContext);
                    }
                }
            }
        }

        void WriteArray(WriteContext context)
        {
            // 先写元素个数
            Array arr = context.Data as Array;
            // 特性指定了长度，这里就不需要再写入长度了
            if (context.Config.Size <= 0) context.Writer.WriteEncodeInt32(arr.Length);

            // 特殊处理字节数组
            if (context.Type == typeof(Byte[]))
            {
                context.Writer.Write((Byte[])context.Data);
                return;
            }

            Int32 n = 0;
            foreach (Object item in arr)
            {
                Type type = null;
                if (item != null) type = item.GetType();
                WriteMember(context.Clone(item, type, type, n++.ToString()) as WriteContext);
            }
        }

        void WriteEnumerable(WriteContext context)
        {
            // 先写元素个数
            IEnumerable arr = context.Data as IEnumerable;
            Int32 count = 0;
            foreach (Object item in arr)
            {
                count++;
            }
            // 特性指定了长度，这里就不需要再写入长度了
            if (context.Config.Size <= 0) context.Writer.WriteEncodeInt32(count);

            Int32 n = 0;
            foreach (Object item in arr)
            {
                Type type = null;
                if (item != null) type = item.GetType();
                WriteMember(context.Clone(item, type, type, n++.ToString()) as WriteContext);
            }
        }
        #endregion

        #region 读取数据
        /// <summary>
        /// 读取对象
        /// </summary>
        /// <remarks>
        /// 非对象类型直接返回数据；
        /// 对象类型且参数context.Data不为空时，填充context.Data；
        /// 对象类型且参数context.Data为空时，创建对象，填充后返回
        /// </remarks>
        /// <param name="context"></param>
        /// <returns></returns>
        public Object ReadMember(ReadContext context)
        {
            WriteLog(context.Node.ToString());

            if (!OnDeserializing(context)) return context.Data;

            #region 基础类型
            TypeCode code = Type.GetTypeCode(context.Type);

            switch (code)
            {
                case TypeCode.Boolean:
                    return context.Reader.ReadBoolean();
                case TypeCode.Byte:
                    return context.Reader.ReadByte();
                case TypeCode.Char:
                    return context.Reader.ReadChar();
                case TypeCode.DBNull:
                    return DBNull.Value;
                case TypeCode.DateTime:
                    return new DateTime(context.Reader.ReadInt64());
                case TypeCode.Decimal:
                    return context.Reader.ReadDecimal();
                case TypeCode.Double:
                    return context.Reader.ReadDouble();
                case TypeCode.Empty:
                    return null; ;
                case TypeCode.Int16:
                    return context.Reader.ReadInt16();
                case TypeCode.Int32:
                    if (!context.Config.EncodeInt)
                        return context.Reader.ReadInt32();
                    else
                        return context.Reader.ReadEncodeInt32();
                case TypeCode.Int64:
                    if (!context.Config.EncodeInt)
                        return context.Reader.ReadInt64();
                    else
                        return context.Reader.ReadEncodeInt64();
                case TypeCode.Object:
                    break;
                case TypeCode.SByte:
                    return context.Reader.ReadSByte();
                case TypeCode.Single:
                    return context.Reader.ReadSingle();
                case TypeCode.String:
                    return context.Reader.ReadString();
                case TypeCode.UInt16:
                    return context.Reader.ReadUInt16();
                case TypeCode.UInt32:
                    return context.Reader.ReadUInt32();
                case TypeCode.UInt64:
                    return context.Reader.ReadUInt64();
                default:
                    break;
            }
            #endregion

            #region 数组、枚举、值类型、对象
            if (context.Type.IsArray)
            {
                return ReadArray(context);
            }
            else if (context.Data is IEnumerable || typeof(IEnumerable).IsAssignableFrom(context.Type))
            {
                return ReadEnumerable(context);
            }
            else if (context.Type.IsValueType)
            {
                if (context.Type == typeof(Guid)) return new Guid(context.Reader.ReadBytes(16));

                return ReadObjectRef(context);
            }
            else
            {
                if (context.Type == typeof(IPAddress))
                {
                    Int32 p = 0;
                    if (!context.Config.EncodeInt)
                        p = context.Reader.ReadInt32();
                    else
                        p = context.Reader.ReadEncodeInt32();
                    context.Data = new IPAddress(p);
                    return context.Data;
                }

                return ReadObjectRef(context);
            }
            #endregion
        }

        Object ReadObjectRef(ReadContext context)
        {
            if (context.Node.Depth > 1 && !context.Config.NotNull)
            {
                // 读取一个字节，探测是否为空
                Byte b = context.Reader.ReadByte();
                if (b == 0)
                {
                    //context.Data = null;
                    return null; ;
                }
            }

            // 创建
            if (context.Data == null)
            {
                context.Data = OnCreateInstance(context, context.Type);
                if (context.Data == null) context.Data = Activator.CreateInstance(context.Type);
            }

            // 先获取类特性，后获取成员特性，所以，成员特性优先于类特性
            context.Config = context.Config.CloneAndMerge(context.Type);

            MemberInfo[] mis = FindAllSerialized(context.Type, context.Config.SerialProperty);
            if (mis != null && mis.Length > 0)
            {
                foreach (MemberInfo item in mis)
                {
                    Object obj = null;
                    if (item is FieldInfo)
                    {
                        FieldInfo fi = item as FieldInfo;
                        // 只有Object类型才计算值
                        if (Type.GetTypeCode(fi.FieldType) == TypeCode.Object) obj = fi.GetValue(context.Data);
                        obj = ReadMember(context.Clone(obj, fi.FieldType, item) as ReadContext);
                        fi.SetValue(context.Data, obj);

                        OnDeserialized(context);
                    }
                    else
                    {
                        PropertyInfo pi = item as PropertyInfo;
                        // 只有Object类型才计算值
                        if (Type.GetTypeCode(pi.PropertyType) == TypeCode.Object) obj = pi.GetValue(context.Data, null);
                        obj = ReadMember(context.Clone(obj, pi.PropertyType, item) as ReadContext);
                        if (pi.GetSetMethod() != null) pi.SetValue(context.Data, obj, null);

                        OnDeserialized(context);
                    }
                }
            }
            return context.Data;
        }

        Object ReadArray(ReadContext context)
        {
            Int32 n = context.Config.Size;
            if (n <= 0) n = context.Reader.ReadEncodeInt32();
            if (n <= 0) return null;

            // 特殊处理字节数组
            if (context.Type == typeof(Byte[])) return context.Reader.ReadBytes(n);

            Type elementType = context.Type.GetElementType();

            return ReadArray(context, elementType, n);
        }

        Array ReadArray(ReadContext context, Type elementType, Int32 n)
        {
            Array arr = Array.CreateInstance(elementType, n);

            for (int i = 0; i < n; i++)
            {
                Object obj = ReadMember(context.Clone(null, elementType, elementType, i.ToString()) as ReadContext);
                arr.SetValue(obj, i);

                OnDeserialized(context);
            }

            return arr;
        }

        Object ReadEnumerable(ReadContext context)
        {
            Int32 n = context.Config.Size;
            if (n <= 0) n = context.Reader.ReadEncodeInt32();

            if (n <= 0) return null;

            Type elementType = context.Type;
            if (context.Type.HasElementType)
                elementType = context.Type.GetElementType();
            else if (context.Type.IsGenericType)
            {
                Type[] ts = context.Type.GetGenericArguments();
                if (ts != null && ts.Length > 0) elementType = ts[0];
            }
            Array arr = ReadArray(context, elementType, n);
            if (arr == null) return null;

            context.Data = Activator.CreateInstance(context.Type, arr);

            return context.Data;
        }
        #endregion

        #region 序列化
        /// <summary>
        /// 把一个对象序列化到指定流中
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="obj"></param>
        public void Serialize(Stream stream, object obj)
        {
            if (obj == null) throw new ArgumentNullException("obj");

            WriteContext context = new WriteContext();
            context.Formatter = this;
            context.Writer = new ProtocolBinaryWriter(stream);
            context.Data = obj;
            context.Type = obj.GetType();

            // 树，用于记录分析所到达的位置
            context.Node = new ProtocolTreeNode(null, context.Type);
            context.Node.Context = context;

            context.Config = FormatterConfig.Default;

            RefObjects = null;

            // 使用默认设置写入头部
            if (!Head.Config.NoHead)
            {
                Head.AssemblyName = context.Type.Assembly.FullName;
                Head.TypeName = context.Type.FullName;
                //WriteMember(writer, Head, config, node.Add("Head", Head.GetType()));
                WriteMember(context.Clone(Head, Head.GetType(), null, "Head") as WriteContext);

                // 只有使用了头部，才使用头部设置信息，否则使用默认设置信息，因为远端需要正确识别数据
                context.Config = Head.Config;
            }

            // 写入主体
            //WriteMember(writer, obj, config, node.Add("Body", obj.GetType()));
            WriteMember(context.Clone(obj, context.Type, null, "Body") as WriteContext);
        }
        #endregion

        #region 反序列化
        /// <summary>
        /// 从指定流中读取一个对象
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public object Deserialize(Stream stream)
        {
            return Deserialize(stream, null);
        }

        /// <summary>
        /// 从指定流读取数据填充到指定对象
        /// </summary>
        /// <param name="stream"></param>
        /// <param name="obj"></param>
        /// <returns></returns>
        public Object Deserialize(Stream stream, Object obj)
        {
            ReadContext context = new ReadContext();
            context.Formatter = this;
            context.Reader = new ProtocolBinaryReader(stream);

            // 树，用于记录分析所到达的位置
            context.Node = new ProtocolTreeNode(null, null);
            context.Node.Context = context;

            Type type = null;
            if (obj != null) type = obj.GetType();

            context.Config = FormatterConfig.Default;
            // 使用默认设置读取头部
            if (!Head.Config.NoHead)
            {
                ReadMember(context.Clone(Head, Head.GetType(), null, "Head") as ReadContext);
                OnDeserialized(context);

                // 只有使用了头部，才使用头部设置信息，否则使用默认设置信息，因为远端需要正确识别数据
                context.Config = Head.Config;
            }

            if (type == null)
            {
                // 读取类名
                Assembly asm = Assembly.Load(Head.AssemblyName);
                if (asm == null) throw new Exception("无法找到程序集" + Head.AssemblyName + "！");

                type = asm.GetType(Head.TypeName);
                if (type == null) throw new Exception("无法找到类" + Head.TypeName + "！");
            }

            context.Type = type;
            context.Node.Type = type;

            if (obj == null) obj = Activator.CreateInstance(type);
            context.Data = obj;

            ReadMember(context.Clone(obj, type, null, "Body") as ReadContext);
            OnDeserialized(context);

            return obj;
        }
        #endregion

        #region 获取字段/属性
        static Dictionary<String, MemberInfo[]> cache1 = new Dictionary<String, MemberInfo[]>();

        /// <summary>
        /// 获取所有可序列化的字段
        /// </summary>
        /// <param name="type"></param>
        /// <param name="serialProperty">主要序列化属性，没有标记的属性全部返回，否则仅返回标记了协议特性的属性</param>
        /// <returns></returns>
        static MemberInfo[] FindAllSerialized(Type type, Boolean serialProperty)
        {
            String key = String.Format("{0}_{1}", type.FullName, serialProperty);
            if (cache1.ContainsKey(key)) return cache1[key];
            lock (cache1)
            {
                if (cache1.ContainsKey(key)) return cache1[key];

                MemberInfo[] mis = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (mis == null || mis.Length < 1) return null;

                List<MemberInfo> list = new List<MemberInfo>();
                foreach (MemberInfo item in mis)
                {
                    if (!(item is FieldInfo || item is PropertyInfo && item.Name != "Item")) continue;

                    // 标记了NonSerialized特性的字段一定不可以序列化
                    if (ProtocolAttribute.GetCustomAttribute<NonSerializedAttribute>(item) != null) continue;
                    if (ProtocolAttribute.GetCustomAttribute<ProtocolNonSerializedAttribute>(item) != null) continue;

                    if (item is FieldInfo && serialProperty || item is PropertyInfo && !serialProperty)
                    {
                        // 非正向，必须标记特性
                        ProtocolAttribute[] atts = ProtocolAttribute.GetCustomAttributes<ProtocolAttribute>(item);
                        if (atts == null || atts.Length < 1) continue;
                    }

                    list.Add(item);
                }

                if (list == null || list.Count < 1)
                    mis = null;
                else
                    mis = list.ToArray();

                cache1.Add(key, mis);

                return mis;
            }
        }
        #endregion

        #region 事件
        /// <summary>
        /// 序列化前
        /// </summary>
        public event EventHandler<ProtocolSerializingEventArgs> Serializing;

        /// <summary>
        /// 序列化后
        /// </summary>
        public event EventHandler<ProtocolSerializedEventArgs> Serialized;

        /// <summary>
        /// 反序列化前
        /// </summary>
        public event EventHandler<ProtocolDeserializingEventArgs> Deserializing;

        /// <summary>
        /// 反序列化后
        /// </summary>
        public event EventHandler<ProtocolDeserializedEventArgs> Deserialized;

        /// <summary>
        /// 创建实例
        /// </summary>
        public event EventHandler<ProtocolCreateInstanceEventArgs> CreateInstance;
        #endregion

        #region 序列化前后
        /// <summary>
        /// 序列化前触发
        /// </summary>
        /// <param name="context"></param>
        /// <returns>是否允许序列化当前字段或属性</returns>
        protected virtual Boolean OnSerializing(WriteContext context)
        {
            Boolean b = true;

            // 事件由外部动态指定，拥有优先处理权
            if (Serializing != null)
            {
                ProtocolSerializingEventArgs e = new ProtocolSerializingEventArgs(context);
                e.Cancel = false;
                Serializing(this, e);
                b = !e.Cancel;
            }

            IProtocolSerializable custom = context.GetCustomInterface();
            if (custom != null) b = custom.OnSerializing(context);

            return b;
        }

        /// <summary>
        /// 序列化后触发
        /// </summary>
        /// <param name="context"></param>
        protected virtual void OnSerialized(WriteContext context)
        {
            // 事件由外部动态指定，拥有优先处理权
            if (Serialized != null) Serialized(this, new ProtocolSerializedEventArgs(context));

            IProtocolSerializable custom = context.GetCustomInterface();
            if (custom != null) custom.OnSerialized(context);
        }

        /// <summary>
        /// 反序列化前触发
        /// </summary>
        /// <param name="context"></param>
        /// <returns>是否允许反序列化当前字段或属性</returns>
        protected virtual Boolean OnDeserializing(ReadContext context)
        {
            Boolean b = true;

            // 事件由外部动态指定，拥有优先处理权
            if (Deserializing != null)
            {
                ProtocolDeserializingEventArgs e = new ProtocolDeserializingEventArgs(context);
                e.Cancel = false;
                Deserializing(this, e);
                b = !e.Cancel;
            }

            IProtocolSerializable custom = context.GetCustomInterface();
            if (custom != null) b = custom.OnDeserializing(context);

            return b;
        }

        /// <summary>
        /// 反序列化后触发
        /// </summary>
        /// <param name="context"></param>
        protected virtual void OnDeserialized(ReadContext context)
        {
            // 事件由外部动态指定，拥有优先处理权
            if (Deserialized != null) Deserialized(this, new ProtocolDeserializedEventArgs(context));

            IProtocolSerializable custom = context.GetCustomInterface();
            if (custom != null) custom.OnDeserialized(context);
        }

        /// <summary>
        /// 为指定类型创建实例时触发
        /// </summary>
        /// <remarks>当内部自定义后外部事件同时存在时，优先外部事件</remarks>
        /// <param name="context"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        protected virtual Object OnCreateInstance(ReadContext context, Type type)
        {
            Object obj = null;

            if (CreateInstance != null)
            {
                ProtocolCreateInstanceEventArgs e = new ProtocolCreateInstanceEventArgs();
                e.Context = context;
                e.Type = type;
                CreateInstance(this, e);
                obj = e.Obj;
            }

            if (obj == null)
            {
                IProtocolSerializable custom = context.GetCustomInterface();
                if (custom != null) obj = custom.OnCreateInstance(context, type);
            }

            return obj;
        }
        #endregion

        #region 日志
        static void WriteLog(String message)
        {
            if (XTrace.Debug) XTrace.WriteLine(message);
        }

        static void WriteLog(String format, params Object[] args)
        {
            if (XTrace.Debug) XTrace.WriteLine(format, args);
        }
        #endregion
    }
}