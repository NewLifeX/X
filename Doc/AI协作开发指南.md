# NewLife 系列项目 AI 协作开发指南

本文档指导 NewLife 生态中依赖 `NewLife.Core` 的开源项目（如 XCode、Cube、Redis、MQTT、Agent 等）如何配置 VS Code Copilot 的 instructions / skills / agents 文件，实现项目级 AI 协作。

---

## 1. 文件结构总览

```
你的项目/
├── .github/
│   ├── copilot-instructions.md          # 主指令（自动加载）
│   ├── instructions/
│   │   ├── {module}.instructions.md     # 模块级指令（按 applyTo 自动加载）
│   │   └── ...
│   ├── skills/
│   │   ├── {topic}.skill.md             # 技能文件（用户 # 引用）
│   │   └── ...
│   └── agents/
│       ├── {role}.agent.md              # 代理文件（用户 @ 调用）
│       └── ...
```

### 三类文件的职责

| 类型 | 加载方式 | 用途 | 内容侧重 |
|------|---------|------|---------|
| instructions | 自动加载（applyTo 匹配或触发信号） | 编码约束与规则 | 必须/禁止、架构约束、命名规范 |
| skills | 用户在 Chat 中 `#` 引用 | 使用指南与示例 | 怎么用、代码示例、最佳实践 |
| agents | 用户在 Chat 中 `@` 调用 | 专用 AI 角色 | 角色定义、工作流、输出格式 |

---

## 2. 主指令 copilot-instructions.md

### 2.1 基础模板

```markdown
# {项目名} Copilot 协作指令

适用于 {项目名} 全部代码。简体中文回复。
本项目依赖 NewLife.Core，核心编码规范继承 NewLife 主仓库 copilot-instructions.md。

---

## 1. 专用指令（前置检查）

**开始任务前，将用户请求与下表逐行匹配，命中则读取对应指令文件。**

| 触发信号 | 指令文件 |
|---------|---------|
| {关键词列表} | `{module}.instructions.md` |

---

## 2. 项目概述

- 项目名：{NuGet 包名}
- 核心功能：一句话描述
- 依赖：NewLife.Core {最低版本}+
- 目标框架：{net45/net6.0/net8.0 等}

---

## 3. 架构约束

（项目专属的架构规则，如实体基类、控制器基类、中间件注册方式等）

---

## 4. 编码规范补充

（在 NewLife 核心规范基础上，项目特有的补充规则）

---

## 5. 常见模式

（项目中高频使用的代码模式，让 AI 优先按此模式生成代码）

---

## 6. 禁止项

（项目特有的禁止操作）

---

（完）
```

### 2.2 要点

- **继承而非重复**：NewLife 核心规范（类型名 `String`/`Int32`、`Pool.StringBuilder` 等）无需重复声明，只写项目专属规则
- **触发信号表**：关键词覆盖项目核心概念，用 `/` 分隔多个关键词
- **applyTo 联动**：触发信号表中的指令文件应与 instructions 目录中的文件一一对应

---

## 3. Instructions 指令文件

### 3.1 文件格式

```markdown
---
applyTo: "**/YourModule/**"
---

# {模块名} 开发指令

## 架构概述
（模块核心架构，3-5 句话）

## 核心接口
（列出关键接口/类及其职责）

## 编码规则

### 必须
- 规则 1
- 规则 2

### 禁止
- 禁止项 1
- 禁止项 2

## 常见模式
（2-3 个典型代码片段）

## 扩展点
（如何扩展此模块：继承哪个基类、实现哪个接口）
```

### 3.2 applyTo 模式参考

| 项目 | 模块 | applyTo 模式 |
|------|------|-------------|
| XCode | 实体模型 | `"**/Entity/**"` 或 `"**/*.Data/**"` |
| XCode | 数据访问 | `"**/XCode/**"` |
| Cube | 控制器 | `"**/Controllers/**"` |
| Cube | 视图 | `"**/Views/**"` |
| Redis | 缓存实现 | `"**/Caching/**"` |
| MQTT | 协议处理 | `"**/Protocol/**"` |
| Agent | 服务代理 | `"**/Services/**"` |

### 3.3 编写原则

1. **规则明确**：每条规则可直接判断"是否违反"，避免模糊描述
2. **配合示例**：关键规则附带 ✅/❌ 对比代码
3. **控制篇幅**：单个文件 100-200 行，过长则拆分模块
4. **引用源码**：可引用项目中实际文件路径作为范例

---

## 4. Skills 技能文件

