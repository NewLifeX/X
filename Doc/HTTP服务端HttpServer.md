# HttpServer 使用手册

本文档基于源码 `NewLife.Core/Http/HttpServer.cs`，用于说明 `HttpServer`（轻量级 HTTP 服务器）的职责、路由注册方式、匹配规则与使用注意事项。

> 关键词：路由映射、通配符 `*`、委托处理器、控制器映射、静态文件、匹配缓存、线程安全。

---

## 1. 概述

`HttpServer` 继承自 `NetServer` 并实现 `IHttpHost`，用于在 TCP 连接之上提供 HTTP 协议处理能力。

主要职责：

1. 保存路由映射 `Routes`，并在收到请求时根据路径匹配 `IHttpHandler`；
2. 为每个网络会话创建对应的 `HttpSession` 协议处理器（`CreateHandler`）；
3. 提供多种 `Map` 重载（委托/控制器/静态文件）以简化注册。

---

## 2. 默认行为与关键属性

### 2.1 基础配置

构造函数中，`HttpServer` 的默认配置为：

- `Name = "Http"`
- `Port = 80`
- `ProtocolType = NetType.Http`
- `ServerName = "NewLife-HttpServer/{Major}.{Minor}"`（从程序集版本生成）

### 2.2 `ServerName`

- 类型：`String`
- 语义：用于 HTTP 响应头中的 `Server` 名称（具体写入由协议栈其它部分完成）。

### 2.3 `Routes`

- 类型：`IDictionary<String, IHttpHandler>`
- Key：路径（区分大小写规则：不区分，`StringComparer.OrdinalIgnoreCase`）
- Value：处理器（`IHttpHandler`）

说明：

- 路由 Key 会在注册时统一确保以 `/` 开头。
- 后注册会覆盖先注册（`Routes[path] = handler`）。

---

## 3. 会话与协议处理

### 3.1 `CreateHandler(INetSession session)`

`HttpServer` 会为每一个底层网络会话创建一个新的 `HttpSession`：

- 返回：`new HttpSession()`

这意味着：

- HTTP 解析、请求/响应生命周期逻辑主要由 `HttpSession` 承担；
- `HttpServer` 更聚焦在“路由表维护”和“匹配处理器”。

---

## 4. 路由注册 API

`HttpServer` 提供多种路由注册方式，最终统一走私有方法 `SetRoute(String path, IHttpHandler handler)`。

### 4.1 映射处理器实例

```csharp
var server = new HttpServer();
server.Map("/api/test", new MyHandler());
```

- `Map(String path, IHttpHandler handler)`

### 4.2 映射委托（Delegate）

适用于快速注册轻量接口。

- `Map(String path, HttpProcessDelegate handler)`
- `Map<TResult>(String path, Func<TResult> handler)`
- `Map<TModel, TResult>(String path, Func<TModel, TResult> handler)`
- `Map<T1, T2, TResult>(String path, Func<T1, T2, TResult> handler)`
- `Map<T1, T2, T3, TResult>(String path, Func<T1, T2, T3, TResult> handler)`
- `Map<T1, T2, T3, T4, TResult>(String path, Func<T1, T2, T3, T4, TResult> handler)`

说明：

- 这些重载会创建 `DelegateHandler` 并把委托赋值到 `Callback`。

示例：

```csharp
server.Map("/health", () => "OK");
```

### 4.3 映射控制器

```csharp
server.MapController<MyController>();
```

- `MapController<TController>(String? path = null)`
- `MapController(Type controllerType, String? path = null)`

规则：

- `path` 为空时：默认为 `/{ControllerName}`，其中 ControllerName 来自 `controllerType.Name.TrimEnd("Controller")`。
- 控制器路由最终会被规范化为：`/{xxx}/*`。
- 注册的处理器类型为 `ControllerHandler`，其 `ControllerType` 指向目标控制器类型。

示例：

```csharp
server.MapController<MyController>("/api");
// 实际注册路由为 /api/*
```

### 4.4 映射静态文件目录

```csharp
server.MapStaticFiles("/js", "./wwwroot/js");
```

- `MapStaticFiles(String path, String contentPath)`

规则：

