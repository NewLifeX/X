using System;
using System.IO;
using System.Text;
using System.Collections.Generic;
using NewLife.Serialization;

namespace NewLife.Compression
{
    /// <summary>Zip实体</summary>
    public class ZipEntry
    {
        #region 数据属性
        private UInt32 _Signature = ZipConstants.ZipEntrySignature;
        /// <summary>签名</summary>
        public UInt32 Signature { get { return _Signature; } private set { _Signature = value; } }

        // ZipDirEntry成员
        private HostSystemID _VersionMadeBy;
        /// <summary>属性说明</summary>
        public HostSystemID VersionMadeBy { get { return _VersionMadeBy; } set { _VersionMadeBy = value; } }

        private UInt16 _VersionNeeded;
        /// <summary>解压缩所需要的版本</summary>
        public UInt16 VersionNeeded { get { return _VersionNeeded; } set { _VersionNeeded = value; } }

        private GeneralBitFlags _BitField;
        /// <summary>标识位</summary>
        public GeneralBitFlags BitField { get { return _BitField; } set { _BitField = value; } }

        private CompressionMethod _CompressionMethod;
        /// <summary>压缩方法</summary>
        public CompressionMethod CompressionMethod { get { return _CompressionMethod; } set { _CompressionMethod = value; } }

        //// ZipDirEntry成员
        //private UInt16 _TimeBlob;
        ///// <summary>属性说明</summary>
        //public UInt16 TimeBlob { get { return _TimeBlob; } set { _TimeBlob = value; } }

        private Int32 _LastModified;
        /// <summary>最后修改时间</summary>
        public DateTime LastModified { get { return ZipFile.DosDateTimeToFileTime(_LastModified); } set { _LastModified = ZipFile.FileTimeToDosDateTime(value); } }

        private UInt32 _Crc;
        /// <summary>CRC校验</summary>
        public UInt32 Crc { get { return _Crc; } set { _Crc = value; } }

        private UInt32 _CompressedSize;
        /// <summary>压缩后大小</summary>
        public UInt32 CompressedSize { get { return _CompressedSize; } set { _CompressedSize = value; } }

        private UInt32 _UncompressedSize;
        /// <summary>原始大小</summary>
        public UInt32 UncompressedSize { get { return _UncompressedSize; } set { _UncompressedSize = value; } }

        private UInt16 _FileNameLength;
        /// <summary>文件名长度</summary>
        private UInt16 FileNameLength { get { return _FileNameLength; } set { _FileNameLength = value; } }

        private UInt16 _ExtraFieldLength;
        /// <summary>扩展数据长度</summary>
        private UInt16 ExtraFieldLength { get { return _ExtraFieldLength; } set { _ExtraFieldLength = value; } }

        // ZipDirEntry成员
        private UInt16 _CommentLength;
        /// <summary>注释长度</summary>
        private UInt16 CommentLength { get { return _CommentLength; } set { _CommentLength = value; } }

        // ZipDirEntry成员
        private UInt16 _DiskNumber;
        /// <summary>属性说明</summary>
        public UInt16 DiskNumber { get { return _DiskNumber; } set { _DiskNumber = value; } }

        // ZipDirEntry成员
        private UInt16 _InternalFileAttrs;
        /// <summary>内部文件属性</summary>
        public UInt16 InternalFileAttrs { get { return _InternalFileAttrs; } set { _InternalFileAttrs = value; } }

        // ZipDirEntry成员
        private UInt32 _ExternalFileAttrs;
        /// <summary>扩展文件属性</summary>
        public UInt32 ExternalFileAttrs { get { return _ExternalFileAttrs; } set { _ExternalFileAttrs = value; } }

        // ZipDirEntry成员
        private UInt32 _RelativeOffsetOfLocalHeader;
        /// <summary>属性说明</summary>
        public UInt32 RelativeOffsetOfLocalHeader { get { return _RelativeOffsetOfLocalHeader; } set { _RelativeOffsetOfLocalHeader = value; } }

        [FieldSize("FileNameLength")]
        private String _FileName;
        /// <summary>文件名</summary>
        public String FileName { get { return _FileName; } set { _FileName = value; } }

        [FieldSize("ExtraFieldLength")]
        private Byte[] _ExtraField;
        /// <summary>扩展字段</summary>
        public Byte[] ExtraField { get { return _ExtraField; } set { _ExtraField = value; } }

        // ZipDirEntry成员
        [FieldSize("CommentLength")]
        private String _Comment;
        /// <summary>注释</summary>
        public String Comment { get { return _Comment; } set { _Comment = value; } }
        #endregion

        #region 属性
        [NonSerialized]
        private Int64 _FileDataPosition;
        /// <summary>文件数据位置</summary>
        public Int64 FileDataPosition { get { return _FileDataPosition; } set { _FileDataPosition = value; } }

        //[NonSerialized]
        //private Boolean _IsDirectory;
        /// <summary>是否目录</summary>
        public Boolean IsDirectory { get { return ("" + FileName).EndsWith("/"); } /*set { _IsDirectory = value; }*/ }
        #endregion

        #region 读取核心
        internal static ZipEntry ReadEntry(ZipFile zipfile, Stream stream, bool first)
        {
            var reader = zipfile.CreateReader(stream);
            var names = new String[] { "_VersionMadeBy", "_CommentLength", "_DiskNumber", "_InternalFileAttrs", "_ExternalFileAttrs", "_RelativeOffsetOfLocalHeader", "_Comment" };
            foreach (var item in names)
            {
                reader.Settings.IgnoreMembers.Add(item);
            }

            // 有时候Zip文件以PK00开头
            if (first && reader.Expect(ZipConstants.PackedToRemovableMedia)) reader.ReadBytes(4);

            // 验证头部
            if (!reader.Expect(ZipConstants.ZipEntrySignature))
            {
                if (!reader.Expect(ZipConstants.ZipDirEntrySignature, ZipConstants.EndOfCentralDirectorySignature))
                    throw new ZipException("0x{0:X8}处签名错误！", stream.Position);

                return null;
            }

            var entry = reader.ReadObject<ZipEntry>();
            entry.FileDataPosition = stream.Position;
            // 如果有扩展，则跳过，20字节
            if (entry.BitField.Has(GeneralBitFlags.Descriptor)) stream.Seek(20, SeekOrigin.Current);

            // 移到文件数据之后，可能是文件头
            stream.Seek(entry.CompressedSize, SeekOrigin.Current);

            return entry;
        }

        internal static ZipEntry ReadDirEntry(ZipFile zipfile, Stream stream)
        {
            var reader = zipfile.CreateReader(stream);

            if (!reader.Expect(ZipConstants.ZipDirEntrySignature))
            {
                if (!reader.Expect(
                    ZipConstants.EndOfCentralDirectorySignature,
                    ZipConstants.Zip64EndOfCentralDirectoryRecordSignature,
                    ZipConstants.ZipEntrySignature))
                {
                    throw new ZipException("0x{0:X8}处签名错误！", stream.Position);
                }
                return null;
            }

            var entry = reader.ReadObject<ZipEntry>();
            return entry;
        }
        #endregion
    }
}