### 4.1 文件格式

```markdown
---
description: "一句话描述技能用途，供 Copilot 搜索匹配"
---

# {技能名}

## 功能概述
（这个技能帮助用户做什么）

## 快速开始

### 安装
```shell
dotnet add package {NuGet包名}
```

### 基础用法
```csharp
// 最简单的使用示例
```

## 核心 API

### {类名/接口名}
```csharp
// 关键方法签名（从源码提取）
```

**用法示例：**
```csharp
// 完整可运行的代码片段
```

## 常见场景

### 场景 1：{描述}
```csharp
// 代码
```

### 场景 2：{描述}
```csharp
// 代码
```

## 注意事项
- 注意点 1
- 注意点 2
```

### 4.2 编写原则

1. **面向任务**：以"我想做 xxx"为导向组织内容，而非 API 手册式罗列
2. **可运行代码**：示例代码应可直接复制使用，包含必要的 using 和上下文
3. **API 签名准确**：从源码提取实际方法签名，不虚构参数
4. **覆盖核心场景**：3-5 个最常见使用场景，不追求全覆盖
5. **篇幅适中**：150-300 行，过长 AI 上下文利用率下降

### 4.3 从 NewLife.Core 继承的技能

你的项目无需重复 NewLife.Core 已提供的技能文件。以下技能由 NewLife.Core 仓库维护：

| 技能 | 覆盖内容 |
|------|---------|
| caching | ICache/MemoryCache/Redis 缓存接口 |
| logging-tracing | ILog/XTrace/ITracer 日志与追踪 |
| networking | NetServer/NetSession 网络编程 |
| serialization | JSON/Binary/CSV 序列化 |
| configuration | Config&lt;T&gt;/IConfigProvider 配置管理 |
| http-client | ApiHttpClient HTTP 客户端 |
| dependency-injection | ObjectContainer/Host 依赖注入 |
| timer-scheduling | TimerX/Cron 定时调度 |
| security | Hash/AES/RSA/JWT 安全加密 |
| type-conversion | ToInt/ToBoolean 类型转换 |

你的项目只需编写**项目特有**的技能文件（如 XCode 的实体操作、Cube 的视图开发等）。

---

## 5. Agents 代理文件

### 5.1 文件格式

```markdown
---
description: "代理的一句话描述"
tools:
  - readFile
  - search
  - editFiles
---

# {代理名称}

## 角色定义
（你是谁，擅长什么）

## 工作流程
1. 步骤 1
2. 步骤 2
3. 步骤 3

## 输出格式
（回复的结构化格式要求）

## 约束
（代理的行为边界）
```

### 5.2 可用工具

| 工具名 | 用途 |
|--------|------|
| `readFile` | 读取工作区文件 |
| `search` | 搜索工作区代码 |
| `editFiles` | 编辑工作区文件 |

### 5.3 推荐代理角色

| 代理 | 适用项目 | 用途 |
|------|---------|------|
| 代码审查 | 所有项目 | 按规范检查代码质量 |
| 项目专家 | 所有项目 | 回答项目使用问题 |
| 项目初始化 | 框架类项目 | 引导创建新项目 |
| 数据模型专家 | XCode | 实体设计与 CRUD 生成 |
| API 设计专家 | Remoting/Cube | 接口设计与文档生成 |
| 运维诊断 | Stardust/Agent | 排查部署与运行问题 |

---

## 6. 各项目实施参考

### 6.1 NewLife.XCode（数据中间件）

```
.github/
├── copilot-instructions.md          # XCode 主指令
├── instructions/
│   ├── xcode.instructions.md        # 实体开发规范（applyTo: **/*.Data/**）
│   └── migration.instructions.md    # 数据迁移规范
├── skills/
│   ├── entity-crud.skill.md         # 实体 CRUD 操作指南
│   ├── model-design.skill.md        # 数据模型设计指南
│   └── data-migration.skill.md      # 正反向工程指南
└── agents/
    ├── xcode-expert.agent.md        # XCode 使用专家
    └── code-review.agent.md         # 代码审查
```

**主指令要点：**
- 实体类继承 `Entity<T>`，禁止手写 CRUD SQL
- `Model.xml` 是数据模型定义的唯一真相源
- 字段命名遵循 XCode 规范（PascalCase 属性名、数据库字段名可不同）
- 索引和关系在 Model.xml 中声明

### 6.2 NewLife.Cube（Web 管理平台）

