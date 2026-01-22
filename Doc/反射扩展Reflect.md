# 反射扩展 Reflect

## 概述

`Reflect` 是 NewLife.Core 中的高性能反射工具类，提供类型获取、方法调用、属性读写、对象拷贝等功能。支持私有成员访问、忽略大小写匹配，并通过 `IReflect` 接口支持可替换的反射实现。

**命名空间**：`NewLife.Reflection`  
**文档地址**：https://newlifex.com/core/reflect

## 核心特性

- **高性能**：默认实现基于缓存，支持切换为 Emit 高性能实现
- **易用性**：所有方法都以扩展方法形式提供
- **完整性**：支持私有成员、静态成员、继承成员的访问
- **灵活性**：支持忽略大小写的成员匹配
- **可扩展**：通过 `IReflect` 接口支持自定义实现

## 快速开始

```csharp
using NewLife.Reflection;

// 创建实例
var obj = typeof(MyClass).CreateInstance();

// 调用方法
obj.Invoke("DoWork", "param1", 123);

// 读取属性
var value = obj.GetValue("Name");

// 设置属性
obj.SetValue("Name", "NewValue");

// 对象拷贝
var target = new MyClass();
target.Copy(source);
```

## API 参考

### 类型获取

#### GetTypeEx

```csharp
public static Type? GetTypeEx(this String typeName)
```

根据类型名称获取类型，可搜索当前目录 DLL 并自动加载。

**示例**：
```csharp
// 获取系统类型
var type1 = "System.String".GetTypeEx();

// 获取带命名空间的类型
var type2 = "MyApp.Models.User".GetTypeEx();

// 获取程序集限定名类型
var type3 = "MyApp.Models.User, MyApp".GetTypeEx();
```

### 成员获取

#### GetMethodEx

```csharp
public static MethodInfo? GetMethodEx(this Type type, String name, params Type[] paramTypes)
```

获取方法，支持参数类型匹配。

**示例**：
```csharp
// 获取无参方法
var method1 = typeof(MyClass).GetMethodEx("DoWork");

// 获取带参方法
var method2 = typeof(MyClass).GetMethodEx("DoWork", typeof(String), typeof(Int32));
```

#### GetMethodsEx

```csharp
public static MethodInfo[] GetMethodsEx(this Type type, String name, Int32 paramCount = -1)
```

获取指定名称的方法集合，支持按参数个数过滤。

**示例**：
```csharp
// 获取所有名为 DoWork 的方法
var methods1 = typeof(MyClass).GetMethodsEx("DoWork");

// 获取参数个数为 2 的 DoWork 方法
var methods2 = typeof(MyClass).GetMethodsEx("DoWork", 2);
```

#### GetPropertyEx

```csharp
public static PropertyInfo? GetPropertyEx(this Type type, String name, Boolean ignoreCase = false)
```

获取属性，搜索私有、静态、基类成员。

**示例**：
```csharp
// 精确匹配
var prop1 = typeof(MyClass).GetPropertyEx("Name");

// 忽略大小写
var prop2 = typeof(MyClass).GetPropertyEx("name", true);

// 获取私有属性
var prop3 = typeof(MyClass).GetPropertyEx("_internalValue");
```

#### GetFieldEx

```csharp
public static FieldInfo? GetFieldEx(this Type type, String name, Boolean ignoreCase = false)
```

获取字段，搜索私有、静态、基类成员。

**示例**：
```csharp
var field = typeof(MyClass).GetFieldEx("_count");
```

#### GetMemberEx

```csharp
public static MemberInfo? GetMemberEx(this Type type, String name, Boolean ignoreCase = false)
```

获取成员（属性或字段），优先返回属性。

**示例**：
```csharp
var member = typeof(MyClass).GetMemberEx("Name", true);
```

#### GetFields / GetProperties

```csharp
public static IList<FieldInfo> GetFields(this Type type, Boolean baseFirst)
public static IList<PropertyInfo> GetProperties(this Type type, Boolean baseFirst)
```

获取用于序列化的字段/属性列表。

**参数说明**：
- `baseFirst`：是否基类成员优先排序

**示例**：
```csharp
// 获取所有可序列化属性，基类优先
var props = typeof(MyClass).GetProperties(baseFirst: true);

// 获取所有可序列化字段
var fields = typeof(MyClass).GetFields(baseFirst: false);
```

### 实例创建与方法调用

