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

        private UInt16 _VersionNeeded;
        /// <summary>解压缩所需要的版本</summary>
        public UInt16 VersionNeeded { get { return _VersionNeeded; } set { _VersionNeeded = value; } }

        private UInt16 _BitField;
        /// <summary>标识位</summary>
        public UInt16 BitField { get { return _BitField; } set { _BitField = value; } }

        private CompressionMethod _CompressionMethod;
        /// <summary>压缩方法</summary>
        public CompressionMethod CompressionMethod { get { return _CompressionMethod; } set { _CompressionMethod = value; } }

        private Int32 _LastModifiedData;
        ///// <summary>最后修改时间</summary>
        //private Int32 LastModifiedData { get { return _LastModifiedData; } set { _LastModifiedData = value; } }

        //[NonSerialized]
        //private DateTime _LastModified;
        /// <summary>最后修改时间</summary>
        public DateTime LastModified { get { return ZipFile.DosDateTimeToFileTime(_LastModifiedData); } set { _LastModifiedData = ZipFile.FileTimeToDosDateTime(value); } }

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

        [FieldSize("FileNameLength")]
        private String _FileName;
        /// <summary>文件名</summary>
        public String FileName { get { return _FileName; } set { _FileName = value; } }

        [FieldSize("ExtraFieldLength")]
        private Byte[] _ExtraField;
        /// <summary>扩展字段</summary>
        public Byte[] ExtraField { get { return _ExtraField; } set { _ExtraField = value; } }
        #endregion

        #region 属性
        [NonSerialized]
        private String _Comment;
        /// <summary>注释</summary>
        public String Comment { get { return _Comment; } set { _Comment = value; } }

        [NonSerialized]
        private Boolean _IsDirectory;
        /// <summary>是否目录</summary>
        public Boolean IsDirectory { get { return _IsDirectory; } set { _IsDirectory = value; } }
        #endregion

        #region ZipDirEntry
        class ZipDirEntry
        {
            #region 数据属性
            private UInt32 _Signature = ZipConstants.ZipEntrySignature;
            /// <summary>签名</summary>
            public UInt32 Signature { get { return _Signature; } set { _Signature = value; } }

            private UInt16 _VersionMadeBy;
            /// <summary>属性说明</summary>
            public UInt16 VersionMadeBy { get { return _VersionMadeBy; } set { _VersionMadeBy = value; } }

            private UInt16 _VersionNeeded;
            /// <summary>属性说明</summary>
            public UInt16 VersionNeeded { get { return _VersionNeeded; } set { _VersionNeeded = value; } }

            private UInt16 _BitField;
            /// <summary>属性说明</summary>
            public UInt16 BitField { get { return _BitField; } set { _BitField = value; } }

            private UInt16 _CompressionMethod;
            /// <summary>属性说明</summary>
            public UInt16 CompressionMethod { get { return _CompressionMethod; } set { _CompressionMethod = value; } }

            private UInt16 _TimeBlob;
            /// <summary>属性说明</summary>
            public UInt16 TimeBlob { get { return _TimeBlob; } set { _TimeBlob = value; } }

            private DateTime _LastModified;
            /// <summary>属性说明</summary>
            public DateTime LastModified { get { return _LastModified; } set { _LastModified = value; } }

            private UInt32 _Crc;
            /// <summary>属性说明</summary>
            public UInt32 Crc { get { return _Crc; } set { _Crc = value; } }

            private UInt32 _CompressedSize;
            /// <summary>压缩后大小</summary>
            public UInt32 CompressedSize { get { return _CompressedSize; } set { _CompressedSize = value; } }

            private UInt32 _UncompressedSize;
            /// <summary>原始大小</summary>
            public UInt32 UncompressedSize { get { return _UncompressedSize; } set { _UncompressedSize = value; } }

            private UInt16 _DiskNumber;
            /// <summary>属性说明</summary>
            public UInt16 DiskNumber { get { return _DiskNumber; } set { _DiskNumber = value; } }

            private UInt16 _InternalFileAttrs;
            /// <summary>内部文件属性</summary>
            public UInt16 InternalFileAttrs { get { return _InternalFileAttrs; } set { _InternalFileAttrs = value; } }

            private UInt32 _ExternalFileAttrs;
            /// <summary>扩展文件属性</summary>
            public UInt32 ExternalFileAttrs { get { return _ExternalFileAttrs; } set { _ExternalFileAttrs = value; } }

            private UInt32 _RelativeOffsetOfLocalHeader;
            /// <summary>属性说明</summary>
            public UInt32 RelativeOffsetOfLocalHeader { get { return _RelativeOffsetOfLocalHeader; } set { _RelativeOffsetOfLocalHeader = value; } }
            #endregion
        }
        #endregion

        #region 读取核心
        internal static ZipEntry ReadEntry(ZipFile zipfile, Stream stream, bool first)
        {
            var reader = ZipFile.CreateReader(stream);

            ZipEntry entry = new ZipEntry();

            // 有时候Zip文件以PK00开头
            if (first && reader.Expect(ZipConstants.PackedToRemovableMedia)) reader.ReadBytes(4);

            // 验证头部
            if (!reader.Expect(ZipConstants.ZipEntrySignature))
            {
                if (!reader.Expect(ZipConstants.ZipDirEntrySignature, ZipConstants.EndOfCentralDirectorySignature))
                    throw new ZipException("0x{0:X8}处签名错误！", stream.Position);

                return null;
            }

            var ze = reader.ReadObject<ZipEntry>();
            return ze;

            //// Store the position in the stream for this entry
            //// change for workitem 8098
            //entry.__FileDataPosition = entry.ArchiveStream.Position;

            //// seek past the data without reading it. We will read on Extract()
            //stream.Seek(entry._CompressedFileDataSize + entry._LengthOfTrailer, SeekOrigin.Current);

            //// ReadHeader moves the file pointer to the end of the entry header,
            //// as well as any encryption header.

            //// CompressedFileDataSize includes:
            ////   the maybe compressed, maybe encrypted file data
            ////   the encryption trailer, if any
            ////   the bit 3 descriptor, if any

            //// workitem 5306
            //// http://www.codeplex.com/DotNetZip/WorkItem/View.aspx?WorkItemId=5306
            //HandleUnexpectedDataDescriptor(entry);

            return entry;
        }

        internal static ZipEntry ReadDirEntry(ZipFile zipfile, Stream stream, Dictionary<String, Object> previouslySeen)
        {
            var reader = ZipFile.CreateReader(stream);

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

            //int bytesRead = 42 + 4;
            //byte[] block = new byte[42];
            //int n = s.Read(block, 0, block.Length);
            //if (n != block.Length) return null;

            //int i = 0;
            var zde = reader.ReadObject<ZipEntry>();
            return null;

            //// workitem 10330
            //// insure unique entry names
            //while (previouslySeen.ContainsKey(zde._FileNameInArchive))
            //{
            //    zde._FileNameInArchive = CopyHelper.AppendCopyToFileName(zde._FileNameInArchive);
            //    zde._metadataChanged = true;
            //}

            //if (zde.AttributesIndicateDirectory)
            //    zde.MarkAsDirectory();  // may append a slash to filename if nec.
            //// workitem 6898
            //else if (zde._FileNameInArchive.EndsWith("/")) zde.MarkAsDirectory();

            //zde._CompressedFileDataSize = zde._CompressedSize;
            //if ((zde._BitField & 0x01) == 0x01)
            //{
            //    // this may change after processing the Extra field
            //    zde._Encryption_FromZipFile = zde._Encryption =
            //        EncryptionAlgorithm.PkzipWeak;
            //    zde._sourceIsEncrypted = true;
            //}

            //if (zde._extraFieldLength > 0)
            //{
            //    zde._InputUsesZip64 = (zde._CompressedSize == 0xFFFFFFFF ||
            //          zde._UncompressedSize == 0xFFFFFFFF ||
            //          zde._RelativeOffsetOfLocalHeader == 0xFFFFFFFF);

            //    // Console.WriteLine("  Input uses Z64?:      {0}", zde._InputUsesZip64);

            //    bytesRead += zde.ProcessExtraField(s, zde._extraFieldLength);
            //    zde._CompressedFileDataSize = zde._CompressedSize;
            //}

            //// we've processed the extra field, so we know the encryption method is set now.
            //if (zde._Encryption == EncryptionAlgorithm.PkzipWeak)
            //{
            //    // the "encryption header" of 12 bytes precedes the file data
            //    zde._CompressedFileDataSize -= 12;
            //}

            //// tally the trailing descriptor
            //if ((zde._BitField & 0x0008) == 0x0008)
            //{
            //    // sig, CRC, Comp and Uncomp sizes
            //    if (zde._InputUsesZip64)
            //        zde._LengthOfTrailer += 24;
            //    else
            //        zde._LengthOfTrailer += 16;
            //}

            //// workitem 12744
            //zde.AlternateEncoding = ((zde._BitField & 0x0800) == 0x0800)
            //    ? Encoding.UTF8
            //    : expectedEncoding;

            //zde.AlternateEncodingUsage = ZipOption.Always;

            //if (zde._commentLength > 0)
            //{
            //    block = new byte[zde._commentLength];
            //    n = s.Read(block, 0, block.Length);
            //    bytesRead += n;
            //    if ((zde._BitField & 0x0800) == 0x0800)
            //    {
            //        // UTF-8 is in use
            //        zde._Comment = Ionic.Zip.SharedUtilities.Utf8StringFromBuffer(block);
            //    }
            //    else
            //    {
            //        zde._Comment = Ionic.Zip.SharedUtilities.StringFromBuffer(block, expectedEncoding);
            //    }
            //}
            //zde._LengthOfDirEntry = bytesRead;
            //return zde;
            return null;
        }
        #endregion
    }
}