# 路径扩展 PathHelper

## 概述

`PathHelper` 是 NewLife.Core 中的路径操作工具类，提供跨平台的文件路径处理、目录管理、文件压缩解压、哈希校验等功能。智能处理相对路径和绝对路径，自动适配 Windows 和 Linux 的路径分隔符。

**命名空间**：`System.IO`（便于直接使用，无需额外引用）  
**文档地址**：https://newlifex.com/core/path_helper

## 核心特性

- **跨平台路径处理**：自动适配 Windows（`\`）和 Linux（`/`）路径分隔符
- **智能路径解析**：支持相对路径、绝对路径、网络路径
- **函数计算支持**：通过命令行参数或环境变量配置基础目录
- **压缩解压支持**：支持 zip、tar、tar.gz、7z 等格式
- **文件哈希校验**：支持 MD5、SHA1、SHA256、SHA512、CRC32

## 快速开始

```csharp
using System.IO;

// 获取完整路径
var path = "config/app.json".GetFullPath();

// 确保目录存在
"logs/2024/01/".EnsureDirectory(false);

// 合并路径
var file = "data".CombinePath("users", "config.json");

// 压缩目录
"output".AsDirectory().Compress("backup.zip");

// 验证文件哈希
var valid = "app.exe".AsFile().VerifyHash("md5$1234567890abcdef");
```

## API 参考

### 路径属性

#### BasePath

```csharp
public static String? BasePath { get; set; }
```

基础目录，用于 `GetBasePath` 方法。主要用于 X 组件内部各目录，专门为函数计算而定制。

**配置方式**（按优先级）：
1. 命令行参数：`-BasePath /app/data` 或 `--BasePath /app/data`
2. 环境变量：`BasePath=/app/data`
3. 默认值：应用程序域基础目录

#### BaseDirectory

```csharp
public static String? BaseDirectory { get; set; }
```

基准目录，用于 `GetFullPath` 方法。支持通过命令行参数和环境变量配置。

### 路径转换

#### GetFullPath

```csharp
public static String GetFullPath(this String path)
```

获取文件或目录基于应用程序域基目录的全路径。

**特点**：
- 自动处理相对路径
- 自动转换路径分隔符
- 支持网络路径（`\\server\share`）
- 支持 `~` 开头的路径

**示例**：
```csharp
// 相对路径转绝对路径
"config/app.json".GetFullPath()      
// Windows: C:\MyApp\config\app.json
// Linux: /home/user/myapp/config/app.json

// 已是绝对路径则原样返回
"C:\\temp\\file.txt".GetFullPath()   // C:\temp\file.txt
"/var/log/app.log".GetFullPath()     // /var/log/app.log

// 网络路径
"\\\\server\\share\\file.txt".GetFullPath()  // \\server\share\file.txt

// ~ 开头的路径
"~/config/app.json".GetFullPath()    // 去除 ~ 后拼接基础目录
```

#### GetBasePath

```csharp
public static String GetBasePath(this String path)
```

获取文件或目录的全路径，用于 X 组件内部各目录。

**示例**：
```csharp
"logs/app.log".GetBasePath()
// 基于 BasePath 的完整路径
```

#### GetCurrentPath

```csharp
public static String GetCurrentPath(this String path)
```

获取文件或目录基于当前工作目录的全路径。

**示例**：
```csharp
"output/result.txt".GetCurrentPath()
// 基于 Environment.CurrentDirectory 的完整路径
```

### 目录操作

#### EnsureDirectory

```csharp
public static String EnsureDirectory(this String path, Boolean isfile = true)
```

确保目录存在，若不存在则创建。

**参数说明**：
- `isfile`：路径是否为文件路径。`true` 时取目录部分；斜杠结尾的路径始终视为目录。

**示例**：
```csharp
// 确保文件所在目录存在
"logs/2024/01/app.log".EnsureDirectory(true);
// 创建 logs/2024/01/ 目录

// 确保目录本身存在
"data/cache/".EnsureDirectory(false);
// 创建 data/cache/ 目录

// 斜杠结尾的路径始终视为目录
"output/temp/".EnsureDirectory();  // isfile 参数被忽略
```

#### CombinePath

```csharp
public static String CombinePath(this String? path, params String[] ps)
```

合并多段路径。

**示例**：
```csharp
"data".CombinePath("users", "config.json")
// Windows: data\users\config.json
// Linux: data/users/config.json

