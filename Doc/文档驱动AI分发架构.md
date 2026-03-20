# 文档驱动 AI 分发架构

**核心洞察**：无论是 VS Code instructions/skills/agents、dotnet tool、NuGet 包、MCP Server，还是 llms.txt，一切分发渠道的根基都是**完整、结构化、可机器解析**的功能文档。先把文档做好，再自动转化为各渠道格式。

---

## 1. 架构总览

```
                    ┌────────────────────────────┐
                    │   各 NewLife 项目源码仓库     │
                    │ (Core/XCode/Cube/Redis/...) │
                    └─────────┬──────────────────┘
                              │ Copilot 分析源码
                              ▼
                    ┌────────────────────────────┐
                    │   标准化 Markdown 文档       │
                    │  Doc/*.md + Doc/索引.json    │
                    │  (每个项目独立维护)           │
                    └─────────┬──────────────────┘
                              │ 推送工具 (dotnet tool)
                              ▼
              ┌───────────────┼───────────────┐
              │               │               │
              ▼               ▼               ▼
    ┌─────────────┐ ┌──────────────┐ ┌──────────────┐
    │ newlifex.com │ │ llms.txt     │ │ copilot 文件  │
    │ 人类文章展示  │ │ LLM 检索索引  │ │ instructions │
    │ + 全文搜索   │ │ + llms-full  │ │ skills/agents│
    └─────────────┘ └──────────────┘ └──────────────┘
              │               │               │
              ▼               ▼               ▼
    ┌─────────────┐ ┌──────────────┐ ┌──────────────┐
    │ 开发者浏览器  │ │ ChatGPT 等   │ │ VS Code      │
    │ 搜索引擎     │ │ 外部 LLM     │ │ Copilot Chat │
    └─────────────┘ └──────────────┘ └──────────────┘
                              │
                    ┌─────────┴──────────┐
                    │  MCP Server (未来)  │
                    │  实时 API 查询       │
                    └────────────────────┘
```

### 关键设计决策

| 决策点 | 方案 | 理由 |
|--------|------|------|
| 文档格式 | Markdown + YAML frontmatter | 人机双可读，Git 友好 |
| 索引格式 | JSON（机器）+ Markdown（人类） | JSON 供工具解析，Markdown 供浏览 |
| 推送方式 | dotnet tool 命令行 | 自动化 CI/CD 友好 |
| 文档粒度 | 每个公共类型一个条目，核心类独立文件 | 平衡覆盖度和维护成本 |
| 版本管理 | 文档随源码版本走，推送时带版本号 | 保证文档与代码一致 |

---

## 2. 文档层次模型

每个 NewLife 项目的文档分三层：

### 第一层：项目索引（1 个文件）

```
Doc/索引.json — 机器可解析的完整模块/类型索引
Doc/AI功能索引.md — 人类可读的模块索引（由 JSON 生成或手动维护）
```

### 第二层：模块文档（每模块 1 个文件）

```
Doc/{模块名}.md — 每个功能模块的完整使用指南
                   覆盖架构、API、示例、注意事项
```

### 第三层：专题文档（按需）

```
Doc/{专题名}.md — 跨模块的专题（如"高级二进制序列化"、"WebSocket双向通信"）
```

---

## 3. 文档索引 JSON 格式

每个项目根目录下 `Doc/索引.json`，作为所有下游渠道的数据源：

