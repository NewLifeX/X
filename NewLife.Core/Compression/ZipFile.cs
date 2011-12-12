using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using NewLife.Linq;
using NewLife.IO;

namespace NewLife.Compression
{
    /// <summary>Zip文件</summary>
    /// <remarks>Zip定义位于 <see cref="http://www.pkware.com/documents/casestudies/APPNOTE.TXT"/></remarks>
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
        public ZipFile() : this(String.Empty, null) { }

        public ZipFile(String fileName) : this(fileName, null) { }

        public ZipFile(Encoding encoding) : this(String.Empty, encoding) { }

        public ZipFile(String fileName, Encoding encoding)
        {
            try
            {
                //AlternateEncoding = encoding;
                //AlternateEncodingUsage = ZipOption.Always;

                Name = fileName;
                //_contentsChanged = true;
                //AddDirectoryWillTraverseReparsePoints = true;  // workitem 8617
                //CompressionLevel = CompressionLevel.Default;
                //ParallelDeflateThreshold = 512 * 1024;

                if (!String.IsNullOrEmpty(fileName) && File.Exists(fileName))
                {
                    //if (FullScan)
                    //ReadIntoInstance_Orig(this);
                    //else
                    Read(File.OpenRead(fileName));

                    //_fileAlreadyExists = true;
                }
            }
            catch (Exception e1)
            {
                throw new ZipException(String.Format("{0} is not a valid zip file", fileName), e1);
            }
        }

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

        public void Dispose()
        {
            Dispose(true);

            GC.SuppressFinalize(this);
        }

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
        Int64 _locEndOfCDS;
        UInt32 _diskNumberWithCd;
        UInt32 _OffsetOfCentralDirectory;
        Int64 _OffsetOfCentralDirectory64;

        private void Read(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            try
            {
                if (!stream.CanSeek)
                {
                    Read_Orig(stream);
                    return;
                }

                if (reader.ReadInt32() == ZipConstants.EndOfCentralDirectorySignature) return;

                // start at the end of the file...
                // seek backwards a bit, then look for the EoCD signature.
                Int32 nTries = 0;
                bool success = false;

                // EndOfCentralDirectory的大小加上两个字节就是18，查找EndOfCentralDirectorySignature签名
                // The size of the end-of-central-directory-footer plus 2 bytes is 18.
                // This implies an archive comment length of 0.  We'll add a margin of
                // safety and start "in front" of that, when looking for the
                // EndOfCentralDirectorySignature
                long posn = stream.Length - 64;
                long maxSeekback = Math.Max(stream.Length - 0x4000, 10);
                do
                {
                    if (posn < 0) posn = 0;  // BOF
                    stream.Seek(posn, SeekOrigin.Begin);

                    //long bytesRead = stream.IndexOf(BitConverter.GetBytes((Int32)ZipConstants.EndOfCentralDirectorySignature));
                    Int64 bytesRead = CentralDirectory.FindSignature(stream);
                    if (bytesRead != -1)
                        success = true;
                    else
                    {
                        if (posn == 0) break; // started at the BOF and found nothing
                        nTries++;
                        // Weird: with NETCF, negative offsets from SeekOrigin.End DO
                        // NOT WORK. So rather than seek a negative offset, we seek
                        // from SeekOrigin.Begin using a smaller number.
                        posn -= (32 * (nTries + 1) * nTries);
                    }
                }
                while (!success && posn > maxSeekback);

                if (success)
                {
                    stream.Seek(-4, SeekOrigin.Current);
                    var cd = new CentralDirectory();
                    cd.Read(stream);

                    _locEndOfCDS = stream.Position - 4;

                    byte[] block = new byte[16];
                    stream.Read(block, 0, block.Length);

                    _diskNumberWithCd = BitConverter.ToUInt16(block, 2);

                    if (_diskNumberWithCd == 0xFFFF) throw new ZipException("Spanned archives with more than 65534 segments are not supported at this time.");

                    _diskNumberWithCd++; // I think the number in the file differs from reality by 1

                    Int32 i = 12;

                    uint offset32 = (uint)BitConverter.ToUInt32(block, i);
                    if (offset32 == 0xFFFFFFFF)
                    {
                        Zip64SeekToCentralDirectory(stream);
                    }
                    else
                    {
                        _OffsetOfCentralDirectory = offset32;
                        // change for workitem 8098
                        stream.Seek(offset32, SeekOrigin.Begin);
                    }

                    ReadCentralDirectory(stream);
                }
                else
                {
                    // Could not find the central directory.
                    // Fallback to the old method.
                    // workitem 8098: ok
                    //s.Seek(zf._originPosition, SeekOrigin.Begin);
                    stream.Seek(0L, SeekOrigin.Begin);
                    Read_Orig(stream);
                }
            }
            catch (Exception ex)
            {
                throw new ZipException("Cannot read that as a ZipFile", ex);
            }
        }

