# NewLife.Core - 核心函式庫

![GitHub License](https://img.shields.io/github/license/NewLifeX/X?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.core?label=dev%20nuget&logo=nuget)
[![NativeAOT](https://github.com/NewLifeX/X/actions/workflows/aot.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/aot.yml)
[![test](https://github.com/NewLifeX/X/actions/workflows/test.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/test.yml)
[![AOT smoke](https://img.shields.io/badge/AOT%20smoke-29%2F29-brightgreen?logo=dotnet)](Doc/AOT兼容性分析.md)
[![TFM](https://img.shields.io/badge/TFM-11%20%28net45%20%E2%86%92%20net10.0%29-512BD4?logo=dotnet)](https://www.nuget.org/packages/NewLife.Core)
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS%20%7C%20Android%20%7C%20iOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20x86%20%7C%20arm64%20%7C%20arm32%20%7C%20LoongArch64-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/X?logo=github)](https://github.com/NewLifeX/X/graphs/contributors)

<!-- lang:start -->
🌐 [中文](Readme.MD) | [English](README.en.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

NewLife 框架是一套完整的 .NET 元件生態系，為建置可擴充的應用程式提供高效能基礎設施。它提供日誌、網路、序列化、快取與多執行緒等基本功能，作為從 Web 服務到 IoT 裝置等一系列應用程式的基礎。  

核心函式庫教學：[https://newlifex.com/core](https://newlifex.com/core)  

> ⚠️ 本翻譯可能落後於[簡體中文 README](Readme.MD)，內容以簡體中文版為準。

**目錄**：[框架概覽](#框架概覽) · [為什麼選擇](#為什麼選擇-newlifecore) · [快速開始](#快速開始) · [主要功能](#主要功能) · [文件索引](#文件索引) · [儲存庫結構](#儲存庫結構) · [建置與測試](#建置與測試) · [專案矩陣](#新生命專案矩陣) · [貢獻與回饋](#貢獻與回饋) · [授權條款](#授權條款)


## 框架概覽

NewLife 由多個元件庫組成，NewLife.Core 提供基礎。此框架支援 net45~net10.0 多目標框架（.NET Framework 4.5 至 .NET 10.0），可在不同環境中達到廣泛的相容性（NewLife.Core 單一套件涵蓋 11 個目標框架）。

**NativeAOT**：核心鏈路已支援（29 個場景實測 + 多 RID 的 CI 冒煙矩陣；win-x64/linux-x64/linux-arm64/linux-la64 為阻斷門檻——LA64 經 QEMU 必選覆蓋，其餘平台觀察期；push/PR + 人工手動觸發）；跨平台單元測試涵蓋 win/linux/osx 的 x64/arm64、LA64 與 musl。外掛、指令碼等動態能力依設計不支援，邊界見 [AOT相容性分析](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md)。


## 為什麼選擇 NewLife.Core

- **一套元件涵蓋常見基礎設施**：日誌、設定、快取、網路、序列化、排程、物件容器與相依性注入，引用一個套件即可開工。
- **跨版本涵蓋廣**：單一套件 11 個目標框架（net45 → net10.0，含 Windows 變體），舊系統與新專案共用同一套 API。
- **NativeAOT 可用**：核心鏈路 29 個場景實測通過，多 RID CI 冒煙矩陣把關，邊界見 [AOT相容性分析](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md)。
- **相依克制、授權寬鬆**：僅使用 BCL 與微軟官方相容套件；MIT 授權，允許修改原始碼、改名私有化（見[授權條款](#授權條款)）。

## 快速開始

### 安裝

```bash
dotnet add package NewLife.Core
```

### 最小範例

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // 主控台日誌（同時寫入檔案）
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // 記憶體快取
cache.Set("key", "value", 60);          // 60 秒後到期
var value = cache.Get<String>("key");   // 讀取
```

### 範本樣板

在命令列執行下列命令，快速體驗 NewLife 元件：

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

### 編譯與客製化

團隊始終秉持開放態度，不僅支援以 Visual Studio（最新正式版）開啟方案編譯，也相容 `dotnet build` 命令列編譯；專案檔捨棄複雜功能以追求簡單易用，真正做到開箱即用。

我們公開強式名稱憑證 `newlife.snk`，支援自行編譯並替換組件。

NewLife 元件由社群共創 20 多年，採用 MIT 開源授權，**任何人皆可任意修改並再次發行**（無須聲明來源）！許多企業以此建置內部開發框架時，甚至可將原始碼中所有 `NewLife` 字串批次替換為貴公司名稱，實現私有化客製。

## 文件索引

### 使用文件

- [核心庫目錄](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md)：核心元件文章索引（依模組分類）
- [AOT相容性分析](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md)：NativeAOT 支援範圍、實測結論（冒煙 29/29）、多 RID 涵蓋矩陣與已知邊界
- [多語言在地化](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md)：例外與日誌的多語言模型、語言表擴充、下游覆寫與執行階段新增語言
- [行動裝置技術架構](https://github.com/NewLifeX/X/blob/dev/Doc/移动端技术架构.md)：.NET 行動技術堆疊分層、UI 框架選型、Mobile 範例專案、APK 封裝與 JIT/Mono AOT/NativeAOT 說明

### 研發過程文件

儲存庫 `Doc/` 目錄同時維護能力文件體系（需求定義 → 功能追蹤 → 架構設計）：

- [需求文件](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md)：願景、核心目標與功能需求
- [功能清單](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md)：全部功能點的實作/測試/註解狀態追蹤
- [架構設計](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md)：分層、模組元件與關鍵設計決策

## 主要功能


<details>
<summary><b>基礎擴充</b>（9 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [型別轉換Utility](https://newlifex.com/core/utility) | 提供高效的型別轉換，支援各種格式與例外處理。 |
| [字串擴充StringHelper](https://newlifex.com/core/string_helper) | 包含多種字串操作，如擷取、編碼與格式化等。 |
| [路徑擴充PathHelper](https://newlifex.com/core/path_helper) | 智慧處理多種路徑格式，實現程式碼跨平台相容性。 |
| [資料擴充IOHelper](https://newlifex.com/core/io_helper) | 最佳化 IO 操作，提供高效的資料流與位元組陣列轉換。 |
| [行程擴充ProcessHelper](https://newlifex.com/core/process_helper) | 簡化行程操作，提供取得與管理行程資訊的便利方法。 |
| [執行階段資訊Runtime](https://newlifex.com/core/runtime) | 提供簡單方法取得應用程式執行階段資訊，便於偵錯診斷。 |
| [可銷毀DisposeBase](https://newlifex.com/core/disposebase) | 提供標準的資源釋放模式，有效防止記憶體與資源洩漏。 |
| [反射擴充Reflect](https://newlifex.com/core/reflect) | 高效能反射工具，快速進行物件成員的讀寫操作。 |
| [安全性擴充SecurityHelper](https://newlifex.com/core/security_helper) | 整合 RSA/AES/DES/MD5 等多種加解密，簡化安全操作。 |

</details>

<details>
<summary><b>日誌與追蹤</b>（2 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [日誌ILog](https://newlifex.com/core/log) | 支援多種日誌輸出方式，提供強大的非同步寫入功能。 |
| [鏈路追蹤ITracer](https://newlifex.com/core/tracer) | 強大的 APM 效能追蹤，支援與星塵平台整合，並與 OpenTelemetry 雙向互通。 |

</details>

<details>
<summary><b>排程</b>（3 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [進階計時器TimerX](https://newlifex.com/core/timerx) | 高精度計時器，支援 Cron 排程與非同步週期執行。 |
| [Cron 運算式](https://newlifex.com/core/cron) | 提供類似 Linux Cron 的定時工作，支援秒級比對。 |
| [時間提供者TimeProvider](https://newlifex.com/core/timerx_timeprovider) | 統一時間抽象與高精度耗時計算，便於跨版本相容。 |

</details>

<details>
<summary><b>設定系統</b>（4 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [設定系統Config](https://newlifex.com/core/config) | 統一設定框架，支援多格式、多來源與熱載入機制。 |
| [設定提供者IConfigProvider](https://github.com/NewLifeX/X/blob/master/Doc/配置提供者IConfigProvider.md) | 統一設定介面，支援回呼與設定節點模型。 |
| [核心設定Setting](https://newlifex.com/core/setting) | 管理元件核心設定，控制如日誌、網路等基礎行為。 |
| [系統設定SysConfig](https://newlifex.com/core/sysconfig) | 管理應用系統設定，支援本機、分散式及加密設定。 |

</details>

<details>
<summary><b>序列化</b>（7 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [Json 序列化](https://newlifex.com/core/json) | 高效能 Json 工具，支援多種複雜場景下的序列化。 |
| [Xml 序列化](https://newlifex.com/core/xml) | 提供 XML 序列化與還原序列化，支援物件與 XML 互轉。 |
| [二進位序列化](https://newlifex.com/core/binary) | 高效能二進位序列化，支援複雜物件圖與版本控制。 |
| [進階二進位序列化](https://newlifex.com/core/advanced_binary) | 協定訊息全程零記憶體配置編解碼的進階指南。 |
| [Csv 解析CsvFile](https://newlifex.com/core/csv_file) | 快速讀寫 CSV 檔案，支援自訂分隔符號與資料繫結。 |
| [Csv 資料庫CsvDb](https://newlifex.com/core/csv_db) | 將 CSV 檔案當作資料庫使用，支援基本的新增、刪除、修改、查詢。 |
| [輕量級Excel 讀取器ExcelReader](https://newlifex.com/core/excel_reader) | 無須 Office 相依，快速讀取 Excel，支援大資料量。 |

</details>

<details>
<summary><b>快取</b>（7 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [統一快取介面ICache](https://newlifex.com/core/icache) | 定義標準快取操作，提供記憶體、Redis 等多種實作。 |
| [快取架構ICacheProvider](https://newlifex.com/core/icacheprovider) | 用於管理與提供 ICache 執行個體，支援多個快取並存。 |
| [記憶體快取MemoryCache](https://newlifex.com/core/memory_cache) | 高效能的單機記憶體快取，支援到期與容量策略。 |
| [物件集區ObjectPool](https://newlifex.com/core/object_pool) | 用於重複使用昂貴物件，提升效能並減少 GC 壓力。 |
| [物件集區Pool](https://github.com/NewLifeX/X/blob/master/Doc/对象池Pool.md) | 基於 CAS 的高效能通用物件集區，降低 GC 壓力。 |
| [分散式快取NewLife.Redis](https://newlifex.com/core/redis) | 基於 Redis 的分散式快取實作，支援多種資料結構。 |
| [向量儲存IVectorStore](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Data/IVectorStore.cs) | 向量集合管理介面，支援 Top-K 相似度檢索，為 AI 應用提供向量檢索基礎設施。 |

</details>

<details>
<summary><b>高效能緩衝區</b>（7 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [資料封包IPacket](https://newlifex.com/core/packet) | 零複製切片、鏈式封包的核心資料封包介面。 |
| [緩衝區Buffers](https://newlifex.com/core/buffers) | 高效能緩衝區操作程式庫，彙整 Span 與集區化寫入等元件。 |
| [集區化寫入器PooledByteBufferWriter](https://newlifex.com/core/pooled_byte_buffer_writer) | 基於 ArrayPool 的集區化緩衝寫入器，支援動態擴充容量。 |
| [Span 讀取器SpanReader](https://newlifex.com/core/span_reader) | 零記憶體配置的高效能二進位資料流解析器，支援大小端。 |
| [Span 寫入器SpanWriter](https://newlifex.com/core/span_writer) | 零記憶體配置的高效能二進位資料寫入器，支援大小端。 |
| [Span 輔助SpanHelper](https://newlifex.com/core/span_helper) | Span/ReadOnlySpan 擴充方法集，簡化記憶體操作。 |
| [序列讀取器SequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | 跨區段順序讀取器（舊框架經墊片補齊，全 11 個 TFM 可用）。 |

</details>

<details>
<summary><b>應用框架</b>（16 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [物件容器ObjectContainer](https://newlifex.com/core/object_container) | 輕量級相依性注入，支援多種生命週期與注入模式。 |
| [輕量級應用主機Host](https://newlifex.com/core/host) | 簡化應用程式裝載，輕鬆建置長時間執行的背景服務。 |
| [外掛框架IPlugin](https://newlifex.com/core/plugin) | 提供標準化的外掛管理機制，實現應用模組化開發。 |
| [平行模型Actor](https://newlifex.com/core/actor) | 簡化的並行程式設計模型，透過訊息佇列處理提升輸送量。 |
| [管線模型Pipeline](https://github.com/NewLifeX/X/blob/master/Doc/管道模型Pipeline.md) | 雙向處理鏈模型，統一 Read/Write/Open/Close 流程。⚠️ 已退役，由協定模式（幀泵）取代。 |
| [延遲佇列DeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | 彙總高頻變更並批次處理落地，降低寫入放大。 |
| [事件匯流排EventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | 行程內事件分派，支援主題路由與佇列式訊息。 |
| [弱引用委派WeakAction](https://newlifex.com/core/weak_action) | 解決事件訂閱記憶體洩漏問題，實現安全的事件處理。 |
| [指令碼引擎ScriptEngine](https://newlifex.com/core/script_engine) | 支援 C# 動態編譯執行，提供彈性的指令碼擴充能力。 |
| [壓縮解壓縮](https://newlifex.com/core/compression) | 整合多種壓縮演算法，提供統一介面進行資料壓縮。 |
| [壓縮歸檔Tar與7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | 涵蓋 Tar/Tar.Gz 與 7Zip 進階歸檔壓縮用法。 |
| [機器資訊MachineInfo](https://newlifex.com/core/machine_info) | 全面取得硬體資訊，支援 Windows 與 Linux 系統。 |
| [拼音庫PinYin](https://newlifex.com/core/pinyin) | 高效能漢字轉拼音程式庫，支援全拼與簡拼，效能優異。 |
| [資料集DbTable](https://newlifex.com/core/dbtable) | 記憶體中的資料表，方便進行資料處理與分析。 |
| [分頁參數PageParameter](https://newlifex.com/core/page_parameter) | 封裝分頁邏輯，支援 Web 與 API 等多種場景分頁。 |
| [雪花演算法Snowflake](https://newlifex.com/core/snow_flake) | 分散式唯一 ID 產生演算法，確保高並行下 ID 唯一性。 |

</details>

<details>
<summary><b>安全性與權杖</b>（3 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [分散式數位簽章權杖TokenProvider](https://newlifex.com/core/token_provider) | 用於產生與驗證分散式權杖，保障多節點間安全。 |
| [Web 通用權杖JwtBuilder](https://newlifex.com/core/jwt) | 提供 JWT 權杖的產生、解碼與驗證，簡化 Web 驗證。 |
| [權杖 HTTP 篩選器TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | HTTP 要求自動注入權杖，攔截 401/403 自動續期。 |

</details>

<details>
<summary><b>雲端服務</b>（1 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [經緯度雜湊GeoHash](https://newlifex.com/core/geo_hash) | 將經緯度編碼為字串，用於鄰近位置搜尋。 |

</details>

<details>
<summary><b>網路程式庫</b>（15 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [網路伺服器端NetServer](https://newlifex.com/core/netserver) | 高效能 TCP/UDP/Unix 網域通訊端伺服器端，封裝了工作階段與資料處理。 |
| [網路用戶端NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | 統一的 TCP/UDP/Unix 網路用戶端，簡化連線與收發。 |
| [訊息IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | 要求-回應訊息契約，乘載方向語意與幀讀寫。 |
| [訊息協定堆疊MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | 幀泵 + 無狀態編解碼器單一協定堆疊，支援大幀串流主體與背壓。 |
| [資料管線Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | 零複製資料管線，對齊 System.IO.Pipelines 形態。 |
| [簡易遠端訊息交換協定SRMP](https://newlifex.com/core/srmp) | 輕量級 RPC 協定，支援二進位序列化與 UDP 傳輸。 |
| [負載平衡與容錯移轉LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | 提供 Failover/輪詢/競速 三種多節點排程策略。 |
| [RPC通訊ApiServer](https://newlifex.com/core/api_server) | ⚠️ 已移至 [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
| [RPC通訊ApiClient](https://newlifex.com/core/api_client) | ⚠️ 已移至 [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting) |
| [HTTP 用戶端ApiHttpClient](https://newlifex.com/core/api_http) | 基於 HttpClient 封裝，簡化 Web API 的呼叫與處理。 |
| [HTTP 伺服器端HttpServer（嵌入式裝置）](https://newlifex.com/core/httpserver) | 輕量級 Http 伺服器，支援中介軟體管線、參數化路由與靜態檔案服務。 |
| [網路下載WebClientX](https://newlifex.com/core/webclient) | 增強版 WebClient，支援多執行緒、中斷續傳下載。 |
| [WebSocket伺服器（物聯網下行通知神器）](https://newlifex.com/core/websocket) | 高效能 WebSocket 伺服器端，適用於物聯網訊息推播。 |
| [WebSocket雙向通訊](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | 介紹握手、幀處理、心跳與連線關閉實務。 |
| [檔案儲存用戶端EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | HTTP 檔案儲存用戶端，對接 EasyIO 或 OSS。 |

</details>

> ⚠️ 百度地圖/高德地圖 已提取為獨立 NuGet 套件。


## 儲存庫結構

| 目錄 | 說明 |
| --- | --- |
| `NewLife.Core/` | 核心函式庫原始碼（日誌、網路、快取、序列化等） |
| `NewLife.Core.SourceGenerator/` | 來源產生器，隨主套件發佈到 `analyzers/` |
| `Doc/` | 能力文件與文章索引 |
| `Samples/` | 主控台 / 服務 / Http / 物聯網 / 行動裝置範例 |
| `Benchmark/` | BenchmarkDotNet 效能基準 |
| `XUnitTest.Core/` | 主要單元測試專案（net10.0） |
| `XUnitTest.Compat/`、`XUnitTest.Net6/` | 跨版本相容性測試 |
| `AotSmoke/` | NativeAOT 冒煙測試專案 |

## 建置與測試

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- 全量平行測試：`run-tests.ps1`（多主機平行 + 逾時保底 + trx 彙總）
- NativeAOT 冒煙：`AotSmoke/`，CI 見 `.github/workflows/aot.yml`
- 效能基準：`Benchmark/`（BenchmarkDotNet）
- 發版：推送 `v` 前置標籤（如 `v12.0.2026.0927`）會觸發 `.github/workflows/publish.yml`，封裝後同時發佈到 NuGet.org 與 GitHub Packages，並建立同名 GitHub Release（說明取自 `ChangeLog.md` 對應段落，資產含 nupkg/snupkg）；手動觸發工作流程只發佈套件，不建立 Release

## 新生命專案矩陣

各專案預設支援 net10.0/netstandard2.1/netstandard2.0/net462/net45，舊版（2024.0801）支援 net4.0/net2.0  

|                               專案                               | 年份  | 說明                                                                                        |
| :--------------------------------------------------------------: | :---: | ------------------------------------------------------------------------------------------- |
|                             基礎元件                             |       | 支撐其它中介軟體以及產品專案                                                                |
|          [NewLife.Core](https://github.com/NewLifeX/X)           | 2002  | 核心函式庫，日誌、設定、快取、網路、序列化、APM 效能追蹤                                     |
|    [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode)    | 2005  | 大數據中介軟體，單表百億級，MySql/SQLite/SqlServer/Oracle/PostgreSql/達夢，自動分割資料表，讀寫分離 |
|      [NewLife.Net](https://github.com/NewLifeX/NewLife.Net)      | 2005  | 網路程式庫，單機千萬級輸送量（2266 萬 tps），單機百萬級連線（400 萬 Tcp 長連線）              |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 2011  | 協定通訊程式庫，提供 CS 應用通訊框架，支援 Http/RPC 通訊框架，高輸送量，物聯網裝置低開銷易接入 |
|     [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube)     | 2010  | 魔方快速開發平台，整合使用者權限、SSO 登入、OAuth 伺服器端等，單表 100 億級專案驗證            |
|    [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent)    | 2008  | 服務管理元件，把應用程式安裝為作業系統守護行程，Windows 服務、Linux 的 Systemd                |
|     [NewLife.Zero](https://github.com/NewLifeX/NewLife.Zero)     | 2020  | Zero 零代樣板，基於 NewLife 元件生態的專案範本 NewLife.Templates，Web、WebApi、Service        |
|                              中介軟體                            |       | 對接知名中介軟體平台                                                                        |
|    [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)    | 2017  | Redis 用戶端，微秒級延遲，百萬級輸送量，豐富的訊息佇列，百億級資料量專案驗證                  |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 2018  | RocketMQ 純受控用戶端，支援 Apache RocketMQ 與阿里雲訊息佇列，十億級專案驗證                   |
|     [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT)     | 2019  | 物聯網訊息協定，MqttClient/MqttServer，用戶端支援阿里雲物聯網                                |
|      [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT)      | 2022  | IoT 標準程式庫，定義物聯網領域的各種通訊協定標準規範                                         |
|   [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus)   | 2022  | ModbusTcp/ModbusRTU/ModbusASCII，基於 IoT 標準程式庫實作，支援 ZeroIoT 平台與 IoTEdge 閘道器  |
|  [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens)  | 2022  | 西門子 PLC 協定，基於 IoT 標準程式庫實作，支援 IoT 平台與 IoTEdge                            |
|      [NewLife.Map](https://github.com/NewLifeX/NewLife.Map)      | 2022  | 地圖元件庫，封裝百度地圖、高德地圖、騰訊地圖、天地圖                                        |
|    [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio)    | 2023  | 音訊編解碼程式庫，PCM/ADPCMA/G711A/G722U/WAV/AAC                                             |
|                             產品平台                             |       | 產品平台級，編譯部署即用，個人化自訂                                                        |
|         [Stardust](https://github.com/NewLifeX/Stardust)         | 2018  | 星塵，分散式服務平台，節點管理、APM 監控中心、設定中心、註冊中心、發佈中心                   |
|           [AntJob](https://github.com/NewLifeX/AntJob)           | 2019  | 螞蟻排程，分散式大數據運算平台（即時/離線），螞蟻搬家分片思想，兆級資料量專案
|      [NewLife.ERP](https://github.com/NewLifeX/NewLife.ERP)      | 2021  | 企業 ERP，產品管理、客戶管理、銷售管理、供應商管理                                           |
|         [CrazyCoder](https://github.com/NewLifeX/XCoder)         | 2006  | 碼神工具，眾多開發者工具，網路、序列埠、加解密、正規表示式、Modbus、MQTT                      |
|           [EasyIO](https://github.com/NewLifeX/EasyIO)           | 2023  | 簡易檔案儲存，支援分散式系統中檔案集中儲存。                                                |
|           [XProxy](https://github.com/NewLifeX/XProxy)           | 2005  | 產品級反向代理，NAT 代理、Http 代理                                                          |
|        [HttpMeter](https://github.com/NewLifeX/HttpMeter)        | 2022  | Http 壓力測試工具                                                                            |
|         [GitCandy](https://github.com/NewLifeX/GitCandy)         | 2015  | Git 原始碼管理系統                                                                          |
|          [SmartOS](https://github.com/NewLifeX/SmartOS)          | 2014  | 嵌入式作業系統，完全獨立自主，支援 ARM Cortex-M 晶片架構                                     |
|          [SmartA2](https://github.com/NewLifeX/SmartA2)          | 2019  | 嵌入式工業電腦，物聯網邊緣閘道器，高效能 .NET8 主機，應用於工業、農業、交通、醫療              |
|                         FIoT物聯網平台                           | 2020  | 物聯網整體解決方案，建築、環保、農業，軟硬體及大數據分析一體化，單機十萬級點位專案驗證          |


---

## 新生命開發團隊

![XCode](https://newlifex.com/logo.png)

新生命團隊（NewLife）成立於 2002 年，是新時代物聯網產業解決方案提供者，致力於提供軟硬體應用方案諮詢、系統架構規劃與開發服務。NuGet 累計下載 523 萬+（全生態）。團隊核心元件（XCode / Redis / Stardust / AntJob / IoT 等）已在電力、工業、交通、環保、醫療、文博等產業大規模應用。

我們持續改進，力求成為值得長期信賴的 .NET / IoT / 分散式技術夥伴。

網站：https://newlifex.com   開源組織：https://github.com/NewLifeX  
微信公眾號：  
![智慧大石頭](https://newlifex.com/stone.jpg)

## 貢獻與回饋

- 提交前請確保 `dotnet build` 與相關單元測試通過；程式碼風格與提交訊息見儲存庫約定。
- 缺陷與建議請提出 [Issue](https://github.com/NewLifeX/X/issues)，歡迎 Star 與 PR。
- 版本變更記錄見 [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md)。

## 授權條款

本專案採用 [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE) 授權；公開強式名稱憑證 `newlife.snk`，支援自行編譯並替換組件。

> 如果本專案對你有幫助，歡迎 Star、分享給團隊，或透過 Issue/PR 回饋改進建議。
