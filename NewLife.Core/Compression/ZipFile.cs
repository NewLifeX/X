using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife.Linq;
using NewLife.Serialization;

namespace NewLife.Compression
{
    /// <summary>Zip文件</summary>
    /// <remarks>
    /// Zip定义位于 <a target="_blank" href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT">http://www.pkware.com/documents/casestudies/APPNOTE.TXT</a>
    /// </remarks>
    public partial class ZipFile : DisposeBase, IEnumerable, IEnumerable<ZipEntry>
    {
        #region 属性
        private String _Name;
        /// <summary>名称</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private String _Comment;
        /// <summary>注释</summary>
        public String Comment { get { return _Comment; } set { _Comment = value; } }

        private Encoding _Encoding = Encoding.Default;
        /// <summary>字符串编码</summary>
        public Encoding Encoding { get { return _Encoding; } set { _Encoding = value; } }

        private String _DefaultExtractPath;
        /// <summary>默认解压目录</summary>
        private String DefaultExtractPath
        {
            get
            {
                if (String.IsNullOrEmpty(_DefaultExtractPath)) _DefaultExtractPath = AppDomain.CurrentDomain.BaseDirectory;
                return _DefaultExtractPath;
            }
            set { _DefaultExtractPath = value; }
        }
        #endregion

        #region 构造
        /// <summary>实例化一个Zip文件对象</summary>
        public ZipFile() { }

        /// <summary>实例化一个Zip文件对象</summary>
        /// <param name="fileName"></param>
        public ZipFile(String fileName) : this(fileName, null) { }

        /// <summary>实例化一个Zip文件对象</summary>
        /// <param name="fileName"></param>
        /// <param name="encoding"></param>
        public ZipFile(String fileName, Encoding encoding)
            : this(File.OpenRead(fileName), encoding)
        {
            if (!String.IsNullOrEmpty(fileName)) DefaultExtractPath = Path.GetDirectoryName(fileName);
        }

        /// <summary>实例化一个Zip文件对象</summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        public ZipFile(Stream stream, Encoding encoding)
        {
            try
            {
                Read(stream);
            }
            catch (Exception ex)
            {
                throw new ZipException("不是有效的Zip格式！", ex);
            }
        }

        /// <summary>释放资源</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            //if (_readStream != null) _readStream.Dispose();
            if (Entries.Count > 0)
            {
                // 是否所有实体，因为里面可能含有数据流
                foreach (var item in Entries.Values)
                {
                    try
                    {
                        item.Dispose();
                    }
                    catch { }
                }

                Entries.Clear();
            }
        }
        #endregion

        #region 读取
        //Stream _readStream;