- `path` 会确保以 `/` 开头；
- 实际用于匹配的路由 Key 为 `path.EnsureEnd("/").EnsureEnd("*")`，例如 `/js/*`；
- `StaticFilesHandler.Path` 为 `path.EnsureEnd("/")`（例如 `/js/`）；
- `StaticFilesHandler.ContentPath` 为传入的 `contentPath`。

---

## 5. 路由设置规范化（`SetRoute`）

所有路由注册最终统一到：

- 参数校验：
  - `path` 不能为空
  - `handler` 不能为空
- 路径规范化：
  - `path = path.EnsureStart("/")`
- 覆盖语义：
  - `Routes[path] = handler`

注意：

- `SetRoute` 不会自动补齐尾部 `/` 或 `*`，这由 `MapController` / `MapStaticFiles` 负责。

---

## 6. 路由匹配规则（`MatchHandler`）

`MatchHandler(String path, HttpRequest? request)` 用于根据“已规范化后的请求路径（不含查询字符串）”匹配处理器。

匹配顺序：

1. **精确匹配**：`Routes.TryGetValue(path, out handler)`
2. **缓存命中**：
   - `_pathCache.TryGetValue(path, out p)`
   - 然后 `Routes.TryGetValue(p, out handler)`
3. **通配符匹配**：枚举 `Routes`，对包含 `*` 的 key 执行：
   - `key.IsMatch(path)`

### 6.1 通配符约定

- 仅当路由 key 包含 `*` 时，才进入模糊匹配。
- 匹配逻辑依赖 `IsMatch` 扩展方法（来自基础库字符串匹配能力）。

### 6.2 匹配缓存 `_pathCache`

- 类型：`IDictionary<String, String>`
- Key：请求路径 `path`
- Value：命中的路由 key（例如 `/api/*`）

缓存策略：

- 命中 `StaticFilesHandler`：缓存该 `path -> routeKey`。
- 非静态文件：仅当 `path.Split('/')` 段数 `<= 3` 才缓存。

目的：

- 避免动态 URL（例如带多段 id 的路径）造成缓存无限膨胀；
- 对常见短路径加速模糊匹配。

缓存实现与上限：

- `_pathCache` 为并发字典，写入走 `MaxPathCacheSize`（默认 4096）限容；达到上限后静默不写，请求走正常路由匹配。

---

## 7. 线程安全与并发注意事项

当前实现的并发语义：

- `Routes` 默认是 `Dictionary`，并非并发容器；
- 典型场景：启动阶段集中注册路由，运行期只读访问；
- 若运行期动态增删路由：需要调用方自行加锁序列化访问。

风险点：

- 在运行期修改 `Routes` 并同时调用 `MatchHandler`，可能触发 `Dictionary` 枚举异常或产生不一致结果。
- `_pathCache` 为并发字典且写入限容（`MaxPathCacheSize`），并发读写安全。

建议：

- 启动完成后不要再变更路由；
- 或者在外部加锁，确保 `Map/SetRoute` 与 `MatchHandler` 不并发执行。

---

## 8. 最小示例

> 说明：示例只演示 `HttpServer` 的路由注册与组合。实际启动监听、会话收发等能力由 `NetServer` 提供，请以项目内现有示例或 `NetServer` 文档为准。

```csharp
using NewLife.Http;

var server = new HttpServer
{
    Port = 8080,
    ServerName = "MyServer/1.0",
};

server.Map("/health", () => "OK");
server.MapStaticFiles("/static", "./wwwroot");
server.MapController<MyController>("/api");

server.Start();
```

---

## 9. 常见问题

### 9.1 为什么 `MapController` 会自动加上 `/*`？

控制器通常需要匹配其“子路径”，例如 `/api/user/list`、`/api/user/detail/123` 等。通过 `/*` 让同一个控制器处理器接管该前缀下的所有请求。

### 9.2 为什么部分路径不做缓存？

对多段动态 URL 全量缓存可能导致 `_pathCache` 持续增长（缓存膨胀）。当前策略仅缓存短路径或静态文件命中，以实现加速与空间之间的折中。

---

## 10. 相关源码

- `NewLife.Core/Http/HttpServer.cs`
- `NewLife.Core/Http/HttpSession.cs`
- `NewLife.Core/Http/Handlers/DelegateHandler.cs`（按项目实际路径为准）
- `NewLife.Core/Http/Handlers/ControllerHandler.cs`
- `NewLife.Core/Http/Handlers/StaticFilesHandler.cs`

