using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;
using NewLife.Reflection;
using NewLife.IO;

namespace NewLife.Compression
{
    partial class ZipFile
    {
        #region CentralDirectory
        class CentralDirectory
        {
            #region 属性
            private Int32 _Signature;
            /// <summary>签名。end of central dir signature</summary>
            public Int32 Signature { get { return _Signature; } set { _Signature = value; } }

            private Int16 _DiskNumber;
            /// <summary>卷号。number of this disk</summary>
            public Int16 DiskNumber { get { return _DiskNumber; } set { _DiskNumber = value; } }

            private Int16 _DiskNumberWithStart;
            /// <summary>number of the disk with the start of the central directory</summary>
            public Int16 DiskNumberWithStart { get { return _DiskNumberWithStart; } set { _DiskNumberWithStart = value; } }

            private Int16 _NumberOfEntriesOnThisDisk;
            /// <summary>total number of entries in the central directory on this disk</summary>
            public Int16 NumberOfEntriesOnThisDisk { get { return _NumberOfEntriesOnThisDisk; } set { _NumberOfEntriesOnThisDisk = value; } }

            private Int16 _NumberOfEntries;
            /// <summary>total number of entries in the central directory</summary>
            public Int16 NumberOfEntries { get { return _NumberOfEntries; } set { _NumberOfEntries = value; } }

            private Int32 _Size;
            /// <summary>size of the central directory</summary>
            public Int32 Size { get { return _Size; } set { _Size = value; } }

            private Int32 _Offset;
            /// <summary>offset of start of central directory with respect to the starting disk number</summary>
            public Int32 Offset { get { return _Offset; } set { _Offset = value; } }

            private String _Comment;
            /// <summary>属性说明</summary>
            public String Comment { get { return _Comment; } set { _Comment = value; } }
            #endregion

            #region 方法
            public void Read(Stream stream)
            {
                BinaryReader reader = new BinaryReader(stream);
                foreach (var item in GetMembers())
                {
                    Object obj = null;
                    switch (Type.GetTypeCode(item.PropertyType))
                    {
                        case TypeCode.Int16:
                            obj = reader.ReadInt16();
                            break;
                        case TypeCode.Int32:
                            obj = reader.ReadInt32();
                            break;
                        case TypeCode.String:
                            Int32 n = reader.ReadInt16();
                            if (n > 0) obj = Encoding.Default.GetString(reader.ReadBytes(n));
                            break;
                        default:
                            break;
                    }
                    PropertyInfoX.Create(item).SetValue(this, obj);
                }
            }

            public void Write(Stream stream)
            {
                BinaryWriter writer = new BinaryWriter(stream);
                foreach (var item in GetMembers())
                {
                    Object obj = PropertyInfoX.Create(item).GetValue(this);
                    switch (Type.GetTypeCode(item.PropertyType))
                    {
                        case TypeCode.Int16:
                            writer.Write((Int16)obj);
                            break;
                        case TypeCode.Int32:
                            writer.Write((Int32)obj);
                            break;
                        case TypeCode.String:
                            if ("" + obj != "")
                            {
                                Byte[] bts = Encoding.Default.GetBytes("" + obj);
                                if (bts.Length > 0)
                                {
                                    writer.Write((Int16)bts.Length);
                                    writer.Write(bts, 0, bts.Length);
                                }
                            }
                            break;
                        default:
                            break;
                    }
                }
            }
            #endregion

            #region 定位
            const Int32 EndOfCentralDirectorySignature = 0x06054b50;

            public static Int64 FindSignature(Stream stream)
            {
                return stream.IndexOf(BitConverter.GetBytes(EndOfCentralDirectorySignature));
            }
            #endregion

            #region 辅助
            static PropertyInfo[] GetMembers()
            {
                return typeof(CentralDirectory).GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.GetProperty | BindingFlags.SetProperty);
            }
            #endregion
        }
        #endregion
    }
}