```
.github/
├── copilot-instructions.md
├── instructions/
│   ├── controller.instructions.md   # 控制器规范（applyTo: **/Controllers/**）
│   └── view.instructions.md         # 视图规范（applyTo: **/Views/**）
├── skills/
│   ├── area-module.skill.md         # 区域模块开发
│   ├── list-form.skill.md           # 列表与表单页面
│   └── permission.skill.md          # 权限配置
└── agents/
    ├── cube-expert.agent.md
    └── code-review.agent.md
```

**主指令要点：**
- 控制器继承 `EntityController<T>`，CRUD 自动生成
- 视图使用 Razor，遵循 Cube 布局约定
- 菜单通过 `MenuAttribute` 声明
- 权限模型：角色 → 菜单 → 按钮级

### 6.3 NewLife.Redis（Redis 客户端）

```
.github/
├── copilot-instructions.md
├── instructions/
│   └── redis.instructions.md        # Redis 实现规范
├── skills/
│   ├── redis-usage.skill.md         # Redis 使用指南
│   └── redis-stream.skill.md        # Stream 消息队列
└── agents/
    └── redis-expert.agent.md
```

### 6.4 NewLife.MQTT（MQTT 客户端/服务端）

```
.github/
├── copilot-instructions.md
├── instructions/
│   └── mqtt.instructions.md         # MQTT 协议规范
├── skills/
│   ├── mqtt-client.skill.md         # 客户端接入
│   └── mqtt-server.skill.md         # 服务端开发
└── agents/
    └── mqtt-expert.agent.md
```

### 6.5 Stardust（微服务平台）

```
.github/
├── copilot-instructions.md
├── instructions/
│   ├── registry.instructions.md     # 服务注册发现
│   └── config.instructions.md       # 配置中心
├── skills/
│   ├── service-registry.skill.md    # 服务注册接入
│   ├── config-center.skill.md       # 配置中心使用
│   └── deploy.skill.md              # 部署与发布
└── agents/
    ├── stardust-expert.agent.md
    └── ops-diagnostic.agent.md      # 运维诊断
```

---

## 7. 实施步骤

### 步骤 1：创建主指令

1. 复制第 2 节模板到 `.github/copilot-instructions.md`
2. 填写项目概述、架构约束、编码规范补充
3. 不要重复 NewLife 核心规范，只写增量规则

### 步骤 2：识别模块边界

1. 按项目目录结构划分模块（通常 1 个项目/目录 = 1 个模块）
2. 每个模块创建一个 instructions 文件
3. 设置正确的 `applyTo` glob 模式

### 步骤 3：编写技能文件

1. 从最常被问到的问题出发（如"怎么创建实体"、"怎么配置 Redis"）
2. 每个技能对应一个独立使用场景
3. 确保代码示例可直接运行

### 步骤 4：定义代理

1. 代码审查代理：所有项目都应有，复用 NewLife.Core 的审查维度
2. 项目专家代理：回答"怎么用"类问题
3. 按需添加专用代理（初始化、运维等）

### 步骤 5：在主指令中注册

1. instructions 文件添加到触发信号表
2. skills 和 agents 文件列入参考章节
3. 确保触发关键词覆盖项目核心概念

---

## 8. 质量检查清单

创建完成后，逐项检查：

- [ ] 主指令包含项目概述和架构约束
- [ ] 触发信号表覆盖项目核心关键词
- [ ] 每个 instructions 文件有正确的 `applyTo` 模式
- [ ] 每个 skills 文件有 `description` 字段
- [ ] 代码示例从源码提取，非虚构
- [ ] 没有重复 NewLife 核心规范内容
- [ ] 文件编码为 UTF-8 无 BOM
- [ ] 文件名使用英文小写加连字符

---

## 9. 常见问题

**Q：项目很小，需要全套文件吗？**
A：不需要。最低配置是 `copilot-instructions.md` + 1 个 skills 文件。按需逐步添加。

**Q：instructions 和 skills 内容有重叠怎么办？**
A：instructions 写"必须/禁止"规则，skills 写"怎么用"指南。前者是约束，后者是教程，角度不同。

**Q：如何测试文件是否生效？**
A：在 VS Code 中打开项目，Copilot Chat 中问一个相关问题，观察回复是否遵循了自定义规则。对于 instructions 可编辑匹配 `applyTo` 的文件触发加载。

**Q：多个项目有相同规则，如何复用？**
A：将通用规则放在 NewLife.Core 的 copilot-instructions.md 中，各项目继承。项目级文件只写增量。

---

（完）
