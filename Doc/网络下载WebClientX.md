# 网络下载WebClientX

## 概述

`WebClientX` 是 NewLife.Core 提供的高级 HTTP 客户端，面向文件下载和网页抓取场景。相比 `HttpClient`，它封装了常用的一次性操作（下载整个文件、抓取全文、解析链接列表），并内置了 CDN 签名认证、断点恢复友好设计、临时文件+原子替换的双阶段写盘策略。

**命名空间**：`NewLife.Web`  
**文档地址**：/core/web_client_x

## 核心特性

- **下载文件到磁盘**：先写临时文件，完成后原子重命名，避免写到一半的文件被误用
- **CDN 签名**：通过 `AuthKey` 生成时间戳签名 URL，适配阿里云/腾讯云等 CDN 鉴权
- **链接提取**：`GetLinks` 解析 HTML `<a>` 标签，`GetLinksInDirectory` 过滤出目录式文件列表
- **下载并解压**：`DownloadLinkAndExtract` 下载压缩包后自动调用解压，支持 zip/tar.gz/7z
- **超时控制**：默认 30 秒，可按需调整

## 快速开始

```csharp
using NewLife.Web;

var client = new WebClientX
{
    Timeout   = 60_000,          // 60 秒超时
    UserAgent = "MyApp/1.0",
};

// 下载文件
await client.DownloadFileAsync("https://example.com/data.zip", @"d:\downloads\data.zip");

// 抓取网页文本
var html = await client.DownloadStringAsync("https://example.com/");

// 发送HTTP请求（通用）
var response = await client.SendAsync("https://api.example.com/v1/data");
```

## API 参考

### 属性

```csharp
/// <summary>超时时间（毫秒），默认 30_000</summary>
public Int32 Timeout { get; set; } = 30_000;

/// <summary>UserAgent 字符串</summary>
public String? UserAgent { get; set; }

/// <summary>Cookie 容器</summary>
public CookieContainer? Cookie { get; set; }

/// <summary>CDN 鉴权密钥。设置后自动为所有请求 URL 添加时间戳签名（阿里云/腾讯云 CDN 鉴权 A 型）</summary>
public String? AuthKey { get; set; }

/// <summary>Referer 请求头</summary>
public String? Referer { get; set; }

/// <summary>Accept 请求头</summary>
public String? Accept { get; set; }
```

### 方法

#### SendAsync - 发送 HTTP 请求

```csharp
public virtual async Task<String?> SendAsync(String url, String? data = null, String? method = null)
```

**参数**：
- `url`：目标 URL
- `data`：请求体（GET 时忽略）
- `method`：HTTP 方法，默认 `data != null` 时为 `POST`，否则为 `GET`

**返回**：响应正文字符串，请求失败或超时时抛出异常

---

#### DownloadStringAsync - 下载文本

```csharp
public async Task<String?> DownloadStringAsync(String url)
```

---

#### DownloadFileAsync - 下载文件到磁盘

```csharp
public virtual async Task<String?> DownloadFileAsync(String url, String fileName)
```

**行为**：
1. 先写入 `fileName + ".tmp"` 临时文件
2. 下载完成后原子重命名为 `fileName`
3. 返回最终文件路径；下载失败时临时文件保留（用于调试），并抛出异常

#### GetHtml - 抓取网页（带编码自动识别）

```csharp
public String? GetHtml(String url)
```

自动根据 `Content-Type` 或 HTML `<meta charset>` 标签检测实际编码（GBK/UTF-8 等）。

---

#### GetLinks - 提取 HTML 中的链接

```csharp
public Link[]? GetLinks(String url)
```

解析目标 URL 页面内的所有 `<a href="">` 链接，返回 `Link` 数组（含 `Name`、`Url`、`Version`、`Time`）。

---

#### GetLinksInDirectory - 提取目录式文件列表

```csharp
public Link[]? GetLinksInDirectory(String url)
```

在 `GetLinks` 基础上过滤掉父目录链接（`../`）、空链接等，只返回正向子项，适合爬取 Apache/Nginx 目录列表。

---

#### DownloadLink - 下载最新版本文件

```csharp
public String? DownloadLink(String url, String? name, String? destdir)
```

**参数**：
- `url`：目录页 URL
- `name`：文件名或通配符（如 `"NewLife.Core*.zip"`）
- `destdir`：保存目录

在目录页中找到匹配 `name` 的最新版本文件（按 `Version` 或 `Time` 排序），下载到 `destdir`，返回本地路径。

---

#### DownloadLinkAndExtract - 下载并解压

```csharp
public String? DownloadLinkAndExtract(String url, String? name, String? destdir)
```

在 `DownloadLink` 基础上，下载完成后自动调用 `ZipFile.ExtractToDirectory`（zip）或 `TarFile`（tar.gz），解压到 `destdir`。

## 使用场景

### 场景一：配合 CDN 鉴权下载配置文件

```csharp
// 阿里云 CDN 鉴权 A 型：URL?auth_key=timestamp-rand-uid-md5hash
var client = new WebClientX
{
    AuthKey = Environment.GetEnvironmentVariable("CDN_AUTH_KEY"),
};

await client.DownloadFileAsync(
    "https://cdn.example.com/configs/appsettings.json",
    @"d:\app\appsettings.json");
```

### 场景二：从文件服务器下载最新安装包

```csharp
var client = new WebClientX { Timeout = 120_000 };

// 访问目录页 http://files.example.com/release/
// 匹配文件名模式 "MyApp*.zip"
// 下载到 d:\downloads\
var localFile = client.DownloadLink(
    "http://files.example.com/release/",
    "MyApp*.zip",
    @"d:\downloads\");

Console.WriteLine($"下载到: {localFile}");
```

### 场景三：下载并自动解压更新包

```csharp
var client = new WebClientX { Timeout = 120_000 };

// 下载后自动解压到目标目录
var destDir = client.DownloadLinkAndExtract(
    "http://files.example.com/release/",
    "MyApp*.zip",
    @"d:\app\");

Console.WriteLine($"解压到: {destDir}");
```

### 场景四：爬取并分析目录列表

```csharp
var client = new WebClientX();
var links  = client.GetLinksInDirectory("http://files.example.com/packages/");

if (links != null)
{
    foreach (var link in links.OrderByDescending(l => l.Time))
        Console.WriteLine($"{link.Name}  {link.Version}  {link.Time:yyyy-MM-dd}  {link.Url}");
}
```

## 注意事项

- **非线程安全**：同一实例不可并发调用，需要并发时请创建多个实例。
- **大文件下载**：`DownloadFileAsync` 使用流式写盘，内存占用较低；但 `DownloadStringAsync` 会将全部内容加载到内存，不适合 GB 级内容。
- **临时文件清理**：下载异常时 `.tmp` 临时文件不会自动删除；应用退出时应检查清理。
- **`Timeout` 是连接超时**：不是总下载时长超时，大文件请根据网速合理评估后设置。
- **代理支持**：若需要通过代理下载，在创建实例前设置 `WebRequest.DefaultWebProxy`。
