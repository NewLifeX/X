using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
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
            foreach (var item in typeof(IBinaryHandler).GetAllSubclasses(true))
            {
                var handler = item.CreateInstance() as IBinaryHandler;
                handler.Host = this;
                list.Add(handler);
            }
            _Handlers = list;
        }
        #endregion

        #region 写入
        /// <summary>写入一个对象</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean Write(Object value)
        {
            if (value == null) return true;

            foreach (var item in Handlers)
            {
                item.Host = this;
                if (item.Write(value)) return true;
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
        public Boolean WriteSize(Int32 size)
        {
            return false;
        }
        #endregion
    }
}