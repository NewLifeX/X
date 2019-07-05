#if NET4
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife;
using NewLife.Serialization;

namespace System.IO.Compression
{
    /// <summary>Zip文件</summary>
    /// <remarks>
    /// Zip定义位于 <a target="_blank" href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">http://www.pkware.com/documents/casestudies/APPNOTE.TXT</a>
    /// 
    /// 本程序只支持Zip基本功能，不支持加密和Zip64（用于超过2G的文件压缩）。
    /// 
    /// 基本常识：GZip/Deflate仅仅是数据压缩算法，只负责压缩一组数据；而Zip仅仅是一种打包用的文件格式，指示多个被压缩后的文件如何组合在一起形成一个压缩包，当然，这些被压缩的文件除了Deflate算法还可能有其它算法。
    /// 
    /// 核心原理：通过二进制序列化框架，实现Zip格式的解析，数据的压缩和解压缩由系统的DeflateStream完成！
    /// 
    /// 关于压缩算法：系统的DeflateStream实现了Deflate压缩算法，但是硬编码了四级压缩（共十级，第四级在快速压缩中压缩率最高）。相关硬编码位于内嵌的FastEncoderWindow类中。
    /// 
    /// 感谢@小董（1287263703）、@Johnses（285732917）的热心帮忙，发现了0字节文件压缩和解压缩的BUG！
    /// </remarks>
    /// <example>
    /// 标准压缩：
    /// <code>
    /// using (ZipFile zf = new ZipFile())
    /// {
    ///     zf.AddDirectory("TestZip");
    /// 
    ///     using (var fs = File.Create("ab.zip"))
    ///     {
    ///         zf.Write(fs);
    ///     }
    /// }
    /// </code>
    /// 
    /// 标准解压缩：
    /// <code>
    /// using (ZipFile zf = new ZipFile(file))
    /// {
    ///     zf.Extract("TestZip");
    /// }
    /// </code>
    /// 
    /// 快速压缩：
    /// <code>
    /// ZipFile.CompressFile("aa.doc");
    /// ZipFile.CompressDirectory("TestZip");
    /// </code>
    /// 
    /// 快速解压缩：
    /// <code>
    /// ZipFile.Extract("aa.zip", "Test");
    /// </code>
    /// </example>
    public partial class ZipFile : IDisposable, IEnumerable, IEnumerable<ZipEntry>
    {
        #region 属性
        private String _Name;
        /// <summary>名称</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private String _Comment;
        /// <summary>注释</summary>
        public String Comment { get { EnsureRead(); return _Comment; } set { _Comment = value; } }

        private Encoding _Encoding;
        /// <summary>字符串编码</summary>
        public Encoding Encoding { get { return _Encoding ?? Encoding.UTF8; } set { _Encoding = value; } }

        private Boolean _UseDirectory;
        /// <summary>是否使用目录。不使用目录可以减少一点点文件大小，网络上的压缩包也这么做，但是Rar压缩的使用了目录</summary>
        public Boolean UseDirectory { get { return _UseDirectory; } set { _UseDirectory = value; } }

