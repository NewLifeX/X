# NewLife.Core AOT 兼容性分析报告

> 分析日期：2026-03-07  
> 分析版本：NewLife.Core 11.13.x  
> AOT 标准：.NET NativeAOT（PublishAot=true）

---

## 概述

本报告对 `NewLife.Core` 库进行全面的 AOT（Ahead-of-Time）兼容性扫描，识别所有在 NativeAOT 编译模式下会导致编译警告、运行时崩溃或功能失效的代码模式，并给出修复建议。

**结论：NewLife.Core 当前大量依赖动态反射机制，其核心设计哲学（运行时动态类型解析、插件扫描、脚本引擎）与 AOT 的静态分析要求存在根本性冲突。** 若要支持 AOT，需要按模块分层处理：部分模块可通过添加 AOT 注解修复，部分模块需要提供替代 AOT 友好实现，少数模块必须标记为 `[RequiresDynamicCode]` 不支持 AOT。

---

## 问题严重级别定义

| 级别 | 含义 |
|------|------|
| 🔴 **严重** | AOT 下会直接崩溃或无法编译，无简单绕过方案 |
| 🟠 **高** | AOT 下运行时异常，需要重构逻辑 |
| 🟡 **中** | AOT 下产生警告，可通过添加注解或源生成器解决 |
| 🟢 **低** | 轻微兼容性问题，有简单修复方案 |

---

## 一、脚本引擎（ScriptEngine）— 🔴 严重，完全不支持 AOT

**文件：** `NewLife.Core/Reflection/ScriptEngine.cs`

### 问题描述

ScriptEngine 是运行时 C# 代码编译引擎，包含以下完全不支持 AOT 的机制：

1. **`CodeDomProvider`** — 调用 `CodeDomProvider.CreateProvider("CSharp")` 在 AOT 下不存在
2. **`System.Reflection.Emit`** — `using System.Reflection.Emit;`，AOT 下 `Emit` 命名空间大部分 API 不可用
3. **`Assembly.Load(byte[])` 动态加载程序集** — AOT 下禁止动态加载代码
4. **`AppDomain.CurrentDomain.AssemblyResolve` 事件** — 部分功能在 AOT 下受限
5. **`rs.CompiledAssembly.GetTypes()` 运行时枚举类型**

```csharp
// ScriptEngine.cs:433 — 完全不可用于 AOT
var provider = CodeDomProvider.CreateProvider("CSharp", opts);

// ScriptEngine.cs:318 — AOT 下禁止动态 IL 加载
Assembly.Load(File.ReadAllBytes(item));

// ScriptEngine.cs:326 — 动态编译产物枚举
Type = rs.CompiledAssembly.GetTypes()[0];
```

**已由 `#if __WIN__` 条件编译保护**，仅 Windows 目标有效，但若 Windows 目标使用 AOT 仍会报错。

### 修复建议

- 整个 `ScriptEngine` 类添加 `[RequiresDynamicCode("ScriptEngine 依赖运行时代码生成，不支持 AOT")]`
- 添加 `[RequiresUnreferencedCode("...")]` 注解
- 文档明确标注该功能需要 `PublishAot=false`

---

## 二、反射子系统（Reflection 模块）— 🔴 严重

该模块是 NewLife.Core 最核心的基础设施，整个框架大量构建于此，AOT 问题影响范围极广。

### 2.1 动态类型查找 — `Type.GetType(string)`

**文件：** `NewLife.Core/Reflection/Reflect.cs`

```csharp
// Reflect.cs:28 — 通过字符串查找类型，AOT 剪裁后类型可能不存在
var type = Type.GetType(typeName);

// Reflect.cs:43 — 同上
var type = Type.GetType(typeName);
```

AOT 剪裁（trimming）会移除未被静态引用的类型，`Type.GetType(string)` 依赖运行时完整类型注册表，剪裁后可能返回 `null`。

**修复建议：** 对外暴露的 `GetType(string)` 方法添加 `[RequiresUnreferencedCode]` 注解，提示调用方需要保留类型元数据。

---

### 2.2 GetAllSubclasses — 全程序集类型扫描

**文件：** `NewLife.Core/Reflection/IReflect.cs`，`AssemblyX.cs`，`Reflect.cs`

```csharp
// IReflect.cs:909 — 扫描所有已加载程序集中的子类型
public virtual IEnumerable<Type> GetAllSubclasses(Type baseType)
{
    foreach (var asm in AppDomain.CurrentDomain.GetAssemblies()) // 🔴
        foreach (var type in GetSubclasses(asm, baseType))
            yield return type;
}

// AssemblyX.cs:156 — 获取所有类型列表
ts = Asm.GetTypes(); // 🟠
```