```json
{
  "$schema": "https://newlifex.com/schemas/doc-index-v1.json",
  "project": "NewLife.Core",
  "nuget": "NewLife.Core",
  "version": "11.10.2026.0319",
  "repository": "https://github.com/NewLifeX/X",
  "description": "新生命核心库，涵盖缓存、网络、序列化、日志追踪等基础组件",
  "updated": "2026-03-19",
  "modules": [
    {
      "namespace": "NewLife.Caching",
      "name": "缓存系统",
      "summary": "统一缓存接口，内存缓存和 Redis 客户端",
      "doc": "缓存系统ICache.md",
      "url": "https://newlifex.com/core/icache",
      "types": [
        {
          "name": "ICache",
          "kind": "interface",
          "summary": "标准缓存操作接口，Get/Set/Remove/GetAll 等",
          "doc": "缓存系统ICache.md",
          "url": "https://newlifex.com/core/icache",
          "keyMethods": [
            "Set(key, value, expire)",
            "Get<T>(key)",
            "Remove(key)",
            "GetAll<T>(keys)"
          ]
        },
        {
          "name": "MemoryCache",
          "kind": "class",
          "summary": "高性能单机内存缓存，支持过期和容量策略",
          "doc": "缓存系统ICache.md",
          "url": "https://newlifex.com/core/memory_cache"
        }
      ]
    }
  ]
}
```

### 字段说明

| 字段 | 必选 | 说明 |
|------|------|------|
| `project` | ✅ | 项目名 |
| `nuget` | ✅ | NuGet 包名 |
| `version` | ✅ | 当前版本号 |
| `modules[].namespace` | ✅ | 命名空间 |
| `modules[].name` | ✅ | 模块中文名 |
| `modules[].summary` | ✅ | 一句话描述 |
| `modules[].doc` | ❌ | 本地文档文件名 |
| `modules[].url` | ❌ | 官网文档 URL |
| `modules[].types[]` | ✅ | 公共类型列表 |
| `types[].keyMethods` | ❌ | 核心方法签名（供 LLM 快速理解） |

---

## 4. llms.txt 规范

