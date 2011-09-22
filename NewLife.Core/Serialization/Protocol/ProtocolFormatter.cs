using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Reflection;
using System.Runtime.Serialization;
using NewLife.IO;
using NewLife.Reflection;
using NewLife.Log;
using NewLife.Collections;

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
        //private Dictionary<Object, Int32> _RefObjects;
        ///// <summary>引用对象，用于检测循环引用</summary>
        //public Dictionary<Object, Int32> RefObjects
        //{
        //    get { return _RefObjects; }
        //    set { _RefObjects = value; }
        //}
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
        /// <remarks>
        /// 分为几种情况：
        /// 1，空引用直接写入0
        /// 2，基本值类型直接写入值
        /// 3，非基本值类型先查找是否已有引用，已有则写入引用计数（从1开始，因为0表示空引用），没有则添加到引用集合，不写引用计数
        /// 4，数组和枚举类型先写入元素个数，再依次写入元素
        /// 5，对象类型，反射得到属性和字段，一个个写入
        /// 值得注意的是，数据和枚举本也使用引用计数防止重复引用，同时包含引用计数和元素个数
        /// </remarks>
        /// <param name="context"></param>
        public void WriteMember(WriteContext context)
        {
            //WriteLog(context.Node.ToString());

            Stream stream = context.Writer.BaseStream;
            Int64 pos = stream.Position;
            if (OnSerializing(context))
            {
                WriteMemberInternal(context);
                OnSerialized(context);
            }
            Int64 pos2 = stream.Position;
            if (pos2 > pos)
            {
                stream.Seek(pos, SeekOrigin.Begin);
                Byte[] buffer = new Byte[pos2 - pos];
                stream.Read(buffer, 0, buffer.Length);

                WriteLog("{0} [{1}] {2}", context.Node.ToString(), buffer.Length, BitConverter.ToString(buffer).Replace("-", " "));
            }
        }

        private void WriteMemberInternal(WriteContext context)
        {
            BinaryWriterX writer = context.Writer;
            Object data = context.Data;
            Boolean encodeInt = context.Config.EncodeInt;

            // 空数据时，写个0字节占位，如果配置为非空，则抛出异常
            if (data == null)
            {
                if (context.Config.NotNull) throw new InvalidOperationException("在非空设置下遇到空节点！" + context.Node.Path);
                writer.Write((Byte)0);
                return;
            }

            // 基础类型
            if (writer.WriteValue(data, encodeInt)) return;

            // 引用对象
            if (context.Config.UseRefObject)
            {
                // 对于非基本类型，先写入对象引用计数
                // 计数0表示是空对象
                if (context.Node.Depth > 1 && !context.Config.NotNull)
                {
                    Int32 n = context.Objects.IndexOf(data);
                    if (n >= 0)
                    {
                        // 该对象已经写入，这里写入引用计数即可
                        writer.WriteEncoded(n + 1);
                        return;
                    }
                }

                context.Objects.Add(data);
                // 不写对象引用计数
                if (context.Node.Depth > 1 && !context.Config.NotNull) writer.WriteEncoded(context.Objects.Count);
            }

            #region 数组、枚举、值类型、对象
            if (context.Type.IsArray)
            {
                WriteArray(context);
                return;
            }

            if (data is IEnumerable)
            {
                WriteEnumerable(context);
                return;
            }

            //else
            {
                if (context.Type == typeof(IPAddress))
                {
                    IPAddress ip = data as IPAddress;
                    Byte[] buffer = ip.GetAddressBytes();
                    if (!encodeInt)
                        writer.Write(buffer.Length);
                    else
                        writer.WriteEncoded(buffer.Length);
                    writer.Write(buffer);
                    return;
                }

                WriteObjectRef(context);
            }
            #endregion
        }

        void WriteObjectRef(WriteContext context)
        {
            //// 检测循环引用
            //if (RefObjects == null) RefObjects = new Dictionary<Object, Int32>();

            //if (RefObjects.ContainsKey(context.Data))
            //    throw new InvalidOperationException("检测到循环引用！" + context.Node);

            //RefObjects.Add(context.Data, RefObjects.Count + 1);

            // 写入一个字节表示当前对象不为空
            // 实际上，还可以建立一个只能容纳255个类型的数组，这个字节用于表示类型
            if (!context.Config.UseRefObject && context.Node.Depth > 1 && !context.Config.NotNull) context.Writer.Write((Byte)1);

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
                        Object obj = FieldInfoX.Create(fi).GetValue(context.Data);
                        type = obj == null ? fi.FieldType : obj.GetType();
                        WriteMember(context.Clone(obj, type, item) as WriteContext);
                    }
                    else
                    {
                        PropertyInfo pi = item as PropertyInfo;
                        Object obj = PropertyInfoX.Create(pi).GetValue(context.Data);
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
            if (context.Config.Size <= 0) context.Writer.WriteEncoded(arr.Length);

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
            if (context.Config.Size <= 0) context.Writer.WriteEncoded(count);

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
        /// <remarks>
        /// 分为几种情况：
        /// 1，空引用直接写入0
        /// 2，基本值类型直接写入值
        /// 3，非基本值类型先查找是否已有引用，已有则写入引用计数（从1开始，因为0表示空引用），没有则添加到引用集合，再写入引用计数
        /// 4，数组和枚举类型先写入元素个数，再依次写入元素
        /// 5，对象类型，反射得到属性和字段，一个个写入
        /// 值得注意的是，数据和枚举本也使用引用计数防止重复引用，同时包含引用计数和元素个数
        /// </remarks>
        /// <param name="context"></param>
        /// <returns></returns>
        public Object ReadMember(ReadContext context)
        {
            //WriteLog(context.Node.ToString());

            if (!OnDeserializing(context)) return context.Data;

            Stream stream = context.Reader.BaseStream;
            Int64 pos = stream.Position;

            Object data = ReadMemberInternal(context);

            Int64 pos2 = stream.Position;
            if (pos2 > pos)
            {
                stream.Seek(pos, SeekOrigin.Begin);
                Byte[] buffer = new Byte[pos2 - pos];
                stream.Read(buffer, 0, buffer.Length);

                WriteLog("{0} [{1}] {2}", context.Node.ToString(), buffer.Length, BitConverter.ToString(buffer).Replace("-", " "));
            }

            return data;
        }

        private Object ReadMemberInternal(ReadContext context)
        {
            BinaryReaderX reader = context.Reader;
            Boolean encodeInt = context.Config.EncodeInt;

            // 基础类型
            Object data = null;
            if (reader.TryReadValue(context.Type, encodeInt, out data))
            {
                context.Data = data;
                return data;
            }

            #region 数组、枚举、值类型、对象
            if (context.Config.UseRefObject)
            {
                // 对于非基本类型，先写入对象引用计数
                if (context.Node.Depth > 1 && !context.Config.NotNull)
                {
                    Int32 n = reader.ReadEncodedInt32();
                    // 计数0表示是空对象
                    if (n == 0)
                    {
                        context.Data = null;
                        return null;
                    }

                    // 从对象集合中找到对象，无效反序列化
                    if (n <= context.Objects.Count)
                    {
                        context.Data = context.Objects[n - 1];
                        return context.Data;
                    }
                    //else if (n == context.Objects.Count + 1)
                    //{
                    //    // 该对象的第一个引用
                    //}
                    //else
                    //    throw new InvalidOperationException("数据异常，从对象集合中找到对象！");
                }
            }

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
                //if (context.Type == typeof(Guid)) return new Guid(reader.ReadBytes(16));

                return ReadObjectRef(context);
            }
            else
            {
                if (context.Type == typeof(IPAddress))
                {
                    Int32 p = 0;
                    if (!encodeInt)
                        p = reader.ReadInt32();
                    else
                        p = reader.ReadEncodedInt32();
                    //if (p <= -1) p = (Int64)(UInt32)p;
                    //context.Data = new IPAddress(p);
                    Byte[] buffer = reader.ReadBytes(p);
                    context.Data = new IPAddress(buffer);
                    return context.Data;
                }

                return ReadObjectRef(context);
            }

            #endregion
        }

        Object ReadObjectRef(ReadContext context)
        {
            if (!context.Config.UseRefObject)
            {
                // 不使用引用对象的时候，这里就需要判断了
                if (context.Node.Depth > 1 && !context.Config.NotNull)
                {
                    // 读取一个字节，探测是否为空
                    Byte b = context.Reader.ReadByte();
                    if (b == 0)
                    {
                        //context.Data = null;
                        return null;
                    }
                }
            }

            // 创建
            if (context.Data == null)
            {
                context.Data = OnCreateInstance(context, context.Type);
                if (context.Data == null) context.Data = Activator.CreateInstance(context.Type);
            }

            // 添加对象到对象集合
            if (context.Config.UseRefObject) context.Objects.Add(context.Data);

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
                        FieldInfoX fix = fi;
                        // 只有Object类型才计算值
                        if (Type.GetTypeCode(fi.FieldType) == TypeCode.Object) obj = fix.GetValue(context.Data);
                        obj = ReadMember(context.Clone(obj, fi.FieldType, item) as ReadContext);
                        fix.SetValue(context.Data, obj);

                        OnDeserialized(context);
                    }
                    else
                    {
                        PropertyInfo pi = item as PropertyInfo;
                        PropertyInfoX pix = pi;
                        // 只有Object类型才计算值
                        if (Type.GetTypeCode(pi.PropertyType) == TypeCode.Object) obj = pix.GetValue(context.Data);
                        obj = ReadMember(context.Clone(obj, pi.PropertyType, item) as ReadContext);
                        if (pi.GetSetMethod() != null) pix.SetValue(context.Data, obj);

                        OnDeserialized(context);
                    }
                }
            }
            return context.Data;
        }

        Object ReadArray(ReadContext context)
        {
            Int32 n = context.Config.Size;
            if (n <= 0) n = context.Reader.ReadEncodedInt32();
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
            if (n <= 0) n = context.Reader.ReadEncodedInt32();

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
            context.Writer = new BinaryWriterX(stream);
            context.Data = obj;
            context.Type = obj.GetType();

            // 树，用于记录分析所到达的位置
            context.Node = new ProtocolTreeNode(null, context.Type);
            context.Node.Context = context;

            //context.Config = FormatterConfig.Default;
            context.Config = Head.Config;

            //RefObjects = null;

            // 使用默认设置写入头部
            if (!Head.Config.NoHead)
            {
                Head.AssemblyName = context.Type.Assembly.FullName;
                Head.TypeName = context.Type.FullName;
                //WriteMember(writer, Head, config, node.Add("Head", Head.GetType()));
                WriteMember(context.Clone(Head, Head.GetType(), null, "Head") as WriteContext);

                //// 只有使用了头部，才使用头部设置信息，否则使用默认设置信息，因为远端需要正确识别数据
                //context.Config = Head.Config;
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
            context.Reader = new BinaryReaderX(stream);

            // 树，用于记录分析所到达的位置
            context.Node = new ProtocolTreeNode(null, null);
            context.Node.Context = context;

            Type type = null;
            if (obj != null) type = obj.GetType();

            //context.Config = FormatterConfig.Default;
            context.Config = Head.Config;

            // 使用默认设置读取头部
            if (!Head.Config.NoHead)
            {
                ReadMember(context.Clone(Head, Head.GetType(), null, "Head") as ReadContext);
                OnDeserialized(context);

                //// 只有使用了头部，才使用头部设置信息，否则使用默认设置信息，因为远端需要正确识别数据
                //context.Config = Head.Config;
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
        static DictionaryCache<String, MemberInfo[]> cache1 = new DictionaryCache<String, MemberInfo[]>();

        /// <summary>
        /// 获取所有可序列化的字段
        /// </summary>
        /// <param name="type"></param>
        /// <param name="serialProperty">主要序列化属性，没有标记的属性全部返回，否则仅返回标记了协议特性的属性</param>
        /// <returns></returns>
        static MemberInfo[] FindAllSerialized(Type type, Boolean serialProperty)
        {
            String key0 = String.Format("{0}_{1}", type.FullName, serialProperty);

            return cache1.GetItem(key0, delegate(String key)
            {
                MemberInfo[] mis = type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                if (mis == null || mis.Length < 1) return null;

                List<MemberInfo> list = new List<MemberInfo>();
                foreach (MemberInfo item in mis)
                {
                    if (!(item is FieldInfo || item is PropertyInfo && item.Name != "Item")) continue;

                    // 标记了NonSerialized特性的字段一定不可以序列化
                    if (ProtocolAttribute.GetCustomAttribute<NonSerializedAttribute>(item) != null) continue;
                    if (ProtocolAttribute.GetCustomAttribute<ProtocolNonSerializedAttribute>(item) != null) continue;

                    // GetFields只能取得本类的字段，没办法取得基类的字段
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

                //cache1.Add(key, mis);

                return mis;
            });
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