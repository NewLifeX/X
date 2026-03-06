using System.Buffers;
using System.IO.Compression;
using System.Runtime.Serialization;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Buffers;

namespace NewLife.Compression;

/// <summary>Tar条目类型</summary>
public enum TarEntryType : Byte
{
    /// <summary>普通文件。此条目类型特定于 Ustar、Pax 和 Gnu 格式。</summary>
    RegularFile = (Byte)'0',

    /// <summary>硬链接。</summary>
    HardLink = (Byte)'1',

    /// <summary>符号链接。</summary>
    SymbolicLink = (Byte)'2',

    /// <summary>字符设备特殊文件。此条目类型仅在 Unix 平台上支持写入。</summary>
    CharacterDevice = (Byte)'3',

    /// <summary>块设备特殊文件。此条目类型仅在 Unix 平台上支持写入。</summary>
    BlockDevice = (Byte)'4',

    /// <summary>目录。</summary>
    Directory = (Byte)'5',

    /// <summary>FIFO 特殊文件。此条目类型仅在 Unix 平台上支持写入。</summary>
    Fifo = (Byte)'6',

    /// <summary>GNU 连续文件。此条目类型特定于 Gnu 格式，并被视为普通文件类型。</summary>
    ContiguousFile = (Byte)'7',

    /// <summary>PAX 扩展属性条目。元数据条目类型。</summary>
    ExtendedAttributes = (Byte)'x',

    /// <summary>PAX 全局扩展属性条目。元数据条目类型。</summary>
    GlobalExtendedAttributes = (Byte)'g',

    /// <summary>GNU 包含条目列表的目录。此条目类型特定于 Gnu 格式，并被视为包含数据部分的目录类型。</summary>
    DirectoryList = (Byte)'D',

    /// <summary>GNU 长链接。元数据条目类型。</summary>
    LongLink = (Byte)'K',

    /// <summary>GNU 长路径。元数据条目类型。</summary>
    LongPath = (Byte)'L',

    /// <summary>GNU 多卷文件。此条目类型特定于 Gnu 格式，不支持写入。</summary>
    MultiVolume = (Byte)'M',

    /// <summary>V7 普通文件。此条目类型特定于 V7 格式。</summary>
    V7RegularFile = (Byte)'\0',

    /// <summary>GNU 文件重命名或符号链接。此条目类型特定于 Gnu 格式，被认为不安全，其他工具会忽略。</summary>
    RenamedOrSymlinked = (Byte)'N',

    /// <summary>GNU 稀疏文件。此条目类型特定于 Gnu 格式，不支持写入。</summary>
    SparseFile = (Byte)'S',

    /// <summary>GNU 磁带卷。此条目类型特定于 Gnu 格式，不支持写入。</summary>
    TapeVolume = (Byte)'V',
}

/// <summary>
/// Tar 文件的压缩与解压缩类，支持创建和提取 Tar 归档文件。
/// </summary>
public class TarFile : DisposeBase
{
    private static readonly Byte[] _endMarker = new Byte[1024];

    #region 属性
    private List<TarEntry> _entries = [];
    /// <summary>文件列表</summary>
    public IReadOnlyCollection<TarEntry> Entries => _entries.AsReadOnly();

    private Stream? _stream;
    private Boolean _leaveOpen = false;
    #endregion

    #region 构造
    /// <summary>实例化</summary>
    public TarFile() { }

    /// <summary>初始化一个 Tar 归档文件</summary>
    /// <param name="stream">要读取或写入的底层数据流</param>
    /// <param name="leveOpen">释放本对象时是否保留底层流为打开状态</param>
    public TarFile(Stream stream, Boolean leveOpen = false)
    {
        _stream = stream;
        _leaveOpen = leveOpen;
    }