AOT 下 `AppDomain.CurrentDomain.GetAssemblies()` 依然可用，但 trimming 后很多类型已被移除，扫描结果不完整。依赖此 API 的功能（插件发现、OAuthClient 子类发现等）在 AOT 下会静默失效。

**AssemblyX.cs 中多处 Assembly 动态加载（🔴 严重）：**

```csharp
// AssemblyX.cs:130 — AssemblyResolve 事件回调中按文件路径加载程序集
private static Assembly? OnAssemblyResolve(...)
{
    var asm = Assembly.LoadFrom(file); // 🔴
}

// AssemblyX.cs:649-655 — 扫描目录后逐个 LoadFrom 加载
foreach (var item in ss)
{
    asm = Assembly.LoadFrom(item); // 🔴
}

// AssemblyX.cs:744-745 — OnResolve 中按路径加载未注册程序集
var asm = Assembly.LoadFrom(file); // 🔴（在 AssemblyPaths 中查找后加载）

// AssemblyX.cs:155-198 — GetTypes + GetNestedTypes 完整类型遍历
ts = Asm.GetTypes(); // 🟠
item.GetNestedTypes(BindingFlags.Public | BindingFlags.NonPublic | ...) // 🟠
```

`Assembly.LoadFrom` 在 NativeAOT 下完全不支持（`PlatformNotSupportedException`），`GetTypes()` + `GetNestedTypes()` 依赖运行时完整类型元数据，在 trimming 后结果不完整。

**受影响的调用方：**
- `Web/OAuthClient.cs:96` — `typeof(OAuthClient).GetAllSubclasses(true)` 动态发现 OAuth 提供商
- `Model/IPlugin.cs:93` — 动态创建插件实例
- `Serialization/Json/JsonTest.cs:25` — 动态发现 IJsonHost 实现

**修复建议：**
- `GetAllSubclasses`/`GetSubclasses`/`FindPlugins` 添加 `[RequiresUnreferencedCode]` 注解
- 为 AOT 场景提供手动注册机制（显式注册子类型，代替自动扫描）

---

### 2.3 动态对象创建 — `Activator.CreateInstance`、`ConstructorInfo.Invoke`

**文件：** `NewLife.Core/Reflection/IReflect.cs`，`NewLife.Core/Model/ObjectContainer.cs`

```csharp
// IReflect.cs:495 — 非公开构造函数创建，AOT 下可能被剪裁
_ => Activator.CreateInstance(type, true),

// IReflect.cs:499 — 带参数创建实例
return Activator.CreateInstance(type, parameters);

// ObjectContainer.cs:212,242 — 扫描构造函数并动态调用
var constructors = type.GetConstructors();
// ...
return constructorInfo.Invoke(pv); // 🔴
```

AOT 下 `Activator.CreateInstance(Type, ...)` 需要保留目标类型的构造函数元数据，否则运行时失败。`GetConstructors()` + `Invoke` 模式是 AOT 的典型高风险模式。

**修复建议：**
- `ObjectContainer.CreateInstance` 方法添加 `[RequiresDynamicCode]` 和 `[RequiresUnreferencedCode]`
- 对于已知类型，使用工厂委托 `Func<IServiceProvider, Object>` 代替动态构造
- 长期：引入源生成器自动为注册类型生成工厂代码

---

### 2.4 DynamicObject — 动态绑定器

**文件：** `NewLife.Core/Reflection/DynamicXml.cs`，`NewLife.Core/Reflection/DynamicInternal.cs`

```csharp
// DynamicXml.cs:7 — 继承 DynamicObject 不支持 AOT
public class DynamicXml : DynamicObject

// DynamicInternal.cs:8 — 同上
public class DynamicInternal : DynamicObject

// DynamicInternal.cs:53 — InvokeMember 在 AOT 下不支持
result = Real.GetType().InvokeMember(binder.Name, 
    BindingFlags.InvokeMethod | ..., null, Real, args, ...);
```

`DynamicObject` 依赖 DLR（Dynamic Language Runtime）动态绑定机制，AOT 下 DLR 不可用（`System.Dynamic` 命名空间中的核心功能在 NativeAOT 下受限）。`InvokeMember` 在 AOT 下完全不可用。

**修复建议：**
- `DynamicXml` 和 `DynamicInternal` 类标记 `[RequiresDynamicCode]`
- 如有 AOT 使用场景，改用强类型包装器代替 `DynamicObject`

---

### 2.5 MakeGenericType — 运行时泛型构造

**文件：** 遍布序列化、配置、反射子系统

