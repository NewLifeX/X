using System;
using NewLife.IO;
using System.IO;
using System.Text;
using NewLife.Compression.LZMA;
using System.Collections.Generic;
using NewLife.Security;
using System.Linq;

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

            // 连续24字节，前4字节是CRC，后面8+8+4字节是数据
            var crc = reader.ReadUInt32();
            var p = stream.Position;
            Int64 nextHeaderOffset = reader.ReadInt64();
            Int64 nextHeaderSize = reader.ReadInt64();
            Int32 nextHeaderCRC = reader.ReadInt32();

            // 检查校验
            var crc2 = Crc32.ComputeRange(stream, p, 0);
            if (crc2 != crc) return false;

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
                            //var p = stream.Position + size;
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

                            //stream.Position = p;
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

        private StreamsInfo ReadPackedStreams(Stream stream)
        {
            var info = new StreamsInfo();
            while (true)
            {
                var prop = (HeaderProperty)stream.ReadByte();
                switch (prop)
                {
                    case HeaderProperty.kUnpackInfo:
                        {
                            ReadUnPackInfo(info, stream);
                        }
                        break;
                    case HeaderProperty.kPackInfo:
                        {
                            var reader = new BinaryReader(stream);
                            info.Offset = reader.ReadInt64();
                            ReadPackInfo(info, stream);
                        }
                        break;
                    case HeaderProperty.kSubStreamsInfo:
                        {
                            //ReadSubStreamsInfo(info, headerBytes);
                        }
                        break;
                    case HeaderProperty.kEnd:
                        return info;
                    default:
                        throw new Exception(prop.ToString());
                }
            }

            return info;
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

        private void ReadUnPackInfo(StreamsInfo info, Stream stream)
        {
            var prop = (HeaderProperty)stream.ReadByte();
            if (prop != HeaderProperty.kFolder) return;

            // 目录个数
            var count = stream.ReadEncodedInt64();

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

            var numOutStreams = list.Sum(e => e.Coders == null ? 0 : e.Coders.Length);
            info.CoderUnpackSizes = new Int64[numOutStreams];
            for (uint j = 0; j < numOutStreams; j++)
            {
                info.CoderUnpackSizes[j] = stream.ReadEncodedInt64();
            }

            prop = (HeaderProperty)stream.ReadByte();
            if (prop != HeaderProperty.kCRC) return;

            var crcs = new UInt32[list.Count];
            //UnPackDigests(stream, list.Count, out crcs);
            for (int i = 0; i < list.Count; i++)
            {
                list[i].UnpackCRC = crcs[i];
            }

            prop = (HeaderProperty)stream.ReadByte();
            if (prop != HeaderProperty.kEnd) throw new Exception("Expected End property");

            //return list;
        }

        //private static void UnPackDigests(Stream stream, int numItems, out uint[] digests)
        //{
        //    // 读取一个字节，分割多个位
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

        private static void ReadPackInfo(StreamsInfo info, Stream stream)
        {
            var reader = new BinaryReader(stream);

            info.PackPosition = stream.ReadEncodedInt64();
            int count = (int)stream.ReadEncodedInt64();

            for (int i = 0; i < count; i++)
            {
                info.PackedStreams.Add(new PackedStreamInfo());
            }
            var prop = (HeaderProperty)stream.ReadByte();
            if (prop != HeaderProperty.kSize) throw new Exception("Expected Size Property");

            for (int i = 0; i < count; i++)
            {
                info.PackedStreams[i].PackedSize = stream.ReadEncodedInt64();

            }
            for (int i = 0; i < count; i++)
            {
                prop = (HeaderProperty)stream.ReadByte();
                if (prop != HeaderProperty.kCRC) break;

                info.PackedStreams[i].Crc = stream.ReadEncodedInt64();
            }
        }
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
        class StreamsInfo
        {
            public Int64 Offset { get; set; }
            public Int64 PackPosition { get; set; }

            private List<PackedStreamInfo> _PackedStreams = new List<PackedStreamInfo>();
            /// <summary>属性说明</summary>
            public List<PackedStreamInfo> PackedStreams { get { return _PackedStreams; } set { _PackedStreams = value; } }

            private Int64[] _CoderUnpackSizes;
            /// <summary>属性说明</summary>
            public Int64[] CoderUnpackSizes { get { return _CoderUnpackSizes; } set { _CoderUnpackSizes = value; } }
        }

        class PackedStreamInfo
        {
            public Int64 PackedSize { get; set; }
            public Int64 Crc { get; set; }
        }

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
                var count = stream.ReadEncodedInt64();
                Coders = new Int32[count];

                var numInStreams = 0;
                for (int i = 0; i < count; i++)
                {
                    Byte mainByte = (Byte)stream.ReadByte();
                    if ((mainByte & 0xC0) != 0) throw new Exception("Unsupported");

                    Int32 idSize = (mainByte & 0xF);
                    if (idSize > 8) throw new Exception("Unsupported");
                    if (idSize > stream.Length - stream.Position) throw new Exception("EndOfData");

                    //const Byte* longID = inByte->GetPtr();
                    //UInt64 id = 0;
                    //for (Int32 j = 0; j < idSize; j++)
                    //    id = ((id << 8) | longID[j]);
                    //var id = stream.ReadEncodedInt64();
                    var id = stream.ReadBytes(idSize).ToUInt32(0, false);
                    //inByte->SkipDataNoCheck(idSize);
                    //stream.Seek(idSize, SeekOrigin.Current);
                    //if (folders.ParsedMethods.IDs.Size() < 128)
                    //    folders.ParsedMethods.IDs.AddToUniqueSorted(id);

                    var coderInStreams = 1;
                    if ((mainByte & 0x10) != 0)
                    {
                        coderInStreams = stream.ReadByte();
                        if (coderInStreams > 64) throw new Exception("Unsupported");
                        if (stream.ReadByte() != 1) throw new Exception("Unsupported");
                    }
                    Coders[i] = coderInStreams;

                    numInStreams += coderInStreams;
                    if (numInStreams > 64) throw new Exception("Unsupported");

                    if ((mainByte & 0x20) != 0)
                    {
                        var propsSize = stream.ReadByte();
                        if (propsSize > stream.Length - stream.Position) throw new Exception("EndOfData");
                        const Int32 k_LZMA2 = 0x21000000;
                        const Int32 k_LZMA = 0x3010100;
                        if (id == k_LZMA2 && propsSize == 1)
                        {
                            //Byte v = *_inByteBack->GetPtr();
                            //if (folders.ParsedMethods.Lzma2Prop < v)
                            //    folders.ParsedMethods.Lzma2Prop = v;
                        }
                        else if (id == k_LZMA && propsSize == 5)
                        {
                            //UInt32 dicSize = GetUi32(_inByteBack->GetPtr() + 1);
                            //if (folders.ParsedMethods.LzmaDic < dicSize)
                            //    folders.ParsedMethods.LzmaDic = dicSize;
                        }
                        //inByte->SkipDataNoCheck((size_t)propsSize);
                        stream.Seek(propsSize, SeekOrigin.Current);
                    }
                }

                if (count == 1 && numInStreams == 1)
                {
                    //indexOfMainStream = 0;
                    //numPackStreams = 1;
                }
                else
                {
                    UInt32 i;
                    var numBonds = count - 1;
                    if (numInStreams < numBonds) throw new Exception("Unsupported");

                    //BoolVector_Fill_False(StreamUsed, numInStreams);
                    //BoolVector_Fill_False(CoderUsed, count);
                    var StreamUsed = new Boolean[numInStreams];
                    var CoderUsed = new Boolean[count];

                    for (i = 0; i < numBonds; i++)
                    {
                        var index = stream.ReadByte();
                        if (index >= numInStreams || StreamUsed[index]) throw new Exception("Unsupported");

                        StreamUsed[index] = true;

                        index = stream.ReadByte();
                        if (index >= count || CoderUsed[index]) throw new Exception("Unsupported");
                        CoderUsed[index] = true;
                    }

                    var numPackStreams = numInStreams - numBonds;

                    if (numPackStreams != 1)
                        for (i = 0; i < numPackStreams; i++)
                        {
                            var index = stream.ReadByte();
                            if (index >= numInStreams || StreamUsed[index]) throw new Exception("Unsupported");
                            StreamUsed[index] = true;
                        }

                    for (i = 0; i < count; i++)
                        if (!CoderUsed[i])
                        {
                            //indexOfMainStream = i;
                            break;
                        }

                    if (i == count) throw new Exception("Unsupported");
                }

                //folders.FoToCoderUnpackSizes[fo] = numCodersOutStreams;
                //numCodersOutStreams += count;
                //folders.FoStartPackStreamIndex[fo] = packStreamIndex;
                //packStreamIndex += numPackStreams;
                //folders.FoToMainUnpackSizeIndex[fo] = (Byte)indexOfMainStream;

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