    /// <summary>打开一个 Tar 归档文件</summary>
    /// <param name="fileName">Tar 文件路径，支持 .tar、.tar.gz/.tgz</param>
    /// <param name="isWrite">是否以可写方式打开；不存在时可创建</param>
    public TarFile(String fileName, Boolean isWrite = false)
    {
        if (fileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(fileName));

        var fi = fileName.AsFile();
        var isGz = fileName.EndsWithIgnoreCase(".gz", ".tgz");

        // 如果文件存在，则打开文件进行读取，否则创建新文件
        if (fi.Exists)
        {
            var fs = new FileStream(fileName, FileMode.Open, isWrite ? FileAccess.ReadWrite : FileAccess.Read);
            if (isGz)
                _stream = new GZipStream(fs, CompressionMode.Decompress, false);
            else
                _stream = fs;

            Read(_stream);
        }
        else
        {
            if (!isWrite)
                throw new FileNotFoundException("文件不存在", fileName);

            var fs = new FileStream(fileName, FileMode.Create, FileAccess.Write);
            if (isGz)
                _stream = new GZipStream(fs, CompressionMode.Compress, false);
            else
                _stream = fs;
        }
    }

    /// <summary>销毁</summary>
    /// <param name="disposing">从 Dispose() 调用或终结器调用</param>
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);

        var stream = _stream;
        if (!_leaveOpen && stream != null)
        {
            stream.Flush();
            stream.Dispose();

            _stream = null!;
        }
    }
    #endregion

    #region 方法
    /// <summary>读取 Tar 归档文件的内容。</summary>
    /// <param name="stream">包含 Tar 数据的输入流</param>
    public void Read(Stream stream)
    {
        while (true)
        {
            // 读取文件头
            var entry = TarEntry.Read(stream);
            if (entry == null || entry.FileName.IsNullOrEmpty()) break;

            entry.Archiver = this;
            entry.ReadContent(stream);

            _entries.Add(entry);
        }
    }

    /// <summary>写入 Tar 归档文件的内容。</summary>
    /// <param name="stream">写入目标流</param>
    public void Write(Stream stream)
    {
        foreach (var entry in _entries)
        {
            entry.Write(stream);
            entry.WriteContent(stream);
        }

        // 写入结束标志（两个空块）
        WriteEndMarker(stream);
        stream.Flush();

        // 修改文件时，截断超长部分
        if (stream is FileStream fs)
            fs.SetLength(fs.Position);
    }

    /// <summary>创建一个 Tar 归档文件的条目。</summary>
    /// <param name="sourceFileName">源文件路径</param>
    /// <param name="entryName">写入归档内的路径名称，留空则使用文件名</param>
    /// <returns>创建的条目</returns>
    public TarEntry CreateEntryFromFile(String sourceFileName, String entryName)
    {
        if (sourceFileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(sourceFileName));
        if (entryName.IsNullOrEmpty()) entryName = Path.GetFileName(sourceFileName);

        var entry = new TarEntry
        {
            Archiver = this,
            FileName = entryName,
            Mode = "0000644", // 普通文件权限
            OwnerId = "0000000",
            GroupId = "0000000",
            //FileSize = fi.Length,
            //LastModified = fi.LastAccessTimeUtc,
            Checksum = 0, // 初始值，写入时会重新计算
            TypeFlag = TarEntryType.RegularFile, // 普通文件
            LinkName = String.Empty,
            Magic = "ustar",
            Version = 0,
            OwnerName = String.Empty,
            GroupName = String.Empty,
            DeviceMajor = "0000000\0",
            DeviceMinor = "0000000\0",
            Prefix = String.Empty
        };

        entry.SetFile(sourceFileName);

        _entries.Add(entry);

        return entry;
    }

    /// <summary>将指定目录中的所有文件打包。</summary>
    /// <param name="sourceDirectoryName">源目录路径</param>
    public void CreateFromDirectory(String sourceDirectoryName)
    {
        if (sourceDirectoryName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(sourceDirectoryName));

        var di = sourceDirectoryName.AsDirectory();
        foreach (var fi in di.GetAllFiles(null, true))
        {
            // 计算相对路径作为文件名
            var relativePath = fi.FullName[(sourceDirectoryName.Length + 1)..].Replace('\\', '/');

            var entry = CreateEntryFromFile(fi.FullName, relativePath);
        }

        Write(_stream!);
    }

    /// <summary>创建一个 Tar 归档文件，将指定目录中的所有文件打包。</summary>
    /// <param name="sourceDirectoryName">源目录路径</param>
    /// <param name="destinationArchiveFileName">目标 Tar 文件路径</param>
    public static void CreateFromDirectory(String sourceDirectoryName, String destinationArchiveFileName)
    {
        if (sourceDirectoryName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(sourceDirectoryName));
        if (destinationArchiveFileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(destinationArchiveFileName));

        using var tar = new TarFile(destinationArchiveFileName, true);
        tar.CreateFromDirectory(sourceDirectoryName);
    }

    /// <summary>解压 Tar 文件到指定目录。</summary>
    /// <param name="destinationDirectoryName">目标目录路径</param>
    /// <param name="overwriteFiles">是否覆盖已存在文件</param>
    public void ExtractToDirectory(String destinationDirectoryName, Boolean overwriteFiles = false)
    {
        foreach (var entry in _entries)
        {
            if (entry.TypeFlag == TarEntryType.RegularFile)
            {
                // 构建目标文件路径
                var filePath = Path.Combine(destinationDirectoryName, entry.FileName.Replace('/', Path.DirectorySeparatorChar));
                if (overwriteFiles || !File.Exists(filePath))
                {
                    filePath.EnsureDirectory(true);

                    using var fs = new FileStream(filePath, FileMode.OpenOrCreate, FileAccess.Write);
                    entry.Open().CopyTo(fs, entry.FileSize, 4096);

                    fs.SetLength(fs.Position);
                }
            }
            else if (entry.TypeFlag == TarEntryType.Directory)
            {
                // 显式目录条目，确保空目录也能创建
                var dirPath = Path.Combine(destinationDirectoryName, entry.FileName.Replace('/', Path.DirectorySeparatorChar));
                dirPath.EnsureDirectory(false);
            }
        }
    }

    /// <summary>解压 Tar 文件到指定目录。</summary>
    /// <param name="sourceArchiveFileName">Tar 文件路径</param>
    /// <param name="destinationDirectoryName">目标目录路径</param>
    /// <param name="overwriteFiles">是否覆盖已存在文件</param>
    public static void ExtractToDirectory(String sourceArchiveFileName, String destinationDirectoryName, Boolean overwriteFiles = false)
    {
        if (sourceArchiveFileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(sourceArchiveFileName));

        destinationDirectoryName.EnsureDirectory(false);

        using var tar = new TarFile(sourceArchiveFileName, false);
        tar.ExtractToDirectory(destinationDirectoryName, overwriteFiles);
    }

    private static void WriteEndMarker(Stream stream)
    {
        if (stream.CanSeek)
        {
            stream.Seek(_endMarker.Length, SeekOrigin.Current);

            try
            {
                var position = stream.Position;
                if (position > stream.Length)
                    stream.SetLength(position);
            }
            catch
            {
                stream.Write(_endMarker, 0, _endMarker.Length);
            }

            return;
        }

        stream.Write(_endMarker, 0, _endMarker.Length);
    }
    #endregion
}

