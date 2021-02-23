using System;
using System.IO;
using System.Text;

#nullable enable
namespace NewLife.IP
{
    class Zip : IDisposable
    {
        #region 属性
        UInt32 Index_Set;
        UInt32 Index_End;
        UInt32 Index_Count;
        //UInt32 Search_Index_Set;
        //UInt32 Search_Index_End;
        //IndexInfo Search_Set;
        //IndexInfo Search_Mid;
        //IndexInfo Search_End;

        /// <summary>数据流</summary>
        public Stream? Stream { get; set; }
        #endregion

        #region 构造
        /// <summary>析构</summary>
        ~Zip() { OnDispose(false); }

        /// <summary>销毁</summary>
        public void Dispose() => OnDispose(true);

        void OnDispose(Boolean disposing)
        {
            if (disposing)
            {
                Stream?.Dispose();

                GC.SuppressFinalize(this);
            }
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
                stream.CopyTo(ms);

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

            var idxSet = 0u;
            var idxEnd = Index_Count - 1u;

            IndexInfo set;
            while (true)
            {
                set = IndexInfoAtPos(idxSet);
                var end = IndexInfoAtPos(idxEnd);
                if (ip >= set.IpSet && ip <= set.IpEnd) break;

                if (ip >= end.IpSet && ip <= end.IpEnd) return ReadAddressInfoAtOffset(end.Offset);

                var mid = IndexInfoAtPos((idxEnd + idxSet) / 2u);
                if (ip >= mid.IpSet && ip <= mid.IpEnd) return ReadAddressInfoAtOffset(mid.Offset);

                if (ip < mid.IpSet)
                    idxEnd = (idxEnd + idxSet) / 2u;
                else
                    idxSet = (idxEnd + idxSet) / 2u;
            }
            return ReadAddressInfoAtOffset(set.Offset);
        }

        String ReadAddressInfoAtOffset(UInt32 Offset)
        {
            var ms = Stream;
            if (ms == null) return String.Empty;

            ms.Position = Offset + 4;
            var tag = GetTag();
            String addr;
            String area;
            if (tag == 1)
            {
                ms.Position = GetOffset();
                tag = GetTag();
                if (tag == 2)
                {
                    var offset = GetOffset();
                    area = ReadArea();
                    ms.Position = offset;
                    addr = ReadString();
                }
                else
                {
                    ms.Position -= 1;
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
                    ms.Position = offset;
                    addr = ReadString();
                }
                else
                {
                    ms.Position -= 1;
                    addr = ReadString();
                    area = ReadArea();
                }
            }
            return (addr + " " + area).Trim();
        }

        UInt32 GetOffset()
        {
            var ms = Stream;
            if (ms == null) return 0;

            return BitConverter.ToUInt32(new Byte[] {
                (Byte)ms.ReadByte(),
                (Byte)ms.ReadByte(),
                (Byte)ms.ReadByte(),
                0 },
                0);
        }

        String ReadArea()
        {
            var ms = Stream;
            if (ms == null) return String.Empty;

            var tag = GetTag();
            if (tag == 1 || tag == 2)
                ms.Position = GetOffset();
            else
                ms.Position -= 1;

            return ReadString();
        }

        String ReadString()
        {
            var ms = Stream;
            if (ms == null) return String.Empty;

            var k = 0;
            var buf = new Byte[256];
            buf[k] = (Byte)ms.ReadByte();
            while (buf[k] != 0)
            {
                k += 1;
                buf[k] = (Byte)ms.ReadByte();
            }

            var str = Encoding.GetEncoding("GB2312").GetString(buf).Trim().Trim('\0').Trim();
            if (str == "CZ88.NET") return String.Empty;

            return str;
        }

        Byte GetTag() => (Byte)(Stream?.ReadByte() ?? 0);

        IndexInfo IndexInfoAtPos(UInt32 Index_Pos)
        {
            var inf = new IndexInfo();
            var ms = Stream;
            if (ms == null) return inf;

            ms.Position = Index_Set + 7u * Index_Pos;
            inf.IpSet = GetUInt32();
            inf.Offset = GetOffset();
            ms.Position = inf.Offset;
            inf.IpEnd = GetUInt32();

            return inf;
        }

        UInt32 GetUInt32()
        {
            var ms = Stream;
            if (ms == null) return 0;

            var array = new Byte[4];
            ms.Read(array, 0, 4);
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
#nullable restore