        /// <summary>从数据流中读取Zip格式数据</summary>
        /// <param name="stream">数据流</param>
        /// <param name="embedFileData">
        /// 当前读取仅读取文件列表等信息，如果设置内嵌数据，则同时把文件数据读取到内存中；否则，在解压缩时需要再次使用数据流。
        /// 如果外部未指定是否内嵌文件数据，则根据数据流是否小于10M来决定是否内嵌。
        /// </param>
        public void Read(Stream stream, Boolean? embedFileData = null)
        {
            //_readStream = stream;

            // 如果外部未指定是否内嵌文件数据，则根据数据流是否小于10M来决定是否内嵌
            Boolean embedfile = embedFileData ?? stream.Length < 10 * 1024 * 1024;

            ZipEntry e;
            bool firstEntry = true;
            while ((e = ZipEntry.ReadEntry(this, stream, firstEntry, embedfile)) != null)
            {
                String name = e.FileName;
                Int32 n = 2;
                while (Entries.ContainsKey(name)) { name = e.FileName + "" + n++; }
                Entries.Add(name, e);
                firstEntry = false;
            }

            // 读取目录结构，但是可能有错误，需要屏蔽
            try
            {
                ZipEntry de;
                while ((de = ZipEntry.ReadDirEntry(this, stream)) != null)
                {
                    e = Entries[de.FileName];
                    if (e != null)
                    {
                        e.Comment = de.Comment;
                        //e.IsDirectory = de.IsDirectory;
                    }
                }

                // 读取目录结构尾记录
                var reader = CreateReader(stream);
                if (reader.Expect(ZipConstants.EndOfCentralDirectorySignature))
                {
                    var entry = reader.ReadObject<EndOfCentralDirectory>();
                    Comment = entry.Comment;
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
            writer.Settings.IgnoreMembers = null;
            var newIgnores = writer.Settings.IgnoreMembers;
            // 写入文件头时忽略掉这些字段，这些都是DirEntry的字段
            var names = new String[] { "_VersionMadeBy", "_CommentLength", "_DiskNumber", "_InternalFileAttrs", "_ExternalFileAttrs", "_RelativeOffsetOfLocalHeader", "_Comment" };
            foreach (var item in names)
            {
                newIgnores.Add(item);
            }

            writer.Settings.IgnoreMembers.Clear();
            foreach (var item in Entries.Values)
            {
                // 这里只写文件
                if (!item.IsDirectory) item.Write(writer);
            }

            var ecd = new EndOfCentralDirectory();
            ecd.Offset = (UInt32)writer.Stream.Position;

            foreach (var item in Entries.Values)
            {
                // 每一个都需要写目录项
                item.WriteDir(writer);
            }

            ecd.Comment = Comment;
            ecd.NumberOfEntries = (UInt16)Count;
            ecd.NumberOfEntriesOnThisDisk = (UInt16)Count;
            ecd.Size = (UInt32)writer.Stream.Position - ecd.Offset;
            writer.WriteObject(ecd);

            writer.Flush();
        }
        #endregion

        #region 解压缩
        /// <summary>解压缩</summary>
        /// <param name="path">目标路径</param>
        /// <param name="overrideExisting">是否覆盖已有文件</param>
        public void Extract(String path, Boolean overrideExisting = true)
        {
            if (String.IsNullOrEmpty(path)) throw new ArgumentNullException("path");
            //if (_readStream == null || !_readStream.CanSeek || !_readStream.CanRead) throw new ZipException("数据流异常！");

            if (!Path.IsPathRooted(path)) path = Path.Combine(DefaultExtractPath, path);

            foreach (var item in Entries.Values)
            {
                item.Extract(path, overrideExisting);
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
            entryName = entryName.Replace(@"\", "/");

            // 判断并添加目录
            String dir = Path.GetDirectoryName(entryName);
            if (!String.IsNullOrEmpty(dir))
            {
                if (!dir.EndsWith("/")) dir += "/";
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

        /// <summary>添加目录。
        /// 必须指定目录<paramref name="dir"/>，如果不指定实体名<paramref name="entryName"/>，则加到顶级目录。</summary>
        /// <param name="dir">目录</param>
        /// <param name="entryName">实体名</param>
        /// <param name="stored">是否仅存储，不压缩</param>
        public void AddDirectory(String dir, String entryName = null, Boolean? stored = false)
        {
            if (String.IsNullOrEmpty(dir)) throw new ArgumentNullException("fileName");
            dir = Path.GetFullPath(dir);

            //// 所有子目录。虽然添加文件的时候也会判断目录，但是那些空目录就没有机会了
            //foreach (var item in Directory.GetDirectories(dir, "*", SearchOption.AllDirectories))
            //{
            //    String name = item;
            //    if (name.StartsWith(dir)) name = name.Substring(dir.Length);
            //    if (name[0] == Path.DirectorySeparatorChar) name = name.Substring(1);
            //    // 加上分隔符，表示目录
            //    if (name[name.Length - 1] != DirSeparator) name += DirSeparator;

            //    if (!String.IsNullOrEmpty(entryName)) name = entryName + DirSeparator + name;

            //    var entry = ZipEntry.Create(null, name, true);
            //    Entries.Add(entry.FileName, entry);
            //}

            if (!String.IsNullOrEmpty(entryName))
            {
                var entry = ZipEntry.Create(null, entryName, true);
                Entries.Add(entry.FileName, entry);
            }

            // 所有文件
            foreach (var item in Directory.GetFiles(dir, "*.*", SearchOption.TopDirectoryOnly))
            {
                String name = item;
                if (name.StartsWith(dir)) name = name.Substring(dir.Length);
                if (name[0] == Path.DirectorySeparatorChar) name = name.Substring(1);

                if (!String.IsNullOrEmpty(entryName)) name = entryName + DirSeparator + name;

                AddFile(item, name, stored);
            }

            foreach (var item in Directory.GetDirectories(dir, "*", SearchOption.TopDirectoryOnly))
            {
                String name = item;
                if (name.StartsWith(dir)) name = name.Substring(dir.Length);
                if (name[0] == Path.DirectorySeparatorChar) name = name.Substring(1);
                // 加上分隔符，表示目录
                if (name[name.Length - 1] != DirSeparator) name += DirSeparator;

                if (!String.IsNullOrEmpty(entryName)) name = entryName + DirSeparator + name;

                AddDirectory(item, name, stored);
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
                if (_Entries == null) _Entries = new Dictionary<string, ZipEntry>(StringComparer.OrdinalIgnoreCase);
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
                var key = NormalizePathForUseInZipFile(fileName);
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
        internal BinaryReaderX CreateReader(Stream stream)
        {
            var reader = new BinaryReaderX() { Stream = stream };
            reader.Settings.EncodeInt = false;
            reader.Settings.UseObjRef = false;
            reader.Settings.SizeFormat = TypeCode.Int16;
            reader.Settings.Encoding = Encoding;
            return reader;
        }

        internal BinaryWriterX CreateWriter(Stream stream)
        {
            var writer = new BinaryWriterX() { Stream = stream };
            writer.Settings.EncodeInt = false;
            writer.Settings.UseObjRef = false;
            writer.Settings.SizeFormat = TypeCode.Int16;
            writer.Settings.Encoding = Encoding;
            return writer;
        }

        private static string NormalizePathForUseInZipFile(string pathName)
        {
            if (String.IsNullOrEmpty(pathName)) return pathName;

            if (pathName.Length >= 2 && pathName[1] == ':' && pathName[2] == '\\') pathName = pathName.Substring(3);

            pathName = pathName.Replace('\\', '/');

            pathName = pathName.TrimStart('/');

            return Path.GetFullPath(pathName);
        }

        internal static DateTime DosDateTimeToFileTime(Int32 value)
        {
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
            Int32 date = (value.Year - 1980) << 9 & value.Month << 5 & value.Day;
            Int32 time = value.Hour << 11 & value.Minute << 5 & value.Second / 2;

            return date << 16 | time;
        }

        internal readonly static Char DirSeparator = Path.AltDirectorySeparatorChar;

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