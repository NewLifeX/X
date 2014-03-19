using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using NewLife.Exceptions;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;

namespace NewLife.Compression
{
    /// <summary>Zip实体。包含文件头信息和文件位置</summary>
    public class ZipEntry : IDisposable
    {
        #region 数据属性
        private UInt32 _Signature = ZipConstants.ZipEntrySignature;
        /// <summary>签名</summary>
        public UInt32 Signature { get { return _Signature; } private set { _Signature = value; } }

        // ZipDirEntry成员
        private HostSystem _VersionMadeBy;
        /// <summary>系统类型</summary>
        HostSystem VersionMadeBy { get { return _VersionMadeBy; } set { _VersionMadeBy = value; } }

        private UInt16 _VersionNeeded = 20;
        /// <summary>解压缩所需要的版本</summary>
        public UInt16 VersionNeeded { get { return _VersionNeeded; } set { _VersionNeeded = value; } }

        private GeneralBitFlags _BitField;
        /// <summary>标识位</summary>
        GeneralBitFlags BitField { get { return _BitField; } set { _BitField = value; } }

        private CompressionMethod _CompressionMethod;
        /// <summary>压缩方法</summary>
        public CompressionMethod CompressionMethod { get { return _CompressionMethod; } set { _CompressionMethod = value; } }

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
        /// <summary>分卷号。</summary>
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
        /// <summary>文件头相对位移</summary>
        public UInt32 RelativeOffsetOfLocalHeader { get { return _RelativeOffsetOfLocalHeader; } set { _RelativeOffsetOfLocalHeader = value; } }

        [FieldSize("FileNameLength")]
        private String _FileName;
        /// <summary>文件名，如果是目录，则以/结束</summary>
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
        //[NonSerialized]
        //private Int64 _FileDataPosition;
        ///// <summary>文件数据位置</summary>
        //private Int64 FileDataPosition { get { return _FileDataPosition; } set { _FileDataPosition = value; } }

        /// <summary>是否目录</summary>
        public Boolean IsDirectory { get { return ("" + FileName).EndsWith(ZipFile.DirSeparator); } }

        [NonSerialized]
        private IDataSource _DataSource;
        /// <summary>数据源</summary>
        private IDataSource DataSource
        {
            get { return _DataSource; }
            set { _DataSource = value; }
        }
        #endregion

        #region 构造
        internal ZipEntry() { }

        /// <summary>释放资源</summary>
        public void Dispose()
        {
            if (DataSource != null)
            {
                try
                {
                    DataSource.Dispose();
                    DataSource = null;
                }
                catch { }
            }
        }
        #endregion

        #region 读取核心
        internal static ZipEntry ReadEntry(ZipFile zipfile, Stream stream, Boolean first, Boolean embedFileData)
        {
            var reader = zipfile.CreateReader(stream);
            // 读取文件头时忽略掉这些字段，这些都是DirEntry的字段
            reader.Settings.IgnoreMembers = dirMembers;

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
            if (entry.IsDirectory) return entry;

            // 0长度的实体不要设置数据源
            if (entry.CompressedSize > 0)
            {
                // 是否内嵌文件数据
                entry.DataSource = embedFileData ? new ArrayDataSource(stream, (Int32)entry.CompressedSize) : new StreamDataSource(stream, stream.Position, 0);
                entry.DataSource.IsCompressed = entry.CompressionMethod != CompressionMethod.Stored;

                // 移到文件数据之后，可能是文件头
                if (!embedFileData) stream.Seek(entry.CompressedSize, SeekOrigin.Current);
            }

            // 如果有扩展，则跳过
            if (entry.BitField.Has(GeneralBitFlags.Descriptor))
            {
                //stream.Seek(20, SeekOrigin.Current);

                // 在某些只读流中，可能无法回头设置校验和大小，此时可通过描述符在文件内容之后设置
                entry.Crc = reader.ReadUInt32();
                entry.CompressedSize = reader.ReadUInt32();
                entry.UncompressedSize = reader.ReadUInt32();
            }

            return entry;
        }