        private Int64 _EmbedFileDataMaxSize = 10 * 1024 * 1024;
        /// <summary>内嵌文件数据最大大小。小于该大小的文件将加载到内存中，否则保持文件流连接，直到读写文件。默认10M</summary>
        public Int64 EmbedFileDataMaxSize { get { return _EmbedFileDataMaxSize; } set { _EmbedFileDataMaxSize = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个Zip文件对象</summary>
        public ZipFile() { }

        /// <summary>实例化一个Zip文件对象。延迟到第一次使用<see cref="Entries"/>时读取</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="encoding">字符串编码</param>
        public ZipFile(String fileName, Encoding encoding = null)
        {
            Name = fileName;
            Encoding = encoding;
            _file = fileName;
        }

        /// <summary>实例化一个Zip文件对象。延迟到第一次使用<see cref="Entries"/>时读取</summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        public ZipFile(Stream stream, Encoding encoding = null)
        {
            Encoding = encoding;
            _stream = stream;
        }

        /// <summary>释放资源</summary>
        public void Dispose()
        {
            var entries = _Entries;
            _Entries = null;
            if (entries != null && entries.Count > 0)
            {
                // 是否所有实体，因为里面可能含有数据流
                entries.Values.TryDispose();
                entries.Clear();
            }
        }
        #endregion

        #region 读取
        String _file;
        Stream _stream;

        /// <summary>使用文件和数据流时，延迟到第一次使用<see cref="Entries"/>时读取</summary>
        void EnsureRead()
        {
            // 这里必须把_file=null这行注释，否则进不去，不知道为什么
            if (!_file.IsNullOrWhiteSpace())
            {
                var f = _file;
                _file = null;
                var fs = File.OpenRead(f);
#if !DEBUG
                try
#endif
                {
                    Read(fs);
                }
#if !DEBUG
                catch (Exception ex)
                {
                    fs.Dispose();
                    throw new ZipException("不是有效的Zip格式！", ex);
                }
#endif

                if (fs.Length < EmbedFileDataMaxSize) fs.Dispose();
            }
            else if (_stream != null)
            {
                var fs = _stream;
                _stream = null;
                try
                {
                    Read(fs);
                }
                catch (Exception ex)
                {
                    throw new ZipException("不是有效的Zip格式！", ex);
                }
            }
        }

        /// <summary>从数据流中读取Zip格式数据</summary>
        /// <param name="stream">数据流</param>
        /// <param name="embedFileData">
        /// 当前读取仅读取文件列表等信息，如果设置内嵌数据，则同时把文件数据读取到内存中；否则，在解压缩时需要再次使用数据流。
        /// 如果外部未指定是否内嵌文件数据，则根据数据流是否小于10M来决定是否内嵌。
        /// </param>
        public void Read(Stream stream, Boolean? embedFileData = null)
        {
            // 如果外部未指定是否内嵌文件数据，则根据数据流是否小于10M来决定是否内嵌
            var embedfile = embedFileData ?? stream.Length < EmbedFileDataMaxSize;

            ZipEntry e;
            bool firstEntry = true;
            while ((e = ZipEntry.ReadEntry(this, stream, firstEntry, embedfile)) != null)
            {
                var name = e.FileName;
                // 特殊处理目录结构
                if (UseDirectory)
                {
                    var dir = name;
                    // 递归多级目录
                    while (!String.IsNullOrEmpty(dir))
                    {
                        var p = dir.LastIndexOf(DirSeparator);
                        if (p <= 0) break;

                        dir = dir.Substring(0, p);
                        if (!Entries.ContainsKey(dir + DirSeparator))
                        {
                            var de = new ZipEntry();
                            // 必须包含分隔符，因为这样才能被识别为目录
                            de.FileName = dir + DirSeparator;
                            Entries.Add(de.FileName, de);
                        }
                    }
                }

                var n = 2;
                while (Entries.ContainsKey(name)) { name = e.FileName + "" + n++; }
                Entries.Add(name, e);
                firstEntry = false;

                if (!UseDirectory && e.IsDirectory) UseDirectory = true;
            }

            // 读取目录结构，但是可能有错误，需要屏蔽
            try
            {
                var reader = CreateReader(stream);

                // 根据签名寻找CentralDirectory，因为文件头数据之后可能有加密相关信息
                //if (stream.IndexOf(BitConverter.GetBytes(ZipConstants.ZipDirEntrySignature)) >= 0)
                {
                    ZipEntry de;
                    while ((de = ZipEntry.ReadDirEntry(this, stream)) != null)
                    {
                        e = Entries[de.FileName];
                        if (e != null)
                        {
                            //e.Comment = de.Comment;
                            //e.IsDirectory = de.IsDirectory;
                            e.CopyFromDirEntry(de);
                        }
                    }

                    // 这里应该是数字签名
                    if (reader.Read<UInt32>() == ZipConstants.DigitalSignature)
                    {
                        // 抛弃数据
                        var n = reader.Read<UInt16>();
                        if (n > 0) reader.Stream.Position += n;
                    }
                    else
                        reader.Stream.Position -= 4;
                }

                // 读取目录结构尾记录
                var v = reader.Read<UInt32>();
                if (v == ZipConstants.EndOfCentralDirectorySignature)
                {
                    reader.Stream.Position -= 4;
                    var ecd = reader.Read<EndOfCentralDirectory>();
                    if (!String.IsNullOrEmpty(ecd.Comment)) Comment = ecd.Comment.TrimEnd('\0');
                }
            }
            catch (ZipException) { }
            catch (IOException) { }
        }
        #endregion

        #region 写入
        /// <summary>把Zip格式数据写入到数据流中</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");
            if (Entries.Count < 1) throw new ZipException("没有添加任何文件！");

            var writer = CreateWriter(stream);
            //writer.Settings.IgnoreMembers = null;
            //// 写入文件头时忽略掉这些字段，这些都是DirEntry的字段
            //writer.Settings.IgnoreMembers = ZipEntry.dirMembers;

            var bn = writer as Binary;
            if (bn != null) bn.IgnoreMembers = ZipEntry.dirMembers;

            foreach (var item in Entries.Values)
            {
                if (UseDirectory || !item.IsDirectory) item.Write(writer);
            }

            var ecd = new EndOfCentralDirectory();
            ecd.Offset = (UInt32)writer.Stream.Position;

            //writer.Settings.IgnoreMembers = null;
            Int32 num = 0;
            foreach (var item in Entries.Values)
            {
                // 每一个都需要写目录项
                if (UseDirectory || !item.IsDirectory)
                {
                    item.WriteDir(writer);
                    num++;
                }
            }

            ecd.Comment = Comment;
            // 加上\0结尾，否则会有一点乱码
            if (!String.IsNullOrEmpty(ecd.Comment) && !ecd.Comment.EndsWith("\0")) ecd.Comment += "\0";
            ecd.NumberOfEntries = (UInt16)num;
            ecd.NumberOfEntriesOnThisDisk = (UInt16)num;
            ecd.Size = (UInt32)writer.Stream.Position - ecd.Offset;

            writer.Write(ecd);

            writer.Stream.Flush();
        }

        /// <summary>把Zip格式数据写入到文件中</summary>
        /// <param name="fileName"></param>
        public void Write(String fileName)
        {
            if (String.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");

            //// 根据文件后缀决定采用的压缩算法
            //var method = CompressionMethod.Stored;
            //var ext = Path.GetExtension(fileName);
            //if (ext == ".7z" || ext == ".lzma")
            //    method = CompressionMethod.LZMA;
            //else
            //    method = CompressionMethod.Deflated;

            //if (method != CompressionMethod.Stored)
            //{
            //    foreach (var item in Entries.Values)
            //    {
            //        item.CompressionMethod = method;
            //    }
            //}

            using (var fs = File.Create(fileName))
            {
                Write(fs);
            }
        }
        #endregion

        #region 解压缩
        /// <summary>解压缩</summary>
        /// <param name="outputPath">目标路径</param>
        /// <param name="overrideExisting">是否覆盖已有文件</param>
        /// <param name="throwException"></param>
        public void Extract(String outputPath, Boolean overrideExisting = true, Boolean throwException = true)
        {
            if (String.IsNullOrEmpty(outputPath)) throw new ArgumentNullException("outputPath");

            foreach (var item in Entries.Values)
            {
                if (throwException)
                    item.Extract(outputPath, overrideExisting);
                else
                {
                    try
                    {
                        item.Extract(outputPath, overrideExisting);
                    }
                    catch { }
                }
            }
        }

        /// <summary>快速解压缩</summary>
        /// <param name="fileName"></param>
        /// <param name="outputPath"></param>
        /// <param name="overrideExisting"></param>
        /// <param name="throwException"></param>
        public static void ExtractToDirectory(String fileName, String outputPath, Boolean overrideExisting = true, Boolean throwException = true)
        {
            if (String.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");
            // 默认使用没有后缀的路径作为目录
            if (String.IsNullOrEmpty(outputPath)) outputPath = Path.GetFileNameWithoutExtension(fileName);
            if (String.IsNullOrEmpty(outputPath)) throw new ArgumentNullException("outputPath");

            using (var zf = new ZipFile(fileName))
            {
                zf.Extract(outputPath, overrideExisting, throwException);
            }
        }
        #endregion

        #region 压缩
        /// <summary>添加文件。
        /// 必须指定文件路径<paramref name="fileName"/>，如果不指定实体名<paramref name="entryName"/>，则使用文件名，并加到顶级目录。</summary>
        /// <param name="fileName">文件路径</param>
        /// <param name="entryName">实体名</param>
        /// <param name="stored">是否仅存储，不压缩</param>
        /// <returns></returns>
        public ZipEntry AddFile(String fileName, String entryName = null, Boolean? stored = false)
        {
            if (String.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");

            if (String.IsNullOrEmpty(entryName)) entryName = Path.GetFileName(fileName);
            entryName = entryName.Replace(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

            // 判断并添加目录
            String dir = Path.GetDirectoryName(entryName);
            if (!String.IsNullOrEmpty(dir))
            {
                if (!dir.EndsWith(DirSeparator)) dir += DirSeparator;
                if (!Entries.ContainsKey(dir))
                {
                    var zde = new ZipEntry();
                    zde.FileName = dir;
                    Entries.Add(dir, zde);
                }
            }

            var entry = ZipEntry.Create(fileName, entryName, stored);
            Entries.Add(entry.FileName, entry);

            return entry;
        }

        /// <summary>添加目录。</summary>
        /// <remarks>必须指定目录<paramref name="dirName"/>，如果不指定实体名<paramref name="entryName"/>，则加到顶级目录。</remarks>
        /// <param name="dirName">目录</param>
        /// <param name="entryName">实体名</param>
        /// <param name="stored">是否仅存储，不压缩</param>
        public void AddDirectory(String dirName, String entryName = null, Boolean? stored = null)
        {
            if (String.IsNullOrEmpty(dirName)) throw new ArgumentNullException("fileName");
            dirName = Path.GetFullPath(dirName);

            if (!String.IsNullOrEmpty(entryName))
            {
                if (!entryName.EndsWith(DirSeparator)) entryName += DirSeparator;
                var entry = ZipEntry.Create(null, entryName, true);
                Entries.Add(entry.FileName, entry);
            }

            // 所有文件
            foreach (var item in Directory.GetFiles(dirName, "*.*", SearchOption.TopDirectoryOnly))
            {
                String name = item;
                if (name.StartsWith(dirName)) name = name.Substring(dirName.Length);
                if (name[0] == Path.DirectorySeparatorChar) name = name.Substring(1);

                if (!String.IsNullOrEmpty(entryName)) name = entryName + name;

                AddFile(item, name, stored);
            }

            foreach (var item in Directory.GetDirectories(dirName, "*", SearchOption.TopDirectoryOnly))
            {
                String name = item;
                if (name.StartsWith(dirName)) name = name.Substring(dirName.Length);
                if (name[0] == Path.DirectorySeparatorChar) name = name.Substring(1);
                // 加上分隔符，表示目录
                if (!name.EndsWith(DirSeparator)) name += DirSeparator;

                if (!String.IsNullOrEmpty(entryName)) name = entryName + name;

                AddDirectory(item, name, stored);
            }
        }

        /// <summary>快速压缩文件。</summary>
        /// <param name="fileName"></param>
        /// <param name="outputName"></param>
        public static void CompressFile(String fileName, String outputName = null)
        {
            if (String.IsNullOrEmpty(fileName)) throw new ArgumentNullException("fileName");
            if (String.IsNullOrEmpty(outputName)) outputName = Path.ChangeExtension(fileName, ".zip");

            using (ZipFile zf = new ZipFile())
            {
                zf.AddFile(fileName);
                zf.Write(outputName);
            }
        }

        /// <summary>快速压缩目录。</summary>
        /// <param name="dirName"></param>
        /// <param name="outputName"></param>
        public static void CompressDirectory(String dirName, String outputName = null)
        {
            if (String.IsNullOrEmpty(dirName)) throw new ArgumentNullException("dirName");
            if (String.IsNullOrEmpty(outputName)) outputName = Path.ChangeExtension(Path.GetFileName(dirName), ".zip");

            using (ZipFile zf = new ZipFile())
            {
                zf.AddDirectory(dirName);
                zf.Write(outputName);
            }
        }
        #endregion

        #region 索引集合
        private Dictionary<String, ZipEntry> _Entries;
        /// <summary>文件实体集合</summary>
        public Dictionary<String, ZipEntry> Entries
        {
            get
            {
                // 不区分大小写
                if (_Entries == null)
                {
                    _Entries = new Dictionary<String, ZipEntry>(StringComparer.OrdinalIgnoreCase);

                    // 第一次访问文件实体集合时，才去读取文件
                    EnsureRead();
                }
                return _Entries;
            }
        }

        /// <summary>返回指定索引处的实体</summary>
        /// <param name="index"></param>
        /// <returns></returns>
        public ZipEntry this[Int32 index] { get { return Entries.Values.ElementAtOrDefault(index); } }

        /// <summary>返回指定名称的实体</summary>
        /// <param name="fileName"></param>
        /// <returns></returns>
        public ZipEntry this[String fileName]
        {
            get
            {
                var key = fileName;
                key = key.Replace('\\', '/');
                key = key.TrimStart('/');
                var entries = Entries;
                ZipEntry e = null;
                if (entries.TryGetValue(key, out e)) return e;

                key = key.Replace("/", "\\");
                if (entries.TryGetValue(key, out e)) return e;

                return null;
            }
        }

        /// <summary>实体个数</summary>
        public Int32 Count { get { return Entries.Count; } }
        #endregion

        #region 辅助
        internal IFormatterX CreateReader(Stream stream)
        {
#if DEBUG
            //stream = new NewLife.Log.TraceStream(stream);
#endif
            var bn = new Binary() { Stream = stream };
            bn.EncodeInt = false;
            bn.UseFieldSize = true;
            bn.UseProperty = false;
            bn.SizeWidth = 2;
            bn.IsLittleEndian = true;
            bn.Encoding = Encoding;
#if DEBUG
            bn.Log = NewLife.Log.XTrace.Log;
#endif

            return bn;
        }

        internal IFormatterX CreateWriter(Stream stream)
        {
#if DEBUG
            //stream = new NewLife.Log.TraceStream(stream);
#endif
            var bn = new Binary() { Stream = stream };
            bn.EncodeInt = false;
            bn.UseFieldSize = true;
            bn.UseProperty = true;
            bn.SizeWidth = 2;
            bn.IsLittleEndian = true;
            bn.Encoding = Encoding;
#if DEBUG
            bn.Log = NewLife.Log.XTrace.Log;
#endif

            return bn;
        }

        internal static readonly DateTime MinDateTime = new DateTime(1980, 1, 1);
        internal static DateTime DosDateTimeToFileTime(Int32 value)
        {
            if (value <= 0) return MinDateTime;

            Int16 time = (Int16)(value & 0x0000FFFF);
            Int16 date = (Int16)((value & 0xFFFF0000) >> 16);

            int year = 1980 + ((date & 0xFE00) >> 9);
            int month = (date & 0x01E0) >> 5;
            int day = date & 0x001F;

            int hour = (time & 0xF800) >> 11;
            int minute = (time & 0x07E0) >> 5;
            int second = (time & 0x001F) * 2;

            return new DateTime(year, month, day, hour, minute, second);
        }

        internal static Int32 FileTimeToDosDateTime(DateTime value)
        {
            if (value <= MinDateTime) value = MinDateTime;

            Int32 date = (value.Year - 1980) << 9 | value.Month << 5 | value.Day;
            Int32 time = value.Hour << 11 | value.Minute << 5 | value.Second / 2;

            return date << 16 | time;
        }

        internal readonly static String DirSeparator = Path.AltDirectorySeparatorChar.ToString();

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString() { return String.Format("{0} [{1}]", Name, Entries.Count); }
        #endregion

        #region IEnumerable<ZipEntry> 成员
        IEnumerator<ZipEntry> IEnumerable<ZipEntry>.GetEnumerator() { return Entries.Values.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return Entries.Values.GetEnumerator(); }
        #endregion

        #region CentralDirectory
        class EndOfCentralDirectory
        {
            #region 属性
            private UInt32 _Signature = ZipConstants.EndOfCentralDirectorySignature;
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
            /// <summary>注释</summary>
            public String Comment { get { return _Comment; } set { _Comment = value; } }
            #endregion
        }
        #endregion
    }
}

// ==================================================================
//
// Information on the ZIP format:
//
// From
// http://www.pkware.com/documents/casestudies/APPNOTE.TXT
//
//  Overall .ZIP file format:
//
//     [local file header 1]
//     [file data 1]
//     [data descriptor 1]  ** sometimes
//     .
//     .
//     .
//     [local file header n]
//     [file data n]
//     [data descriptor n]   ** sometimes
//     [archive decryption header]
//     [archive extra data record]
//     [central directory]
//     [zip64 end of central directory record]
//     [zip64 end of central directory locator]
//     [end of central directory record]
//
// Local File Header format:
//         local file header signature ... 4 bytes  (0x04034b50)
//         version needed to extract ..... 2 bytes
//         general purpose bit field ..... 2 bytes
//         compression method ............ 2 bytes
//         last mod file time ............ 2 bytes
//         last mod file date............. 2 bytes
//         crc-32 ........................ 4 bytes
//         compressed size................ 4 bytes
//         uncompressed size.............. 4 bytes
//         file name length............... 2 bytes
//         extra field length ............ 2 bytes
//         file name                       varies
//         extra field                     varies
//
//
// Data descriptor:  (used only when bit 3 of the general purpose bitfield is set)
//         (although, I have found zip files where bit 3 is not set, yet this descriptor is present!)
//         local file header signature     4 bytes  (0x08074b50)  ** sometimes!!! Not always
//         crc-32                          4 bytes
//         compressed size                 4 bytes
//         uncompressed size               4 bytes
//
//
//   Central directory structure:
//
//       [file header 1]
//       .
//       .
//       .
//       [file header n]
//       [digital signature]
//
//
//       File header:  (This is a ZipDirEntry)
//         central file header signature   4 bytes  (0x02014b50)
//         version made by                 2 bytes
//         version needed to extract       2 bytes
//         general purpose bit flag        2 bytes
//         compression method              2 bytes
//         last mod file time              2 bytes
//         last mod file date              2 bytes
//         crc-32                          4 bytes
//         compressed size                 4 bytes
//         uncompressed size               4 bytes
//         file name length                2 bytes
//         extra field length              2 bytes
//         file comment length             2 bytes
//         disk number start               2 bytes
//         internal file attributes **     2 bytes
//         external file attributes ***    4 bytes
//         relative offset of local header 4 bytes
//         file name (variable size)
//         extra field (variable size)
//         file comment (variable size)
//
// ** The internal file attributes, near as I can tell,
// uses 0x01 for a file and a 0x00 for a directory.
//
// ***The external file attributes follows the MS-DOS file attribute byte, described here:
// at http://support.microsoft.com/kb/q125019/
// 0x0010 => directory
// 0x0020 => file
//
//
// End of central directory record:
//
//         end of central dir signature    4 bytes  (0x06054b50)
//         number of this disk             2 bytes
//         number of the disk with the
//         start of the central directory  2 bytes
//         total number of entries in the
//         central directory on this disk  2 bytes
//         total number of entries in
//         the central directory           2 bytes
//         size of the central directory   4 bytes
//         offset of start of central
//         directory with respect to
//         the starting disk number        4 bytes
//         .ZIP file comment length        2 bytes
//         .ZIP file comment       (variable size)
//
// date and time are packed values, as MSDOS did them
// time: bits 0-4 : seconds (divided by 2)
//            5-10: minute
//            11-15: hour
// date  bits 0-4 : day
//            5-8: month
//            9-15 year (since 1980)
//
// see http://msdn.microsoft.com/en-us/library/ms724274(VS.85).aspx
#endif