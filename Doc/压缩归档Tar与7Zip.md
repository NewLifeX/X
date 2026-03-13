# 压缩归档Tar与SevenZip

## 概述

NewLife.Core 内置对 TAR 归档格式（含 gzip 压缩）以及 7-Zip 工具的封装，适用于部署包打包、日志归档、插件分发等场景。

- **`TarFile`**：纯托管实现，读写 `.tar` / `.tar.gz` / `.tgz` 文件，无需任何外部依赖
- **`SevenZip`**：调用外部 `7z.exe`，支持更多格式（`.7z`/`.zip`/`.rar`/`.iso` 等），通过进程调用

**命名空间**：`NewLife.IO`  
**文档地址**：/core/tar_sevenzp

---

## TarFile

### 快速开始

```csharp
using NewLife.IO;

// 写入 tar.gz
using var tar = new TarFile("backup.tar.gz", isWrite: true);
tar.AddFile(@"d:\data\config.json", "config.json");
tar.AddFile(@"d:\data\app.db",       "data/app.db");
// Dispose 时自动关闭并 flush

// 读取 tar.gz
using var tar2 = new TarFile("backup.tar.gz");
foreach (var entry in tar2.Entries)
{
    Console.WriteLine($"{entry.Name}  {entry.Size}  {entry.LastModified:yyyy-MM-dd}");
    if (entry.Type == TarEntryType.RegularFile)
        entry.ExtractTo(@"d:\restore\");
}
```

### 构造函数

```csharp
/// <summary>默认构造，可后续设置 Stream</summary>
public TarFile()

/// <summary>从流读写 TAR 内容</summary>
/// <param name="stream">底层流（.tar 或已解压的 .tar.gz 流）</param>
/// <param name="leaveOpen">Dispose 时是否保留流，默认 false</param>
public TarFile(Stream stream, Boolean leaveOpen = false)

/// <summary>从文件名打开</summary>
/// <param name="fileName">文件路径。扩展名为 .gz/.tgz 时自动包裹 GZipStream</param>
/// <param name="isWrite">true=写入模式，false=读取模式（默认）</param>
public TarFile(String fileName, Boolean isWrite = false)
```

### TarEntryType 枚举

```csharp
public enum TarEntryType : Byte
{
    /// <summary>普通文件</summary>
    RegularFile    = (Byte)'0',
    /// <summary>硬链接</summary>
    HardLink       = (Byte)'1',
    /// <summary>符号链接（软链接）</summary>
    SymbolicLink   = (Byte)'2',
    /// <summary>字符设备（Linux）</summary>
    CharacterDevice = (Byte)'3',
    /// <summary>块设备（Linux）</summary>
    BlockDevice    = (Byte)'4',
    /// <summary>目录</summary>
    Directory      = (Byte)'5',
    /// <summary>FIFO 管道</summary>
    FifoFile       = (Byte)'6',
    /// <summary>GNU 长文件名扩展</summary>
    GNULongName    = (Byte)'L',
}
```

### TarEntry 属性

```csharp
public class TarEntry
{
    /// <summary>条目名（包含相对路径）</summary>
    public String Name { get; }

    /// <summary>条目类型</summary>
    public TarEntryType Type { get; }

    /// <summary>文件大小（字节），目录为0</summary>
    public Int64 Size { get; }

    /// <summary>最后修改时间</summary>
    public DateTime LastModified { get; }

    /// <summary>Unix 文件权限（八进制）</summary>
    public Int32 Mode { get; }

    /// <summary>提取到指定目录</summary>
    public void ExtractTo(String destDir);
}
```

### Entries 属性

```csharp
/// <summary>所有条目（惰性读取）</summary>
public IReadOnlyCollection<TarEntry> Entries { get; }
```

### AddFile 方法

```csharp
/// <summary>向归档中添加文件</summary>
/// <param name="sourceFile">源文件路径</param>
/// <param name="entryName">在归档中的名称（含相对路径）</param>
public void AddFile(String sourceFile, String entryName)
```

### 使用场景

#### 打包发布目录