        internal static ZipEntry ReadDirEntry(ZipFile zipfile, Stream stream)
        {
            var reader = zipfile.CreateReader(stream);

            if (!reader.Expect(ZipConstants.ZipDirEntrySignature))
            {
                if (!reader.Expect(
                    ZipConstants.EndOfCentralDirectorySignature,
                    //ZipConstants.Zip64EndOfCentralDirectoryRecordSignature,
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

        #region 写入核心
        internal void Write(IWriter writer)
        {
            Signature = ZipConstants.ZipEntrySignature;

            // 取得数据流位置
            RelativeOffsetOfLocalHeader = (UInt32)writer.Stream.Position;

            if (IsDirectory)
            {
                // 写入头部
                writer.WriteObject(this);

                return;
            }

            Int32 dsLen = (Int32)DataSource.Length;
            //if (dsLen <= 0) CompressionMethod = CompressionMethod.Stored;

            // 计算签名和大小
            if (Crc == 0) Crc = DataSource.GetCRC();
            if (UncompressedSize == 0) UncompressedSize = (UInt32)dsLen;
            if (CompressionMethod == CompressionMethod.Stored) CompressedSize = UncompressedSize;
            if (DataSource.IsCompressed) CompressedSize = (UInt32)dsLen;

            // 写入头部
            writer.WriteObject(this);

            // 没有数据，直接跳过
            if (dsLen <= 0) return;

            #region 写入文件数据
            //#if DEBUG
            //            var ts = writer.Stream as NewLife.Log.TraceStream;
            //            if (ts != null) ts.UseConsole = false;
            //#endif

            // 数据源。只能用一次，因为GetData的时候把数据流移到了合适位置
            var source = DataSource.GetData();
            if (DataSource.IsCompressed)
            {
                // 可能数据源是曾经被压缩过了的，比如刚解压的实体
                source.CopyTo(writer.Stream, 0, dsLen);
                return;
            }

            switch (CompressionMethod)
            {
                case CompressionMethod.Stored:
                    // 原始数据流直接拷贝到目标。必须指定大小，否则可能读过界
                    source.CopyTo(writer.Stream, 0, dsLen);
                    break;
                case CompressionMethod.Deflated:
                    {
                        // 记录数据流位置，待会用来计算已压缩大小
                        Int64 p = writer.Stream.Position;
                        using (var stream = new DeflateStream(writer.Stream, CompressionMode.Compress, true))
                        {
                            source.CopyTo(stream);
                            stream.Close();
                        }
                        CompressedSize = (UInt32)(writer.Stream.Position - p);
                        //#if DEBUG
                        //                        if (ts != null) ts.UseConsole = true;
                        //#endif

                        // 回头重新修正压缩后大小CompressedSize
                        p = writer.Stream.Position;
                        // 计算好压缩大小字段所在位置
                        writer.Stream.Seek(RelativeOffsetOfLocalHeader + 18, SeekOrigin.Begin);
                        var wr = writer as IWriter2;
                        wr.Write(CompressedSize);
                        writer.Stream.Seek(p, SeekOrigin.Begin);
                    }

                    break;
                case CompressionMethod.LZMA:
                    {
                        // 记录数据流位置，待会用来计算已压缩大小
                        Int64 p = writer.Stream.Position;
                        source.CompressLzma(writer.Stream, 12);
                        CompressedSize = (UInt32)(writer.Stream.Position - p);
                        //#if DEBUG
                        //                        if (ts != null) ts.UseConsole = true;
                        //#endif

                        // 回头重新修正压缩后大小CompressedSize
                        p = writer.Stream.Position;
                        // 计算好压缩大小字段所在位置
                        writer.Stream.Seek(RelativeOffsetOfLocalHeader + 18, SeekOrigin.Begin);
                        var wr = writer as IWriter2;
                        wr.Write(CompressedSize);
                        writer.Stream.Seek(p, SeekOrigin.Begin);
                    }

                    break;
                default:
                    throw new XException("无法处理的压缩算法{0}！", CompressionMethod);
            }
            //#if DEBUG
            //            if (ts != null) ts.UseConsole = true;
            //#endif
            #endregion
        }

        internal void WriteDir(IWriter writer)
        {
            Signature = ZipConstants.ZipDirEntrySignature;

            // 写入头部
            writer.WriteObject(this);
        }
        #endregion

        #region 解压缩
        /// <summary>解压缩</summary>
        /// <param name="path">目标路径</param>
        /// <param name="overrideExisting">是否覆盖已有文件</param>
        public void Extract(String path, Boolean overrideExisting = true)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException("path");

            if (!IsDirectory)
            {
                //if (DataSource == null) throw new ZipException("文件数据不正确！");
                //if (CompressedSize <= 0) throw new ZipException("文件大小不正确！");

                String file = Path.Combine(path, FileName);
                if (!overrideExisting && File.Exists(file)) return;

                path = Path.GetDirectoryName(file);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);

                using (var stream = File.Create(file))
                {
                    // 没有数据直接跳过。放到这里，保证已经创建文件
                    if (DataSource != null && DataSource.Length > 0 && CompressedSize > 0) Extract(stream);
                }

                // 修正时间
                if (LastModified > ZipFile.MinDateTime)
                {
                    FileInfo fi = new FileInfo(file);
                    fi.LastWriteTime = LastModified;
                }
            }
            else
            {
                path = Path.Combine(path, FileName);
                if (!Directory.Exists(path)) Directory.CreateDirectory(path);
            }
        }

        /// <summary>解压缩</summary>
        /// <param name="outStream">目标数据流</param>
        public void Extract(Stream outStream)
        {
            if (outStream == null || !outStream.CanWrite) throw new ArgumentNullException("outStream");
            //if (DataSource == null) throw new ZipException("文件数据不正确！");

            // 没有数据直接跳过
            if (DataSource == null || DataSource.Length <= 0) return;

            if (CompressedSize <= 0) throw new ZipException("文件大小不正确！");

            try
            {
                switch (CompressionMethod)
                {
                    case CompressionMethod.Stored:
                        DataSource.GetData().CopyTo(outStream);
                        break;
                    case CompressionMethod.Deflated:
                        using (var stream = new DeflateStream(DataSource.GetData(), CompressionMode.Decompress, true))
                        {
                            stream.CopyTo(outStream);
                            stream.Close();
                        }
                        break;
                    case CompressionMethod.LZMA:
                        DataSource.GetData().DecompressLzma(outStream);
                        break;
                    default:
                        throw new XException("无法处理的压缩算法{0}！", CompressionMethod);
                }
            }
            catch (Exception ex) { throw new ZipException(String.Format("解压缩{0}时出错！", FileName), ex); }
        }
        #endregion

        #region 压缩
        /// <summary>从文件创建实体</summary>
        /// <param name="fileName"></param>
        /// <param name="entryName"></param>
        /// <param name="stored"></param>
        /// <returns></returns>
        public static ZipEntry Create(String fileName, String entryName = null, Boolean? stored = false)
        {
            if (String.IsNullOrEmpty(entryName))
            {
                if (String.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");

                entryName = Path.GetFileName(fileName);
            }

            IDataSource ds = null;
            if (!entryName.EndsWith(ZipFile.DirSeparator))
            {
                if (String.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");

                ds = new FileDataSource(fileName);
            }

            var entry = Create(entryName, ds, stored);

            // 读取最后修改时间
            if (entry.LastModified <= ZipFile.MinDateTime)
            {
                FileInfo fi = new FileInfo(fileName);
                entry.LastModified = fi.LastWriteTime;
            }

            return entry;
        }

        /// <summary>从数据流创建实体</summary>
        /// <param name="stream"></param>
        /// <param name="entryName"></param>
        /// <param name="stored"></param>
        /// <param name="embedFileData"></param>
        /// <returns></returns>
        public static ZipEntry Create(Stream stream, String entryName, Boolean stored = false, Boolean embedFileData = false)
        {
            //if (stream == null) throw new ArgumentNullException("stream");

            // 有可能从文件流中获取文件名
            String fileName = null;
            if (String.IsNullOrEmpty(entryName) && stream != null && stream is FileStream)
                entryName = fileName = Path.GetFileName((stream as FileStream).Name);

            if (String.IsNullOrEmpty(entryName)) throw new ArgumentNullException("entryName");

            // 是否内嵌文件数据
            IDataSource ds = null;
            if (stream != null) ds = embedFileData ? new ArrayDataSource(stream, 0) : new StreamDataSource(stream, stream.Position, 0);

            var entry = Create(entryName, ds, stored);

            // 读取最后修改时间
            if (!String.IsNullOrEmpty(fileName) && entry.LastModified <= ZipFile.MinDateTime)
            {
                FileInfo fi = new FileInfo(fileName);
                entry.LastModified = fi.LastWriteTime;
            }

            return entry;
        }

        private static ZipEntry Create(String entryName, IDataSource datasource, Boolean? stored)
        {
            if (String.IsNullOrEmpty(entryName)) throw new ArgumentNullException("entryName");
            entryName = entryName.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var entry = new ZipEntry();
            entry.FileName = entryName;
            entry.CompressionMethod = stored ?? IsZip(entryName) ? CompressionMethod.Stored : CompressionMethod.Deflated;
            entry.DataSource = datasource;

            return entry;
        }
        #endregion

        #region 辅助
#if NET4
        internal static readonly ICollection<String> dirMembers = new HashSet<String>(new String[] { "_VersionMadeBy", "_CommentLength", "_DiskNumber", "_InternalFileAttrs", "_ExternalFileAttrs", "_RelativeOffsetOfLocalHeader", "_Comment" }, StringComparer.OrdinalIgnoreCase);
#else
        internal static readonly ICollection<String> dirMembers = new HashSet<String>(new String[] { "_VersionMadeBy", "_CommentLength", "_DiskNumber", "_InternalFileAttrs", "_ExternalFileAttrs", "_RelativeOffsetOfLocalHeader", "_Comment" }, StringComparer.OrdinalIgnoreCase) { IsReadOnly = true };
#endif
        /// <summary>复制DirEntry专属的字段</summary>
        /// <param name="entry"></param>
        internal void CopyFromDirEntry(ZipEntry entry)
        {
            Type type = this.GetType();
            foreach (var item in dirMembers)
            {
                this.SetValue(item, entry.GetValue(item));
            }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return FileName; }

        static String[] zips = new String[] { ".zip", ".rar", ".iso" };
        static Boolean IsZip(String name)
        {
            String ext = Path.GetExtension(name);
            if (String.IsNullOrEmpty(ext)) return false;

            ext = ext.ToLower();
            return Array.IndexOf(zips, ext) >= 0;
        }
        #endregion

        #region 数据源
        interface IDataSource : IDisposable
        {
            Stream GetData();

            UInt32 GetCRC();

            Int64 Length { get; }

            Boolean IsCompressed { get; set; }
        }

        #region 数据流
        class StreamDataSource : /*DisposeBase, */IDataSource
        {
            private Stream _Stream;
            /// <summary>数据流</summary>
            public virtual Stream Stream
            {
                get { return _Stream; }
                set { _Stream = value; }
            }

            private Int64 _Offset;
            /// <summary>位移</summary>
            public Int64 Offset
            {
                get { return _Offset; }
                set { _Offset = value; }
            }

            private Int64 _Length;
            /// <summary>长度</summary>
            public Int64 Length
            {
                get { return _Length > 0 ? _Length : Stream.Length; }
                set { _Length = value; }
            }

            private Boolean _IsCompressed;
            /// <summary>是否被压缩</summary>
            public Boolean IsCompressed
            {
                get { return _IsCompressed; }
                set { _IsCompressed = value; }
            }

            public StreamDataSource() { }

            public StreamDataSource(Stream stream, Int64 offset, Int64 length)
            {
                Stream = stream;
                Offset = offset;
                Length = length;
            }

            public void Dispose()
            {
                if (_Stream != null)
                {
                    try
                    {
                        _Stream.Dispose();
                        _Stream = null;
                    }
                    catch { }
                }
            }

            #region IDataSource 成员

            public Stream GetData()
            {
                Stream.Seek(Offset, SeekOrigin.Begin);
                return Stream;
            }

            public uint GetCRC()
            {
                Stream.Seek(Offset, SeekOrigin.Begin);
                return new Crc32().Update(Stream, Length).Value;
            }
            #endregion
        }
        #endregion

        #region 字节数组
        class ArrayDataSource : StreamDataSource
        {
            private Byte[] _Buffer;
            /// <summary>字节数组</summary>
            public Byte[] Buffer
            {
                get { return _Buffer; }
                set { _Buffer = value; }
            }

            /// <summary>数据流</summary>
            public override Stream Stream { get { return base.Stream ?? (base.Stream = new MemoryStream(_Buffer)); } set { base.Stream = value; } }

            public ArrayDataSource(Byte[] buffer) { Buffer = buffer; }

            public ArrayDataSource(Stream stream, Int32 length) : this(stream.ReadBytes(length)) { }
        }
        #endregion

        #region 文件
        class FileDataSource : StreamDataSource
        {
            private String _FileName;
            /// <summary>文件名</summary>
            public String FileName
            {
                get { return _FileName; }
                set { _FileName = value; }
            }

            /// <summary>数据流</summary>
            public override Stream Stream { get { return base.Stream ?? (base.Stream = new FileStream(FileName, FileMode.Open, FileAccess.Read)); } set { base.Stream = value; } }

            public FileDataSource(String fileName) { FileName = fileName; }
        }
        #endregion
        #endregion
    }
}