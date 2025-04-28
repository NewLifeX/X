using System.IO.Compression;
using System.Runtime.Serialization;
using System.Text;
using System.Web.Script.Serialization;
using System.Xml.Serialization;
using NewLife.Serialization;

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
    public TarFile(Stream stream, Boolean leveOpen = false)
    {
        _stream = stream;
        _leaveOpen = leveOpen;
    }

    /// <summary>打开一个 Tar 归档文件</summary>
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
    /// <param name="disposing"></param>
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
    /// <param name="stream"></param>
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
    public void Write(Stream stream)
    {
        foreach (var entry in _entries)
        {
            entry.Write(stream);
            entry.WriteContent(stream);
        }

        // 写入结束标志（两个空块）
        stream.Write(new Byte[1024], 0, 1024);
        stream.Flush();

        // 修改文件时，截断超长部分
        if (stream is FileStream fs)
            fs.SetLength(fs.Position);
    }

    /// <summary>创建一个 Tar 归档文件的条目。</summary>
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
    public void CreateFromDirectory(String sourceDirectoryName)
    {
        if (sourceDirectoryName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(sourceDirectoryName));

        var di = sourceDirectoryName.AsDirectory();
        foreach (var fi in di.GetAllFiles(null, true))
        {
            // 计算相对路径作为文件名
            var relativePath = fi.FullName.Substring(sourceDirectoryName.Length + 1).Replace('\\', '/');

            var entry = CreateEntryFromFile(fi.FullName, relativePath);
        }

        Write(_stream);
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
    #endregion
}

/// <summary>
/// 表示 Tar 文件头的类，存储文件的元数据信息。
/// 遵循 POSIX ustar 格式，文件头长度为 512 字节。
/// </summary>
public class TarEntry
{
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
    public void Write(Stream stream)
    {
        var name = FileName;
        var type = TypeFlag;
        var entry2 = this;
        if (name.Length > 100)
        {
            entry2 = (MemberwiseClone() as TarEntry)!;

            name = "././@LongLink";
            type = TarEntryType.LongPath;
            FileSize = FileName.Length;
        }

        var header = new Byte[512];
        WriteString(header, 0, name, 100);
        WriteString(header, 100, Mode, 8);
        WriteString(header, 108, OwnerId, 8);
        WriteString(header, 116, GroupId, 8);
        WriteString(header, 124, Convert.ToString(FileSize, 8).PadLeft(11, '0') + "\0", 12);
        WriteString(header, 136, Convert.ToString(LastModified.ToInt(), 8).PadLeft(11, '0') + "\0", 12);
        WriteString(header, 148, "000000\0 ", 8);
        header[156] = (Byte)type;
        WriteString(header, 157, LinkName, 100);
        WriteString(header, 257, Magic, 6);
        WriteString(header, 263, Version.ToString("00"), 2);
        WriteString(header, 265, OwnerName, 32);
        WriteString(header, 297, GroupName, 32);
        WriteString(header, 329, DeviceMajor, 8);
        WriteString(header, 337, DeviceMinor, 8);
        WriteString(header, 345, Prefix, 155);

        // 计算校验和
        Int64 checksum = 0;
        for (var i = 0; i < 512; i++)
        {
            if (i is >= 148 and < 156)
                checksum += ' '; // 校验和区域按空格计算
            else
                checksum += header[i];
        }
        WriteString(header, 148, Convert.ToString(checksum, 8).PadLeft(6, '0') + "\0 ", 8);

        stream.Write(header, 0, 512);

        // 超长文件名
        if (type == TarEntryType.LongPath)
        {
            var longName = Encoding.ASCII.GetBytes(FileName);
            stream.Write(longName, 0, longName.Length);

            var padding = 512 - longName.Length % 512;
            if (padding > 0) stream.Write(new Byte[padding], 0, padding);

            entry2.FileName = "@PathCut";
            entry2.Write(stream);
        }
    }

