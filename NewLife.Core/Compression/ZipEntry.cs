#if NET4
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Xml.Serialization;
using NewLife;
using NewLife.Reflection;
using NewLife.Security;
using NewLife.Serialization;

namespace System.IO.Compression
{
    /// <summary>Zip实体。包含文件头信息和文件位置</summary>
    public class ZipEntry : IDisposable
    {
        #region 数据属性
#pragma warning disable 0169, 0649

        /// <summary>签名</summary>
        public UInt32 Signature;

        // ZipDirEntry成员
        /// <summary>系统类型</summary>
        HostSystem VersionMadeBy;

        /// <summary>解压缩所需要的版本</summary>
        public UInt16 VersionNeeded;

        /// <summary>标识位</summary>
        GeneralBitFlags BitField;

        /// <summary>压缩方法</summary>
        public CompressionMethod CompressionMethod;

        private Int32 _LastModified;
        /// <summary>最后修改时间</summary>
        [XmlIgnore]
        public DateTime LastModified { get { return ZipFile.DosDateTimeToFileTime(_LastModified); } set { _LastModified = ZipFile.FileTimeToDosDateTime(value); } }

        /// <summary>CRC校验</summary>
        public UInt32 Crc;

        /// <summary>压缩后大小</summary>
        public UInt32 CompressedSize;

        /// <summary>原始大小</summary>
        public UInt32 UncompressedSize;

        /// <summary>文件名长度</summary>
        private UInt16 FileNameLength;

        /// <summary>扩展数据长度</summary>
        private UInt16 ExtraFieldLength;

        // ZipDirEntry成员
        /// <summary>注释长度</summary>
        private UInt16 CommentLength;

        // ZipDirEntry成员
        /// <summary>分卷号。</summary>
        public UInt16 DiskNumber;

        // ZipDirEntry成员
        /// <summary>内部文件属性</summary>
        public UInt16 InternalFileAttrs;

        // ZipDirEntry成员
        /// <summary>扩展文件属性</summary>
        public UInt32 ExternalFileAttrs;

        // ZipDirEntry成员
        /// <summary>文件头相对位移</summary>
        public UInt32 RelativeOffsetOfLocalHeader;

        /// <summary>文件名，如果是目录，则以/结束</summary>
        [FieldSize("FileNameLength")]
        public String FileName;

        /// <summary>扩展字段</summary>
        [FieldSize("ExtraFieldLength")]
        public Byte[] ExtraField;

        // ZipDirEntry成员
        /// <summary>注释</summary>
        [FieldSize("CommentLength")]
        public String Comment;

#pragma warning restore 0169, 0649
        #endregion

        #region 属性
        /// <summary>是否目录</summary>
        [XmlIgnore]
        public Boolean IsDirectory { get { return ("" + FileName).EndsWith(ZipFile.DirSeparator); } }

        /// <summary>数据源</summary>
        [NonSerialized]
        private IDataSource DataSource;
        #endregion

        #region 构造
        /// <summary>实例化Zip实体</summary>
        public ZipEntry()
        {
            Signature = ZipConstants.ZipEntrySignature;
            VersionNeeded = 20;
        }

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
            //reader.Settings.IgnoreMembers = dirMembers;

            var bn = (reader as Binary);
            if (bn != null) bn.IgnoreMembers = dirMembers;

            // 有时候Zip文件以PK00开头
            if (first)
            {
                if (reader.Read<UInt32>() != ZipConstants.PackedToRemovableMedia) reader.Stream.Position -= 4;
            }

            // 验证头部
            var v = reader.Read<UInt32>();
            if (v != ZipConstants.ZipEntrySignature)
            {
                if (v != ZipConstants.ZipDirEntrySignature && v != ZipConstants.EndOfCentralDirectorySignature)
                    throw new ZipException("0x{0:X8}处签名错误！", stream.Position);

                return null;
            }
            reader.Stream.Position -= 4;

            var entry = reader.Read<ZipEntry>();
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
                entry.Crc = reader.Read<UInt32>();
                entry.CompressedSize = reader.Read<UInt32>();
                entry.UncompressedSize = reader.Read<UInt32>();
            }

            return entry;
        }

        internal static ZipEntry ReadDirEntry(ZipFile zipfile, Stream stream)
        {
            var reader = zipfile.CreateReader(stream);

            var v = reader.Read<UInt32>();
            if (v != ZipConstants.ZipDirEntrySignature)
            {
                if (v != ZipConstants.EndOfCentralDirectorySignature && v != ZipConstants.ZipEntrySignature)
                {
                    throw new ZipException("0x{0:X8}处签名错误！", stream.Position);
                }
                return null;
            }
            reader.Stream.Position -= 4;

            var entry = reader.Read<ZipEntry>();
            return entry;
        }
        #endregion

        #region 写入核心
        internal void Write(IFormatterX writer)
        {
            Signature = ZipConstants.ZipEntrySignature;

            // 取得数据流位置
            RelativeOffsetOfLocalHeader = (UInt32)writer.Stream.Position;

            if (IsDirectory)
            {
                // 写入头部
                writer.Write(this);

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
            writer.Write(this);

            // 没有数据，直接跳过
            if (dsLen <= 0) return;

            #region 写入文件数据
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

                        // 回头重新修正压缩后大小CompressedSize
                        p = writer.Stream.Position;
                        // 计算好压缩大小字段所在位置
                        writer.Stream.Seek(RelativeOffsetOfLocalHeader + 18, SeekOrigin.Begin);
                        //var wr = writer as IWriter2;
                        //wr.Write(CompressedSize);
                        writer.Write(CompressedSize);
                        writer.Stream.Seek(p, SeekOrigin.Begin);
                    }

                    break;
                //case CompressionMethod.LZMA:
                //    {
                //        // 记录数据流位置，待会用来计算已压缩大小
                //        Int64 p = writer.Stream.Position;
                //        source.CompressLzma(writer.Stream, 10);
                //        CompressedSize = (UInt32)(writer.Stream.Position - p);

                //        // 回头重新修正压缩后大小CompressedSize
                //        p = writer.Stream.Position;
                //        // 计算好压缩大小字段所在位置
                //        writer.Stream.Seek(RelativeOffsetOfLocalHeader + 18, SeekOrigin.Begin);
                //        var wr = writer as IWriter2;
                //        wr.Write(CompressedSize);
                //        writer.Stream.Seek(p, SeekOrigin.Begin);
                //    }

                //    break;
                default:
                    throw new XException("无法处理的压缩算法{0}！", CompressionMethod);
            }
            //#if DEBUG
            //            if (ts != null) ts.UseConsole = true;
            //#endif
            #endregion
        }

        internal void WriteDir(IFormatterX writer)
        {
            Signature = ZipConstants.ZipDirEntrySignature;

            // 写入头部
            writer.Write(this);
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
                    //case CompressionMethod.LZMA:
                    //    DataSource.GetData().DecompressLzma(outStream);
                    //    break;
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
        internal static readonly ICollection<String> dirMembers = new HashSet<String>(new String[] {
            "VersionMadeBy", "CommentLength", "DiskNumber", "InternalFileAttrs", "ExternalFileAttrs", "RelativeOffsetOfLocalHeader", "Comment" }, StringComparer.OrdinalIgnoreCase);

        /// <summary>复制DirEntry专属的字段</summary>
        /// <param name="entry"></param>
        internal void CopyFromDirEntry(ZipEntry entry)
        {
            var type = GetType();
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
#endif