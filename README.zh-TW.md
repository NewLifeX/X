# NewLife.Core - 核心函式庫

![GitHub License](https://img.shields.io/github/license/NewLifeX/X?logo=github)
![Nuget Downloads](https://img.shields.io/nuget/dt/newlife.core?logo=nuget)
![Nuget](https://img.shields.io/nuget/v/newlife.core?logo=nuget)
![Nuget (with prereleases)](https://img.shields.io/nuget/vpre/newlife.core?label=dev%20nuget&logo=nuget)
[![NativeAOT](https://github.com/NewLifeX/X/actions/workflows/aot.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/aot.yml)
[![test](https://github.com/NewLifeX/X/actions/workflows/test.yml/badge.svg?branch=dev)](https://github.com/NewLifeX/X/actions/workflows/test.yml)
<!-- metrics:start -->
[![AOT smoke](https://img.shields.io/badge/AOT%20smoke-31%2F31-brightgreen?logo=dotnet)](Doc/AOT兼容性分析.md)
[![TFM](https://img.shields.io/badge/TFM-11%20%28net45%20%E2%86%92%20net10.0%29-512BD4?logo=dotnet)](https://www.nuget.org/packages/NewLife.Core)
[![locales](https://img.shields.io/badge/locales-31-2ea44f?logo=dotnet)](Doc/多语言本地化.md)
<!-- metrics:end -->
![platform](https://img.shields.io/badge/platform-Windows%20%7C%20Linux%20%7C%20macOS%20%7C%20Android%20%7C%20iOS-blue)
![arch](https://img.shields.io/badge/arch-x64%20%7C%20x86%20%7C%20arm64%20%7C%20arm%20%7C%20loongarch64%20%7C%20riscv64%20%7C%20s390x-orange)
[![contributors](https://img.shields.io/github/contributors/NewLifeX/X?logo=github)](https://github.com/NewLifeX/X/graphs/contributors)

<!-- lang:start -->
🌐 [English](README.md) | [简体中文](README.zh-CN.md) | [Español](README.es.md) | [Deutsch](README.de.md) | [日本語](README.ja.md) | [Français](README.fr.md) | [Português](README.pt.md) | [Русский](README.ru.md) | [한국어](README.ko.md) | [العربية](README.ar.md)
<!-- lang:end -->

**NewLife.Core** 是 NewLife 生態系的基礎：日誌、設定、快取、網路、序列化、排程、物件容器與 APM 追蹤，一個套件即可開工。原生 AOT 就緒，內建 <!--locale-count:start-->31<!--locale-count:end--> 種語言執行階段訊息，單一套件涵蓋 net45 → net10.0 共 11 個目標框架，支撐 XCode、Cube、Redis、Stardust 等 30 多個兄弟專案。

核心函式庫教學：[https://newlifex.com/core](https://newlifex.com/core)  

> ℹ️ 本繁體中文版與 [English README](README.md) 同批維護；若兩者內容不一致，以英文版為對外口徑。

**目錄**：[快速開始](#快速開始) · [為什麼選擇 NewLife.Core](#為什麼選擇-newlifecore) · [主要功能](#主要功能) · [生態版圖](#生態版圖) · [文件與範例](#文件與範例) · [參與貢獻](#參與貢獻) · [新生命開發團隊](#新生命開發團隊) · [授權條款](#授權條款)

## 快速開始

### 安裝

```bash
dotnet add package NewLife.Core
```

### 最小範例

```csharp
using NewLife.Caching;
using NewLife.Log;

XTrace.UseConsole();                    // console log (also writes to file)
XTrace.WriteLine("Hello NewLife");

var cache = new MemoryCache();          // in-memory cache
cache.Set("key", "value", 60);          // expires in 60 seconds
var value = cache.Get<String>("key");   // read back
```

### 範本樣板

在命令列執行以下命令，快速體驗 NewLife 元件：

```
dotnet new install NewLife.Templates
dotnet new nconsole --name test
cd test
dotnet run
```

### 平台支援

| 項目 | 涵蓋範圍 |
| --- | --- |
| 目標框架 | 單一套件 11 個：net45、net461、net462、netstandard2.0、netstandard2.1、net6.0、net6.0-windows、net8.0、net8.0-windows、net10.0、net10.0-windows |
| 系統與架構 | Windows、Linux、macOS、Android、iOS；x64、x86、arm64、arm、loongarch64、riscv64、s390x |
| NativeAOT | <!--aot-scenarios:start-->31<!--aot-scenarios:end--> 個核心場景實測通過，多 RID CI 冒煙矩陣把關（win-x64、linux-x64、linux-arm64、linux-loongarch64 為阻斷門檻）。外掛、指令碼等動態能力依設計不支援，邊界見 [AOT相容性分析](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md) |
| 執行階段在地化 | 內建 <!--locale-count:start-->31<!--locale-count:end--> 種語言；日誌與例外訊息在執行階段在地化，無須修改呼叫點（見 [多語言在地化](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md)） |
| 舊版框架 | net40：`dotnet add package NewLife.Core --version 10.10.2024.801-net40`（含 net40/net45/net461）；net20：`--version 10.10.2024.701-net20`（僅 net20）。兩者為 2024-08 凍結版，不隨主線更新。 |

## 為什麼選擇 NewLife.Core

- **一套元件涵蓋常見基礎設施**：日誌、設定、快取、網路、序列化、排程、物件容器與相依性注入，引用一個套件即可開工。
- **涵蓋廣、執行階段現代**：單一套件 11 個目標框架（net45 → net10.0，含 Windows 變體），原生 AOT 就緒，執行階段訊息支援 <!--locale-count:start-->31<!--locale-count:end--> 種語言，舊系統與新專案共用同一套 API。
- **經生態規模驗證**：同一套底座支撐 XCode（單表百億級）、Net（TCP 1KB 單機 177 萬 pps、14.5 Gbps）、Redis 與 Stardust（百億級資料量）等專案。
- **相依克制、授權寬鬆**：僅使用 BCL 與微軟官方相容套件；MIT 授權，允許修改原始碼、改名私有化（見[授權條款](#授權條款)）。

## 主要功能

<details>
<summary><b>基礎擴充</b>（10 項）</summary>

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
| [經緯度雜湊GeoHash](https://newlifex.com/core/geo_hash) | 將經緯度編碼為字串，用於鄰近位置搜尋。 |

</details>

<details>
<summary><b>日誌與追蹤</b>（2 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [日誌ILog](https://newlifex.com/core/log) | 支援多種日誌輸出方式，提供強大的非同步寫入功能。 |
| [追蹤ITracer](https://newlifex.com/core/tracer) | 強大的 APM 效能追蹤，支援與星塵平台整合。 |

</details>

<details>
<summary><b>排程</b>（3 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [進階計時器TimerX](https://newlifex.com/core/timerx) | 高精度計時器，支援 Cron 排程與非同步週期執行。 |
| [Cron 運算式](https://newlifex.com/core/cron) | 提供類似 Linux Cron 的排程任務，支援秒級比對。 |
| [時間提供者TimeProvider](https://newlifex.com/core/timerx_timeprovider) | 統一的時間抽象與高精度耗時計算，便於跨版本相容。 |

</details>

<details>
<summary><b>設定系統</b>（4 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [設定系統Config](https://newlifex.com/core/config) | 統一設定框架，支援多格式、多來源與熱載入。 |
| [設定提供者IConfigProvider](https://github.com/NewLifeX/X/blob/master/Doc/配置提供者IConfigProvider.md) | 統一設定介面，支援回呼與設定節點模型。 |
| [核心設定Setting](https://newlifex.com/core/setting) | 管理核心元件設定，控制日誌、網路等基礎行為。 |
| [系統設定SysConfig](https://newlifex.com/core/sysconfig) | 管理應用系統設定，支援本機、分散式及加密設定。 |

</details>

<details>
<summary><b>序列化</b>（7 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [Json序列化](https://newlifex.com/core/json) | 高效能 Json 工具，支援多種複雜場景的序列化。 |
| [Xml序列化](https://newlifex.com/core/xml) | 提供 XML 序列化與還原序列化，支援物件與 XML 互轉。 |
| [二進位序列化](https://newlifex.com/core/binary) | 高效能二進位序列化，支援複雜物件圖與版本控制。 |
| [進階二進位序列化](https://newlifex.com/core/advanced_binary) | 協定訊息全程零配置編解碼的進階指南。 |
| [Csv解析CsvFile](https://newlifex.com/core/csv_file) | 快速讀寫 CSV 檔案，支援自訂分隔符號與資料繫結。 |
| [Csv資料庫CsvDb](https://newlifex.com/core/csv_db) | 將 CSV 檔案當作資料庫使用，支援基本的新增、讀取、更新與刪除。 |
| [輕量級Excel讀取器ExcelReader](https://newlifex.com/core/excel_reader) | 無需 Office 相依，快速讀取 Excel，支援大資料量。 |

</details>

<details>
<summary><b>快取</b>（7 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [統一快取介面ICache](https://newlifex.com/core/icache) | 定義標準快取操作，提供記憶體、Redis 等多種實作。 |
| [快取架構ICacheProvider](https://newlifex.com/core/icacheprovider) | 管理並提供 ICache 執行個體，支援多快取並存。 |
| [記憶體快取MemoryCache](https://newlifex.com/core/memory_cache) | 高效能單機記憶體快取，支援到期與容量策略。 |
| [物件池ObjectPool](https://newlifex.com/core/object_pool) | 重複使用昂貴物件，提升效能並降低 GC 壓力。 |
| [物件池Pool](https://github.com/NewLifeX/X/blob/master/Doc/对象池Pool.md) | 基於 CAS 的高效能通用物件池，降低 GC 壓力。 |
| [分散式快取NewLife.Redis](https://newlifex.com/core/redis) | 基於 Redis 的分散式快取實作，支援多種資料結構。 |
| [向量儲存IVectorStore](https://github.com/NewLifeX/X/blob/master/NewLife.Core/Data/IVectorStore.cs) | 向量集合管理介面，支援 Top-K 相似度檢索，為 AI 應用提供向量檢索基礎設施。 |

</details>

<details>
<summary><b>高效能緩衝區</b>（7 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [資料封包IPacket](https://newlifex.com/core/packet) | 零複製切片、鏈式封包的核心資料封包介面。 |
| [緩衝區Buffers](https://newlifex.com/core/buffers) | 高效能緩衝區操作庫，彙整 Span 與集區化寫入等元件。 |
| [集區化寫入器PooledByteBufferWriter](https://newlifex.com/core/pooled_byte_buffer_writer) | 基於 ArrayPool 的集區化緩衝寫入器，支援動態擴充。 |
| [Span讀取器SpanReader](https://newlifex.com/core/span_reader) | 零配置高效能二進位流解析器，支援大小端。 |
| [Span寫入器SpanWriter](https://newlifex.com/core/span_writer) | 零配置高效能二進位資料寫入器，支援大小端。 |
| [Span輔助SpanHelper](https://newlifex.com/core/span_helper) | Span/ReadOnlySpan 擴充方法集，簡化記憶體操作。 |
| [序列讀取器SequenceReader](https://github.com/NewLifeX/X/blob/dev/Doc/序列读取器SequenceReader.md) | 跨段順序讀取器（舊框架經墊片補齊，全 11 個 TFM 可用）。 |

</details>

<details>
<summary><b>應用框架</b>（15 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [物件容器ObjectContainer](https://newlifex.com/core/object_container) | 輕量級相依性注入，支援多種生命週期與注入模式。 |
| [輕量級應用程式主機Host](https://newlifex.com/core/host) | 簡化應用程式裝載，輕鬆建置長時間執行的背景服務。 |
| [外掛框架IPlugin](https://newlifex.com/core/plugin) | 提供標準化的外掛管理機制，實現應用程式模組化開發。 |
| [平行模型Actor](https://newlifex.com/core/actor) | 簡化的並行編程模型，透過訊息佇列處理提升輸送量。 |
| [延遲佇列DeferredQueue](https://github.com/NewLifeX/X/blob/master/Doc/延迟队列DeferredQueue.md) | 彙總高頻變更並批次寫入，降低寫入放大。 |
| [事件匯流排EventBus](https://github.com/NewLifeX/X/blob/master/Doc/事件总线EventBus.md) | 處理程序內事件分派，支援主題路由與佇列式訊息。 |
| [弱式參考委派WeakAction](https://newlifex.com/core/weak_action) | 解決事件訂閱的記憶體洩漏問題，實現安全的事件處理。 |
| [指令碼引擎ScriptEngine](https://newlifex.com/core/script_engine) | 支援 C# 動態編譯執行，提供彈性的指令碼擴充能力。 |
| [壓縮與解壓縮](https://newlifex.com/core/compression) | 整合多種壓縮演算法，提供統一的資料壓縮介面。 |
| [壓縮封存Tar與7Zip](https://github.com/NewLifeX/X/blob/master/Doc/压缩归档Tar与7Zip.md) | 涵蓋 Tar/Tar.Gz 與 7Zip 進階封存壓縮用法。 |
| [機器資訊MachineInfo](https://newlifex.com/core/machine_info) | 全面取得硬體資訊，支援 Windows 與 Linux 系統。 |
| [拼音庫PinYin](https://newlifex.com/core/pinyin) | 高效漢字轉拼音庫，支援全拼與簡拼，效能優越。 |
| [資料表DbTable](https://newlifex.com/core/dbtable) | 記憶體中的資料表，方便進行資料處理與分析。 |
| [分頁參數PageParameter](https://newlifex.com/core/page_parameter) | 封裝分頁邏輯，支援 Web 與 API 等多種場景分頁。 |
| [雪花演算法Snowflake](https://newlifex.com/core/snow_flake) | 分散式唯一 ID 產生演算法，保證高併發下 ID 唯一性。 |

</details>

<details>
<summary><b>安全性與權杖</b>（3 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [分散式數位簽章權杖TokenProvider](https://newlifex.com/core/token_provider) | 產生與驗證分散式權杖，確保多節點間安全。 |
| [Web通用權杖JwtBuilder](https://newlifex.com/core/jwt) | 提供 JWT 權杖的產生、解碼與驗證，簡化 Web 驗證。 |
| [權杖HTTP篩選器TokenHttpFilter](https://github.com/NewLifeX/X/blob/master/Doc/令牌HTTP过滤器TokenHttpFilter.md) | 自動為 HTTP 要求注入權杖，攔截 401/403 自動續期。 |

</details>

<details>
<summary><b>網路程式庫</b>（13 項）</summary>

| 功能點 | 主要特點 |
| --- | --- |
| [網路服務端NetServer](https://newlifex.com/core/netserver) | 高效能 TCP/UDP/Unix 網域通訊端服務端，封裝工作階段與資料處理。 |
| [網路用戶端NetClient](https://github.com/NewLifeX/X/blob/master/Doc/网络客户端NetClient.md) | 統一的 TCP/UDP/Unix 網路用戶端，簡化連線與收發。 |
| [訊息IMessage](https://github.com/NewLifeX/X/blob/master/Doc/消息IMessage.md) | 要求-回應訊息契約，承載方向語意與訊框讀寫。 |
| [訊息協定堆疊MessagePump](https://github.com/NewLifeX/X/blob/dev/Doc/消息协议栈.md) | 訊框幫浦 + 無狀態編解碼器單一堆疊，支援大訊框串流主體與背壓。 |
| [資料管道Pipe](https://github.com/NewLifeX/X/blob/dev/Doc/数据管道Pipe.md) | 零複製資料管道，對齊 System.IO.Pipelines 形態。 |
| [簡易遠端訊息交換協定SRMP](https://newlifex.com/core/srmp) | 輕量級 RPC 協定，支援二進位序列化與 UDP 傳輸。 |
| [負載平衡與容錯移轉LoadBalancer](https://github.com/NewLifeX/X/blob/master/Doc/负载均衡与故障转移LoadBalancer.md) | 提供 Failover、輪詢、競速三種多節點排程策略。 |
| [HTTP用戶端ApiHttpClient](https://newlifex.com/core/api_http) | 基於 HttpClient 封裝，簡化 Web API 的呼叫與處理。 |
| [HTTP服務端HttpServer（嵌入式裝置）](https://newlifex.com/core/httpserver) | 輕量級 Http 伺服器，支援中介軟體管道、參數化路由與靜態檔案服務。 |
| [網路下載WebClientX](https://newlifex.com/core/webclient) | 增強版 WebClient，支援多執行緒與斷點續傳下載。 |
| [WebSocket服務端（物聯網下行通知利器）](https://newlifex.com/core/websocket) | 高效能 WebSocket 服務端，適用於物聯網訊息推播。 |
| [WebSocket雙向通訊](https://github.com/NewLifeX/X/blob/master/Doc/WebSocket双向通信.md) | 介紹握手、訊框處理、心跳與連線關閉實務。 |
| [檔案儲存用戶端EasyClient](https://github.com/NewLifeX/X/blob/master/NewLife.Core/IO/EasyClient.cs) | HTTP 檔案儲存用戶端，對接 EasyIO 或 OSS。 |

</details>

> RPC（`ApiServer`/`ApiClient`）已移至 [NewLife.Remoting](https://www.nuget.org/packages/NewLife.Remoting)。

## 生態版圖

NewLife 生態系按相依分層，各專案獨立版本、獨立發行。下面的版圖在所有 NewLife 儲存庫中逐字一致；每個專案連結到程式碼儲存庫與主 NuGet 套件。

| 專案 | 一句話定位 | 下載量 |
| --- | --- | --- |
| **基礎底座** | | |
| [NewLife.Core](https://github.com/NewLifeX/X) | 日誌、設定、快取、網路、序列化、排程與 APM 追蹤。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Core?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Core) |
| [NewLife.XCode](https://github.com/NewLifeX/NewLife.XCode) | 資料中介層：單表百億級、自動分表、讀寫分離，支援 10 多種資料庫。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCode?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCode) |
| [NewLife.Net](https://github.com/NewLifeX/NewLife.Net) | 高效能網路庫：單機 177 萬 pps、14.5 Gbps（TCP 1KB，2026-10 實測），400 萬 Tcp 長連線。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Net?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Net) |
| [NewLife.Remoting](https://github.com/NewLifeX/NewLife.Remoting) | 協定通訊與 RPC 框架，面向 CS 應用程式與物聯網裝置。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Remoting?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Remoting) |
| [NewLife.Agent](https://github.com/NewLifeX/NewLife.Agent) | 將 .NET 應用程式安裝為作業系統常駐服務：Windows 服務與 Linux systemd。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Agent?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Agent) |
| [NewLife.Templates](https://github.com/NewLifeX/NewLife.Zero) | `dotnet new` 專案範本：主控台、Web、API、服務、資料與物聯網。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Templates?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Templates) |
| **資料儲存與訊息** | | |
| [NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis) | Redis 用戶端：微秒級延遲、百萬級吞吐，內建訊息佇列。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Redis?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Redis) |
| [NewLife.RocketMQ](https://github.com/NewLifeX/NewLife.RocketMQ) | 純受控 RocketMQ 用戶端，支援 Apache RocketMQ 與阿里雲訊息佇列。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.RocketMQ?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.RocketMQ) |
| [NewLife.MQTT](https://github.com/NewLifeX/NewLife.MQTT) | MQTT 用戶端與服務端，含阿里雲物聯網接入。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MQTT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MQTT) |
| [NewLife.NovaDb](https://github.com/NewLifeX/NewLife.NovaDb) | C# 混合式資料庫：內嵌或網路雙模，關係型、時序、訊息佇列與 KV 於一體。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.NovaDb?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.NovaDb) |
| [NewLife.MySql](https://github.com/NewLifeX/NewLife.MySql) | 建構於 NewLife 資料堆疊之上的 MySql 驅動與輔助層。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.MySql?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.MySql) |
| **Web、AI 與分散式** | | |
| [NewLife.Cube](https://github.com/NewLifeX/NewLife.Cube) | 快速後台開發框架：權限、SSO、OAuth 服務端與 CRUD 樣板。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Cube?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Cube) |
| [Stardust](https://github.com/NewLifeX/Stardust) | 分散式服務平台：節點管理、APM、設定中心、註冊中心與發佈中心。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Stardust?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Stardust) |
| [AntJob](https://github.com/NewLifeX/AntJob) | 分散式任務與即時資料排程平台，支援分片、重試與回溯補算。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AntJob?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AntJob) |
| [NewLife.AI](https://github.com/NewLifeX/NewLife.AI) | 統一接入 46 家大模型服務商，附帶開箱即用的 ChatAI Web 對話應用。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.AI?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.AI) |
| **工業與物聯網** | | |
| [NewLife.IoT](https://github.com/NewLifeX/NewLife.IoT) | 物聯網標準庫，定義通訊協定契約與裝置抽象。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IoT?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IoT) |
| [NewLife.Modbus](https://github.com/NewLifeX/NewLife.Modbus) | 建構於 NewLife.IoT 之上的 Modbus TCP/RTU/ASCII 實作。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Modbus?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Modbus) |
| [NewLife.Siemens](https://github.com/NewLifeX/NewLife.Siemens) | 建構於 NewLife.IoT 之上的西門子 PLC 協定。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Siemens?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Siemens) |
| [NewLife.Omron](https://github.com/NewLifeX/NewLife.Omron) | 建構於 NewLife.IoT 之上的歐姆龍 PLC 協定。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Omron?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Omron) |
| [NewLife.Melsec](https://github.com/NewLifeX/NewLife.Melsec) | 建構於 NewLife.IoT 之上的三菱 Melsec PLC 協定。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Melsec?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Melsec) |
| [NewLife.OPC](https://github.com/NewLifeX/NewLife.OPC) | 工業資料存取的 OPC 用戶端。 | — |
| [NewLife.BACnet](https://github.com/NewLifeX/NewLife.BACnet) | BACnet 樓宇自動化協定堆疊。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.BACnet?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.BACnet) |
| [ZeroIoT](https://github.com/NewLifeX/ZeroIoT) | 開源輕量級物聯網平台，含閘道與裝置管理。 | — |
| **車聯網** | | |
| [NewLife.JT808](https://github.com/NewLifeX/NewLife.JT808) | JT/T 808 車輛定位協定：70+ 訊息體，涵蓋 2011/2013/2019 三個版本。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT808?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT808) |
| [NewLife.JT809](https://github.com/NewLifeX/NewLife.JT809) | JT/T 809 部標平台互聯協定，面向運輸管理部門。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT809?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT809) |
| [NewLife.JT1078](https://github.com/NewLifeX/NewLife.JT1078) | JT/T 1078 車載視訊：RTP 解析、FLV/fMP4 轉封裝與直播推送。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.JT1078?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.JT1078) |
| **領域元件** | | |
| [NewLife.Office](https://github.com/NewLifeX/NewLife.Office) | 一個套件涵蓋 Excel、Word、PPT、PDF 等 15+ 文件格式。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Office?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Office) |
| [NewLife.Audio](https://github.com/NewLifeX/NewLife.Audio) | 跨平台音訊：編解碼、裝置 IO、DSP、容器與串流處理。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Audio?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Audio) |
| [NewLife.Video](https://github.com/NewLifeX/NewLife.Video) | 視訊編解碼、封裝格式、影格處理與串流。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Video?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Video) |
| [NewLife.Map](https://github.com/NewLifeX/NewLife.Map) | 百度／高德／騰訊／天地圖統一介面，支援地理編碼與座標轉換。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.Map?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.Map) |
| [NewLife.IP](https://github.com/NewLifeX/NewLife.IP) | IPv4 歸屬地：離線記憶體映射資料庫 + 線上 API 雙來源。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.IP?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.IP) |
| **工具與產品** | | |
| [CrazyCoder](https://github.com/NewLifeX/XCoder) | 開發者工具箱：網路、序列埠、加解密、正規表示式、Modbus 與 MQTT。 | [![NuGet](https://img.shields.io/nuget/dt/NewLife.XCoder?style=flat-square&logo=nuget&label=downloads)](https://www.nuget.org/packages/NewLife.XCoder) |
| [GitCandy](https://github.com/NewLifeX/GitCandy) | 自架 Git 原始碼管理系統。 | — |
| [LuckyClover](https://github.com/NewLifeX/LuckyClover) | 從較舊的宿主啟動時，自動安裝相符的 .NET 執行階段。 | — |

> 徽章取各專案主套件；主題套件、擴充套件與衛星套件見對應 NuGet 頁面。未在表中列出的 30 多個套件（FTP、LoRa、Office 擴充、各類協定驅動等）見 [NewLifeX 組織](https://github.com/NewLifeX) 與 [newlifex.com](https://newlifex.com)。

## 文件與範例

### 使用文件

- [核心庫目錄](https://github.com/NewLifeX/X/blob/master/Doc/核心库目录.md)：核心元件文章索引（依模組分類）
- [AOT相容性分析](https://github.com/NewLifeX/X/blob/master/Doc/AOT兼容性分析.md)：NativeAOT 支援範圍、實測結論（冒煙 <!--aot-smoke:start-->31/31<!--aot-smoke:end-->）、RID 涵蓋矩陣與已知邊界
- [多語言在地化](https://github.com/NewLifeX/X/blob/master/Doc/多语言本地化.md)：支援的語言與基本用法
- [行動裝置技術架構](https://github.com/NewLifeX/X/blob/dev/Doc/移动端技术架构.md)：.NET 行動技術堆疊、UI 框架選型、APK 封裝與 AOT 說明
- [Samples](https://github.com/NewLifeX/X/tree/master/Samples)：主控台、服務、Http、物聯網與行動裝置範例

### 版本記錄

- [ChangeLog.md](https://github.com/NewLifeX/X/blob/master/ChangeLog.md)：版本變更記錄

## 參與貢獻

### 建置與測試

解決方案支援以 Visual Studio（最新正式版）開啟編譯，也相容 `dotnet build` 命令列；專案檔保持簡單，複製後即可開箱建置。

```bash
dotnet build NewLife.Core/NewLife.Core.csproj -c Release
dotnet test XUnitTest.Core -c Release
```

- 全量平行測試：`run-tests.ps1`（多宿主平行 + 逾時防護 + trx 彙總）
- NativeAOT 冒煙：`AotSmoke/`，CI 見 `.github/workflows/aot.yml`
- 效能基準：`Benchmark/`（BenchmarkDotNet）

### 儲存庫結構

| 目錄 | 說明 |
| --- | --- |
| `NewLife.Core/` | 核心庫原始碼（日誌、網路、快取、序列化等） |
| `NewLife.Core.SourceGenerator/` | 來源產生器，隨主套件分發到 `analyzers/` |
| `Doc/` | 能力文件與文章索引 |
| `Samples/` | 主控台 / 服務 / Http / 物聯網 / 行動裝置範例 |
| `Benchmark/` | BenchmarkDotNet 效能基準 |
| `XUnitTest.Core/` | 主單元測試專案（net10.0） |
| `XUnitTest.Compat/`、`XUnitTest.Net6/` | 跨版本相容性測試 |
| `AotSmoke/` | NativeAOT 冒煙專案 |

### 發行

推送 `v` 前綴標籤（例如 `v12.0.2026.0927`）觸發 `.github/workflows/publish.yml`。工作流程完成封裝後同時發佈到 NuGet.org 與 GitHub Packages，並建立同名 GitHub Release（說明取自 `ChangeLog.md` 對應段落，資產含 nupkg/snupkg）；手動觸發工作流程只發佈套件，不建立 Release。

### 研發過程文件

`Doc/` 目錄同時維護與程式碼同步的能力文件體系：需求定義 → 功能追蹤 → 架構設計。這些文件以中文撰寫。

- [需求文件](https://github.com/NewLifeX/X/blob/master/Doc/需求文档.md)：願景、核心目標與功能需求
- [功能清單](https://github.com/NewLifeX/X/blob/master/Doc/功能清单.md)：全部功能點的實作/測試/註解狀態追蹤
- [架構設計](https://github.com/NewLifeX/X/blob/master/Doc/架构设计.md)：分層、模組元件與關鍵設計決策

### 意見回饋

- 提交前請確保 `dotnet build` 與相關單元測試通過；程式碼風格與提交訊息見儲存庫慣例。
- 缺陷與建議請提 [Issue](https://github.com/NewLifeX/X/issues)，歡迎 Star 與 PR。

## 新生命開發團隊

<img src="https://newlifex.com/logo.png" width="48" align="right" alt="NewLife" />

新生命團隊成立於 2002 年，二十餘年專注 .NET 與物聯網技術生態，主導開源專案近百個（生態版圖收錄 34 個主幹專案），為電力、物流、醫療、工控、網際網路等行業提供軟硬體一體化解決方案；以「學無先後達者為師」為信條。

開源元件在 NuGet 累計下載 <!--nuget-total:start-->6.48M<!--nuget-total:end--> 次（全生態口徑，截至 2026-10）。

網站：https://newlifex.com · 開源組織：https://github.com/NewLifeX

<details>
<summary>微信公眾號：智能大石头（掃碼關注）</summary>

![智能大石头](https://newlifex.com/stone.jpg)

</details>

## 授權條款

本專案採用 [MIT](https://github.com/NewLifeX/X/blob/master/LICENSE) 授權；公開強式名稱憑證 `newlife.snk`，支援自行編譯並替換組件，也可修改原始碼後再次發行而無需聲明來源。

> 如果本專案對你有幫助，歡迎 Star、分享給團隊，或透過 Issue/PR 提供改進建議。