```csharp
// Configuration/ConfigHelper.cs:191
typeof(Dictionary<,>).MakeGenericType(pi.PropertyType.GetGenericArguments())

// Configuration/ConfigHelper.cs:323
typeof(List<>).MakeGenericType(elementType).CreateInstance()

// Reflection/IReflect.cs:843
baseType = baseType.MakeGenericType(type.GenericTypeArguments);

// Serialization/Json/JsonReader.cs:94
type = typeof(List<>).MakeGenericType(elmType);

// Serialization/ServiceTypeResolver.cs:33,38
typeof(List<>).MakeGenericType(...) / typeof(Dictionary<,>).MakeGenericType(...)
```

`MakeGenericType` 在 AOT 下需要目标泛型实例化（如 `List<String>`、`Dictionary<String, Int32>`）在编译时已存在。对于**未提前枚举的类型组合**，AOT 下运行时会抛出异常。

常见组合（`List<String>`、`Dictionary<String, Object>`）通常已在编译时存在，风险较低；但对于用户自定义类型的动态组合，存在运行时失败风险。

**修复建议：**
- 添加 `[RequiresDynamicCode("使用 MakeGenericType 构造运行时泛型类型")]` 注解
- 对于有限的常见类型组合，可改用 `switch` 或预先注册的方式

---

## 三、序列化子系统（Serialization 模块）— 🟠 高

### 3.1 Expression.Compile() — 动态委托生成

**文件：** `NewLife.Core/Serialization/SpanSerializer.cs`

```csharp
// SpanSerializer.cs:63 — 编译属性取值 Lambda
return Expression.Lambda<Func<Object, Object?>>(body, target).Compile(); // 🟠

// SpanSerializer.cs:78 — 编译属性赋值 Lambda
return Expression.Lambda<Action<Object, Object?>>(assign, target, value).Compile(); // 🟠
```

`Expression.Compile()` 在 AOT 下依赖解释执行模式（`CompileToMethod` 已不可用），性能会大幅下降（无 JIT），甚至在某些 AOT 配置下抛出异常。

**修复建议：**
- 对 AOT 场景，改用直接反射（`PropertyInfo.GetValue`/`SetValue`）或通过源生成器预生成访问代码
- 添加运行时检测：`#if NET7_0_OR_GREATER` 可使用 `RuntimeFeature.IsDynamicCodeSupported` 选择降级路径
- 在 `BuildGetter`/`BuildSetter` 方法上添加 `[RequiresDynamicCode]`

---

### 3.2 BinaryFormatter — 已废弃，不支持 AOT

**文件：** `NewLife.Core/Serialization/Binary/BinaryUnknown.cs`，`JsonTest.cs`

```csharp
// BinaryUnknown.cs:35,60 — BinaryFormatter 在 .NET 9+ 已移除
var bf = new BinaryFormatter();
```

`BinaryFormatter` 在 .NET 5 开始标记废弃，.NET 9 默认禁用（需要 `AppContext.SetSwitch("System.Runtime.Serialization.EnableUnsafeBinaryFormatterSerialization", true)`），NativeAOT 下完全不可用。

**修复建议：**
- 替换为 `System.Text.Json`、自定义二进制序列化或 `MemoryPack`
- `JsonTest.cs` 是测试代码，可直接移除相关测试用例

---

### 3.3 XmlSerializer — 动态代码生成

**文件：** `NewLife.Core/Xml/SerializableDictionary.cs`

```csharp
// SerializableDictionary.cs:88,98 — 动态生成序列化代码
var xs = new XmlSerializer(type);
```

`XmlSerializer` 在构造时会动态生成序列化/反序列化代码（在 .NET Framework 下运行时生成 DLL），AOT 下需要预生成（使用 `XmlSerializer` 的源生成模式或手动预编译）。

**修复建议：**
- 使用 `[XmlSerializerGenerator]` 源生成器预生成序列化代码（.NET 8+）
- 或改用 `System.Text.Json` + 手动序列化

---

### 3.4 DefaultJsonTypeInfoResolver — 基于反射的 JSON 元数据

**文件：** `NewLife.Core/Serialization/Json/IJsonHost.cs`

```csharp
// IJsonHost.cs:326 — DefaultJsonTypeInfoResolver 依赖反射，剪裁不安全
opt.TypeInfoResolver = new System.Text.Json.Serialization.Metadata.DefaultJsonTypeInfoResolver
{
    Modifiers = { ... }
};
```

`DefaultJsonTypeInfoResolver` 使用反射扫描类型元数据，在 trimming 启用时会产生 `IL2026` 警告，AOT 下反射扫描到的属性可能已被剪裁。