遵循 [llms.txt 标准](https://llmstxt.org/)，在 `newlifex.com` 根目录放置两个文件：

### 4.1 `/llms.txt` — 精简索引

```
# NewLife 开源组件

> 新生命团队基础组件和中间件，20年+ 积累，支持 .NET Framework 4.5 到 .NET 10.0。

## 核心库 NewLife.Core

- [缓存系统](https://newlifex.com/core/icache): 统一 ICache 接口，内存缓存 + Redis
- [网络库](https://newlifex.com/core/netserver): 高性能 TCP/UDP 服务器，单机 2266万tps
- [序列化](https://newlifex.com/core/json): JSON/Binary/XML/CSV 多格式序列化
- [日志追踪](https://newlifex.com/core/tracer): ILog 日志 + ITracer APM 链路追踪
- [配置系统](https://newlifex.com/core/config): 多源配置，支持本地/HTTP/Apollo
- [依赖注入](https://newlifex.com/core/object_container): 轻量级 IoC 容器
- [定时调度](https://newlifex.com/core/timerx): TimerX 高精度定时器 + Cron
- [安全加密](https://newlifex.com/core/security_helper): RSA/AES/SM4/JWT
- [类型转换](https://newlifex.com/core/utility): ToInt/ToBoolean 等高效转换
- [HTTP客户端](https://newlifex.com/core/api_http): 多节点负载均衡 HTTP 客户端

## 数据中间件 NewLife.XCode

- [XCode 文档](https://newlifex.com/xcode): ORM + 大数据中间件，单表百亿级

## Web 平台 NewLife.Cube

- [Cube 文档](https://newlifex.com/cube): 魔方 Web 快速开发平台

## 分布式缓存 NewLife.Redis

- [Redis 文档](https://newlifex.com/core/redis): 高性能 Redis 客户端，百亿级验证

## 微服务平台 Stardust

- [Stardust 文档](https://newlifex.com/stardust): 星尘，服务注册发现 + 配置中心 + APM

## Optional

- [MQTT 物联网](https://newlifex.com/core/mqtt): MQTT 客户端和服务端
- [RocketMQ](https://newlifex.com/core/rocketmq): 纯托管 RocketMQ 客户端
- [蚂蚁调度](https://newlifex.com/antjob): 分布式大数据计算
- [服务管理](https://newlifex.com/core/agent): Windows 服务 / Linux Systemd
```

### 4.2 `/llms-full.txt` — 完整文档

由工具程序自动生成，将所有项目的模块文档拼接为一个大文本，供 LLM 深度阅读。结构：

```
# NewLife 完整技术文档

> 版本 11.10 | 更新于 2026-03-19

---

## NewLife.Core

### 缓存系统 (NewLife.Caching)

{缓存系统ICache.md 全文内容}

### 网络库 (NewLife.Net)

{网络服务端NetServer.md 全文内容}

... 所有模块依次展开 ...

---

## NewLife.XCode

{XCode 各模块文档}

...
```

### 4.3 维护规则

- `llms.txt` 精简版**手动维护**（通常只在新增/删除项目时更新）
- `llms-full.txt` 由推送工具**自动生成**（拼接所有 Doc/*.md）
- 每次发布新版本时自动更新

---

## 5. 推送工具设计

### 5.1 工具形态

`NewLife.DocPublisher` — dotnet tool 或独立控制台程序

```shell
# 安装
dotnet tool install NewLife.DocPublisher -g

# 扫描当前项目并生成索引
doc-publisher index

# 推送到官网
doc-publisher push --site https://newlifex.com --token {api-token}

# 生成 llms.txt
doc-publisher llms --output ./llms-full.txt

# 全流程：索引 → 推送 → llms
doc-publisher publish --site https://newlifex.com
```

### 5.2 核心功能

| 命令 | 输入 | 输出 | 说明 |
|------|------|------|------|
| `index` | Doc/*.md | Doc/索引.json | 扫描文档，生成/更新 JSON 索引 |
| `push` | Doc/索引.json + Doc/*.md | HTTP API 调用 | 推送到官网 CMS |
| `llms` | Doc/索引.json + Doc/*.md | llms.txt / llms-full.txt | 生成 LLM 索引 |
| `validate` | Doc/索引.json | 控制台报告 | 检查文档完整性和链接有效性 |
| `diff` | Git 历史 | 变更列表 | 仅推送有变更的文档 |

### 5.3 官网 API 接口

官网接收推送需提供以下 API：

```
POST /api/doc/publish
Content-Type: application/json
Authorization: Bearer {token}

{
  "project": "NewLife.Core",
  "version": "11.10.2026.0319",
  "articles": [
    {
      "slug": "core/icache",
      "title": "统一缓存接口ICache",
      "category": "NewLife.Core/缓存系统",
      "content": "... markdown 全文 ...",
      "summary": "标准缓存操作接口",
      "updated": "2026-03-19"
    }
  ]
}
```

### 5.4 CI/CD 集成

```yaml
# GitHub Actions 示例
- name: Publish docs
  if: github.ref == 'refs/heads/master'
  run: |
    dotnet tool install NewLife.DocPublisher -g
    doc-publisher publish --site https://newlifex.com --token ${{ secrets.DOC_TOKEN }}
```

---

## 6. 多渠道格式转换

从标准化 Markdown 文档出发，自动生成各渠道所需格式：

```
Doc/*.md (标准源)
    │
    ├──→ newlifex.com 文章    (push 命令，Markdown → CMS)
    ├──→ llms.txt             (llms 命令，精简索引)
    ├──→ llms-full.txt        (llms 命令，全文拼接)
    ├──→ instructions/*.md    (extract 命令，提取规则)
    ├──→ skills/*.md          (extract 命令，提取指南)
    └──→ MCP Server 知识库    (未来，JSON API 导入)
```

### 格式转换规则

| 源 | 目标 | 转换逻辑 |
|----|------|---------|
| 模块文档 | llms-full.txt | 按索引顺序拼接，添加层级标题 |
| 索引.json | llms.txt | 提取 module.summary，生成链接列表 |
| 模块文档 | 官网文章 | 直接推送 Markdown，官网渲染 |
| 模块文档 | skills/*.md | 提取"快速开始"和"常见场景"段落 |
| 模块文档 | instructions | 提取"规则"和"禁止"段落 |

---

## 7. 文档生成工作流（Copilot 自动化）

### 7.1 单项目文档生成流程

```
1. Copilot 读取项目源码结构（命名空间、公共类型列表）
2. 对每个命名空间，分析核心接口/类的：
   - 公共 API 签名
   - XML 文档注释
   - 已有单元测试（作为用法示例参考）
   - 已有文档（Doc/*.md）
3. 对缺失文档的类型，生成标准格式文档
4. 更新 Doc/索引.json
5. 更新 Doc/AI功能索引.md
```

### 7.2 生成 Prompt 模板

每个项目可用以下 Prompt 触发 Copilot 生成文档（详见 `Doc/文档生成Prompt.md`）。

### 7.3 人工审核检查点

| 阶段 | 自动化 | 人工 |
|------|--------|------|
| 类型扫描 | ✅ Copilot 分析源码 | — |
| 文档草稿 | ✅ Copilot 生成 | — |
| 内容审核 | — | ✅ 确认准确性 |
| 索引更新 | ✅ 工具生成 | ✅ 确认分类正确 |
| 推送发布 | ✅ CI/CD | ✅ 审批发布 |

---

## 8. NewLife.Core 文档覆盖度现状

当前覆盖率：**73%**（146/200 个公共类型有文档）

### 缺失文档优先级

| 优先级 | 模块 | 缺失数 | 缺失率 | 说明 |
|--------|------|--------|--------|------|
| P0 | Security | 8/9 | 89% | 核心安全模块，仅 SecurityHelper 有文档 |
| P0 | Algorithms | 4/4 | 100% | 完全无文档 |
| P1 | Configuration | 6/10 | 60% | 多个 Provider 缺文档 |
| P1 | Log | 5/9 | 56% | 具体日志实现类缺文档 |
| P1 | Yun | 3/3 | 100% | 地图 API 完全缺文档 |
| P2 | Http | 4/7 | 57% | 内部组件，优先级可降 |
| P2 | Collections | 4/7 | 57% | 工具类，按需补充 |
| P2 | Net | 3/8 | 38% | 会话和编解码器 |
| P3 | Data/Web/其它 | 各 1-3 | 低 | 按需补充 |

### 建议执行顺序

1. **第一批（P0）**：Security 全模块 + Algorithms 全模块 → 覆盖率提升至 79%
2. **第二批（P1）**：Configuration 各 Provider + Log 实现类 + Yun → 覆盖率提升至 89%
3. **第三批（P2+P3）**：剩余零散类型 → 覆盖率提升至 95%+

---

## 9. 实施路线图

### 阶段一：文档补全（当前）

- [ ] 为 NewLife.Core 54 个缺失文档的类型逐批生成文档
- [ ] 建立 Doc/索引.json 标准索引
- [ ] 统一现有文档格式（添加 YAML frontmatter）

### 阶段二：推送工具

- [ ] 开发 NewLife.DocPublisher 工具
- [ ] 实现 `index` / `push` / `llms` / `validate` 命令
- [ ] 官网 API 对接
- [ ] 在 newlifex.com 放置 llms.txt 和 llms-full.txt

### 阶段三：其它项目推广

- [ ] 按 AI协作开发指南.md 为 XCode/Cube/Redis 等项目创建文档
- [ ] 各项目接入推送工具
- [ ] CI/CD 自动化

### 阶段四：MCP Server

- [ ] 基于索引 JSON 构建 MCP Server
- [ ] 提供实时 API 查询能力
- [ ] 支持跨项目联合检索

---

## 10. 关键优势

| 传统方式 | 文档驱动方式 |
|---------|-------------|
| 每个渠道独立维护 | 一份文档，多渠道自动分发 |
| 格式不统一 | JSON 索引 + Markdown 标准化 |
| 人工同步更新 | CI/CD 自动推送 |
| AI 无法理解 | llms.txt 供 LLM 检索 |
| 仅服务开发者 | 同时服务人类和 AI |

**核心理念**：AI 生态工具（instructions/skills/agents/MCP）会持续演变，但高质量结构化文档是不变的基础。只要文档准备好，任何新格式都可以自动转化。

---

（完）