/// <summary>
/// 表示 Tar 文件头的类，存储文件的元数据信息。
/// 遵循 POSIX ustar 格式，文件头长度为 512 字节。
/// </summary>
public class TarEntry
{
    private static readonly Byte[] _paddingBlock = new Byte[512];

    #region 属性
    /// <summary>归档器</summary>
    [XmlIgnore, ScriptIgnore, IgnoreDataMember]
    public TarFile Archiver { get; set; } = null!;

    /// <summary>文件名，最长 100 字节。</summary>
    public String FileName { get; set; } = null!;

    /// <summary>文件权限，8 字节，例如 "0000644"。</summary>
    public String Mode { get; set; } = null!;

    /// <summary>所有者 ID，8 字节。</summary>
    public String OwnerId { get; set; } = null!;

    /// <summary>组 ID，8 字节。</summary>
    public String GroupId { get; set; } = null!;

    /// <summary>文件大小，12 字节（八进制字符串）。</summary>
    public Int64 FileSize { get; set; }

    /// <summary>最后修改时间，12 字节（八进制 Unix 时间戳）。</summary>
    public DateTime LastModified { get; set; }

    /// <summary>校验和，8 字节（八进制）。</summary>
    public UInt64 Checksum { get; set; }

    /// <summary>文件类型标志，1 字节（'0' 表示普通文件）。</summary>
    public TarEntryType TypeFlag { get; set; }

