using System.IO.Compression;
using System.Text;

namespace NewLife.Compression;

/// <summary>
/// Tar 文件的压缩与解压缩类，支持创建和提取 Tar 归档文件。
/// </summary>
public class TarArchiver : DisposeBase
{
    #region 属性
    // 存储文件清单
    private readonly List<String> _fileList = [];

    /// <summary>
    /// 获取归档中的文件清单。
    /// </summary>
    public IReadOnlyList<String> FileList => _fileList.AsReadOnly();

    /// <summary>获取或设置编码方式，默认为 ASCII 编码。</summary>
    public Encoding Encoding { get; set; } = Encoding.ASCII;

    private List<TarArchiveEntry> _entries = [];
    /// <summary>文件列表</summary>
    public IReadOnlyCollection<TarArchiveEntry> Entries => _entries.AsReadOnly();

    private Stream _stream;
    private Boolean _leaveOpen = false;
    #endregion

    #region 构造
    public TarArchiver() { }

    /// <summary>初始化一个 Tar 归档文件</summary>
    public TarArchiver(Stream stream, Boolean leveOpen = false)
    {
        _stream = stream;
        _leaveOpen = leveOpen;
    }

    /// <summary>打开一个 Tar 归档文件</summary>
    public TarArchiver(String fileName, Boolean isWrite = false)
    {
        if (fileName.IsNullOrEmpty()) throw new ArgumentNullException(nameof(fileName));

        var fi = fileName.AsFile();
        var isGz = fileName.EndsWithIgnoreCase(".gz");

        // 如果文件存在，则打开文件进行读取，否则创建新文件
        if (fi.Exists)
        {
            var fs = new FileStream(fileName, FileMode.Open, isWrite ? FileAccess.ReadWrite : FileAccess.Read);
            if (isGz)
                _stream = new GZipStream(fs, CompressionMode.Decompress, false);
            else
                _stream = fs;
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

        Read(_stream);
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

    #region 读写方法
    /// <summary>读取 Tar 归档文件的内容。</summary>
    /// <param name="stream"></param>
    public void Read(Stream stream)
    {
        while (true)
        {
            // 读取文件头
            var header = TarArchiveEntry.Read(stream);
            if (header == null || header.FileName.IsNullOrEmpty()) break;

            header.ReadContent(stream);

            _entries.Add(header);
        }
    }
    #endregion

    /// <summary>
    /// 创建一个 Tar 归档文件，将指定目录中的所有文件打包。
    /// </summary>
    /// <param name="sourceDirectory">源目录路径</param>
    /// <param name="tarFilePath">目标 Tar 文件路径</param>
    public void CreateTar(String sourceDirectory, String tarFilePath)
    {
        _fileList.Clear();

        using var stream = new FileStream(tarFilePath, FileMode.Create, FileAccess.Write);
        foreach (var filePath in Directory.GetFiles(sourceDirectory, "*", SearchOption.AllDirectories))
        {
            // 计算相对路径作为文件名
            var relativePath = filePath.Substring(sourceDirectory.Length + 1).Replace('\\', '/');
            _fileList.Add(relativePath);

            // 读取文件内容
            var fileData = File.ReadAllBytes(filePath);
            Int64 fileSize = fileData.Length;
            var time = File.GetLastWriteTimeUtc(filePath);

            // 创建文件头
            var header = new TarArchiveEntry
            {
                FileName = relativePath,
                Mode = "0000644", // 普通文件权限
                OwnerId = "0000000",
                GroupId = "0000000",
                FileSize = fileSize,
                LastModified = time,
                Checksum = 0, // 初始值，写入时会重新计算
                TypeFlag = '0', // 普通文件
                LinkName = String.Empty,
                Magic = "ustar",
                Version = 0,
                OwnerName = String.Empty,
                GroupName = String.Empty,
                DeviceMajor = "0000000\0",
                DeviceMinor = "0000000\0",
                Prefix = String.Empty
            };

            // 写入文件头
            header.Write(stream);

            // 写入文件内容
            stream.Write(fileData, 0, fileData.Length);

            // 补齐到 512 字节的倍数
            var padding = (512 - fileSize % 512) % 512;
            stream.Write(new Byte[padding], 0, (Int32)padding);
        }

        // 写入结束标志（两个空块）
        stream.Write(new Byte[1024], 0, 1024);
    }

    /// <summary>
    /// 解压 Tar 文件到指定目录。
    /// </summary>
    /// <param name="tarFilePath">Tar 文件路径</param>
    /// <param name="destinationDirectory">目标目录路径</param>
    public void ExtractTar(String tarFilePath, String destinationDirectory)
    {
        _fileList.Clear();

        destinationDirectory.EnsureDirectory(false);

        using var stream = new FileStream(tarFilePath, FileMode.Open, FileAccess.Read);
        while (true)
        {
            // 读取文件头
            var header = TarArchiveEntry.Read(stream);
            if (header == null || String.IsNullOrEmpty(header.FileName)) break;

            _fileList.Add(header.FileName);

            // 仅处理普通文件
            if (header.TypeFlag == '0')
            {
                // 构建目标文件路径
                var filePath = Path.Combine(destinationDirectory, header.FileName.Replace('/', Path.DirectorySeparatorChar));
                var dirPath = Path.GetDirectoryName(filePath);
                if (!Directory.Exists(dirPath))
                    Directory.CreateDirectory(dirPath);

                // 读取文件内容
                var fileData = new Byte[header.FileSize];
                stream.Read(fileData, 0, fileData.Length);
                File.WriteAllBytes(filePath, fileData);

                // 跳过补齐字节
                var padding = (512 - header.FileSize % 512) % 512;
                stream.Seek(padding, SeekOrigin.Current);
            }
            else
            {
                // 跳过非普通文件的内容
                var padding = (512 - header.FileSize % 512) % 512;
                stream.Seek(header.FileSize + padding, SeekOrigin.Current);
            }
        }
    }
}

/// <summary>
/// 表示 Tar 文件头的类，存储文件的元数据信息。
/// 遵循 POSIX ustar 格式，文件头长度为 512 字节。
/// </summary>
public class TarArchiveEntry
{
    #region 属性
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
    public Char TypeFlag { get; set; }

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
    #endregion

    /// <summary>将文件头写入到流中，固定 512 字节。</summary>
    public void Write(Stream stream)
    {
        var header = new Byte[512];
        WriteString(header, 0, FileName, 100);
        WriteString(header, 100, Mode, 8);
        WriteString(header, 108, OwnerId, 8);
        WriteString(header, 116, GroupId, 8);
        WriteString(header, 124, Convert.ToString(FileSize, 8).PadLeft(11, '0') + "\0", 12);
        WriteString(header, 136, Convert.ToString(LastModified.ToInt(), 8).PadLeft(11, '0') + "\0", 12);
        WriteString(header, 148, "000000\0 ", 8);
        header[156] = (Byte)TypeFlag;
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
    }

    /// <summary>从流中读取文件头。</summary>
    public static TarArchiveEntry? Read(Stream stream)
    {
        var header = new Byte[512];
        var read = stream.Read(header, 0, 512);
        if (read < 512 || header.All(e => e == 0)) return null;

        var tarHeader = new TarArchiveEntry
        {
            FileName = ReadString(header, 0, 100),
            Mode = ReadString(header, 100, 8),
            OwnerId = ReadString(header, 108, 8),
            GroupId = ReadString(header, 116, 8),
            FileSize = Convert.ToInt64(ReadString(header, 124, 12).Trim(), 8),
            LastModified = ReadString(header, 136, 12).Trim('\0').TrimStart('0').ToInt().ToDateTime(),
            Checksum = (UInt64)ReadString(header, 148, 8).ToLong(),
            TypeFlag = (Char)header[156],
            LinkName = ReadString(header, 157, 100),
            Magic = ReadString(header, 257, 6),
            Version = (UInt16)ReadString(header, 263, 2).ToInt(),
            OwnerName = ReadString(header, 265, 32),
            GroupName = ReadString(header, 297, 32),
            DeviceMajor = ReadString(header, 329, 8),
            DeviceMinor = ReadString(header, 337, 8),
            Prefix = ReadString(header, 345, 155),
        };

        return tarHeader;
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

    /// <summary>打开流</summary>
    public Stream Open()
    {
        var stream = _stream;
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
}