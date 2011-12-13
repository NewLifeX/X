using System;
using System.IO;
using System.Reflection;
using System.Text;
using NewLife.IO;
using NewLife.Reflection;

namespace NewLife.Compression
{
    partial class ZipFile
    {
        #region 二进制序列化
        class BinaryEntry//<TEntity> where TEntity : BinaryEntity<TEntity>
        {
            #region 方法
            public virtual void Read(Stream stream)
            {
                BinaryReader reader = new BinaryReader(stream);
                foreach (var item in GetMembers())
                {
                    Object obj = null;
                    switch (Type.GetTypeCode(item.PropertyType))
                    {
                        case TypeCode.UInt16:
                            obj = reader.ReadUInt16();
                            break;
                        case TypeCode.UInt32:
                            obj = reader.ReadUInt32();
                            break;
                        case TypeCode.UInt64:
                            obj = reader.ReadUInt64();
                            break;
                        case TypeCode.String:
                            Int32 n = reader.ReadUInt16();
                            if (n > 0) obj = Encoding.Default.GetString(reader.ReadBytes(n));
                            break;
                        default:
                            continue;
                    }
                    PropertyInfoX.Create(item).SetValue(this, obj);
                }
            }

            public virtual void Write(Stream stream)
            {
                BinaryWriter writer = new BinaryWriter(stream);
                foreach (var item in GetMembers())
                {
                    Object obj = PropertyInfoX.Create(item).GetValue(this);
                    switch (Type.GetTypeCode(item.PropertyType))
                    {
                        case TypeCode.UInt16:
                            writer.Write((UInt16)obj);
                            break;
                        case TypeCode.UInt32:
                            writer.Write((UInt32)obj);
                            break;
                        case TypeCode.UInt64:
                            writer.Write((UInt64)obj);
                            break;
                        case TypeCode.String:
                            if ("" + obj != "")
                            {
                                Byte[] bts = Encoding.Default.GetBytes("" + obj);
                                if (bts.Length > 0)
                                {
                                    writer.Write((UInt16)bts.Length);
                                    writer.Write(bts, 0, bts.Length);
                                }
                            }
                            break;
                        default:
                            continue;
                    }
                }
            }
            #endregion

            #region 辅助

            private PropertyInfo[] GetMembers()
            {
                return this.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty);
            }

            #endregion
        }
        #endregion

        #region CentralDirectory
        class EndOfCentralDirectory : BinaryEntry
        {
            #region 属性
            private UInt32 _Signature;
            /// <summary>签名。end of central dir signature</summary>
            public UInt32 Signature { get { return _Signature; } set { _Signature = value; } }

            private UInt16 _DiskNumber;
            /// <summary>卷号。number of this disk</summary>
            public UInt16 DiskNumber { get { return _DiskNumber; } set { _DiskNumber = value; } }

            private UInt16 _DiskNumberWithStart;
            /// <summary>number of the disk with the start of the central directory</summary>
            public UInt16 DiskNumberWithStart { get { return _DiskNumberWithStart; } set { _DiskNumberWithStart = value; } }

            private UInt16 _NumberOfEntriesOnThisDisk;
            /// <summary>total number of entries in the central directory on this disk</summary>
            public UInt16 NumberOfEntriesOnThisDisk { get { return _NumberOfEntriesOnThisDisk; } set { _NumberOfEntriesOnThisDisk = value; } }

            private UInt16 _NumberOfEntries;
            /// <summary>total number of entries in the central directory</summary>
            public UInt16 NumberOfEntries { get { return _NumberOfEntries; } set { _NumberOfEntries = value; } }

            private UInt32 _Size;
            /// <summary>size of the central directory</summary>
            public UInt32 Size { get { return _Size; } set { _Size = value; } }

            private UInt32 _Offset;
            /// <summary>offset of start of central directory with respect to the starting disk number</summary>
            public UInt32 Offset { get { return _Offset; } set { _Offset = value; } }

            private String _Comment;
            /// <summary>属性说明</summary>
            public String Comment { get { return _Comment; } set { _Comment = value; } }

            #endregion

            #region 定位
            public const UInt32 DefaultSignature = 0x06054b50;

            public static Int64 FindSignature(Stream stream)
            {
                return stream.IndexOf(BitConverter.GetBytes(DefaultSignature));
            }

            #endregion
        }
        #endregion

        #region Zip64CentralDirectory
        class Zip64EndOfCentralDirectory : BinaryEntry
        {
            #region 属性
            private UInt32 _Signature;
            /// <summary>签名</summary>
            public UInt32 Signature
            {
                get { return _Signature; }
                set { _Signature = value; }
            }

            private UInt64 _DataSize;
            /// <summary>大小</summary>
            public UInt64 DataSize
            {
                get { return _DataSize; }
                set { _DataSize = value; }
            }

            private UInt16 _VersionMadeBy;
            /// <summary>压缩的版本</summary>
            public UInt16 VersionMadeBy
            {
                get { return _VersionMadeBy; }
                set { _VersionMadeBy = value; }
            }

            private UInt16 _VersionNeededToExtract;
            /// <summary>需要用于解压缩的版本</summary>
            public UInt16 VersionNeededToExtract
            {
                get { return _VersionNeededToExtract; }
                set { _VersionNeededToExtract = value; }
            }

            private UInt32 _DiskNumber;
            /// <summary>卷号。number of this disk</summary>
            public UInt32 DiskNumber { get { return _DiskNumber; } set { _DiskNumber = value; } }

            private UInt32 _DiskNumberWithStart;
            /// <summary>number of the disk with the start of the central directory</summary>
            public UInt32 DiskNumberWithStart { get { return _DiskNumberWithStart; } set { _DiskNumberWithStart = value; } }

            private UInt64 _NumberOfEntriesOnThisDisk;
            /// <summary>total number of entries in the central directory on this disk</summary>
            public UInt64 NumberOfEntriesOnThisDisk { get { return _NumberOfEntriesOnThisDisk; } set { _NumberOfEntriesOnThisDisk = value; } }

            private UInt64 _NumberOfEntries;
            /// <summary>total number of entries in the central directory</summary>
            public UInt64 NumberOfEntries { get { return _NumberOfEntries; } set { _NumberOfEntries = value; } }

            private UInt64 _Size;
            /// <summary>size of the central directory</summary>
            public UInt64 Size { get { return _Size; } set { _Size = value; } }

            private UInt64 _Offset;
            /// <summary>offset of start of central directory with respect to the starting disk number</summary>
            public UInt64 Offset { get { return _Offset; } set { _Offset = value; } }

            private Byte[] _Extend;
            /// <summary>扩展</summary>
            public Byte[] Extend
            {
                get { return _Extend; }
                set { _Extend = value; }
            }
            #endregion

            #region 方法
            public override void Read(Stream stream)
            {
                base.Read(stream);

                UInt64 n = DataSize - 44;
                if (n > 0)
                {
                    Extend = new Byte[n];
                    stream.Read(Extend, 0, (Int32)n);
                }
            }

            public override void Write(Stream stream)
            {
                if (Extend != null && Extend.Length > 0) DataSize = 44 + (UInt64)Extend.Length;
                base.Write(stream);
                stream.Write(Extend, 0, Extend.Length);
            }
            #endregion

            #region 定位
            public const UInt32 DefaultSignature = 0x06064b50;
            #endregion
        }
        #endregion
    }
}