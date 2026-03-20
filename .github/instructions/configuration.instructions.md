---
applyTo: "**/Configuration/**"
---

# 配置系统模块开发指令

适用于 `NewLife.Configuration` 命名空间下的配置系统开发。

---

## 1. 架构分层

| 层级 | 类型 | 说明 |
|------|------|------|
| 统一接口 | `IConfigProvider` | 标准配置操作契约 |
| 泛型基类 | `Config<T>` | 强类型配置单例 |
| 本地实现 | `JsonConfigProvider`、`XmlConfigProvider`、`IniConfigProvider` | 文件配置 |
| 远程实现 | `HttpConfigProvider`、`ApolloConfigProvider` | 配置中心适配 |
| 命令行 | `CommandParser` | 命令行参数解析 |
| 节点模型 | `IConfigSection` | 树状配置节点 |

---

## 2. Config\<T\> 使用规范

### 2.1 定义方式

```csharp
public class MySetting : Config<MySetting>
{
    public String Name { get; set; } = "默认值";
    public Int32 Port { get; set; } = 8080;

    protected override void OnLoaded()
    {
        // 加载后校验/修正
        if (Port <= 0) Port = 8080;
    }
}
```

### 2.2 使用方式

- `MySetting.Current` — 单例访问，首次访问自动从配置文件加载
- `MySetting.Current.Save()` — 保存修改到文件
- `IsNew` 属性判断是否首次创建（配置文件不存在时为 `true`）

### 2.3 Provider 覆盖

```csharp
// 使用 HTTP 配置中心
MySetting.Provider = new HttpConfigProvider { Server = "http://config-center" };
```

---

## 3. IConfigProvider 开发规范

### 3.1 核心方法

- `LoadAll()` / `SaveAll()` — 全量加载/保存
- `Load<T>()` — 加载为强类型对象
- `Save<T>(model)` — 保存强类型对象
- `Bind<T>(model, autoReload)` — 绑定对象并自动重载
- `Changed` 事件 — 配置变更通知
- 索引器 `this[key]` 支持冒号分隔的多级 key：`"Database:ConnectionString"`

### 3.2 节点操作

- `Root` 属性为根节点（`IConfigSection`）
- `GetSection(key)` 获取子节点
- 节点支持树状结构，子节点通过 `Childs` 属性访问

---

## 4. HttpConfigProvider 规范

- `Server` — 配置中心地址
- `AppId` / `Secret` — 应用标识和密钥
- `Scope` — 作用域（如环境名）
- `Period` — 轮询间隔秒数（默认 60）
- `CacheLevel` — 本地缓存级别（加密/明文/不缓存）
- `Action` — API 路径（默认 `Config/GetAll`，适配星尘配置中心）
- 适配 Apollo 时使用 `ApolloConfigProvider`

---

## 5. CommandParser 规范

```csharp
var parser = new CommandParser { IgnoreCase = true };
var args = parser.Parse(Environment.GetCommandLineArgs());
// --port 8080 → args["port"] = "8080"
// -v → args["v"] = null
```

- `TrimStart` 默认 `true`，自动去除 `--` / `-` 前缀
- `TrimQuote` 去除值两端引号
- `Split` 将命令行字符串分割为参数数组

---

## 6. 常见错误

- ❌ 在构造函数中访问 `Config<T>.Current`（可能触发递归加载）
- ❌ 手动 `new` Config 对象而非使用 `Current` 单例
- ❌ `HttpConfigProvider` 未设置 `AppId`（配置中心会拒绝）
- ❌ `Bind` 后修改对象属性未调用 `Save()`（变更不会持久化）