#### CreateInstance

```csharp
public static Object? CreateInstance(this Type type, params Object?[] parameters)
```

反射创建指定类型的实例。

**示例**：
```csharp
// 调用无参构造函数
var obj1 = typeof(MyClass).CreateInstance();

// 调用带参构造函数
var obj2 = typeof(MyClass).CreateInstance("name", 123);
```

#### Invoke

```csharp
public static Object? Invoke(this Object target, String name, params Object?[] parameters)
public static Object? Invoke(this Object? target, MethodBase method, params Object?[]? parameters)
```

反射调用方法。

**示例**：
```csharp
var obj = new MyClass();

// 调用实例方法
var result = obj.Invoke("Calculate", 10, 20);

// 调用静态方法（target 为类型）
var result2 = typeof(MyClass).Invoke("StaticMethod", "param");

// 调用私有方法
var result3 = obj.Invoke("PrivateMethod");
```

#### TryInvoke

```csharp
public static Boolean TryInvoke(this Object target, String name, out Object? value, params Object?[] parameters)
```

尝试调用方法，不存在时返回 false 而不抛出异常。

**示例**：
```csharp
if (obj.TryInvoke("MaybeExists", out var result, "param"))
{
    Console.WriteLine($"结果: {result}");
}
else
{
    Console.WriteLine("方法不存在");
}
```

#### InvokeWithParams

```csharp
public static Object? InvokeWithParams(this Object? target, MethodBase method, IDictionary? parameters)
```

使用字典参数调用方法，适合参数名匹配场景。

**示例**：
```csharp
var parameters = new Dictionary<String, Object>
{
    ["name"] = "test",
    ["count"] = 10
};
var result = obj.InvokeWithParams(method, parameters);
```

### 属性读写

#### GetValue

```csharp
public static Object? GetValue(this Object target, String name, Boolean throwOnError = true)
public static Object? GetValue(this Object? target, MemberInfo member)
```

获取属性/字段值。

**示例**：
```csharp
var obj = new MyClass { Name = "test" };

// 按名称获取
var name = obj.GetValue("Name");

// 不存在时返回 null 而不抛异常
var value = obj.GetValue("NotExists", throwOnError: false);

// 按成员获取
var prop = typeof(MyClass).GetPropertyEx("Name");
var name2 = obj.GetValue(prop);
```

#### SetValue

```csharp
public static Boolean SetValue(this Object target, String name, Object? value)
public static void SetValue(this Object target, MemberInfo member, Object? value)
```

设置属性/字段值。

**示例**：
```csharp
var obj = new MyClass();

// 按名称设置
obj.SetValue("Name", "newValue");

// 按成员设置
var prop = typeof(MyClass).GetPropertyEx("Name");
obj.SetValue(prop, "anotherValue");

// 检查是否设置成功
if (obj.SetValue("MaybeExists", "value"))
{
    Console.WriteLine("设置成功");
}
```

### 对象拷贝

#### Copy

```csharp
public static void Copy(this Object target, Object src, Boolean deep = false, params String[] excludes)
public static void Copy(this Object target, IDictionary<String, Object?> dic, Boolean deep = false)
```

从源对象或字典拷贝数据到目标对象。

**参数说明**：
- `deep`：是否深度拷贝（复制值而非引用）
- `excludes`：要排除的成员名称

**示例**：
```csharp
var source = new User { Name = "张三", Age = 25 };
var target = new UserDto();

// 浅拷贝
target.Copy(source);

// 深拷贝
target.Copy(source, deep: true);

// 排除某些字段
target.Copy(source, excludes: "Password", "Secret");

// 从字典拷贝
var dic = new Dictionary<String, Object?>
{
    ["Name"] = "李四",
    ["Age"] = 30
};
target.Copy(dic);
```

### 类型辅助

#### GetElementTypeEx

```csharp
public static Type? GetElementTypeEx(this Type type)
```

获取类型的元素类型（集合、数组等）。

**示例**：
```csharp
typeof(List<String>).GetElementTypeEx()   // typeof(String)
typeof(String[]).GetElementTypeEx()       // typeof(String)
typeof(Dictionary<String, Int32>).GetElementTypeEx()  // typeof(KeyValuePair<String, Int32>)
```

#### ChangeType

```csharp
public static Object? ChangeType(this Object? value, Type conversionType)
public static TResult? ChangeType<TResult>(this Object? value)
```

