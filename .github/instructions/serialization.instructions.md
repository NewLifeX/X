---
applyTo: "**/Serialization/**"
---

# 序列化模块开发指令

适用于 `NewLife.Serialization` 命名空间下的序列化开发。

---

## 1. 架构分层

| 层级 | 说明 |
|------|------|
| 扩展方法入口 | `JsonHelper`（`ToJson`/`ToJsonEntity`）、`XmlHelper` |
| JSON 宿主 | `IJsonHost` → `FastJson`（默认）/ `SystemJson`（可切换） |
| 二进制序列化 | `Binary` 类，Handler 链式处理 |
| Span 序列化 | `SpanSerializer`，零分配高性能 |
| 接口层 | `IFormatterX`（通用）、`IBinary`（二进制）、`IJsonHost`（JSON） |

---

## 2. JSON 序列化规范

### 2.1 统一入口

- **序列化**：`obj.ToJson()` 扩展方法，禁止直接构造 `JsonWriter`
- **反序列化**：`json.ToJsonEntity<T>()` 或 `json.ToJsonEntity(type)`
- **格式化**：`JsonHelper.Format(json)` 格式化美化

### 2.2 选项控制

```csharp
// 通过 JsonOptions 控制行为
obj.ToJson(new JsonOptions { Indented = true, NullValue = false, CamelCase = true });

// 或快捷参数
obj.ToJson(indented: true, nullValue: false, camelCase: true);
```

### 2.3 IJsonHost 切换

- `JsonHelper.Default` 默认为 `FastJson`（NewLife 内置高性能实现）
- 需要 `System.Text.Json` 兼容性时可切换 `SystemJson`
- 自定义时实现 `IJsonHost` 接口并赋值给 `JsonHelper.Default`

---

## 3. Binary 序列化规范

### 3.1 核心属性（必须理解）

| 属性 | 默认 | 说明 |
|------|------|------|
| `EncodeInt` | `false` | 7位编码压缩整数（变长编码，小值省空间） |
| `IsLittleEndian` | `true` | 字节序，与硬件通信时注意 |
| `SizeWidth` | `0` | 集合/字符串长度字段宽度：0/1/2/4 字节 |
| `TrimZero` | `false` | 字符串是否截断零字节 |
| `UseRef` | `false` | 引用关系序列化（复杂对象图） |
| `FullTime` | `false` | DateTime 用 8 字节完整时间还是 4 字节秒数 |
| `Version` | `null` | 协议版本，影响读写行为 |

### 3.2 Handler 扩展

Binary 使用处理器链，通过 `AddHandler<T>()` 添加自定义处理器：
- 处理器实现 `IBinaryHandler`，覆盖 `Write`/`TryRead`
- 优先级数值越小越先执行
- 内置处理器：`BinaryGeneral`、`BinaryNormal`、`BinaryComposite`、`BinaryList`、`BinaryDictionary`

---

## 4. SpanSerializer 规范（高性能场景）

- 适用于 IoT 协议、网络帧等零分配场景
- 目标类型实现 `ISpanSerializable` 接口
- 序列化：`SpanSerializer.Serialize(obj)` 返回 `IOwnerPacket`（需 `Dispose` 归还缓冲区）
- 反序列化：`SpanSerializer.Deserialize<T>(span)`
- `SpanWriter`/`SpanReader` 为底层 Span 读写工具

---

## 5. 常见错误

- ❌ JSON 反序列化未处理 `null` 返回值
- ❌ Binary 序列化双方 `IsLittleEndian`/`SizeWidth` 不一致导致协议不匹配
- ❌ `IOwnerPacket` 未 `Dispose` 导致 ArrayPool 泄漏
- ❌ 在热点路径用 `ToJson()` 生成临时字符串（应考虑 `SpanSerializer` 或 `Binary`）
