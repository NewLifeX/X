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
        readonly HostSystem VersionMadeBy;

        /// <summary>解压缩所需要的版本</summary>
        public UInt16 VersionNeeded;

        /// <summary>标识位</summary>
        readonly GeneralBitFlags BitField;

        /// <summary>压缩方法</summary>
        public CompressionMethod CompressionMethod;

        private Int32 _LastModified;
        /// <summary>最后修改时间</summary>
        [XmlIgnore]
        public DateTime LastModified { get => ZipFile.DosDateTimeToFileTime(_LastModified); set => _LastModified = ZipFile.FileTimeToDosDateTime(value); }

        /// <summary>CRC校验</summary>
        public UInt32 Crc;

        /// <summary>压缩后大小</summary>
        public UInt32 CompressedSize;

        /// <summary>原始大小</summary>
        public UInt32 UncompressedSize;

        /// <summary>文件名长度</summary>
        private readonly UInt16 FileNameLength;

        /// <summary>扩展数据长度</summary>
        private readonly UInt16 ExtraFieldLength;

        // ZipDirEntry成员
        /// <summary>注释长度</summary>
        private readonly UInt16 CommentLength;

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
        public Boolean IsDirectory => ("" + FileName).EndsWith(ZipFile.DirSeparator);

        /// <summary>数据源</summary>
        [NonSerialized]
        private IDataSource DataSource;

        /// <summary>全名</summary>
        public String FullName => FileName;

        /// <summary>长度</summary>
        public Int32 Length => ExtraFieldLength;
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
                entry.DataSource = embedFileData ? new ArrayDataSource(stream, (Int32)entry.CompressedSize) : new StreamDataSource(stream);
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

            var dsLen = (Int32)DataSource.Length;
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
                        var p = writer.Stream.Position;
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
                default:
                    throw new XException("无法处理的压缩算法{0}！", CompressionMethod);
            }
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
        /// <param name="destinationFileName">目标路径</param>
        /// <param name="overrideExisting">是否覆盖已有文件</param>
        public void ExtractToFile(String destinationFileName, Boolean overrideExisting = true)
        {
            if (destinationFileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(destinationFileName));

            if (!IsDirectory)
            {
                //if (DataSource == null) throw new ZipException("文件数据不正确！");
                //if (CompressedSize <= 0) throw new ZipException("文件大小不正确！");

                var file = destinationFileName.GetFullPath();
                if (!overrideExisting && File.Exists(file)) return;

                file.EnsureDirectory(true);

                using (var stream = File.Create(file))
                {
                    // 没有数据直接跳过。放到这里，保证已经创建文件
                    if (DataSource != null && DataSource.Length > 0 && CompressedSize > 0) Extract(stream);
                }

                // 修正时间
                if (LastModified > ZipFile.MinDateTime)
                {
                    var fi = new FileInfo(file)
                    {
                        LastWriteTime = LastModified
                    };
                }
            }
            else
            {
                destinationFileName = Path.Combine(destinationFileName, FileName);
                if (!Directory.Exists(destinationFileName)) Directory.CreateDirectory(destinationFileName);
            }
        }

        /// <summary>解压缩</summary>
        /// <param name="outStream">目标数据流</param>
        public void Extract(Stream outStream)
        {
            if (outStream == null || !outStream.CanWrite) throw new ArgumentNullException(nameof(outStream));
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
            if (entryName.IsNullOrEmpty())
            {
                if (fileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(fileName));

                entryName = Path.GetFileName(fileName);
            }

            IDataSource ds = null;
            if (!entryName.EndsWith(ZipFile.DirSeparator))
            {
                if (fileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(fileName));

                ds = new FileDataSource(fileName);
            }

            var entry = Create(entryName, ds, stored);

            // 读取最后修改时间
            if (entry.LastModified <= ZipFile.MinDateTime)
            {
                var fi = new FileInfo(fileName);
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

            if (entryName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(entryName));

            // 是否内嵌文件数据
            IDataSource ds = null;
            if (stream != null) ds = embedFileData ? new ArrayDataSource(stream, -1) : new StreamDataSource(stream);

            var entry = Create(entryName, ds, stored);

            // 读取最后修改时间
            if (!String.IsNullOrEmpty(fileName) && entry.LastModified <= ZipFile.MinDateTime)
            {
                var fi = new FileInfo(fileName);
                entry.LastModified = fi.LastWriteTime;
            }

            return entry;
        }

        private static ZipEntry Create(String entryName, IDataSource datasource, Boolean? stored)
        {
            if (entryName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(entryName));
            entryName = entryName.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            var entry = new ZipEntry
            {
                FileName = entryName,
                CompressionMethod = stored ?? IsZip(entryName) ? CompressionMethod.Stored : CompressionMethod.Deflated,
                DataSource = datasource
            };

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
            foreach (var item in dirMembers)
            {
                this.SetValue(item, entry.GetValue(item));
            }
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => FileName;

        static readonly String[] zips = new String[] { ".zip", ".rar", ".iso" };
        static Boolean IsZip(String name)
        {
            var ext = Path.GetExtension(name);
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
        class StreamDataSource : IDataSource
        {
            /// <summary>数据流</summary>
            public virtual Stream Stream { get; protected set; }

            /// <summary>位移</summary>
            public Int64 Offset { get; }

            /// <summary>长度</summary>
            public Int64 Length { get; }

            /// <summary>是否被压缩</summary>
            public Boolean IsCompressed { get; set; }

            public StreamDataSource(Stream stream)
            {
                Stream = stream;
                Offset = stream.Position;
                Length = stream.Length;
            }

            public void Dispose() => Stream.TryDispose();

            #region IDataSource 成员
            public Stream GetData()
            {
                Stream.Seek(Offset, SeekOrigin.Begin);
                return Stream;
            }

            public UInt32 GetCRC()
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
            public ArrayDataSource(Stream stream, Int32 length) : base(new MemoryStream(stream.ReadBytes(length))) { }
        }
        #endregion

        #region 文件
        class FileDataSource : StreamDataSource
        {
            /// <summary>文件名</summary>
            public String FileName { get; set; }

            public FileDataSource(String fileName) : base(new FileStream(fileName, FileMode.Open, FileAccess.Read)) => FileName = fileName;
        }
        #endregion
        #endregion
    }
}
#endif