**SystemJson 的 `Read` 方法：**
```csharp
// IJsonHost.cs:398 — 使用运行时 Type 对象反序列化，不安全
return JsonSerializer.Deserialize(json, type, opt);
```

**修复建议：**
- 为 AOT 场景提供 `JsonSerializerContext`（源生成器）实现
- `SystemJson` 类标记 `[RequiresUnreferencedCode]`

---

### 3.5 FastJson — 全反射 JSON 序列化

**文件：** `NewLife.Core/Serialization/Json/JsonReader.cs`，`JsonWriter.cs`

```csharp
// JsonReader.cs:246 — 通过字符串反序列化 Type 对象
if (type == typeof(Type) && value is String str) 
    return Type.GetType(str) ?? Type.GetType("System." + str);

// JsonReader.cs:94,148 — 运行时泛型构造
type = typeof(List<>).MakeGenericType(elmType);
type = typeof(Dictionary<,>).MakeGenericType(types[0], types[1]);
```

FastJson 完全基于反射，不支持 AOT。

**修复建议：**
- FastJson 整体标记 `[RequiresUnreferencedCode]` 和 `[RequiresDynamicCode]`
- AOT 场景应使用 `SystemJson`（配合源生成器）

---

### 3.6 XmlHelper — 方法名字符串动态查找 — 🟠 高

**文件：** `NewLife.Core/Xml/XmlHelper.cs`

```csharp
// XmlHelper.cs:241 — 拼接类型名查找 XmlConvert 的 ToString 重载
var method = typeof(XmlConvert).GetMethodEx("ToString", type);

// XmlHelper.cs:258 — 拼接字符串 "To" + 类型名查找转换方法
var method = typeof(XmlConvert).GetMethodEx("To" + type.Name, typeof(String));
```

`GetMethodEx` 是 `Type.GetMethod(name, paramTypes)` 的扩展包装。`"To" + type.Name` 在运行时动态构造方法名（如 `"ToInt32"`、`"ToBoolean"`），AOT trimming 会按静态引用图剪裁 `XmlConvert` 的重载，若应用中没有静态调用 `XmlConvert.ToInt32(string)`，该方法可能在编译时被移除，导致运行时 `GetMethodEx` 返回 `null` 并抛出 `XException`。

**受影响的方法：**
- `XmlHelper.XmlConvertToString(Object)` — 将基础类型转为 XML 字符串
- `XmlHelper.XmlConvertFromString(Type, String)` — 从 XML 字符串还原基础类型值

**修复建议：**
- 改用显式 `switch(TypeCode)` 分支调用具体的 `XmlConvert` 方法，完全消除反射
- 参考 `TypeCode` 枚举覆盖所有基础类型，静态分支对 AOT 安全且性能更好

```csharp
// 建议替换为 switch 分支（AOT 安全）
internal static String? XmlConvertToString(Object value)
{
    return value.GetType().GetTypeCode() switch
    {
        TypeCode.Boolean => XmlConvert.ToString((Boolean)value),
        TypeCode.Int32   => XmlConvert.ToString((Int32)value),
        TypeCode.Double  => XmlConvert.ToString((Double)value),
        // ... 其他基础类型
        _ => throw new XException($"Type {value.GetType()} does not support XmlConvert")
    };
}
```

---

## 四、数据层（Data 模块）— 🟡 中

### 4.1 DbTable — 运行时类型反序列化

**文件：** `NewLife.Core/Data/DbTable.cs`

```csharp
// DbTable.cs:286,288,812,814 — 从字符串还原 .NET 类型
ts[i] = Type.GetType("System." + tc) ?? typeof(Object);
ts[i] = Type.GetType(binary.Read<String>() + "") ?? typeof(Object);
```

从持久化数据中读取类型名称并动态解析，在 AOT 下字符串对应的类型可能已被剪裁。

**修复建议：**
- 对系统类型（`System.*`）可改用 `TypeCode` 枚举映射，无需 `Type.GetType`
- 自定义类型反序列化添加 `[RequiresUnreferencedCode]` 注解

---

### 4.2 BinaryTree — 表达式编译

**文件：** `NewLife.Core/Data/BinaryTree.cs`

```csharp
// BinaryTree.cs:136 — 通过字符串查找方法，反射，AOT 风险
ops.Add(left => Expression.Call(typeof(Math).GetMethod("Sqrt"), left));

// BinaryTree.cs:141 — 同上
ops.Add(left => Expression.Call(
    typeof(BinaryTree).GetMethod(nameof(Cbrt), 
        BindingFlags.NonPublic | BindingFlags.Static), left));

// BinaryTree.cs:190 — 表达式编译
var compiled = Expression.Lambda<Func<Double>>(exp).Compile();
```