```csharp
using NewLife.IO;

var releaseDir = @"d:\publish\MyApp";
var archivePath = $"MyApp-{DateTime.Today:yyyyMMdd}.tar.gz";

using var tar = new TarFile(archivePath, isWrite: true);
foreach (var file in Directory.GetFiles(releaseDir, "*", SearchOption.AllDirectories))
{
    var relative = Path.GetRelativePath(releaseDir, file).Replace('\\', '/');
    tar.AddFile(file, relative);
}
Console.WriteLine($"已打包: {archivePath}");
```

#### 解压到目标目录

```csharp
using var tar = new TarFile("release.tar.gz");
foreach (var entry in tar.Entries)
{
    if (entry.Type == TarEntryType.RegularFile)
        entry.ExtractTo(@"d:\deploy\");
    else if (entry.Type == TarEntryType.Directory)
    {
        var dir = Path.Combine(@"d:\deploy\", entry.Name);
        Directory.CreateDirectory(dir);
    }
}
```

---

## SevenZip

### 快速开始

```csharp
using NewLife.IO;

// 压缩
SevenZip.Compress(@"d:\data\", @"d:\backup\data.7z");

// 解压
SevenZip.Extract(@"d:\backup\data.7z", @"d:\restore\");
```

### 7z.exe 自动发现路径

调用时按以下顺序查找 `7z.exe`，找到即停止：

1. 当前目录 `.\7z.exe`
2. 插件目录 `.\Plugins\7z.exe`
3. 子目录 `.\7z\7z.exe`
4. 上级目录 `..\7z\7z.exe`
5. 尝试从 `PluginServer`（可配置的插件服务器）自动下载

### Compress - 压缩

```csharp
/// <summary>将文件或目录压缩为归档</summary>
/// <param name="sourcePath">源文件或目录路径</param>
/// <param name="destFile">目标归档文件路径（扩展名决定格式：.7z/.zip等）</param>
/// <param name="overwrite">目标已存在时是否覆盖，默认 true</param>
public static void Compress(String sourcePath, String destFile, Boolean overwrite = true)
```

```csharp
// 压缩目录为 .7z
SevenZip.Compress(@"d:\logs\", @"d:\archive\logs-2025.7z");

// 压缩为 .zip（更通用）
SevenZip.Compress(@"d:\data\report.xlsx", @"d:\archive\report.zip");
```

### Extract - 解压

```csharp
/// <summary>解压归档文件</summary>
/// <param name="archiveFile">归档文件路径</param>
/// <param name="destDir">目标目录（不存在时自动创建）</param>
/// <param name="overwrite">同名文件是否覆盖，默认 true</param>
public static void Extract(String archiveFile, String destDir, Boolean overwrite = true)
```

```csharp
// 解压到指定目录
SevenZip.Extract(@"d:\packages\plugin.7z", @"d:\app\plugins\");
```

### 使用场景：自动更新

```csharp
// 下载更新包并解压覆盖
var updatePkg = @"d:\temp\update.zip";
await httpClient.DownloadFileAsync(updateUrl, updatePkg);

SevenZip.Extract(updatePkg, AppDomain.CurrentDomain.BaseDirectory, overwrite: true);
File.Delete(updatePkg);  // 清理临时文件
```

---

## 使用建议

| 场景 | 推荐方案 |
|------|---------|
| 纯 C# 运行时打包/解包 tar.gz | `TarFile` |
| Linux 容器镜像层解析 | `TarFile` |
| 需要支持 .7z / .rar / .iso | `SevenZip` |
| 部署环境无法保证有 7z.exe | `TarFile` 或 `System.IO.Compression.ZipFile` |
| 极高压缩比（冷备份） | `SevenZip`（7z 格式） |

## 注意事项

- **`TarFile` 流式读取**：`Entries` 惰性枚举，不能重复遍历，需要多次访问时 `ToList()`。
- **压缩文件路径**：`TarFile.AddFile` 中 `entryName` 建议使用正斜杠 `/` 分隔，保持跨平台兼容。
- **`SevenZip` 依赖进程**：需确保 `7z.exe` 已在自动发现路径内；在 CI/CD 容器中通常不预装，可配置 `SevenZip.PluginServer` 自动下载。
- **`SevenZip` 大文件异步**：`Compress/Extract` 是同步调用（等待进程退出），大文件时应在后台线程/异步上下文中调用。
