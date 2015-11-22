using System;
using NewLife.IO;
using System.IO;
using System.Text;
using NewLife.Compression.LZMA;
using System.Collections.Generic;

namespace NewLife.Compression
{
    /// <summary>7z压缩包</summary>
    public class SevenZip
    {
        #region 属性
        private static readonly byte[] SIGNATURE = new byte[] { (byte)'7', (byte)'z', 0xBC, 0xAF, 0x27, 0x1C };

        private String _Name;
        /// <summary>名称</summary>
        public String Name { get { return _Name; } set { _Name = value; } }

        private String _Comment;
        /// <summary>注释</summary>
        public String Comment { get { EnsureRead(); return _Comment; } set { _Comment = value; } }

        private Encoding _Encoding;
        /// <summary>字符串编码</summary>
        public Encoding Encoding { get { return _Encoding ?? Encoding.Default; } set { _Encoding = value; } }

        private Version _Version;
        /// <summary>版本</summary>
        public Version Version { get { return _Version; } set { _Version = value; } }

        private UInt32 _Crc;
        /// <summary>校验</summary>
        public UInt32 Crc { get { return _Crc; } set { _Crc = value; } }
        #endregion

        #region 构造
        /// <summary>实例化一个Zip文件对象</summary>
        public SevenZip() { }

        /// <summary>实例化一个Zip文件对象。延迟到第一次使用<see cref="Entries"/>时读取</summary>
        /// <param name="fileName"></param>
        public SevenZip(String fileName) : this(fileName, null) { }

        /// <summary>实例化一个Zip文件对象。延迟到第一次使用<see cref="Entries"/>时读取</summary>
        /// <param name="fileName">文件名</param>
        /// <param name="encoding">字符串编码</param>
        public SevenZip(String fileName, Encoding encoding)
        {
            Name = fileName;
            Encoding = encoding;
            _file = fileName;
        }