        private void Read_Orig(Stream stream)
        {
            ZipEntry e;
            bool firstEntry = true;
            while ((e = ZipEntry.ReadEntry(this, stream, firstEntry)) != null)
            {
                Entries.Add(e.FileName, e);
                firstEntry = false;
            }

            // 读取目录结构，但是可能有错误，需要屏蔽
            try
            {
                ZipEntry de;
                var previouslySeen = new Dictionary<String, Object>();
                while ((de = ZipEntry.ReadDirEntry(this, stream, previouslySeen)) != null)
                {
                    // Housekeeping: Since ZipFile exposes ZipEntry elements in the enumerator,
                    // we need to copy the comment that we grab from the ZipDirEntry
                    // into the ZipEntry, so the application can access the comment.
                    // Also since ZipEntry is used to Write zip files, we need to copy the
                    // file attributes to the ZipEntry as appropriate.
                    e = Entries[de.FileName];
                    if (e != null)
                    {
                        e.Comment = de.Comment;
                        e.IsDirectory = de.IsDirectory;
                    }
                    previouslySeen.Add(de.FileName, null);
                }

                if (_locEndOfCDS > 0) stream.Seek(_locEndOfCDS, SeekOrigin.Begin);

                ReadCentralDirectoryFooter(stream);
            }
            catch (ZipException) { }
            catch (IOException) { }
        }

        private void Zip64SeekToCentralDirectory(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);

            byte[] block = new byte[16];

            // seek back to find the ZIP64 EoCD.
            // I think this might not work for .NET CF ?
            stream.Seek(-40, SeekOrigin.Current);
            stream.Read(block, 0, 16);

            Int64 offset64 = BitConverter.ToInt64(block, 8);
            _OffsetOfCentralDirectory = 0xFFFFFFFF;
            _OffsetOfCentralDirectory64 = offset64;
            // change for workitem 8098
            stream.Seek(offset64, SeekOrigin.Begin);
            //zf.SeekFromOrigin(Offset64);

            uint datum = reader.ReadUInt32();
            if (datum != ZipConstants.Zip64EndOfCentralDirectoryRecordSignature)
                throw new ZipException("  Bad signature (0x{0:X8}) looking for ZIP64 EoCD Record at position 0x{1:X8}", datum, stream.Position);

            stream.Read(block, 0, 8);
            Int64 Size = BitConverter.ToInt64(block, 0);

            block = new byte[Size];
            stream.Read(block, 0, block.Length);