`typeof(Math).GetMethod("Sqrt")` 虽然在 AOT 下方法通常存在，但结合 `Expression.Compile()` 的使用，整个数学表达式解析器在 AOT 下存在风险。

**修复建议：**
- 使用 `nameof` + 强类型委托代替字符串查找（`typeof(Math).GetMethod("Sqrt")` → 直接引用 `Math.Sqrt`）
- `Expression.Compile()` 替换为预先静态编译的逻辑（switch 分发）

---

## 五、缓存系统（Caching 模块）— 🟡 中

### 5.1 MemoryCache — 类型名称解析

**文件：** `NewLife.Core/Caching/MemoryCache.cs`

```csharp
// MemoryCache.cs:914 — 从字符串名称获取类型
var type = Type.GetType("System." + code);
```

用于从序列化数据恢复缓存值的类型信息，AOT 下可能找不到类型。

**修复建议：** 改用 `TypeCode` 枚举映射代替字符串类型查找。

---

### 5.2 Redis — GetType().CreateInstance() 自克隆 — 🟡 中

**文件：** `NewLife.Core/Caching/Redis.cs`

```csharp
// Redis.cs:540 — 通过运行时类型反射克隆自身，用于创建子库实例
public virtual Redis CreateSub(Int32 db)
{
    var rds = GetType().CreateInstance() as Redis; // 🟡
    rds.Server = Server;
    rds.Db = db;
    // ... 复制属性
    return rds;
}
```

`GetType().CreateInstance()` 调用的是扩展方法 `Activator.CreateInstance(type, true)`，获取的是**运行时的实际派生类型**（如 `FullRedis`），AOT 下派生类型的无参构造函数可能因 trimming 被移除。

**修复建议：**
- 将 `CreateSub` 方法改为 `virtual`，要求派生类 override 并手动 `new` 自身，避免反射克隆
- 或使用 `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors)]` 注解 `Redis` 类，保留所有子类无参构造

---

## 六、公共模块（Common）— 🟢 低

### 6.1 Runtime — 运行时环境检测

**文件：** `NewLife.Core/Common/Runtime.cs`

```csharp
// Runtime.cs:21 — 检测 Mono 运行时
Mono = Type.GetType("Mono.Runtime") != null;

// Runtime.cs:26 — 检测 Unity 运行时
Unity = Type.GetType("UnityEngine.Application, UnityEngine") != null;

// Runtime.cs:91 — 检测 ASP.NET Core
var asm = AppDomain.CurrentDomain.GetAssemblies()
    .FirstOrDefault(e => e.GetName().Name == "Microsoft.AspNetCore");
```

运行时环境检测代码在 AOT 下行为不一致（Mono 和 Unity 场景下 AOT 检测有意义，但 `Type.GetType` 可能返回 null）。AOT 目标通常不是 Mono/Unity，此处风险较低，但应明确处理。

**修复建议：**
- 可使用条件编译 `#if UNITY_*` 替代运行时检测
- `AppDomain.CurrentDomain.GetAssemblies()` 扫描可改为检测 `Type.GetType("Microsoft.AspNetCore.Hosting.IWebHostEnvironment")`（仍有风险但更安全）

---

## 七、Web 与插件模块 — 🔴 严重

### 7.1 PluginHelper — 动态程序集加载

**文件：** `NewLife.Core/Web/PluginHelper.cs`

```csharp
// PluginHelper.cs:51 — 从文件动态加载程序集
var asm = Assembly.LoadFrom(file); // 🔴

// PluginHelper.cs:24,68 — 通过字符串查找类型
var type = Type.GetType(typeName); // 🔴
```

`Assembly.LoadFrom` 在 NativeAOT 下完全不支持，动态程序集加载是 AOT 的根本限制。

**修复建议：**
- 整个 `PluginHelper` 类标记 `[RequiresDynamicCode]`
- AOT 场景应使用静态注册机制代替插件文件加载

---

### 7.2 OAuthClient — 动态子类扫描

**文件：** `NewLife.Core/Web/OAuthClient.cs`

```csharp
// OAuthClient.cs:96 — 扫描所有程序集中的 OAuthClient 子类
foreach (var item in typeof(OAuthClient).GetAllSubclasses(true))
```

AOT 剪裁会移除未被直接引用的子类，动态扫描结果不完整。

**修复建议：**
- 提供静态注册 API：`OAuthClient.Register<GitHubAuthClient>()`
- 或使用源生成器在编译时生成子类注册列表

---

## 八、HTTP 处理器（Http 模块）— 🟠 高

