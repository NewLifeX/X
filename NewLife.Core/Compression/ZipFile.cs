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
    /// <remarks>Zip定义位于 <see href="http://www.pkware.com/documents/casestudies/APPNOTE.TXT"/></remarks>
    public partial class ZipFile : IEnumerable, IEnumerable<ZipEntry>, IDisposable
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
        #endregion

        #region 构造
        /// <summary>实例化一个Zip文件对象</summary>
        public ZipFile() : this(String.Empty, null) { }

        /// <summary>实例化一个Zip文件对象</summary>
        /// <param name="fileName"></param>
        public ZipFile(String fileName) : this(fileName, null) { }

        /// <summary>实例化一个Zip文件对象</summary>
        /// <param name="encoding"></param>
        public ZipFile(Encoding encoding) : this(String.Empty, encoding) { }

        /// <summary>实例化一个Zip文件对象</summary>
        /// <param name="fileName"></param>
        /// <param name="encoding"></param>
        public ZipFile(String fileName, Encoding encoding)
        {
            try
            {
                Name = fileName;

                if (!String.IsNullOrEmpty(fileName) && File.Exists(fileName)) Read(File.OpenRead(fileName));
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", fileName), e1);
            }
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
                throw new ZipException("stream is not a valid zip file", ex);
            }
        }

        /// <summary>释放资源</summary>
        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

        /// <summary>释放资源</summary>
        /// <param name="disposeManagedResources"></param>
        protected virtual void Dispose(bool disposeManagedResources)
        {
            //if (!this._disposed)
            //{
            //    if (disposeManagedResources)
            //    {
            //        // dispose managed resources
            //        if (_ReadStreamIsOurs)
            //        {
            //            if (_readstream != null)
            //            {
            //                _readstream.Dispose();
            //                _readstream = null;
            //            }
            //        }
            //        // only dispose the writestream if there is a backing file
            //        if (_temporaryFileName != null && !String.IsNullOrEmpty(_name))
            //            if (_writestream != null)
            //            {
            //                _writestream.Dispose();
            //                _writestream = null;
            //            }

            //        if (this.ParallelDeflater != null)
            //        {
            //            this.ParallelDeflater.Dispose();
            //            this.ParallelDeflater = null;
            //        }
            //    }
            //    this._disposed = true;
            //}
        }

        //public static ZipFile Create(String fileName)
        //{
        //    if (fileName == null) throw new ArgumentNullException("fileName");

        //    FileStream fs = File.Create(fileName);

        //    ZipFile result = new ZipFile();
        //    result.name_ = fileName;
        //    result.baseStream_ = fs;
        //    result.isStreamOwner = true;
        //    return result;
        //}

        //public static ZipFile Create(Stream outStream)
        //{
        //    if (outStream == null) throw new ArgumentNullException("outStream");
        //    if (!outStream.CanWrite) throw new ArgumentException("Stream is not writeable", "outStream");
        //    if (!outStream.CanSeek) throw new ArgumentException("Stream is not seekable", "outStream");

        //    ZipFile result = new ZipFile();
        //    result.baseStream_ = outStream;
        //    return result;
        //}
        #endregion

        #region 读取核心
        private void Read(Stream stream)
        {
            ZipEntry e;
            bool firstEntry = true;
            while ((e = ZipEntry.ReadEntry(this, stream, firstEntry)) != null)
            {
                if (Entries.ContainsKey(e.FileName)) Console.WriteLine("");
                Entries.Add(e.FileName, e);
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

                //if (cd.NumberOfEntriesOnThisDisk > 0) stream.Seek(cd.NumberOfEntriesOnThisDisk, SeekOrigin.Begin);

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

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            return String.Format("{0} [{1}]", Name, Entries.Count);
        }
        #endregion

        #region IEnumerable<ZipEntry> 成员

        IEnumerator<ZipEntry> IEnumerable<ZipEntry>.GetEnumerator() { return Entries.Values.GetEnumerator(); }

        IEnumerator IEnumerable.GetEnumerator() { return Entries.Values.GetEnumerator(); }

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