            offset64 = BitConverter.ToInt64(block, 36);
            // change for workitem 8098
            stream.Seek(offset64, SeekOrigin.Begin);
            //zf.SeekFromOrigin(Offset64);
        }

        private void ReadCentralDirectory(Stream stream)
        {
            // We must have the central directory footer record, in order to properly
            // read zip dir entries from the central directory.  This because the logic
            // knows when to open a spanned file when the volume number for the central
            // directory differs from the volume number for the zip entry.  The
            // _diskNumberWithCd was set when originally finding the offset for the
            // start of the Central Directory.

            bool inputUsesZip64 = false;
            ZipEntry de;
            var previouslySeen = new Dictionary<String, object>();
            while ((de = ZipEntry.ReadDirEntry(this, stream, previouslySeen)) != null)
            {
                Entries.Add(de.FileName, de);

                //if (de._InputUsesZip64) inputUsesZip64 = true;
                previouslySeen.Add(de.FileName, null); // to prevent dupes
            }

            //if (inputUsesZip64) UseZip64WhenSaving = Zip64Option.Always;

            // workitem 8299
            if (_locEndOfCDS > 0) stream.Seek(_locEndOfCDS, SeekOrigin.Begin);

            ReadCentralDirectoryFooter(stream);
        }

        UInt16 _versionMadeBy;
        UInt16 _versionNeededToExtract;

        private void ReadCentralDirectoryFooter(Stream stream)
        {
            BinaryReader reader = new BinaryReader(stream);
            Int32 signature = reader.ReadInt32();

            byte[] block = null;
            Int32 j = 0;
            if (signature == ZipConstants.Zip64EndOfCentralDirectoryRecordSignature)
            {
                // We have a ZIP64 EOCD
                // This data block is 4 bytes sig, 8 bytes size, 44 bytes fixed data,
                // followed by a variable-sized extension block.  We have read the sig already.
                // 8 - datasize (64 bits)
                // 2 - version made by
                // 2 - version needed to extract
                // 4 - number of this disk
                // 4 - number of the disk with the start of the CD
                // 8 - total number of entries in the CD on this disk
                // 8 - total number of entries in the CD
                // 8 - size of the CD
                // 8 - offset of the CD
                // -----------------------
                // 52 bytes

                block = new byte[8 + 44];
                stream.Read(block, 0, block.Length);

                Int64 DataSize = BitConverter.ToInt64(block, 0);  // == 44 + the variable length

                if (DataSize < 44)
                    throw new ZipException("Bad size in the ZIP64 Central Directory.");

                _versionMadeBy = BitConverter.ToUInt16(block, j);
                j += 2;
                _versionNeededToExtract = BitConverter.ToUInt16(block, j);
                j += 2;
                _diskNumberWithCd = BitConverter.ToUInt32(block, j);
                j += 2;

                //zf._diskNumberWithCd++; // hack!!

                // read the extended block
                block = new byte[DataSize - 44];
                stream.Read(block, 0, block.Length);
                // discard the result

                signature = reader.ReadInt32();
                if (signature != ZipConstants.Zip64EndOfCentralDirectoryLocatorSignature) throw new ZipException("Inconsistent metadata in the ZIP64 Central Directory.");

                block = new byte[16];
                stream.Read(block, 0, block.Length);
                // discard the result

                signature = reader.ReadInt32();
            }

            // Throw if this is not a signature for "end of central directory record"
            // This is a sanity check.
            if (signature != ZipConstants.EndOfCentralDirectorySignature)
            {
                stream.Seek(-4, SeekOrigin.Current);
                throw new ZipException(String.Format("Bad signature ({0:X8}) at position 0x{1:X8}", signature, stream.Position));
            }

            // read the End-of-Central-Directory-Record
            block = new byte[16];
            stream.Read(block, 0, block.Length);

            // off sz  data
            // -------------------------------------------------------
            //  0   4  end of central dir signature (0x06054b50)
            //  4   2  number of this disk
            //  6   2  number of the disk with start of the central directory
            //  8   2  total number of entries in the  central directory on this disk
            // 10   2  total number of entries in  the central directory
            // 12   4  size of the central directory
            // 16   4  offset of start of central directory with respect to the starting disk number
            // 20   2  ZIP file comment length
            // 22  ??  ZIP file comment

            if (_diskNumberWithCd == 0) _diskNumberWithCd = BitConverter.ToUInt16(block, 2);

            //byte[] block = new byte[2];
            //zf.ReadStream.Read(block, 0, block.Length);

            //Int16 commentLength = (short)(block[0] + block[1] * 256);
            Int16 commentLength = reader.ReadInt16();
            if (commentLength > 0)
            {
                block = reader.ReadBytes(commentLength);

                // workitem 10392 - prefer ProvisionalAlternateEncoding,
                // first.  The fix for workitem 6513 tried to use UTF8
                // only as necessary, but that is impossible to test
                // for, in this direction. There's no way to know what
                // characters the already-encoded bytes refer
                // to. Therefore, must do what the user tells us.

                Comment = Encoding.GetString(block, 0, block.Length);
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

        public ZipEntry this[Int32 index] { get { return Entries.Values.ElementAtOrDefault(index); } }

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

        public Int32 Count { get { return Entries.Count; } }
        #endregion

        #region 辅助
        static string NormalizePathForUseInZipFile(string pathName)
        {
            if (String.IsNullOrEmpty(pathName)) return pathName;

            if (pathName.Length >= 2 && pathName[1] == ':' && pathName[2] == '\\') pathName = pathName.Substring(3);

            pathName = pathName.Replace('\\', '/');

            pathName = pathName.TrimStart('/');

            return Path.GetFullPath(pathName);
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