# 可销毁 DisposeBase

## 概述

`DisposeBase` 是 NewLife.Core 中实现 `IDisposable` 模式的抽象基类，提供标准的资源释放模式，有效防止内存和资源泄漏。它确保释放逻辑只执行一次，并支持销毁事件通知。

**命名空间**：`NewLife`  
**文档地址**：https://newlifex.com/core/disposebase

## 核心特性

- **标准 Dispose 模式**：正确实现 `IDisposable` 接口
- **单次释放保证**：通过原子操作确保释放逻辑只执行一次
- **终结器支持**：在忘记调用 `Dispose` 时提供兜底释放
- **销毁事件**：支持在对象销毁时触发事件通知
- **辅助方法**：提供 `ThrowIfDisposed` 和 `TryDispose` 等辅助方法

## 快速开始

```csharp
using NewLife;

// 继承 DisposeBase 实现资源管理
public class MyResource : DisposeBase
{
    private Stream _stream;
    
    public MyResource(String path)
    {
        _stream = File.OpenRead(path);
    }
    
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);
        
        if (disposing)
        {
            // 释放托管资源
            _stream?.Dispose();
            _stream = null;
        }
    }
}

// 使用 using 语句自动释放
using var resource = new MyResource("data.txt");
```

## API 参考

### IDisposable2 接口

```csharp
public interface IDisposable2 : IDisposable
{
    Boolean Disposed { get; }
    event EventHandler OnDisposed;
}
```

扩展的可销毁接口，增加了 `Disposed` 状态属性和销毁事件。

### DisposeBase 类

#### 属性

##### Disposed

```csharp
public Boolean Disposed { get; }
```

表示对象是否已被释放。

**示例**：
```csharp
var resource = new MyResource();
Console.WriteLine(resource.Disposed);  // False

resource.Dispose();
Console.WriteLine(resource.Disposed);  // True
```

#### 方法

##### Dispose

```csharp
public void Dispose()
```

释放资源。调用后会触发 `OnDisposed` 事件，并通知 GC 不再调用终结器。

##### Dispose(Boolean)

```csharp
protected virtual void Dispose(Boolean disposing)
```

实际的资源释放方法，子类重载此方法实现具体的释放逻辑。

**参数说明**：
- `disposing`：`true` 表示从 `Dispose()` 方法调用，应释放所有资源；`false` 表示从终结器调用，只应释放非托管资源

**重载示例**：
```csharp
protected override void Dispose(Boolean disposing)
{
    // 1. 首先调用基类方法
    base.Dispose(disposing);
    
    // 2. 释放托管资源（仅当从 Dispose() 调用时）
    if (disposing)
    {
        _managedResource?.Dispose();
        _managedResource = null;
    }
    
    // 3. 释放非托管资源（两种路径都执行）
    if (_handle != IntPtr.Zero)
    {
        CloseHandle(_handle);
        _handle = IntPtr.Zero;
    }
}
```

##### ThrowIfDisposed

```csharp
protected void ThrowIfDisposed()
```

在公开方法中调用，若对象已释放则抛出 `ObjectDisposedException`。

**示例**：
```csharp
public class MyResource : DisposeBase
{
    public void DoWork()
    {
        ThrowIfDisposed();  // 已释放时抛出异常
        
        // 执行实际工作...
    }
}
```

#### 事件

##### OnDisposed

```csharp
public event EventHandler? OnDisposed
```

对象被销毁时触发的事件。

**注意**：事件可能在终结器线程中触发，订阅方应避免依赖特定线程上下文。

**示例**：
```csharp
var resource = new MyResource();
resource.OnDisposed += (sender, e) =>
{
    Console.WriteLine("资源已释放");
};

resource.Dispose();  // 输出：资源已释放
```

### DisposeHelper 辅助类

#### TryDispose

```csharp
public static Object? TryDispose(this Object? obj)
```

尝试销毁对象，如果对象实现了 `IDisposable` 则调用其 `Dispose` 方法。支持集合类型，会遍历销毁所有元素。

**示例**：
```csharp
// 销毁单个对象
var stream = File.OpenRead("test.txt");
stream.TryDispose();

// 销毁集合中的所有对象
var streams = new List<Stream>
{
    File.OpenRead("a.txt"),
    File.OpenRead("b.txt"),
    File.OpenRead("c.txt")
};
streams.TryDispose();  // 销毁所有流
```

## 使用场景

### 1. 管理文件句柄

```csharp
public class FileProcessor : DisposeBase
{
    private FileStream? _fileStream;
    private StreamReader? _reader;
    
    public FileProcessor(String path)
    {
        _fileStream = File.OpenRead(path);
        _reader = new StreamReader(_fileStream);
    }
    
    public String ReadLine()
    {
        ThrowIfDisposed();
        return _reader?.ReadLine() ?? String.Empty;
    }
    
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);
        
        if (disposing)
        {
            _reader?.Dispose();
            _reader = null;
            _fileStream?.Dispose();
            _fileStream = null;
        }
    }
}
```