    /// <summary>链接目标文件名，100 字节。</summary>
    public String? LinkName { get; set; }

    /// <summary>魔法字符串，6 字节（"ustar"）。</summary>
    public String? Magic { get; set; }

    /// <summary>版本，2 字节（"00"）。</summary>
    public UInt16 Version { get; set; }

    /// <summary>所有者名称，32 字节。</summary>
    public String? OwnerName { get; set; }

    /// <summary>组名称，32 字节。</summary>
    public String? GroupName { get; set; }

    /// <summary>设备主编号，8 字节。</summary>
    public String? DeviceMajor { get; set; }

    /// <summary>设备次编号，8 字节。</summary>
    public String? DeviceMinor { get; set; }

    /// <summary>文件名前缀，155 字节。</summary>
    public String? Prefix { get; set; }

    private Stream? _stream;
    private Int64 _position;
    private String? _file;
    #endregion

    /// <summary>将文件头写入到流中，固定 512 字节。</summary>
    /// <param name="stream">写入目标流</param>
    public void Write(Stream stream)
    {
        var name = FileName;
        var type = TypeFlag;
        var entry2 = this;
        Int64? origSize = null;
        var writeLongPathMeta = name.Length > 100 && type != TarEntryType.LongPath && type != TarEntryType.LongLink;
        if (writeLongPathMeta)
        {
            // 克隆条目用于实际文件头，当前条目将写入长路径元数据条目
            entry2 = (MemberwiseClone() as TarEntry)!;

            name = "././@LongLink";
            type = TarEntryType.LongPath;

            // 暂存原始大小，将当前条目的大小设置为长路径字节长度（ASCII）
            origSize = FileSize;
            var longNameBytesLen = Encoding.ASCII.GetByteCount(FileName);
            FileSize = longNameBytesLen;
        }

        var rented = ArrayPool<Byte>.Shared.Rent(512);
        try
        {
            var header = rented.AsSpan(0, 512);
            header.Clear();

            var writer = new SpanWriter(header);
            writer.Write(name, 100, Encoding.ASCII);
            writer.Write(Mode, 8, Encoding.ASCII);
            writer.Write(OwnerId, 8, Encoding.ASCII);
            writer.Write(GroupId, 8, Encoding.ASCII);
            WriteOctal(ref writer, (UInt64)FileSize, 12, false);
            WriteOctal(ref writer, (UInt64)LastModified.ToInt(), 12, false);
            writer.Write("000000\0 ", 8, Encoding.ASCII);
            writer.Write((Byte)type);
            writer.Write(LinkName, 100, Encoding.ASCII);
            writer.Write(Magic, 6, Encoding.ASCII);
            writer.Write(Version.ToString("00"), 2, Encoding.ASCII);
            writer.Write(OwnerName, 32, Encoding.ASCII);
            writer.Write(GroupName, 32, Encoding.ASCII);
            writer.Write(DeviceMajor, 8, Encoding.ASCII);
            writer.Write(DeviceMinor, 8, Encoding.ASCII);
            writer.Write(Prefix, 155, Encoding.ASCII);

            // 计算校验和
            Int64 checksum = 0;
            for (var i = 0; i < 512; i++)
            {
                if (i is >= 148 and < 156)
                    checksum += ' '; // 校验和区域按空格计算
                else
                    checksum += header[i];
            }

            writer.Position = 148;
            WriteOctal(ref writer, (UInt64)checksum, 8, true);

            stream.Write(rented, 0, 512);
        }
        finally
        {
            ArrayPool<Byte>.Shared.Return(rented);
        }

        // 超长文件名
        if (writeLongPathMeta)
        {
            var longNameLen = Encoding.ASCII.GetByteCount(FileName);
            var longName = ArrayPool<Byte>.Shared.Rent(longNameLen);
            try
            {
                var written = Encoding.ASCII.GetBytes(FileName, 0, FileName.Length, longName, 0);
                stream.Write(longName, 0, written);

                var padding = 512 - written % 512;
                if (padding > 0)
                    WritePadding(stream, padding);
            }
            finally
            {
                ArrayPool<Byte>.Shared.Return(longName);
            }

            // 写入真实文件头（已在克隆对象中保留原始 FileSize）
            entry2.FileName = "@PathCut";
            entry2.Write(stream);

            // 恢复当前条目的文件大小，供后续 WriteContent 使用
            if (origSize.HasValue) FileSize = origSize.Value;
        }
    }