---

## 11. v12 行为变更（升级须知）

| 变更 | 说明 |
|------|------|
| 路由参数优先 | 路径参数（`{id}` 等）在 `PrepareRequest` 解析完之后合并，因此**优先于**查询串/表单同名字段。此前 `?id=999` 可覆盖路径参数，现在以路径参数为准 |
| 响应体所有权随响应 | 处理器返回后 `HttpResponse.Body` 会被归还池化缓冲。处理器若在返回后仍持有该引用（审计、重试、日志），需先自行复制 |
| `Content-Length` 规则 | 204/304 响应不再声明 `Content-Length`；`Build` 与 `BuildHeaderPacket`（HEAD/流式）两条路径同规则（RFC 7230 §3.3.2） |
| `Transfer-Encoding: chunked` 请求 | **已支持分块解码**（含块扩展 `;` 与终止块后的 trailer 段）：跨轮累积原始字节，收齐终止块后解码交付，并同步移除 `Transfer-Encoding` 头、补齐 `Content-Length`——业务侧看到的主体与头部，与普通 `Content-Length` 请求完全一致。坏块长度行回 `400`，原始字节超 `MaxRequestLength` 回 `413`，其它传输编码（gzip/identity）仍回 `400` |
| 路径缓存并发化 | `_pathCache` 改为并发字典并新增 `MaxPathCacheSize`（默认 4096），超限后静默不写 |
| WebSocket 握手校验 | 带 `Sec-WebSocket-Key` 但握手四要素不合法时回 `400 Bad Request`（此前会落到普通路由报 404，掩盖真实原因）；`Upgrade`/`Connection`/`Sec-WebSocket-Version` 按逗号分隔的**令牌列表**解析，版本含 `13` 即接受 |
| 非法 `Content-Length` | `Content-Length` 非数字、负数或超出范围时回 `400 Bad Request` 并关闭连接；此前解析失败静默当 0 处理，请求体边界随之错位 |
| HEAD 不发实体 | HEAD 请求只发送响应头，实体流（静态文件/嵌入资源）就地释放；此前会把整个实体发出去 |

---

## 12. 能力边界（不做什么）

`HttpServer` 的定位是**轻量级嵌入式 HTTP 服务器**，不是通用 Web 服务器。新增能力只按两条判据取舍——**下游能正常用**、**不在连接边界上制造歧义**；其余需求请直接用 ASP.NET Core / Kestrel。

**明确不做**：

| 不做项 | 替代方式 |
|--------|----------|
| HTTP/2、HTTP/3 | Kestrel |
| HTTP 层 TLS/SNI | `NetServer` 的 SSL 配置，或前置反向代理 |
| 响应压缩（gzip/deflate） | 前置反向代理 |
| `Range` 分段 / `206` | 断点续传属下载器能力（`WebClientX`），不属服务端 |
| `ETag` / `Last-Modified` / 条件请求 | 无缓存语义，静态文件靠前置反向代理 |
| 请求级慢速攻击专项防护 | 已由 `HttpSession.MaxRequestIdle`（请求停滞上限，默认 30 秒回 408）与网络层会话超时兜底 |

**`MapController` 封边**：只提供“最小 MVC”——按路径段定位方法、从参数字典绑参、调 DI、返回 `HttpResponse`。以下一律不做，避免膨胀为标准 MVC：

- 特性路由（`[Route]`/`[HttpGet]` 等）
- 模型绑定验证与 `ModelState`
- 过滤器管道（`IActionFilter` 那一套）
- 内容协商与格式化器

**已知限制**（明确接受，不修）：

- `Transfer-Encoding` 与 `Content-Length` 同时出现时不拒绝，按 chunked 处理且不强制关闭连接。只在**与前置代理共存**时构成请求走私面；内网/嵌入式无代理部署不成立，收紧与轻量取向相悖。
- HTTP/1.0 请求携带 `Transfer-Encoding` 不做特殊处理。HTTP/1.0 客户端不会产生该组合，实际不可达。
- 分块请求体的原始字节全量缓冲（受 `MaxRequestLength` 约束），不做流式交付；业务侧始终拿到完整主体。
- `Expect` 只处理 `100-continue`，其余期望一律忽略（不回 `417`）。