### 2. 管理网络连接

```csharp
public class TcpConnection : DisposeBase
{
    private Socket? _socket;
    private NetworkStream? _stream;
    
    public TcpConnection(String host, Int32 port)
    {
        _socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        _socket.Connect(host, port);
        _stream = new NetworkStream(_socket, ownsSocket: false);
    }
    
    public void Send(Byte[] data)
    {
        ThrowIfDisposed();
        _stream?.Write(data, 0, data.Length);
    }
    
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);
        
        if (disposing)
        {
            _stream?.Dispose();
            _stream = null;
            _socket?.Dispose();
            _socket = null;
        }
    }
}
```

### 3. 资源池管理

```csharp
public class ResourcePool<T> : DisposeBase where T : IDisposable
{
    private readonly ConcurrentBag<T> _pool = new();
    private readonly Func<T> _factory;
    
    public ResourcePool(Func<T> factory)
    {
        _factory = factory;
    }
    
    public T Rent()
    {
        ThrowIfDisposed();
        return _pool.TryTake(out var item) ? item : _factory();
    }
    
    public void Return(T item)
    {
        if (Disposed)
        {
            item.Dispose();
            return;
        }
        _pool.Add(item);
    }
    
    protected override void Dispose(Boolean disposing)
    {
        base.Dispose(disposing);
        
        if (disposing)
        {
            while (_pool.TryTake(out var item))
            {
                item.Dispose();
            }
        }
    }
}
```

### 4. 监听销毁事件

```csharp
public class ResourceMonitor
{
    public void Monitor(IDisposable2 resource)
    {
        resource.OnDisposed += (sender, e) =>
        {
            var name = sender?.GetType().Name;
            Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] {name} 已释放");
        };
    }
}

// 使用
var monitor = new ResourceMonitor();
var resource = new MyResource();
monitor.Monitor(resource);

resource.Dispose();  // 输出：[12:30:45] MyResource 已释放
```

## 最佳实践

### 1. 总是调用基类方法

```csharp
protected override void Dispose(Boolean disposing)
{
    // ? 首先调用基类方法
    base.Dispose(disposing);
    
    // 然后释放自己的资源
    if (disposing) { /* ... */ }
}
```

### 2. 使用 using 语句

```csharp
// ? 推荐：使用 using 语句确保释放
using var resource = new MyResource();

// ? 不推荐：手动调用 Dispose，容易遗忘
var resource = new MyResource();
try
{
    // 使用资源
}
finally
{
    resource.Dispose();
}
```

### 3. 在公开方法中检查状态

```csharp
public class MyResource : DisposeBase
{
    public void DoWork()
    {
        // ? 在方法开始时检查是否已释放
        ThrowIfDisposed();
        
        // 执行实际工作
    }
}
```

### 4. 正确处理托管和非托管资源

```csharp
protected override void Dispose(Boolean disposing)
{
    base.Dispose(disposing);
    
    // 托管资源：仅在 disposing=true 时释放
    if (disposing)
    {
        _managedObject?.Dispose();
    }
    
    // 非托管资源：两种情况都要释放
    if (_nativeHandle != IntPtr.Zero)
    {
        NativeMethods.CloseHandle(_nativeHandle);
        _nativeHandle = IntPtr.Zero;
    }
}
```

### 5. 避免在终结器中抛出异常

```csharp
// DisposeBase 已经处理了终结器中的异常
// 子类的 Dispose(Boolean) 方法也应该捕获可能的异常
protected override void Dispose(Boolean disposing)
{
    base.Dispose(disposing);
    
    try
    {
        // 释放资源
    }
    catch (Exception ex)
    {
        // 记录但不抛出
        XTrace.WriteException(ex);
    }
}
```

## Dispose 模式详解

```
调用 Dispose()
       │
       ▼
  Dispose(true)
       │
       ├──? 释放托管资源
       │
       ├──? 释放非托管资源
       │
       ├──? 触发 OnDisposed 事件
       │
       └──? GC.SuppressFinalize(this)
              │
              └──? 终结器不再被调用


终结器 ~DisposeBase()  (如果忘记调用 Dispose)
       │
       ▼
  Dispose(false)
       │
       ├──? 释放非托管资源
       │
       └──? 触发 OnDisposed 事件
```

## 相关链接

- [对象池 ObjectPool](object_pool-对象池ObjectPool.md)
- [对象容器 ObjectContainer](object_container-对象容器ObjectContainer.md)