    /// <summary>从流中读取文件头。</summary>
    public static TarEntry? Read(Stream stream)
    {
        var header = new Byte[512];
        var read = stream.Read(header, 0, 512);
        if (read < 512 || header.All(e => e == 0)) return null;

        var entry = new TarEntry
        {
            FileName = ReadString(header, 0, 100),
            Mode = ReadString(header, 100, 8),
            OwnerId = ReadString(header, 108, 8),
            GroupId = ReadString(header, 116, 8),
            FileSize = Convert.ToInt64(ReadString(header, 124, 12).Trim(), 8),
            LastModified = ReadString(header, 136, 12).Trim('\0').TrimStart('0').ToInt().ToDateTime(),
            Checksum = (UInt64)ReadString(header, 148, 8).ToLong(),
            TypeFlag = (TarEntryType)header[156],
            LinkName = ReadString(header, 157, 100),
            Magic = ReadString(header, 257, 6),
            Version = (UInt16)ReadString(header, 263, 2).ToInt(),
            OwnerName = ReadString(header, 265, 32),
            GroupName = ReadString(header, 297, 32),
            DeviceMajor = ReadString(header, 329, 8),
            DeviceMinor = ReadString(header, 337, 8),
            Prefix = ReadString(header, 345, 155),
        };

        if (entry.TypeFlag is TarEntryType.ExtendedAttributes)
        {
        }
        // 处理长文件名
        else if (entry.TypeFlag is TarEntryType.LongLink or TarEntryType.LongPath)
        {
            read = stream.Read(header, 0, 512);
            if (read > 0)
            {
                var str = Encoding.ASCII.GetString(header, 0, Math.Min(read, (Int32)entry.FileSize)).Trim('\0');
                if (entry.TypeFlag == TarEntryType.LongLink)
                    entry.LinkName = str;
                else
                    entry.FileName = str;

                entry.TypeFlag = TarEntryType.RegularFile;

                // 对齐512字节，如果刚好是512倍数，也要空出来512字节
                var padding = (512 - read % 512);
                if (padding > 0)
                {
                    //read = stream.Read(header, 0, padding);
                    var entry2 = Read(stream);
                    if (entry2 != null) entry.FileSize = entry2.FileSize;
                }
            }
        }

        return entry;
    }

    /// <summary>读取内容。返回是否读取成功</summary>
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
            var buf = stream.ReadBytes(FileSize);
            _stream = new MemoryStream(buf);
            _position = 0;

            // 跳过补齐字节
            var padding = (512 - FileSize % 512) % 512;
            if (padding > 0)
            {
                var buffer = new Byte[padding];
#pragma warning disable CA2022 // 避免使用 "Stream.Read" 进行不准确读取
                stream.Read(buffer, 0, buffer.Length);
#pragma warning restore CA2022 // 避免使用 "Stream.Read" 进行不准确读取
            }
        }

        return true;
    }

    /// <summary>写入内存到数据流</summary>
    /// <param name="stream"></param>
    public void WriteContent(Stream stream)
    {
        var ms = Open();
        ms.CopyTo(stream, FileSize, 4096);

        var padding = (512 - FileSize % 512) % 512;
        if (padding > 0) stream.Seek(padding, SeekOrigin.Current);
    }

    /// <summary>设置文件</summary>
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
    public Stream Open()
    {
        var stream = _stream;
        if (stream == null && !_file.IsNullOrEmpty())
            stream = _stream = new FileStream(_file, FileMode.Open, FileAccess.Read);

        stream!.Position = _position;

        return stream;
    }

    private static void WriteString(Byte[] buffer, Int32 offset, String? value, Int32 length)
    {
        var bytes = Encoding.ASCII.GetBytes(value ?? String.Empty);
        Array.Copy(bytes, 0, buffer, offset, Math.Min(bytes.Length, length));
    }

    private static String ReadString(Byte[] buffer, Int32 offset, Int32 length)
    {
        var end = offset;
        while (end < offset + length && buffer[end] != 0) end++;
        return Encoding.ASCII.GetString(buffer, offset, end - offset);
    }

    /// <summary>已重载。</summary>
    /// <returns></returns>
    public override String ToString() => FileName ?? base.ToString()!;
}