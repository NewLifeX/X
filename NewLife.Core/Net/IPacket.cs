using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Net
{
    /// <summary>封包接口</summary>
    public interface IPacket
    {
        /// <summary>验证数据包是否完整</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        Boolean Valid(Stream stream);

        Int32 Write(Stream stream);
        Stream Read();
    }

    /// <summary>头部指明长度的封包格式</summary>
    public class HeaderLengthPacket : IPacket
    {
        #region 属性
        /// <summary>长度所在位置，默认0</summary>
        public Int32 Offset { get; set; }

        /// <summary>长度占据字节数，1/2/4个字节，默认0表示压缩编码整数</summary>
        public Int32 Size { get; set; }
        #endregion

        /// <summary>验证数据包是否完整</summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public Boolean Valid(Stream stream)
        {
            // 移动到长度所在位置
            if (Offset > 0) stream.Seek(Offset, SeekOrigin.Current);

            // 读取大小
            var s = 0;
            switch (Size)
            {
                case 0:
                    s = stream.ReadEncodedInt();
                    break;
                case 1:
                    s = stream.ReadByte();
                    break;
                case 2:
                    s = stream.ReadBytes(2).ToInt();
                    break;
                case 4:
                    s = (Int32)stream.ReadBytes(4).ToUInt32();
                    break;
                default:
                    throw new NotSupportedException();
            }

            // 判断后续数据是否足够
            return stream.Position + s <= stream.Length;
        }

        private MemoryStream _ms;
        public Int32 Write(Stream stream)
        {
            if (_ms == null) _ms = new MemoryStream();
            stream.CopyTo(_ms);

            return (Int32)_ms.Length;
        }

        public Stream Read()
        {
            return _ms;
        }
    }
}