// 支持空路径
"".CombinePath("logs", "app.log")  // logs/app.log
```

### 文件操作

#### AsFile

```csharp
public static FileInfo AsFile(this String file)
```

将路径字符串转换为 `FileInfo` 对象。

**示例**：
```csharp
var fi = "config/app.json".AsFile();
if (fi.Exists)
{
    Console.WriteLine($"文件大小: {fi.Length}");
}
```

#### ReadBytes

```csharp
public static Byte[] ReadBytes(this FileInfo file, Int32 offset = 0, Int32 count = -1)
```

从文件读取字节数据。

**示例**：
```csharp
// 读取整个文件
var data = "data.bin".AsFile().ReadBytes();

// 读取指定范围
var header = "data.bin".AsFile().ReadBytes(0, 100);  // 前100字节
var tail = "data.bin".AsFile().ReadBytes(1000, 50);  // 从1000开始的50字节
```

#### WriteBytes

```csharp
public static FileInfo WriteBytes(this FileInfo file, Byte[] data, Int32 offset = 0)
```

向文件写入字节数据。

**示例**：
```csharp
var data = new Byte[] { 1, 2, 3, 4, 5 };
"output.bin".AsFile().WriteBytes(data);
```

#### CopyToIfNewer

```csharp
public static Boolean CopyToIfNewer(this FileInfo fi, String destFileName)
```

仅当源文件比目标文件新时才复制。

**示例**：
```csharp
var source = "src/app.dll".AsFile();
if (source.CopyToIfNewer("dest/app.dll"))
{
    Console.WriteLine("文件已更新");
}
```

### 目录操作

#### AsDirectory

```csharp
public static DirectoryInfo AsDirectory(this String dir)
```

将路径字符串转换为 `DirectoryInfo` 对象。

**示例**：
```csharp
var di = "data/cache".AsDirectory();
if (di.Exists)
{
    Console.WriteLine($"包含 {di.GetFiles().Length} 个文件");
}
```

#### GetAllFiles

```csharp
public static IEnumerable<FileInfo> GetAllFiles(this DirectoryInfo di, String? exts = null, Boolean allSub = false)
```

获取目录内所有符合条件的文件，支持多扩展名匹配。

**示例**：
```csharp
var dir = "src".AsDirectory();

// 获取所有文件
var allFiles = dir.GetAllFiles();

// 获取指定扩展名文件
var csharpFiles = dir.GetAllFiles("*.cs");

// 多扩展名匹配（分号、竖线、逗号分隔）
var codeFiles = dir.GetAllFiles("*.cs;*.xaml;*.json");

// 包含子目录
var allCsharp = dir.GetAllFiles("*.cs", true);
```

#### CopyTo

```csharp
public static String[] CopyTo(this DirectoryInfo di, String destDirName, String? exts = null, Boolean allSub = false, Action<String>? callback = null)
```

复制目录中的文件到目标目录。

**示例**：
```csharp
var copied = "src".AsDirectory().CopyTo("backup", "*.cs;*.json", true, name =>
{
    Console.WriteLine($"复制: {name}");
});
Console.WriteLine($"共复制 {copied.Length} 个文件");
```

#### CopyToIfNewer

```csharp
public static String[] CopyToIfNewer(this DirectoryInfo di, String destDirName, String? exts = null, Boolean allSub = false, Action<String>? callback = null)
```

仅复制源目录中比目标目录更新的文件。

**示例**：
```csharp
var updated = "src".AsDirectory().CopyToIfNewer("dest", "*.dll;*.exe", true);
```

### 压缩解压

#### Extract（文件解压）

```csharp
public static void Extract(this FileInfo fi, String destDir, Boolean overwrite = false)
```

解压文件到指定目录。

**支持格式**：zip、tar、tar.gz、tgz、7z（仅 Windows）

**示例**：
```csharp
// 解压 zip 文件
"package.zip".AsFile().Extract("output");

// 解压 tar.gz 文件
"archive.tar.gz".AsFile().Extract("output", overwrite: true);

// 默认解压到同名目录
"app.zip".AsFile().Extract("");  // 解压到 app/ 目录
```

#### Compress（文件压缩）

```csharp
public static void Compress(this FileInfo fi, String destFile)
```

压缩单个文件。

**示例**：
```csharp
"large-file.log".AsFile().Compress("large-file.zip");
"data.bin".AsFile().Compress("data.tar.gz");
```

#### Compress（目录压缩）

```csharp
public static void Compress(this DirectoryInfo di, String? destFile = null)
public static void Compress(this DirectoryInfo di, String? destFile, Boolean includeBaseDirectory)
```

压缩整个目录。

**示例**：
```csharp
// 压缩目录（默认 zip 格式）
"src".AsDirectory().Compress("src.zip");

// 压缩为 tar.gz
"dist".AsDirectory().Compress("dist.tar.gz");