    /// <summary>从流中读取文件头。</summary>
    /// <param name="stream">包含 Tar 数据的输入流</param>
    public static TarEntry? Read(Stream stream)
    {
        var rented = ArrayPool<Byte>.Shared.Rent(512);
        try
        {
            if (!ReadExactly(stream, rented, 512)) return null;

            var header = rented.AsSpan(0, 512);
            if (IsAllZero(header)) return null;

            var reader = new SpanReader(header);

            var entry = new TarEntry
            {
                FileName = ReadField(ref reader, 100),
                Mode = ReadField(ref reader, 8),
                OwnerId = ReadField(ref reader, 8),
                GroupId = ReadField(ref reader, 8),
                FileSize = (Int64)ReadOctal(ref reader, 12),
                LastModified = ((Int32)ReadOctal(ref reader, 12)).ToDateTime(),
                Checksum = ReadOctal(ref reader, 8),
                TypeFlag = (TarEntryType)reader.ReadByte(),
                LinkName = ReadField(ref reader, 100),
                Magic = ReadField(ref reader, 6),
                Version = (UInt16)ReadOctal(ref reader, 2),
                OwnerName = ReadField(ref reader, 32),
                GroupName = ReadField(ref reader, 32),
                DeviceMajor = ReadField(ref reader, 8),
                DeviceMinor = ReadField(ref reader, 8),
                Prefix = ReadField(ref reader, 155),
            };

            if (entry.TypeFlag is TarEntryType.ExtendedAttributes)
            {
            }
            // 处理长文件名
            else if (entry.TypeFlag is TarEntryType.LongLink or TarEntryType.LongPath)
            {
                var size = (Int32)entry.FileSize;
                if (size > 0)
                {
                    var nameBuffer = ArrayPool<Byte>.Shared.Rent(size);
                    try
                    {
                        if (ReadExactly(stream, nameBuffer, size))
                        {
                            var str = Encoding.ASCII.GetString(nameBuffer, 0, size).Trim('\0');
                            if (entry.TypeFlag == TarEntryType.LongLink)
                                entry.LinkName = str;
                            else
                                entry.FileName = str;
                        }
                    }
                    finally
                    {
                        ArrayPool<Byte>.Shared.Return(nameBuffer);
                    }
                }

                entry.TypeFlag = TarEntryType.RegularFile;

                var padding = (512 - size % 512) % 512;
                SkipPadding(stream, padding);

                var entry2 = Read(stream);
                if (entry2 != null) entry.FileSize = entry2.FileSize;
            }

            return entry;
        }
        finally
        {
            ArrayPool<Byte>.Shared.Return(rented);
        }
    }

    /// <summary>读取内容。返回是否读取成功</summary>
    /// <param name="stream">包含 Tar 数据的输入流</param>
    public Boolean ReadContent(Stream stream)
    {
        // 如果不是可读流，则读取出来在内存中
        if (stream.CanSeek)
        {
            _stream = stream;
            _position = stream.Position;

            // 跳过补齐字节
            var padding = (512 - FileSize % 512) % 512;
            stream.Seek(FileSize + padding, SeekOrigin.Current);
        }
        else
        {
            var buf = stream.ReadExactly(FileSize);
            _stream = new MemoryStream(buf);
            _position = 0;

            // 跳过补齐字节
            var padding = (512 - FileSize % 512) % 512;
            if (padding > 0)
                SkipPadding(stream, (Int32)padding);
        }

        return true;
    }

    /// <summary>写入内存到数据流</summary>
    /// <param name="stream">写入目标流</param>
    public void WriteContent(Stream stream)
    {
        var ms = Open();
        ms.CopyTo(stream, FileSize, 4096);

        // 按 512 字节对齐，显式写入填充零，不使用 Seek 以避免未真正写入
        var padding = (512 - FileSize % 512) % 512;
        if (padding > 0)
            WritePadding(stream, (Int32)padding);
    }

