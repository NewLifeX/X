# 数据模型接口（IExtend / IModel / IFilter）

## 概述

`NewLife.Data` 命名空间提供了三个轻量级数据模型接口，用于对象的扩展属性、模型属性索引和过滤器责任链场景。

**命名空间**：`NewLife.Data`

---

## IExtend — 扩展数据接口

用于在对象上携带"非模型/非常规"属性的数据字典，区别于 `IModel` 的"模型属性"访问。

```csharp
public interface IExtend
{
    /// <summary>扩展数据项字典</summary>
    IDictionary<String, Object?> Items { get; }

    /// <summary>设置或获取扩展数据项</summary>
    Object? this[String key] { get; set; }
}
```

### 约定

- `Items` 应返回非空实例，避免调用方判空
- 索引读取缺失键时应返回 `null`，而非抛出异常
- 典型使用场景：网络会话、上下文对象等需要按需挂载临时数据

---

## IModel — 模型数据接口

用于以字符串键索引访问"模型属性"，而不直接使用反射。常用于 WebApi 模型、XCode 实体、对象拷贝与绑定。

```csharp
public interface IModel
{
    /// <summary>设置或获取模型数据项</summary>
    Object? this[String key] { get; set; }
}
```

### 约定

- 实现者应将键与公开属性名（或逻辑名）映射
- 缺失键读取返回 `null`；写入缺失键由实现自行决定行为
- 大部分场景逐步替代 `IExtend`；`IExtend` 适合"临时扩展字段"，而 `IModel` 面向"模型属性"

---

## IFilter — 数据过滤器接口

责任链模式过滤器，用于按顺序对 `FilterContext` 进行处理。常用于协议管道的数据预处理或后处理。

```csharp
public interface IFilter
{
    /// <summary>下一个过滤器</summary>
    IFilter? Next { get; }

    /// <summary>对封包执行过滤器</summary>
    void Execute(FilterContext context);
}
```

### FilterContext

```csharp
public class FilterContext
{
    /// <summary>封包。为 null 表示终止后续过滤</summary>
    public virtual IPacket? Packet { get; set; }
}
```

### 辅助方法

```csharp
public static class FilterHelper
{
    /// <summary>在链条中查找指定类型的过滤器</summary>
    public static IFilter? Find(this IFilter filter, Type filterType);

    /// <summary>泛型版本</summary>
    public static TFilter? Find<TFilter>(this IFilter filter) where TFilter : IFilter;
}
```
