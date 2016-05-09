using System;
using System.Net;

namespace NewLife.Serialization
{
    /// <summary>常用类型编码</summary>
    public class BinaryNormal : BinaryHandlerBase
    {
        /// <summary>初始化</summary>
        public BinaryNormal()
        {
            // 优先级
            Priority = 12;
        }

        /// <summary>写入</summary>
        /// <param name="value"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public override Boolean Write(Object value, Type type)
        {
            if (type == typeof(Guid))
            {
                Write(((Guid)value).ToByteArray(), -1);
                return true;
            }
            else if (type == typeof(Byte[]))
            {
                //Write((Byte[])value);
                var bn = Host as Binary;
                var bc = bn.GetHandler<BinaryGeneral>();
                bc.Write((Byte[])value);
                
                return true;
            }
            else if (type == typeof(Char[]))
            {
                //Write((Char[])value);
                var bn = Host as Binary;
                var bc = bn.GetHandler<BinaryGeneral>();
                bc.Write((Char[])value, 0, -1);

                return true;
            }
            else if (type == typeof(IPAddress))
            {
                Host.Write(((IPAddress)value).GetAddressBytes());
                return true;
            }
            else if (type == typeof(IPEndPoint))
            {
                var ep = value as IPEndPoint;
                Host.Write(ep.Address.GetAddressBytes());
                Host.Write((UInt16)ep.Port);
                return true;
            }

            return false;
        }

        /// <summary>写入字节数组，自动计算长度</summary>
        /// <param name="buffer">缓冲区</param>
        /// <param name="count">数量</param>
        private void Write(Byte[] buffer, Int32 count)
        {
            if (buffer == null) return;

            if (count < 0 || count > buffer.Length) count = buffer.Length;

            Host.Write(buffer, 0, count);
        }

        /// <summary>读取</summary>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public override Boolean TryRead(Type type, ref Object value)
        {
            if (type == typeof(Guid))
            {
                value = new Guid(ReadBytes(16));
                return true;
            }
            else if (type == typeof(Byte[]))
            {
                value = ReadBytes(-1);
                return true;
            }
            else if (type == typeof(Char[]))
            {
                value = ReadChars(-1);
                return true;
            }
            else if (type == typeof(IPAddress))
            {
                value = new IPAddress(ReadBytes(-1));
                return true;
            }
            else if (type == typeof(IPEndPoint))
            {
                var ip = new IPAddress(ReadBytes(-1));
                var port = Host.Read<UInt16>();
                value = new IPEndPoint(ip, port);
                return true;
            }

            return false;
        }

        /// <summary>从当前流中将 count 个字节读入字节数组，如果count小于0，则先读取字节数组长度。</summary>
        /// <param name="count">要读取的字节数。</param>
        /// <returns></returns>
        protected virtual Byte[] ReadBytes(Int32 count)
        {
            var bn = Host as Binary;
            var bc = bn.GetHandler<BinaryGeneral>();

            return bc.ReadBytes(count);
        }

        /// <summary>从当前流中读取 count 个字符，以字符数组的形式返回数据，并根据所使用的 Encoding 和从流中读取的特定字符，提升当前位置。</summary>
        /// <param name="count">要读取的字符数。</param>
        /// <returns></returns>
        public virtual Char[] ReadChars(Int32 count)
        {
            if (count < 0) count = Host.ReadSize();

            // 首先按最小值读取
            var data = ReadBytes(count);

            return Host.Encoding.GetChars(data);
        }
    }
}