        /// <summary>实例化一个Zip文件对象。延迟到第一次使用<see cref="Entries"/>时读取</summary>
        /// <param name="stream"></param>
        /// <param name="encoding"></param>
        public SevenZip(Stream stream, Encoding encoding)
        {
            Encoding = encoding;
            _stream = stream;
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
        public Boolean Read(Stream stream)
        {
            // 检查签名
            var sign = stream.ReadBytes(SIGNATURE.Length);
            if (sign.CompareTo(SIGNATURE) != 0) return false;

            var reader = new BinaryReader(stream);

            Version = new Version(reader.ReadByte(), reader.ReadByte());
            Crc = reader.ReadUInt32();

            Int64 nextHeaderOffset = reader.ReadInt64();
            Int64 nextHeaderSize = reader.ReadInt64();
            Int32 nextHeaderCRC = reader.ReadInt32();

            stream.Seek(nextHeaderOffset, SeekOrigin.Current);
            ReadArchive(stream, nextHeaderOffset, nextHeaderSize);
            //PostProcess();

            return true;
        }

        private void ReadArchive(Stream stream, Int64 offset, Int64 length)
        {
            while (true)
            {
                var prop = (HeaderProperty)stream.ReadByte();
                switch (prop)
                {
                    case HeaderProperty.kEnd:
                        {
                            //ReadFileHeader(headerBytes);
                            return;
                        }
                    case HeaderProperty.kEncodedHeader:
                        {
                            var size = stream.ReadByte();
                            stream.Seek(size, SeekOrigin.Current);

                            ReadPackedStreams(stream);
                            //stream.Seek(offset + ArchiveInfo.PackPosition, SeekOrigin.Begin);
                            //var firstFolder = ArchiveInfo.Folders.First();

                            //ulong unpackSize = firstFolder.GetUnpackSize();

                            //ulong packSize = ArchiveInfo.PackedStreams.Select(x => x.PackedSize)
                            //    .Aggregate((ulong)0, (sum, size) => sum + size);

                            //byte[] unpackedBytes = new byte[(int)unpackSize];
                            //var decoder = new Decoder();
                            //decoder.SetDecoderProperties(firstFolder.Coders[0].Properties);
                            //using (MemoryStream outStream = new MemoryStream(unpackedBytes))
                            //{
                            //    decoder.Code(stream, outStream, (long)(packSize), (long)unpackSize, null);
                            //}

                            //headerBytes = new HeaderBuffer { Bytes = unpackedBytes };
                        }
                        break;
                    case HeaderProperty.kHeader:
                        {
                            //ReadFileHeader(headerBytes);
                            return;
                        }
                    default:
                        {
                            //throw new NotSupportedException("7Zip header " + prop);

                            var size = stream.ReadByte();
                            stream.Seek(size, SeekOrigin.Current);

                            break;
                        }
                }
            }
        }

        //private void ReadFileHeader(HeaderBuffer headerBytes)
        //{
        //    while (true)
        //    {
        //        var prop = headerBytes.ReadProperty();
        //        switch (prop)
        //        {
        //            case HeaderProperty.kMainStreamsInfo:
        //                {
        //                    FilesInfo = ReadPackedStreams(headerBytes);
        //                }
        //                break;
        //            case HeaderProperty.kFilesInfo:
        //                {
        //                    Entries = ReadFilesInfo(FilesInfo, headerBytes);
        //                }
        //                break;
        //            case HeaderProperty.kEnd:
        //                return;
        //            default:
        //                throw new InvalidFormatException(prop.ToString());
        //        }
        //    }
        //}
        //private static HeaderEntry[] ReadFilesInfo(StreamsInfo info, HeaderBuffer headerBytes)
        //{
        //    var entries = headerBytes.CreateArray<HeaderEntry>();
        //    int numEmptyStreams = 0;
        //    while (true)
        //    {
        //        var type = headerBytes.ReadProperty();
        //        if (type == HeaderProperty.kEnd)
        //        {
        //            break;
        //        }

        //        var size = (int)headerBytes.ReadEncodedInt64();

        //        switch (type)
        //        {
        //            case HeaderProperty.kName:
        //                {
        //                    if (headerBytes.ReadByte() != 0)
        //                    {
        //                        throw new InvalidFormatException("Cannot be external");
        //                    }
        //                    entries.ForEach(f => f.Name = headerBytes.ReadName());
        //                    break;
        //                }
        //            case HeaderProperty.kEmptyStream:
        //                {
        //                    info.EmptyStreamFlags = headerBytes.ReadBoolFlags(entries.Length);
        //                    numEmptyStreams = info.EmptyStreamFlags.Where(x => x).Count();
        //                    break;
        //                }
        //            case HeaderProperty.kEmptyFile: //just read bytes
        //            case HeaderProperty.kAnti:
        //                {
        //                    info.EmptyFileFlags = headerBytes.ReadBoolFlags(numEmptyStreams);
        //                    break;
        //                }
        //            default:
        //                {
        //                    headerBytes.ReadBytes(size);
        //                    break;
        //                }
        //        }
        //    }
        //    int emptyFileIndex = 0;
        //    int sizeIndex = 0;
        //    for (int i = 0; i < entries.Length; i++)
        //    {
        //        HeaderEntry file = entries[i];
        //        file.IsAnti = false;
        //        if (info.EmptyStreamFlags == null)
        //        {
        //            file.HasStream = true;
        //        }
        //        else
        //        {
        //            file.HasStream = !info.EmptyStreamFlags[i];
        //        }
        //        if (file.HasStream)
        //        {
        //            file.IsDirectory = false;
        //            file.Size = info.UnpackedStreams[sizeIndex].UnpackedSize;
        //            file.FileCRC = info.UnpackedStreams[sizeIndex].Digest;
        //            sizeIndex++;
        //        }
        //        else
        //        {
        //            if (info.EmptyFileFlags == null)
        //            {
        //                file.IsDirectory = true;
        //            }
        //            else
        //            {
        //                file.IsDirectory = !info.EmptyFileFlags[emptyFileIndex];
        //            }
        //            emptyFileIndex++;
        //            file.Size = 0;
        //        }
        //    }
        //    return entries;
        //}

        private void ReadPackedStreams(Stream stream)
        {
            //StreamsInfo info = new StreamsInfo();
            while (true)
            {
                var prop = (HeaderProperty)stream.ReadByte();
                switch (prop)
                {
                    case HeaderProperty.kUnpackInfo:
                        {
                            var folders = ReadUnPackInfo(stream);
                        }
                        break;
                    case HeaderProperty.kPackInfo:
                        {
                            //ReadPackInfo(info, headerBytes);
                        }
                        break;
                    case HeaderProperty.kSubStreamsInfo:
                        {
                            //ReadSubStreamsInfo(info, headerBytes);
                        }
                        break;
                    case HeaderProperty.kEnd:
                        //return info;
                        return;
                    default:
                        throw new Exception(prop.ToString());
                }
            }
        }

        //private static void ReadSubStreamsInfo(StreamsInfo info, HeaderBuffer headerBytes)
        //{
        //    info.UnpackedStreams = new List<UnpackedStreamInfo>();
        //    foreach (var folder in info.Folders)
        //    {
        //        folder.UnpackedStreams = new UnpackedStreamInfo[1];
        //        folder.UnpackedStreams[0] = new UnpackedStreamInfo();
        //        info.UnpackedStreams.Add(folder.UnpackedStreams[0]);
        //    }

        //    bool loop = true;
        //    var prop = HeaderProperty.kEnd;
        //    while (loop)
        //    {
        //        prop = headerBytes.ReadProperty();
        //        switch (prop)
        //        {
        //            case HeaderProperty.kNumUnPackStream:
        //                {
        //                    info.UnpackedStreams.Clear();
        //                    foreach (var folder in info.Folders)
        //                    {
        //                        var numStreams = (int)headerBytes.ReadEncodedInt64();
        //                        folder.UnpackedStreams = new UnpackedStreamInfo[numStreams];
        //                        folder.UnpackedStreams.Initialize(() => new UnpackedStreamInfo());
        //                        info.UnpackedStreams.AddRange(folder.UnpackedStreams);
        //                    }
        //                }
        //                break;
        //            case HeaderProperty.kCRC:
        //            case HeaderProperty.kSize:
        //            case HeaderProperty.kEnd:
        //                {
        //                    loop = false;
        //                }
        //                break;
        //            default:
        //                throw new InvalidFormatException(prop.ToString());
        //        }
        //    }

        //    int si = 0;
        //    for (int i = 0; i < info.Folders.Length; i++)
        //    {
        //        var folder = info.Folders[i];
        //        ulong sum = 0;
        //        if (folder.UnpackedStreams.Length == 0)
        //        {
        //            continue;
        //        }
        //        if (prop == HeaderProperty.kSize)
        //        {
        //            for (int j = 1; j < folder.UnpackedStreams.Length; j++)
        //            {
        //                ulong size = headerBytes.ReadEncodedInt64();
        //                info.UnpackedStreams[si].UnpackedSize = size;
        //                sum += size;
        //                si++;
        //            }
        //        }
        //        info.UnpackedStreams[si].UnpackedSize = folder.GetUnpackSize() - sum;
        //        si++;
        //    }
        //    if (prop == HeaderProperty.kSize)
        //    {
        //        prop = headerBytes.ReadProperty();
        //    }

        //    int numDigests = 0;
        //    foreach (var folder in info.Folders)
        //    {
        //        if (folder.UnpackedStreams.Length != 1 || !folder.UnpackCRC.HasValue)
        //        {
        //            numDigests += folder.UnpackedStreams.Length;
        //        }
        //    }

        //    si = 0;
        //    while (true)
        //    {
        //        if (prop == HeaderProperty.kCRC)
        //        {
        //            int digestIndex = 0;
        //            uint?[] digests2;
        //            UnPackDigests(headerBytes, numDigests, out digests2);
        //            for (uint i = 0; i < info.Folders.Length; i++)
        //            {
        //                Folder folder = info.Folders[i];
        //                if (folder.UnpackedStreams.Length == 1 && folder.UnpackCRC.HasValue)
        //                {
        //                    info.UnpackedStreams[si].Digest = folder.UnpackCRC;
        //                    si++;
        //                }
        //                else
        //                {
        //                    for (uint j = 0; j < folder.UnpackedStreams.Length; j++, digestIndex++)
        //                    {
        //                        info.UnpackedStreams[si].Digest = digests2[digestIndex];
        //                        si++;
        //                    }
        //                }
        //            }
        //        }
        //        else if (prop == HeaderProperty.kEnd)
        //            return;
        //        prop = headerBytes.ReadProperty();
        //    }
        //}

        private List<Folder> ReadUnPackInfo(Stream stream)
        {
            var prop = (HeaderProperty)stream.ReadByte();
            int count = stream.ReadEncodedInt();

            var list = new List<Folder>();
            if (stream.ReadByte() != 0) throw new NotSupportedException("External flag");

            for (int i = 0; i < count; i++)
            {
                var fd = new Folder();
                fd.Read(stream);
                list.Add(fd);
            }

            prop = (HeaderProperty)stream.ReadByte();
            if (prop != HeaderProperty.kCodersUnpackSize) throw new Exception("Expected Size Property");

            foreach (var folder in list)
            {
                //int numOutStreams = folder.Coders.Aggregate(0, (sum, coder) => sum + (int)coder.NumberOfOutStreams);

                //folder.UnpackedStreamSizes = new UInt64[numOutStreams];

                //for (uint j = 0; j < numOutStreams; j++)
                //{
                //    folder.UnpackedStreamSizes[j] = (UInt64)stream.ReadEncodedInt();
                //}
            }

            prop = (HeaderProperty)stream.ReadByte();
            if (prop != HeaderProperty.kCRC) return list;

            UInt32[] crcs = new UInt32[0];
            //UnPackDigests(stream, list.Count, out crcs);
            for (int i = 0; i < list.Count; i++)
            {
                Folder folder = list[i];
                folder.UnpackCRC = crcs[i];
            }

            prop = (HeaderProperty)stream.ReadByte();
            if (prop != HeaderProperty.kEnd) throw new Exception("Expected End property");

            return list;
        }

        //private static void UnPackDigests(HeaderBuffer headerBytes, int numItems, out uint?[] digests)
        //{
        //    var digestsDefined = headerBytes.ReadBoolFlagsDefaultTrue(numItems);
        //    digests = new uint?[numItems];
        //    for (int i = 0; i < numItems; i++)
        //    {
        //        if (digestsDefined[i])
        //        {
        //            digests[i] = headerBytes.ReadUInt32();
        //        }
        //    }
        //}

        //private Folder ReadFolder(HeaderBuffer headerBytes)
        //{
        //    Folder folder = new Folder(this);
        //    folder.Coders = headerBytes.CreateArray<CodersInfo>();

        //    int numInStreams = 0;
        //    int numOutStreams = 0;

        //    foreach (var coder in folder.Coders)
        //    {
        //        byte mainByte = headerBytes.ReadByte();
        //        int size = (byte)(mainByte & 0xF);
        //        coder.Method = headerBytes.ReadBytes(size);
        //        if ((mainByte & 0x10) != 0)
        //        {
        //            coder.NumberOfInStreams = headerBytes.ReadEncodedInt64();
        //            coder.NumberOfOutStreams = headerBytes.ReadEncodedInt64();
        //        }
        //        else
        //        {
        //            coder.NumberOfInStreams = 1;
        //            coder.NumberOfOutStreams = 1;
        //        }
        //        if ((mainByte & 0x20) != 0)
        //        {
        //            ulong propertiesSize = headerBytes.ReadEncodedInt64();
        //            coder.Properties = headerBytes.ReadBytes((int)propertiesSize);
        //        }
        //        while ((mainByte & 0x80) != 0)
        //        {
        //            mainByte = headerBytes.ReadByte();
        //            headerBytes.ReadBytes(mainByte & 0xF);
        //            if ((mainByte & 0x10) != 0)
        //            {
        //                headerBytes.ReadEncodedInt64();
        //                headerBytes.ReadEncodedInt64();
        //            }
        //            if ((mainByte & 0x20) != 0)
        //            {
        //                ulong propertiesSize = headerBytes.ReadEncodedInt64();
        //                headerBytes.ReadBytes((int)propertiesSize);
        //            }
        //        }
        //        numInStreams += (int)coder.NumberOfInStreams;
        //        numOutStreams += (int)coder.NumberOfOutStreams;
        //    }

        //    int numBindPairs = numOutStreams - 1;
        //    folder.BindPairs = new BindPair[numBindPairs];

        //    for (int i = 0; i < numBindPairs; i++)
        //    {
        //        BindPair bindpair = new BindPair();
        //        folder.BindPairs[i] = bindpair;
        //        bindpair.InIndex = headerBytes.ReadEncodedInt64();
        //        bindpair.OutIndex = headerBytes.ReadEncodedInt64();
        //    }


        //    int numPackedStreams = numInStreams - numBindPairs;

        //    folder.PackedStreamIndices = new ulong[numPackedStreams];

        //    if (numPackedStreams == 1)
        //    {
        //        uint pi = 0;
        //        for (uint j = 0; j < numInStreams; j++)
        //        {
        //            if (!folder.BindPairs.Where(x => x.InIndex == j).Any())
        //            {
        //                folder.PackedStreamIndices[pi++] = j;
        //                break;
        //            }
        //        }
        //    }
        //    else
        //    {
        //        for (uint i = 0; i < numPackedStreams; i++)
        //        {
        //            folder.PackedStreamIndices[i] = headerBytes.ReadEncodedInt64();
        //        }
        //    }
        //    return folder;
        //}

        //private static void ReadPackInfo(StreamsInfo info, HeaderBuffer headerBytes)
        //{
        //    info.PackPosition = headerBytes.ReadEncodedInt64();
        //    int count = (int)headerBytes.ReadEncodedInt64();

        //    info.PackedStreams = new PackedStreamInfo[count];
        //    for (int i = 0; i < count; i++)
        //    {
        //        info.PackedStreams[i] = new PackedStreamInfo();
        //    }
        //    var prop = headerBytes.ReadProperty();
        //    if (prop != HeaderProperty.kSize)
        //    {
        //        throw new InvalidFormatException("Expected Size Property");
        //    }
        //    for (int i = 0; i < count; i++)
        //    {
        //        info.PackedStreams[i].PackedSize = headerBytes.ReadEncodedInt64();

        //    }
        //    for (int i = 0; i < count; i++)
        //    {
        //        prop = headerBytes.ReadProperty();
        //        if (prop != HeaderProperty.kCRC)
        //        {
        //            break;
        //        }
        //        info.PackedStreams[i].Crc = headerBytes.ReadEncodedInt64();
        //    }
        //}
        #endregion

        #region 写入
        /// <summary>把Zip格式数据写入到数据流中</summary>
        /// <param name="stream"></param>
        public void Write(Stream stream)
        {
            if (stream == null) throw new ArgumentNullException("stream");

            //writer.Flush();
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

        #region 内部
        class Folder
        {
            private Int32[] _Coders;
            /// <summary>属性说明</summary>
            public Int32[] Coders { get { return _Coders; } set { _Coders = value; } }

            private UInt64[] _UnpackedStreamSizes;
            /// <summary>属性说明</summary>
            public UInt64[] UnpackedStreamSizes { get { return _UnpackedStreamSizes; } set { _UnpackedStreamSizes = value; } }

            private UInt32 _UnpackCRC;
            /// <summary>属性说明</summary>
            public UInt32 UnpackCRC { get { return _UnpackCRC; } set { _UnpackCRC = value; } }

            public Boolean Read(Stream stream)
            {


                return true;
            }
        }
        #endregion
    }

    /// <summary>
    /// Class to pack data into archives supported by 7-Zip.
    /// </summary>
    /// <example>
    /// var compr = new SevenZipCompressor();
    /// compr.CompressDirectory(@"C:\Dir", @"C:\Archive.7z");
    /// </example>
    public sealed partial class SevenZipCompressor
    {
        private static volatile int _lzmaDictionarySize = 1 << 22;

        /// <summary>
        /// Checks if the specified stream supports compression.
        /// </summary>
        /// <param name="stream">The stream to check.</param>
        private static void ValidateStream(Stream stream)
        {
            if (!stream.CanWrite || !stream.CanSeek)
            {
                throw new ArgumentException("The specified stream can not seek or is not writable.", "stream");
            }
        }

        /// <summary>
        /// Gets or sets the dictionary size for the managed LZMA algorithm.
        /// </summary>
        public static int LzmaDictionarySize { get { return _lzmaDictionarySize; } set { _lzmaDictionarySize = value; } }

        internal static void WriteLzmaProperties(LzmaEncoder encoder)
        {
            #region LZMA properties definition

            CoderPropID[] propIDs =          
            {
                    CoderPropID.DictionarySize,
                    CoderPropID.PosStateBits,
                    CoderPropID.LitContextBits,
                    CoderPropID.LitPosBits,
                    CoderPropID.Algorithm,
                    CoderPropID.NumFastBytes,
                    CoderPropID.MatchFinder,
                    CoderPropID.EndMarker
                };
            object[] properties =
            {
                    _lzmaDictionarySize,
                    2,
                    3,
                    0,
                    2,
                    256,
                    "bt4",
                    false
                };

            #endregion

            encoder.SetCoderProperties(propIDs, properties);
        }

        /// <summary>
        /// Compresses the specified stream with LZMA algorithm (C# inside)
        /// </summary>
        /// <param name="inStream">The source uncompressed stream</param>
        /// <param name="outStream">The destination compressed stream</param>
        /// <param name="inLength">The length of uncompressed data (null for inStream.Length)</param>
        /// <param name="codeProgressEvent">The event for handling the code progress</param>
        public static void CompressStream(Stream inStream, Stream outStream, int? inLength)
        {
            if (!inStream.CanRead || !outStream.CanWrite)
            {
                throw new ArgumentException("The specified streams are invalid.");
            }
            var encoder = new LzmaEncoder();
            WriteLzmaProperties(encoder);
            encoder.WriteCoderProperties(outStream);
            long streamSize = inLength.HasValue ? inLength.Value : inStream.Length;
            for (int i = 0; i < 8; i++)
            {
                outStream.WriteByte((byte)(streamSize >> (8 * i)));
            }
            encoder.Code(inStream, outStream, -1, -1, null);
        }

        /// <summary>
        /// Compresses byte array with LZMA algorithm (C# inside)
        /// </summary>
        /// <param name="data">Byte array to compress</param>
        /// <returns>Compressed byte array</returns>
        public static byte[] CompressBytes(byte[] data)
        {
            using (var inStream = new MemoryStream(data))
            {
                using (var outStream = new MemoryStream())
                {
                    var encoder = new LzmaEncoder();
                    WriteLzmaProperties(encoder);
                    encoder.WriteCoderProperties(outStream);
                    long streamSize = inStream.Length;
                    for (int i = 0; i < 8; i++)
                        outStream.WriteByte((byte)(streamSize >> (8 * i)));
                    encoder.Code(inStream, outStream, -1, -1, null);
                    return outStream.ToArray();
                }
            }
        }
    }

    enum HeaderProperty
    {
        kEnd,

        kHeader,

        kArchiveProperties,

        kAdditionalStreamsInfo,
        kMainStreamsInfo,
        kFilesInfo,

        kPackInfo,
        kUnpackInfo,
        kSubStreamsInfo,

        kSize,
        kCRC,

        kFolder,

        kCodersUnpackSize,
        kNumUnpackStream,

        kEmptyStream,
        kEmptyFile,
        kAnti,

        kName,
        kCTime,
        kATime,
        kMTime,
        kWinAttrib,
        kComment,

        kEncodedHeader,

        kStartPos,
        kDummy

        // kNtSecure,
        // kParent,
        // kIsAux
    };

}