### 8.1 IHttpHandler — Delegate.DynamicInvoke

**文件：** `NewLife.Core/Http/IHttpHandler.cs`

```csharp
// IHttpHandler.cs:52,69 — DynamicInvoke 在 AOT 下不可用
if (pis.Length == 0) return handler.DynamicInvoke(); // 🔴
return handler.DynamicInvoke(args); // 🔴

// IHttpHandler.cs:86 — 运行时反射访问 Task<T>.Result
var resultProperty = taskType.GetProperty("Result", BindingFlags.Public | BindingFlags.Instance);
```

`Delegate.DynamicInvoke` 在 AOT 下不支持（依赖 DLR），会抛出 `PlatformNotSupportedException`。

**修复建议：**
- 改用强类型委托调用代替 `DynamicInvoke`
- `Task<T>.Result` 访问改用 `((Task<T>)task).Result` 强类型转换，或使用 `await`

---

### 8.2 ControllerHandler — 运行时方法查找与调用

**文件：** `NewLife.Core/Http/ControllerHandler.cs`

```csharp
// ControllerHandler.cs:42 — 通过名称字符串查找方法
method = type.GetMethod(methodName, BindingFlags.Public | BindingFlags.NonPublic | ...);

// ControllerHandler.cs:47 — 通过反射调用方法
var result = controller.InvokeWithParams(method, context.Parameters as IDictionary);
```

HTTP 路由到控制器方法的动态分发机制完全基于反射，AOT 下方法可能被剪裁。

**修复建议：**
- 改用源生成器预生成路由分发代码
- 或要求控制器方法通过委托显式注册

---

## 九、扩展模块 — 🟡 中

### 9.1 SpeakProvider / SpeechRecognition — 动态程序集加载

**文件：** `NewLife.Core/Extension/SpeakProvider.cs`，`Windows/SpeechRecognition.cs`

```csharp
// SpeakProvider.cs:26,32 — 动态加载语音程序集
asm ??= Assembly.Load("System.Speech, Version=4.0.0.0, ...");
asm ??= Assembly.Load("System.Speech");

// SpeakProvider.cs:17 — 字符串类型解析
_type = Type.GetType(typeName);
```

已在 `#if __WIN__` 或环境判断内，但仍是 AOT 不支持的模式。

**修复建议：** 整体标记 `[RequiresDynamicCode]`，AOT 场景跳过语音功能。

---

### 9.3 配置提供者工厂 — 动态 CreateInstance — 🟡 中

**文件：** `NewLife.Core/Configuration/IConfigProvider.cs`

```csharp
// IConfigProvider.cs:393 — 从注册类型字典动态创建配置提供者实例
public static IConfigProvider? Create(String? name)
{
    // ...
    if (!_providers.TryGetValue(ext, out var type)) throw new Exception(...);

    var config = type.CreateInstance() as IConfigProvider; // 🟡
    return config;
}
```

`_providers` 字典存储的是 `Type` 对象（通过 `Register<TProvider>(name)` 在静态构造中注册）。`type.CreateInstance()` 等同于 `Activator.CreateInstance(type, true)`，AOT 下这些 `Type` 对象本身可以保留，但无参构造函数若未被静态引用则可能已被 trimming 剪裁。

**内置注册的实现类型：**
- `IniConfigProvider`、`XmlConfigProvider`、`JsonConfigProvider`、`HttpConfigProvider`

这些类型在静态构造中已经通过 `Register<T>` 保留了类型信息，但 AOT trimmer 并不能从 `ConcurrentDictionary<string, Type>` 中推断 trimming 边界。

**修复建议：**
- 添加 `[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicParameterlessConstructor)]` 到 `_providers` 字典的值类型
- 或改用工厂委托 `ConcurrentDictionary<String, Func<IConfigProvider>>`，彻底消除反射

```csharp
// AOT 安全的工厂方式
Register("xml", () => new XmlConfigProvider());
Register("json", () => new JsonConfigProvider());
```

**文件：** `NewLife.Core/Extension/EnumHelper.cs`

```csharp
// EnumHelper.cs:56 — 通过字段名字符串查找字段
var field = type.GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);

// EnumHelper.cs:61 — 读取 DescriptionAttribute
var description = field.GetCustomAttribute<DescriptionAttribute>(false);
```

枚举字段反射在 AOT 下通常比较安全（枚举字段名和特性不会被剪裁），但 `GetCustomAttribute<T>()` 在极端剪裁配置下可能丢失特性数据。

**修复建议：** 添加 `[RequiresUnreferencedCode]` 注解以告知调用方在剪裁时需要保留枚举元数据。

