using System;
using System.IO;
using System.Text;

namespace NewLife.IP
{
    class Zip : IDisposable
    {
        #region 属性
        UInt32 Index_Set;
        UInt32 Index_End;
        UInt32 Index_Count;
        UInt32 Search_Index_Set;
        UInt32 Search_Index_End;
        IndexInfo Search_Set;
        IndexInfo Search_Mid;
        IndexInfo Search_End;

        /// <summary>数据流</summary>
        public Stream Stream { get; set; }
        #endregion

        #region 构造
        /// <summary>析构</summary>
        ~Zip() { OnDispose(false); }

        /// <summary>销毁</summary>
        public void Dispose() => OnDispose(true);

        void OnDispose(Boolean disposing)
        {
            if (Stream != null) Stream.Dispose();

            if (disposing) GC.SuppressFinalize(this);
        }
        #endregion

        #region 数据源
        public Zip SetStream(Stream stream)
        {
            var ms = new MemoryStream();

            var buf = new Byte[3];
            stream.Read(buf, 0, buf.Length);
            stream.Position -= 3;

            // 仅支持Gzip压缩，可用7z软件先压缩为gz格式
            if (buf[0] == 0x1F & buf[1] == 0x8B && buf[2] == 0x08)
                IOHelper.DecompressGZip(stream, ms);
            else
                IOHelper.CopyTo(stream, ms);

            ms.Position = 0;
            Stream = ms;

            Index_Set = GetUInt32();
            Index_End = GetUInt32();
            Index_Count = (Index_End - Index_Set) / 7u + 1u;

            return this;
        }
        #endregion

        #region 方法
        public String GetAddress(UInt32 ip)
        {
            if (Stream == null) return "";

            Search_Index_Set = 0u;
            Search_Index_End = Index_Count - 1u;

            while (true)
            {
                Search_Set = IndexInfoAtPos(Search_Index_Set);
                Search_End = IndexInfoAtPos(Search_Index_End);
                if (ip >= Search_Set.IpSet && ip <= Search_Set.IpEnd) break;

                if (ip >= Search_End.IpSet && ip <= Search_End.IpEnd) return ReadAddressInfoAtOffset(Search_End.Offset);

                Search_Mid = IndexInfoAtPos((Search_Index_End + Search_Index_Set) / 2u);
                if (ip >= Search_Mid.IpSet && ip <= Search_Mid.IpEnd) return ReadAddressInfoAtOffset(Search_Mid.Offset);

                if (ip < Search_Mid.IpSet)
                    Search_Index_End = (Search_Index_End + Search_Index_Set) / 2u;
                else
                    Search_Index_Set = (Search_Index_End + Search_Index_Set) / 2u;
            }
            return ReadAddressInfoAtOffset(Search_Set.Offset);
        }

        String ReadAddressInfoAtOffset(UInt32 Offset)
        {
            Stream.Position = Offset + 4;
            var tag = GetTag();
            String addr;
            String area;
            if (tag == 1)
            {
                Stream.Position = GetOffset();
                tag = GetTag();
                if (tag == 2)
                {
                    var offset = GetOffset();
                    area = ReadArea();
                    Stream.Position = offset;
                    addr = ReadString();
                }
                else
                {
                    Stream.Position -= 1;
                    addr = ReadString();
                    area = ReadArea();
                }
            }
            else
            {
                if (tag == 2)
                {
                    var offset = GetOffset();
                    area = ReadArea();
                    Stream.Position = offset;
                    addr = ReadString();
                }
                else
                {
                    Stream.Position -= 1;
                    addr = ReadString();
                    area = ReadArea();
                }
            }
            return (addr + " " + area).Trim();
        }

        UInt32 GetOffset()
        {
            return BitConverter.ToUInt32(new Byte[]
                {
                    (Byte)Stream.ReadByte(),
                    (Byte)Stream.ReadByte(),
                    (Byte)Stream.ReadByte(),
                    0
                }, 0);
        }

        String ReadArea()
        {
            var tag = GetTag();
            if (tag == 1 || tag == 2)
                Stream.Position = GetOffset();
            else
                Stream.Position -= 1;

            return ReadString();
        }

        String ReadString()
        {
            var k = 0;
            var buf = new Byte[256];
            buf[k] = (Byte)Stream.ReadByte();
            while (buf[k] != 0)
            {
                k += 1;
                buf[k] = (Byte)Stream.ReadByte();
            }
            var str = Encoding.GetEncoding("GB2312").GetString(buf).Trim().Trim('\0').Trim();
            if (str == "CZ88.NET") return null;
            return str;
        }

        Byte GetTag() => (Byte)Stream.ReadByte();

        IndexInfo IndexInfoAtPos(UInt32 Index_Pos)
        {
            var inf = new IndexInfo();
            Stream.Position = Index_Set + 7u * Index_Pos;
            inf.IpSet = GetUInt32();
            inf.Offset = GetOffset();
            Stream.Position = inf.Offset;
            inf.IpEnd = GetUInt32();
            return inf;
        }

        UInt32 GetUInt32()
        {
            var array = new Byte[4];
            Stream.Read(array, 0, 4);
            return BitConverter.ToUInt32(array, 0);
        }
        #endregion

        /// <summary>索引结构</summary>
        class IndexInfo
        {
            public UInt32 IpSet;
            public UInt32 IpEnd;
            public UInt32 Offset;
        }
    }
}