类型转换。

**示例**：
```csharp
// 泛型转换
var num = "123".ChangeType<Int32>();     // 123
var date = "2024-01-15".ChangeType<DateTime>();

// 非泛型转换
var value = "true".ChangeType(typeof(Boolean));
```

#### GetName

```csharp
public static String GetName(this Type type, Boolean isfull = false)
```

获取类型的友好名称。

**示例**：
```csharp
typeof(List<String>).GetName()        // "List<String>"
typeof(List<String>).GetName(true)    // "System.Collections.Generic.List<System.String>"
typeof(Dictionary<String, Int32>).GetName()  // "Dictionary<String, Int32>"
```

## 使用场景

### 1. ORM 实体映射

```csharp
public class EntityMapper
{
    public T Map<T>(IDataReader reader) where T : new()
    {
        var entity = new T();
        var props = typeof(T).GetProperties(baseFirst: false);
        
        foreach (var prop in props)
        {
            var ordinal = reader.GetOrdinal(prop.Name);
            if (ordinal >= 0 && !reader.IsDBNull(ordinal))
            {
                var value = reader.GetValue(ordinal);
                entity.SetValue(prop, value);
            }
        }
        
        return entity;
    }
}
```

### 2. 配置绑定

```csharp
public class ConfigBinder
{
    public void Bind(Object target, IConfiguration config)
    {
        var props = target.GetType().GetProperties(baseFirst: true);
        
        foreach (var prop in props)
        {
            var value = config[prop.Name];
            if (value != null)
            {
                var converted = value.ChangeType(prop.PropertyType);
                target.SetValue(prop, converted);
            }
        }
    }
}
```

### 3. 插件系统

```csharp
public class PluginLoader
{
    public IPlugin? LoadPlugin(String typeName)
    {
        var type = typeName.GetTypeEx();
        if (type == null) return null;
        
        return type.CreateInstance() as IPlugin;
    }
    
    public void InvokeAction(IPlugin plugin, String action, params Object[] args)
    {
        if (plugin.TryInvoke(action, out var result, args))
        {
            Console.WriteLine($"执行成功: {result}");
        }
    }
}
```

### 4. DTO 转换

```csharp
public static class DtoExtensions
{
    public static TDto ToDto<TDto>(this Object entity) where TDto : new()
    {
        var dto = new TDto();
        dto.Copy(entity);
        return dto;
    }
    
    public static void UpdateFrom(this Object entity, Object dto, params String[] excludes)
    {
        entity.Copy(dto, excludes: excludes);
    }
}

// 使用
var dto = user.ToDto<UserDto>();
user.UpdateFrom(dto, "Id", "CreateTime");
```

## 最佳实践

### 1. 使用 TryInvoke 避免异常

```csharp
// ? 推荐：使用 TryInvoke
if (obj.TryInvoke("Method", out var result))
{
    // 处理结果
}

// ? 不推荐：可能抛异常
try
{
    var result = obj.Invoke("Method");
}
catch (XException) { }
```

### 2. 缓存反射元数据

```csharp
// ? 推荐：缓存 PropertyInfo
private static readonly PropertyInfo _nameProp = typeof(User).GetPropertyEx("Name");

public String GetName(User user) => user.GetValue(_nameProp) as String;

// ? 不推荐：每次都查找
public String GetName(User user) => user.GetValue("Name") as String;
```

### 3. 使用忽略大小写匹配

```csharp
// 处理 JSON 反序列化等场景
var value = obj.GetValue("username", throwOnError: false);
if (value == null)
{
    // 尝试忽略大小写
    var member = obj.GetType().GetMemberEx("username", ignoreCase: true);
    if (member != null) value = obj.GetValue(member);
}
```

## 性能说明

- 默认 `DefaultReflect` 实现使用缓存，适合大多数场景
- 高频反射场景可切换为 `EmitReflect` 实现：
  ```csharp
  Reflect.Provider = new EmitReflect();
  ```
- `GetProperties` 和 `GetFields` 结果会被缓存
- 成员查找使用字典缓存，首次访问后性能接近直接调用

## 相关链接

- [运行时信息 Runtime](runtime-运行时信息Runtime.md)
- [脚本引擎 ScriptEngine](script_engine-脚本引擎ScriptEngine.md)
- [对象容器 ObjectContainer](object_container-对象容器ObjectContainer.md)