---

## 十、P/Invoke（DllImport）模块 — 🟢 低

**全量扫描确认的文件（共 10 个，含 DllImport 数量）：**

| 文件 | DllImport 数量 |
|------|---------------|
| `Common/Runtime.cs` | 2 |
| `Common/MachineInfo.cs` | 2 |
| `Extension/ProcessHelper.cs` | 5 |
| `Log/CodeTimer.cs` | 3 |
| `Net/NetHelper.cs` | 1 |
| `Net/TcpConnectionInformation2.cs` | 1 |
| `Security/Certificate.cs` | 13 |
| `Windows/ConsoleHelper.cs` | 6 |
| `Windows/ControlHelper.cs` | 1 |
| `Windows/PowerStatus.cs` | 1 |

`Log/CodeTimer.cs` 用于精确统计线程 CPU 周期数和线程时间，调用 Win32 API：

```csharp
// CodeTimer.cs:95
[DllImport("kernel32.dll")]
[return: MarshalAs(UnmanagedType.Bool)]
static extern Boolean QueryThreadCycleTime(IntPtr threadHandle, ref UInt64 cycleTime);

// CodeTimer.cs:99
[DllImport("kernel32.dll")]
static extern IntPtr GetCurrentThread();

// CodeTimer.cs:102
[DllImport("kernel32.dll", SetLastError = true)]
static extern Boolean GetThreadTimes(IntPtr hThread, out Int64 lpCreationTime, ...);
```

P/Invoke（`[DllImport]`）本身支持 AOT，但建议迁移到更 AOT 友好的 `[LibraryImport]`（.NET 7+）。

```csharp
// 现有写法（可用但次优）
[DllImport("kernel32.dll")]
static extern IntPtr GetCurrentProcess();

// 推荐写法（AOT 更友好，源生成器处理 marshaling）
[LibraryImport("kernel32.dll")]
static partial IntPtr GetCurrentProcess();
```

主要风险在于 `Marshal.PtrToStructure<T>` 的使用：
```csharp
// ProcessHelper.cs:169 — 泛型版已支持 AOT，低风险
val = (T)Marshal.PtrToStructure(ptr, typeof(T))!;

// 推荐改用泛型重载
val = Marshal.PtrToStructure<T>(ptr);
```

**修复建议：** 低优先级，可在专项 P/Invoke 重构中批量处理。

---

## 十一、ObjectContainer（IoC 容器）— 🟠 高

**文件：** `NewLife.Core/Model/ObjectContainer.cs`

```csharp
// ObjectContainer.cs:212 — 扫描所有构造函数
var constructors = type.GetConstructors();

// ObjectContainer.cs:242 — 动态调用构造函数
return constructorInfo.Invoke(pv);
```

整个 IoC 容器的实例创建机制依赖运行时构造函数扫描和动态调用。这是 AOT 的核心挑战之一，与 `Microsoft.Extensions.DependencyInjection` 在 .NET 8+ 中已添加 AOT 支持的做法相同。

**修复建议：**
- 参考 `Microsoft.Extensions.DependencyInjection.ActivatorUtilities` 的 AOT 源生成器方案
- 要求注册类型时提供工厂委托，避免运行时构造函数扫描
- 提供 `[ServiceRegistration]` 源生成器自动生成工厂代码

---

## 十二、汇总与优先级

### 按模块分类

