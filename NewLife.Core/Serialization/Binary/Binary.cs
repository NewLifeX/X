using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife.Log;
using NewLife.Reflection;

namespace NewLife.Serialization
{
    /// <summary>二进制序列化</summary>
    public class Binary : IBinary
    {
        #region 属性
        private Stream _Stream;
        /// <summary>数据流</summary>
        public Stream Stream { get { return _Stream ?? (_Stream = new MemoryStream()); } set { _Stream = value; } }

        private Boolean _EncodeInt;
        /// <summary>使用7位编码整数</summary>
        public Boolean EncodeInt { get { return _EncodeInt; } set { _EncodeInt = value; } }

        private Boolean _IsLittleEndian;
        /// <summary>小端字节序</summary>
        public Boolean IsLittleEndian { get { return _IsLittleEndian; } set { _IsLittleEndian = value; } }

        private Encoding _Encoding = Encoding.UTF8;
        /// <summary>字符串编码</summary>
        public Encoding Encoding { get { return _Encoding; } set { _Encoding = value; } }

        private List<IBinaryHandler> _Handlers;
        /// <summary>处理器列表</summary>
        public List<IBinaryHandler> Handlers { get { return _Handlers; } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public Binary()
        {
            // 遍历所有处理器实现
            var list = new List<IBinaryHandler>();
            //foreach (var item in typeof(IBinaryHandler).GetAllSubclasses(true))
            //{
            //    var handler = item.CreateInstance() as IBinaryHandler;
            //    handler.Host = this;
            //    list.Add(handler);
            //}
            list.Add(new BinaryGeneral { Host = this });
            list.Add(new BinaryComposite { Host = this });
            // 根据优先级排序
            list.Sort();
            
            _Handlers = list;
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
                _Handlers.Add(handler);
                // 根据优先级排序
                _Handlers.Sort();
            }

            return this;
        }

        /// <summary>添加处理器</summary>
        /// <typeparam name="THandler"></typeparam>
        /// <param name="priority"></param>
        /// <returns></returns>
        public Binary AddHandler<THandler>(Int32 priority = 0) where THandler : IBinaryHandler, new()
        {
            var handler = new THandler();
            if (priority != 0) handler.Priority = priority;

            return AddHandler(handler);
        }
        #endregion

        #region 写入
        /// <summary>写入一个对象</summary>
        /// <param name="value">目标对象</param>
        /// <param name="type">类型</param>
        /// <returns></returns>
        public Boolean Write(Object value, Type type = null)
        {
            if (type == null)
            {
                if (value == null) return true;

                type = value.GetType();
            }

            foreach (var item in Handlers)
            {
                item.Host = this;
                if (item.Write(value, type)) return true;
            }
            return false;
        }

        /// <summary>写入字节</summary>
        /// <param name="value"></param>
        public void Write(Byte value)
        {
            Stream.WriteByte(value);
        }

        /// <summary>将字节数组部分写入当前流，不写入数组长度。</summary>
        /// <param name="buffer">包含要写入的数据的字节数组。</param>
        /// <param name="offset">buffer 中开始写入的起始点。</param>
        /// <param name="count">要写入的字节数。</param>
        public virtual void Write(byte[] buffer, int offset, int count)
        {
            Stream.Write(buffer, offset, count);
        }

        /// <summary>写入大小</summary>
        /// <param name="size"></param>
        /// <returns></returns>
        public void WriteSize(Int32 size) { WriteEncoded(size); }

        /// <summary>
        /// 以7位压缩格式写入32位整数，小于7位用1个字节，小于14位用2个字节。
        /// 由每次写入的一个字节的第一位标记后面的字节是否还是当前数据，所以每个字节实际可利用存储空间只有后7位。
        /// </summary>
        /// <param name="value">数值</param>
        /// <returns>实际写入字节数</returns>
        Int32 WriteEncoded(Int32 value)
        {
            var list = new List<Byte>();

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
        #endregion

        #region 跟踪日志
        /// <summary>使用跟踪流。实际上是重新包装一次Stream，必须在设置Stream，使用之前</summary>
        public virtual void EnableTrace()
        {
            var stream = Stream;
            if (stream == null || stream is TraceStream) return;

            Stream = new TraceStream(stream) { Encoding = this.Encoding, IsLittleEndian = this.IsLittleEndian };
        }
        #endregion
    }
}