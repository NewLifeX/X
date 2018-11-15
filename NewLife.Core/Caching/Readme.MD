缓存架构以ICache接口为核心，包括MemoryCache、Redis和DbCache实现！  
后续例程与使用说明均以Redis为例，各缓存实现类似。  

### 内存缓存 MemoryCache
MemoryCache核心是并行字典ConcurrentDictionary，由于省去了序列化和网络通信，使得它具有千万级超高性能。  
MemoryCache支持过期时间，默认容量10万个，未过期key超过该值后，每60秒根据LRU清理溢出部分。  
常用于进程内千万级以下数据缓存场景。  

```csharp
// 缓存默认实现Cache.Default是MemoryCache，可修改
//var ic = Cache.Default;
//var ic = new MemoryCache();
```

### 基础 Redis
Redis实现标准协议以及基础字符串操作，完整实现由独立开源项目[NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)提供。  
采取连接池加同步阻塞架构，具有超低延迟（200~600us）以及超高吞吐量的特点。  
在物流行业大数据实时计算中广泛应有，经过日均100亿次调用量验证。  

```csharp
// 实例化Redis，默认端口6379可以省略，密码有两种写法
//var ic = Redis.Create("127.0.0.1", 7);
var ic = Redis.Create("pass@127.0.0.1:6379", 7);
//var ic = Redis.Create("server=127.0.0.1:6379;password=pass", 7);
ic.Log = XTrace.Log; // 调试日志。正式使用时注释
```

### 数据库 DbCache
DbCache属于实验性质，采用数据库存储数据，默认SQLite。  

### 基本操作
在基本操作之前，我们先做一些准备工作：
+ 新建控制台项目，并在入口函数开头加上 `XTrace.UseConsole();` ，这是为了方便查看调试日志  
+ 具体测试代码之前，需要加上前面MemoryCache或Redis的实例化代码   
+ 准备一个模型类User  
```csharp
class User
{
    public String Name { get; set; }
    public DateTime CreateTime { get; set; }
}
```

添删改查：  
```csharp
var user = new User { Name = "NewLife", CreateTime = DateTime.Now };
ic.Set("user", user, 3600);
var user2 = ic.Get<User>("user");
XTrace.WriteLine("Json: {0}", ic.Get<String>("user"));
if (ic.ContainsKey("user")) XTrace.WriteLine("存在！");
ic.Remove("user");
```

执行结果：  
```csharp
14:14:25.990  1 N - SELECT 7
14:14:25.992  1 N - => OK
14:14:26.008  1 N - SETEX user 3600 [53]
14:14:26.021  1 N - => OK
14:14:26.042  1 N - GET user
14:14:26.048  1 N - => [53]
14:14:26.064  1 N - GET user
14:14:26.065  1 N - => [53]
14:14:26.066  1 N - Json: {"Name":"NewLife","CreateTime":"2018-09-25 14:14:25"}
14:14:26.067  1 N - EXISTS user
14:14:26.068  1 N - => 1
14:14:26.068  1 N - 存在！
14:14:26.069  1 N - DEL user
14:14:26.070  1 N - => 1
```

保存复杂对象时，默认采用Json序列化，所以上面可以按字符串把结果取回来，发现正是Json字符串。  
Redis的strings，实质上就是带有长度前缀的二进制数据，[53]表示一段53字节长度的二进制数据。

### 集合操作
GetAll/SetAll 在Redis上是很常用的批量操作，同时获取或设置多个key，一般有10倍以上吞吐量。   

批量操作：
```csharp
var dic = new Dictionary<String, Object>
{
    ["name"] = "NewLife",
    ["time"] = DateTime.Now,
    ["count"] = 1234
};
ic.SetAll(dic, 120);

var vs = ic.GetAll<String>(dic.Keys);
XTrace.WriteLine(vs.Join(",", e => $"{e.Key}={e.Value}"));
```

执行结果：
```csharp
MSET name NewLife time 2018-09-25 15:56:26 count 1234
=> OK
EXPIRE name 120
EXPIRE time 120
EXPIRE count 120
MGET name time count
name=NewLife,time=2018-09-25 15:56:26,count=1234
```

集合操作里面还有 `GetList/GetDictionary/GetQueue/GetSet` 四个类型集合，分别代表Redis的列表、哈希、队列、Set集合等。  
基础版Redis不支持这四个集合，完整版[NewLife.Redis](https://github.com/NewLifeX/NewLife.Redis)支持，MemoryCache则直接支持。  

### 高级操作
+ Add 添加，当key不存在时添加，已存在时返回false。  
+ Replace 替换，替换已有值为新值，返回旧值。  
+ Increment 累加，原子操作  
+ Decrement 递减，原子操作  

高级操作：  
```csharp
var flag = ic.Add("count", 5678);
XTrace.WriteLine(flag ? "Add成功" : "Add失败");
var ori = ic.Replace("count", 777);
var count = ic.Get<Int32>("count");
XTrace.WriteLine("count由{0}替换为{1}", ori, count);

ic.Increment("count", 11);
var count2 = ic.Decrement("count", 10);
XTrace.WriteLine("count={0}", count2);
```

执行结果：  
```csharp
SETNX count 5678
=> 0
Add失败
GETSET count 777
=> 1234
GET count
=> 777
count由1234替换为777
INCRBY count 11
=> 788
DECRBY count 10
=> 778
count=778
```

### 性能测试
Bench 会分根据线程数分多组进行添删改压力测试。    
rand 参数，是否随机产生key/value。  
batch 批大小，分批执行读写操作，借助GetAll/SetAll进行优化。  

Redis默认设置AutoPipeline=100，无分批时打开管道操作，对添删改优化。  