    /// <summary>设置文件</summary>
    /// <param name="fileName">要写入的源文件路径</param>
    public void SetFile(String fileName)
    {
        var fi = fileName.AsFile();

        FileSize = fi.Length;
        LastModified = fi.LastWriteTimeUtc;

        _file = fileName;
        _stream = null;
        _position = 0;
    }

    /// <summary>打开流</summary>
    /// <returns>可读取条目内容的流</returns>
    public Stream Open()
    {
        var stream = _stream;
        if (stream == null && !_file.IsNullOrEmpty())
            stream = _stream = new FileStream(_file, FileMode.Open, FileAccess.Read);

        stream!.Position = _position;

        return stream;
    }

    private static void WriteOctal(ref SpanWriter writer, UInt64 value, Int32 totalLength, Boolean trailingSpace)
    {
        var span = writer.GetSpan(totalLength);
        var target = span[..totalLength];

        target.Clear();
        var digitLength = trailingSpace ? totalLength - 2 : totalLength - 1;
        target[..digitLength].Fill((Byte)'0');

        var p = digitLength - 1;
        while (p >= 0 && value > 0)
        {
            target[p--] = (Byte)('0' + (value & 0x07));
            value >>= 3;
        }

        if (trailingSpace)
            target[totalLength - 1] = (Byte)' ';

        writer.Advance(totalLength);
    }

    private static UInt64 ReadOctal(ref SpanReader reader, Int32 length)
    {
        var data = reader.ReadBytes(length);

        UInt64 value = 0;
        foreach (var item in data)
        {
            if (item == 0 || item == ' ') break;
            if (item < '0' || item > '7') continue;

            value = (value << 3) + (UInt64)(item - '0');
        }

        return value;
    }

    private static String ReadField(ref SpanReader reader, Int32 length)
    {
        var data = reader.ReadBytes(length);

        var end = 0;
        while (end < data.Length && data[end] != 0) end++;

        return end <= 0 ? String.Empty : Encoding.ASCII.GetString(data[..end]);
    }

    private static Boolean IsAllZero(ReadOnlySpan<Byte> span)
    {
        foreach (var item in span)
        {
            if (item != 0) return false;
        }

        return true;
    }

    private static Boolean ReadExactly(Stream stream, Byte[] buffer, Int32 count)
    {
        var offset = 0;
        while (offset < count)
        {
#pragma warning disable CA2022 // 避免使用 "Stream.Read" 进行不准确读取
            var read = stream.Read(buffer, offset, count - offset);
#pragma warning restore CA2022 // 避免使用 "Stream.Read" 进行不准确读取
            if (read <= 0) return false;
            offset += read;
        }

        return true;
    }

    private static void SkipPadding(Stream stream, Int32 padding)
    {
        if (padding <= 0) return;

        if (stream.CanSeek)
        {
            stream.Seek(padding, SeekOrigin.Current);
            return;
        }

        var left = padding;
        var buffer = ArrayPool<Byte>.Shared.Rent(Math.Min(left, 512));
        try
        {
            while (left > 0)
            {
                var count = Math.Min(left, buffer.Length);
#pragma warning disable CA2022 // 避免使用 "Stream.Read" 进行不准确读取
                var read = stream.Read(buffer, 0, count);
#pragma warning restore CA2022 // 避免使用 "Stream.Read" 进行不准确读取
                if (read <= 0) break;

                left -= read;
            }
        }
        finally
        {
            ArrayPool<Byte>.Shared.Return(buffer);
        }
    }

    private static void WritePadding(Stream stream, Int32 padding)
    {
        if (padding <= 0) return;

        // 可定位流优先移动指针，避免多余写入；对不可定位流仍需写入真实零字节。
        if (stream.CanSeek)
        {
            stream.Seek(padding, SeekOrigin.Current);

            return;
        }

        var left = padding;
        while (left > 0)
        {
            var count = Math.Min(left, _paddingBlock.Length);
            stream.Write(_paddingBlock, 0, count);
            left -= count;
        }
    }

    /// <summary>已重载。</summary>
    /// <returns>文件名</returns>
    public override String ToString() => FileName ?? base.ToString()!;
}