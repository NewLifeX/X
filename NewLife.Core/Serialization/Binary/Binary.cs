using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using NewLife.Data;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>二进制序列化</summary>
    public class Binary : FormatterBase, IBinary
    {
        #region 属性
        /// <summary>使用7位编码整数。默认false不使用</summary>
        public Boolean EncodeInt { get; set; }

        /// <summary>小端字节序。默认false大端</summary>
        public Boolean IsLittleEndian { get; set; }

        /// <summary>使用指定大小的FieldSizeAttribute特性，默认false</summary>
        public Boolean UseFieldSize { get; set; }

        /// <summary>使用对象引用，默认true</summary>
        public Boolean UseRef { get; set; } = true;

        /// <summary>大小宽度。可选0/1/2/4，默认0表示压缩编码整数</summary>
        public Int32 SizeWidth { get; set; }

        /// <summary>要忽略的成员</summary>
        public ICollection<String> IgnoreMembers { get; set; }

        ///// <summary>是否写入名称。默认false</summary>
        //public Boolean UseName { get; set; }

        /// <summary>处理器列表</summary>
        public IList<IBinaryHandler> Handlers { get; private set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public Binary()
        {
            //UseName = false;
            IgnoreMembers = new HashSet<String>();

            // 遍历所有处理器实现
            var list = new List<IBinaryHandler>
            {
                new BinaryGeneral { Host = this },
                new BinaryNormal { Host = this },
                new BinaryComposite { Host = this },
                new BinaryList { Host = this },
                new BinaryDictionary { Host = this }
            };
            // 根据优先级排序
            list.Sort();

            Handlers = list;
        }
        #endregion

        #region 处理器
        /// <summary>添加处理器</summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public Binary AddHandler(IBinaryHandler handler)
        {
            if (handler != null)
            {
                handler.Host = this;
                Handlers.Add(handler);
                // 根据优先级排序
                (Handlers as List<IBinaryHandler>).Sort();
            }

            return this;
        }

        /// <summary>添加处理器</summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="priority"></param>
        /// <returns></returns>
        public Binary AddHandler<THandler>(Int32 priority = 0) where THandler : IBinaryHandler, new()
        {
            var handler = new THandler
            {
                Host = this
            };
            if (priority != 0) handler.Priority = priority;

            return AddHandler(handler);
        }

        /// <summary>获取处理器</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        public T GetHandler<T>() where T : class, IBinaryHandler
        {
            foreach (var item in Handlers)
            {
                if (item is T) return item as T;
            }

            return default(T);
        }
        #endregion

        #region 写入
        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        //[DebuggerHidden]
        public virtual Boolean Write(Object value, Type type = null)
        {
            if (type == null)
            {
                if (value == null) return true;

                type = value.GetType();

                // 一般类型为空是顶级调用
                if (Hosts.Count == 0) WriteLog("BinaryWrite {0} {1}", type.Name, value);
            }

            // 优先 IAccessor 接口
            if (value is IAccessor acc)
            {
                if (acc.Write(Stream, this)) return true;
            }

            foreach (var item in Handlers)
            {
                if (item.Write(value, type)) return true;
            }
            return false;
        }

        /// <summary>写入字节</summary>
        /// <param name="value"></param>
        public virtual void Write(Byte value) => Stream.WriteByte(value);

        /// <summary>将字节数组部分写入当前流，不写入数组长度。</summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="offset">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        public virtual void Write(Byte[] buffer, Int32 offset, Int32 count)
        {
            if (count < 0) count = buffer.Length - offset;
            Stream.Write(buffer, offset, count);
        }

        /// <summary>写入大小，如果有FieldSize则返回，否则写入编码的大小</summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public virtual Int32 WriteSize(Int32 size)
        {
            if (UseFieldSize)
            {
                var fieldsize = GetFieldSize();
                if (fieldsize >= 0) return fieldsize;
            }

            switch (SizeWidth)
            {
                case 1:
                    Write((Byte)size);
                    break;
                case 2:
                    Write((Int16)size);
                    break;
                case 4:
                    Write(size);
                    break;
                case 0:
                default:
                    //if (EncodeInt)
                    WriteEncoded(size);
                    //else
                    //    Write(size);
                    break;
            }

            return -1;
        }

        /// <summary>写7位压缩编码整数</summary>
        /// <remarks>
        /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </remarks>
        /// <param name="value">数值</param>
        /// <returns>实际写入字节数</returns>
        Int32 WriteEncoded(Int32 value)
        {
            var arr = new Byte[16];
            var k = 0;

            var count = 1;
            var num = (UInt32)value;
            while (num >= 0x80)
            {
                arr[k++] = (Byte)(num | 0x80);
                num = num >> 7;

                count++;
            }
            arr[k++] = (Byte)num;

            Write(arr, 0, k);

            return count;
        }
        #endregion

        #region 读取
        /// <summary>读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <returns></returns>
        //[DebuggerHidden]
        public virtual Object Read(Type type)
        {
            //var value = type.CreateInstance();
            Object value = null;
            if (!TryRead(type, ref value)) throw new Exception("读取失败！");

            return value;
        }

        /// <summary>读取指定类型对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        //[DebuggerHidden]
        public T Read<T>() => (T)Read(typeof(T));

        /// <summary>尝试读取指定类型对象</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        //[DebuggerHidden]
        public virtual Boolean TryRead(Type type, ref Object value)
        {
            if (Hosts.Count == 0) WriteLog("BinaryRead {0} {1}", type.Name, value);

            // 优先 IAccessor 接口
            if (value is IAccessor acc)
            {
                if (acc.Read(Stream, this)) return true;
            }
            if (value == null && type.As<IAccessor>())
            {
                value = type.CreateInstance();
                if (value is IAccessor acc2)
                {
                    if (acc2.Read(Stream, this)) return true;
                }
            }

            foreach (var item in Handlers)
            {
                if (item.TryRead(type, ref value)) return true;
            }
            return false;
        }

        /// <summary>读取字节</summary>
        /// <returns></returns>
        public virtual Byte ReadByte()
        {
            var b = Stream.ReadByte();
            if (b < 0) throw new Exception("数据流超出范围！");
            return (Byte)b;
        }

        /// <summary>从当前流中将 count 个字节读入字节数组</summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        public virtual Byte[] ReadBytes(Int32 count)
        {
            var buffer = new Byte[count];
            Stream.Read(buffer, 0, count);

            return buffer;
        }

        /// <summary>读取大小</summary>
        /// <returns></returns>
        public virtual Int32 ReadSize()
        {
            if (UseFieldSize)
            {
                var size = GetFieldSize();
                if (size >= 0) return size;
            }

            switch (SizeWidth)
            {
                case 1:
                    return ReadByte();
                case 2:
                    return (Int16)Read(typeof(Int16));
                case 4:
                    return (Int32)Read(typeof(Int32));
                case 0:
                    return ReadEncodedInt32();
                default:
                    return -1;
            }
        }

        Int32 GetFieldSize()
        {
            if (Member is MemberInfo member)
            {
                // 获取FieldSizeAttribute特性
                var att = member.GetCustomAttribute<FieldSizeAttribute>();
                if (att != null)
                {
                    // 如果指定了固定大小，直接返回
                    if (att.Size > 0 && String.IsNullOrEmpty(att.ReferenceName)) return att.Size;

                    // 如果指定了引用字段，则找引用字段所表示的长度
                    var size = att.GetReferenceSize(Hosts.Peek(), member);
                    if (size >= 0) return size;
                }
            }

            return -1;
        }
        #endregion

        #region 有符号整数
        /// <summary>读取整数的字节数组，某些写入器（如二进制写入器）可能需要改变字节顺序</summary>
        /// <param name="count">数量</param>
        /// <returns></returns>
        Byte[] ReadIntBytes(Int32 count) => Stream.ReadBytes(count);

        /// <summary>从当前流中读取 2 字节有符号整数，并使流的当前位置提升 2 个字节。</summary>
        /// <returns></returns>
        Int16 ReadInt16() => BitConverter.ToInt16(ReadIntBytes(2), 0);

        /// <summary>从当前流中读取 4 字节有符号整数，并使流的当前位置提升 4 个字节。</summary>
        /// <returns></returns>
        Int32 ReadInt32() => BitConverter.ToInt32(ReadIntBytes(4), 0);
        #endregion

        #region 7位压缩编码整数
        /// <summary>以压缩格式读取16位整数</summary>
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

        /// <summary>以压缩格式读取32位整数</summary>
        /// <returns></returns>
        public Int32 ReadEncodedInt32()
        {
            Byte b;
            var rs = 0;
            Byte n = 0;
            while (true)
            {
                b = ReadByte();
                // 必须转为Int32，否则可能溢出
                rs += (b & 0x7f) << n;
                if ((b & 0x80) == 0) break;

                n += 7;
                if (n >= 32) throw new FormatException("数字值过大，无法使用压缩格式读取！");
            }
            return rs;
        }

        /// <summary>以压缩格式读取64位整数</summary>
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

        #region 辅助函数

        #endregion

        #region 跟踪日志
#if !__MOBILE__
        /// <summary>使用跟踪流。实际上是重新包装一次Stream，必须在设置Stream后，使用之前</summary>
        public virtual void EnableTrace()
        {
            var stream = Stream;
            if (stream == null || stream is TraceStream) return;

            Stream = new TraceStream(stream) { Encoding = Encoding, IsLittleEndian = IsLittleEndian };
        }
#endif
        #endregion

        #region 快捷方法
        /// <summary>快速读取</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="stream">数据流</param>
        /// <param name="encodeInt">使用7位编码整数</param>
        /// <returns></returns>
        public static T FastRead<T>(Stream stream, Boolean encodeInt = true)
        {
            var bn = new Binary() { Stream = stream, EncodeInt = encodeInt };
            return bn.Read<T>();
        }

        /// <summary>快速写入</summary>
        /// <param name="value">对象</param>
        /// <param name="encodeInt">使用7位编码整数</param>
        /// <returns></returns>
        public static Packet FastWrite(Object value, Boolean encodeInt = true)
        {
            // 头部预留8字节，方便加协议头
            var bn = new Binary { EncodeInt = encodeInt };
            bn.Stream.Seek(8, SeekOrigin.Current);
            bn.Write(value);

            var buf = bn.GetBytes();
            return new Packet(buf, 8, buf.Length - 8);
        }
        #endregion
    }
}