// 包含根目录名称
"project".AsDirectory().Compress("project.zip", true);
```

### 文件哈希校验

#### VerifyHash

```csharp
public static Boolean VerifyHash(this FileInfo file, String hash)
```

验证文件哈希是否匹配预期值。

**支持的算法**：
- MD5（16位或32位）
- SHA1
- SHA256
- SHA512
- CRC32

**哈希格式**：
- 带前缀：`md5$abc123...`、`sha256$def456...`、`crc32$12345678`
- 无前缀：根据长度自动识别
  - 8 字符：CRC32
  - 16/32 字符：MD5
  - 40 字符：SHA1
  - 64 字符：SHA256
  - 128 字符：SHA512

**示例**：
```csharp
var file = "app.exe".AsFile();

// 带算法前缀
file.VerifyHash("md5$d41d8cd98f00b204e9800998ecf8427e")
file.VerifyHash("sha256$e3b0c44298fc1c149afbf4c8996fb92427ae41e4649b934ca495991b7852b855")
file.VerifyHash("crc32$00000000")

// 无前缀（自动识别）
file.VerifyHash("d41d8cd98f00b204e9800998ecf8427e")  // 32位 -> MD5
file.VerifyHash("d41d8cd98f00b204")                  // 16位 -> MD5（前8字节）
file.VerifyHash("12345678")                          // 8位 -> CRC32
```

## 使用场景

### 1. 配置文件管理

```csharp
public class ConfigManager
{
    public T Load<T>(String configName) where T : new()
    {
        var path = $"config/{configName}.json".GetFullPath();
        path.EnsureDirectory(true);
        
        if (File.Exists(path))
        {
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<T>(json) ?? new T();
        }
        
        return new T();
    }
}
```

### 2. 日志目录管理

```csharp
public class LogManager
{
    public String GetLogPath()
    {
        var today = DateTime.Today;
        var path = $"logs/{today:yyyy}/{today:MM}/{today:dd}/".GetBasePath();
        path.EnsureDirectory(false);
        return path;
    }
}
```

### 3. 软件更新与校验

```csharp
public class UpdateManager
{
    public async Task<Boolean> UpdateAsync(String url, String expectedHash)
    {
        var tempFile = Path.GetTempFileName();
        
        // 下载文件
        await DownloadAsync(url, tempFile);
        
        // 校验哈希
        if (!tempFile.AsFile().VerifyHash(expectedHash))
        {
            File.Delete(tempFile);
            return false;
        }
        
        // 解压更新
        tempFile.AsFile().Extract("update_temp", overwrite: true);
        
        return true;
    }
}
```

### 4. 项目部署

```csharp
public class Deployer
{
    public void Deploy(String sourceDir, String targetDir)
    {
        var source = sourceDir.AsDirectory();
        
        // 复制所有更新的文件
        var updated = source.CopyToIfNewer(targetDir, "*.dll;*.exe;*.json", true, name =>
        {
            Console.WriteLine($"更新: {name}");
        });
        
        Console.WriteLine($"共更新 {updated.Length} 个文件");
        
        // 压缩备份
        targetDir.AsDirectory().Compress($"backup_{DateTime.Now:yyyyMMdd}.zip");
    }
}
```

## 最佳实践

### 1. 始终使用 GetFullPath 处理路径

```csharp
// 推荐：使用扩展方法获取完整路径
var path = "config/app.json".GetFullPath();

// 不推荐：直接使用相对路径
var path = "config/app.json";  // 可能在不同环境下行为不一致
```

### 2. 创建文件前确保目录存在

```csharp
// 推荐：先确保目录存在
var path = "logs/2024/01/app.log".GetFullPath();
path.EnsureDirectory(true);
File.WriteAllText(path, content);

// 不推荐：可能抛出 DirectoryNotFoundException
File.WriteAllText("logs/2024/01/app.log", content);
```

### 3. 使用 AsFile/AsDirectory 链式操作

```csharp
// 简洁的链式操作
var size = "data.bin".AsFile().ReadBytes().Length;
var files = "src".AsDirectory().GetAllFiles("*.cs", true).Count();
```

## 平台差异

| 功能 | Windows | Linux |
|------|---------|-------|
| 路径分隔符 | `\` | `/` |
| 7z 压缩 | ? 支持 | ? 不支持 |
| tar.gz 压缩 | .NET 7+ 原生支持 | .NET 7+ 原生支持 |

## 相关链接

- [数据扩展 IOHelper](io_helper-数据扩展IOHelper.md)
- [压缩解压缩](compression-压缩解压缩.md)
- [安全扩展 SecurityHelper](security_helper-安全扩展SecurityHelper.md)