| 模块 | 严重级别 | 主要问题 | 可否简单修复 |
|------|----------|----------|-------------|
| `ScriptEngine` | 🔴 严重 | CSharpCodeProvider + Emit | 否，需标记不支持 AOT |
| `DynamicXml`/`DynamicInternal` | 🔴 严重 | DynamicObject + InvokeMember | 否，需重构或标记 |
| `PluginHelper` | 🔴 严重 | Assembly.LoadFrom | 否，需重新设计插件机制 |
| `Delegate.DynamicInvoke` (IHttpHandler) | 🔴 严重 | DynamicInvoke | 是，改为强类型调用 |
| `BinaryFormatter` | 🔴 严重 | 已废弃 API | 是，替换为其他序列化 |
| `ObjectContainer` | 🟠 高 | 构造函数动态扫描+调用 | 中等，需引入工厂模式 |
| `GetAllSubclasses` | 🟠 高 | 程序集枚举 | 中等，需手动注册机制 |
| `ControllerHandler` | 🟠 高 | 运行时方法查找+调用 | 中等，需源生成器支持 |
| `SpanSerializer` | 🟠 高 | Expression.Compile() | 部分，可降级为直接反射 |
| `BinaryTree` | 🟠 高 | Expression.Compile() | 是，改用 switch 分发 |
| `XmlSerializer` (动态) | 🟠 高 | 运行时代码生成 | 中等，需源生成器预生成 |
| `XmlHelper` 方法名字符串查找 | 🟠 高 | GetMethodEx("To"+type.Name) | 是，改用 switch(TypeCode) 分支 |
| `ConfigProvider.Create()` 工厂 | 🟡 中 | type.CreateInstance() 动态创建 | 是，改用工厂委托 |
| `Redis.CreateSub` 自克隆 | 🟡 中 | GetType().CreateInstance() | 是，override 或加 DynamicallyAccessedMembers |
| `FastJson` | 🟡 中 | 全反射序列化 | 需整体添加 AOT 注解 |
| `DefaultJsonTypeInfoResolver` | 🟡 中 | 反射元数据扫描 | 是，改用 JsonSerializerContext |
| `DbTable` 类型解析 | 🟡 中 | Type.GetType() | 是，改用 TypeCode 映射 |
| `MakeGenericType` | 🟡 中 | 运行时泛型构造 | 部分，需注解+类型约束 |
| `SpeakProvider` | 🟡 中 | Assembly.Load | 是，标记不支持 AOT |
| `Type.GetType()` 系列 | 🟡 中 | 字符串类型查找 | 是，添加注解 |
| `EnumHelper` | 🟢 低 | GetCustomAttribute | 是，添加注解 |
| `DllImport` P/Invoke（10个文件含 CodeTimer） | 🟢 低 | 建议迁移 LibraryImport | 是，可批量处理 |
| `OAuthClient` 子类扫描 | 🟢 低 | GetAllSubclasses | 是，改为手动注册 |

---

### AOT 适配路线图建议

#### 第一阶段：注解标记（低成本，减少警告）

为以下 API 添加 `[RequiresUnreferencedCode]` 和 `[RequiresDynamicCode]` 注解：
- `ScriptEngine` 整个类
- `DynamicXml`、`DynamicInternal` 整个类
- `PluginHelper` 整个类
- `SpeakProvider` 相关方法
- `GetAllSubclasses`、`GetSubclasses`、`FindPlugins`
- `FastJson.Read`、`FastJson.Write`（带 Type 参数
- `ObjectContainer.CreateInstance`

#### 第二阶段：替换高风险 API（中等成本）

- `Delegate.DynamicInvoke` → 强类型委托调用
- `BinaryFormatter` → 移除或替换
- `Type.GetType()` → TypeCode 枚举映射（对系统类型）
- `BinaryTree.Expression.Compile()` → switch 分发静态逻辑

#### 第三阶段：架构性重构（高成本，长期方向）

- `ObjectContainer` 引入源生成器工厂
- `ControllerHandler` 引入源生成器路由分发
- `SystemJson` 配套 `JsonSerializerContext` 源生成器  
- 插件系统改为静态注册机制
- OAuthClient 子类改为显式注册

---

## 附录：AOT 关键 API 参考

| 需要迁移的 API | AOT 友好替代方案 |
|----------------|----------------|
| `Type.GetType(string)` | 预注册类型字典 / 源生成器 |
| `Assembly.Load(string/byte[])` | 静态引用 / 不支持 |
| `Assembly.GetTypes()` | 源生成器枚举已知类型 |
| `Activator.CreateInstance(Type)` | 工厂委托 / `ActivatorUtilities` |
| `ConstructorInfo.Invoke(...)` | 工厂委托 |
| `MethodInfo.Invoke(...)` | 强类型委托 `Delegate.CreateDelegate` |
| `Delegate.DynamicInvoke(...)` | 强类型委托调用 |
| `Expression.Compile()` | 预编译静态委托 / 解释执行 |
| `MakeGenericType` (动态类型) | 源生成器 / 预注册有限组合 |
| `DynamicObject` | 强类型包装器 |
| `BinaryFormatter` | `System.Text.Json` / 自定义 |
| `XmlSerializer(Type)` | `[XmlSerializerGenerator]` 源生成器 |
| `DefaultJsonTypeInfoResolver` | `JsonSerializerContext`（源生成器）|
| `[DllImport]` | `[LibraryImport]`（.NET 7+）|
| `InvokeMember` | 反射（带注解）或强类型 |
| `CodeDomProvider` | 不支持 AOT，标记 `[RequiresDynamicCode]` |

---

*本报告基于源码静态分析生成，未覆盖条件编译分支内的所有变体。建议在启用 `<PublishAot>true</PublishAot>` 和 `<IsTrimmable>true</IsTrimmable>` 的目标项目上实际编译以获取完整的 IL2026/IL3050 警告列表。*
