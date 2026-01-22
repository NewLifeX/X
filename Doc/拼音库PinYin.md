# 拼音库 PinYin

## 概述

`PinYin` 是 NewLife.Core 中的汉字拼音转换工具类，提供高效的汉字转拼音功能。支持 GB2312 一级和二级汉字，能够获取汉字的全拼或拼音首字母，适用于搜索、排序、输入法等场景。

**命名空间**：`NewLife.Common`  
**文档地址**：https://newlifex.com/core/pinyin

## 核心特性

- **高性能**：基于 GB2312 编码和区位码算法，无需加载大型字典文件
- **全汉字支持**：覆盖 GB2312 一级汉字（3755个）和二级汉字（3008个）
- **多种输出**：支持全拼、首字母、单字拼音等多种形式
- **轻量级**：无外部依赖，纯算法实现
- **特殊处理**：支持"重庆"等多音字的常见读音

## 快速开始

```csharp
using NewLife.Common;

// 获取单字拼音
var py = PinYin.Get('中');           // "Zhong"

// 获取字符串全拼
var fullPy = PinYin.Get("新生命");    // "XinShengMing"

// 获取拼音首字母
var first = PinYin.GetFirst("新生命"); // "XSM"

// 获取拼音数组
var arr = PinYin.GetAll("你好");      // ["Ni", "Hao"]
```

## API 参考

### Get（单字拼音）

```csharp
public static String Get(Char ch)
```

获取单个汉字的拼音。

**参数说明**：
- `ch`：要转换的字符

**返回值**：
- 汉字返回首字母大写的拼音（如 "Zhong"）
- 拉丁字符、标点、非中文字符原样返回
- 无法识别的汉字返回空字符串

**示例**：
```csharp
PinYin.Get('中')     // "Zhong"
PinYin.Get('国')     // "Guo"
PinYin.Get('A')      // "A"
PinYin.Get('，')     // "，"
PinYin.Get('①')     // "①"
```

### Get（字符串全拼）

```csharp
public static String Get(String str)
```

获取字符串的完整拼音，各字拼音直接连接。

**示例**：
```csharp
PinYin.Get("中国")           // "ZhongGuo"
PinYin.Get("新生命团队")     // "XinShengMingTuanDui"
PinYin.Get("Hello世界")      // "HelloShiJie"
```

### GetAll

```csharp
public static String[] GetAll(String str)
```

获取字符串中每个字符的拼音，返回字符串数组。

**特殊处理**：
- "重庆" 特殊处理为 ["Chong", "Qing"]

**示例**：
```csharp
PinYin.GetAll("你好")        // ["Ni", "Hao"]
PinYin.GetAll("重庆")        // ["Chong", "Qing"]
PinYin.GetAll("ABC")         // ["A", "B", "C"]
PinYin.GetAll("Hello")       // ["H", "e", "l", "l", "o"]
```

### GetFirst（拼音首字母）

```csharp
public static Char GetFirst(Char ch)
public static String GetFirst(String str)
```

获取汉字或字符串的拼音首字母。

**示例**：
```csharp
// 单字首字母
PinYin.GetFirst('中')        // 'Z'
PinYin.GetFirst('国')        // 'G'
PinYin.GetFirst('A')         // 'A'

// 字符串首字母
PinYin.GetFirst("新生命")    // "XSM"
PinYin.GetFirst("中国")      // "ZG"
PinYin.GetFirst("Hello")     // "Hello"
```

## 使用场景

### 1. 搜索匹配

```csharp
public class UserService
{
    /// <summary>根据拼音首字母搜索用户</summary>
    public List<User> SearchByPinyin(List<User> users, String keyword)
    {
        var upperKeyword = keyword.ToUpper();
        
        return users.Where(u =>
        {
            // 全名匹配
            if (u.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase))
                return true;
            
            // 拼音首字母匹配
            var firstLetters = PinYin.GetFirst(u.Name);
            return firstLetters.Contains(upperKeyword, StringComparison.OrdinalIgnoreCase);
            
        }).ToList();
    }
}

// 使用示例
var users = new List<User>
{
    new User { Name = "张三" },
    new User { Name = "李四" },
    new User { Name = "王五" }
};

var result = service.SearchByPinyin(users, "ZS");  // 找到 "张三"
var result2 = service.SearchByPinyin(users, "LS"); // 找到 "李四"
```

### 2. 按拼音排序

```csharp
public class ProductSorter
{
    /// <summary>按拼音排序商品名称</summary>
    public List<Product> SortByPinyin(List<Product> products)
    {
        return products
            .OrderBy(p => PinYin.Get(p.Name))
            .ToList();
    }
}

// 使用示例
var products = new List<Product>
{
    new Product { Name = "苹果" },
    new Product { Name = "香蕉" },
    new Product { Name = "橙子" }
};

var sorted = sorter.SortByPinyin(products);
// 排序结果：橙子(ChengZi) -> 苹果(PingGuo) -> 香蕉(XiangJiao)
```

### 3. 生成拼音索引

```csharp
public class ContactIndexer
{
    /// <summary>生成联系人拼音索引</summary>
    public Dictionary<Char, List<Contact>> BuildIndex(List<Contact> contacts)
    {
        var index = new Dictionary<Char, List<Contact>>();
        
        foreach (var contact in contacts)
        {
            var firstLetter = PinYin.GetFirst(contact.Name[0]);
            
            if (!index.ContainsKey(firstLetter))
                index[firstLetter] = new List<Contact>();
            
            index[firstLetter].Add(contact);
        }
        
        return index;
    }
}

// 使用示例
var contacts = new List<Contact>
{
    new Contact { Name = "张三" },
    new Contact { Name = "赵四" },
    new Contact { Name = "李五" }
};

var index = indexer.BuildIndex(contacts);
// index['Z'] = [张三, 赵四]
// index['L'] = [李五]
```

### 4. 输入法提示

```csharp
public class InputSuggestion
{
    private readonly List<String> _words;
    
    public InputSuggestion(List<String> words)
    {
        _words = words;
    }
    
    /// <summary>根据输入获取建议词</summary>
    public List<String> GetSuggestions(String input)
    {
        if (input.IsNullOrEmpty()) return new List<String>();
        
        var upperInput = input.ToUpper();
        
        return _words
            .Where(w =>
            {
                // 支持首字母匹配
                var first = PinYin.GetFirst(w);
                if (first.StartsWith(upperInput, StringComparison.OrdinalIgnoreCase))
                    return true;
                
                // 支持全拼匹配
                var full = PinYin.Get(w);
                return full.StartsWith(upperInput, StringComparison.OrdinalIgnoreCase);
            })
            .Take(10)
            .ToList();
    }
}

// 使用示例
var words = new List<String> { "新生命", "新年好", "新手入门", "生日快乐" };
var suggester = new InputSuggestion(words);

suggester.GetSuggestions("XS");   // ["新生命", "新手入门"]
suggester.GetSuggestions("Xin");  // ["新生命", "新年好", "新手入门"]
```

### 5. 数据库存储优化

```csharp
public class User
{
    public Int32 Id { get; set; }
    public String Name { get; set; }
    
    /// <summary>姓名拼音（用于搜索）</summary>
    public String NamePinyin { get; set; }
    
    /// <summary>姓名首字母（用于索引）</summary>
    public String NameFirst { get; set; }
    
    /// <summary>保存前自动生成拼音字段</summary>
    public void BeforeSave()
    {
        NamePinyin = PinYin.Get(Name);
        NameFirst = PinYin.GetFirst(Name);
    }
}

// 数据库查询时可利用拼音字段加速搜索
// SELECT * FROM Users WHERE NameFirst LIKE 'ZS%'
// SELECT * FROM Users WHERE NamePinyin LIKE 'Zhang%'
```

## 技术细节

### 编码基础

PinYin 类基于 GB2312 编码实现：

1. **一级汉字**：共 3755 个，按拼音顺序排列
2. **二级汉字**：共 3008 个，按部首笔画顺序排列
3. **特殊汉字**：超出 GB2312 范围的常用汉字单独处理

### 算法原理

```
字符 → GB2312编码 → 区位码 → 拼音映射
```

1. 将汉字转换为 GB2312 字节
2. 计算区位码（两字节相乘再减偏移）
3. 一级汉字通过区间映射快速定位拼音
4. 二级汉字通过数组查找获取拼音

### 性能特点

- **无字典加载**：算法直接计算，启动即可用
- **O(1) 复杂度**：一级汉字通过分块算法快速定位
- **内存占用小**：仅存储拼音对照数组

## 限制说明

### 多音字

目前不支持多音字上下文判断，多音字取常用读音：

```csharp
PinYin.Get('重')     // "Zhong"（而非 "Chong"）
PinYin.Get("重庆")   // "ChongQing"（特殊处理）
PinYin.Get("重要")   // "ZhongYao"（按常用读音）
```

### 生僻字

超出 GB2312 范围的生僻字可能无法转换：

```csharp
PinYin.Get('?')     // "?"（无法识别，原样返回）
```

### 繁体字

不支持繁体字转换，需要先转简体：

```csharp
PinYin.Get('國')     // "國"（无法识别）
PinYin.Get('国')     // "Guo"（简体正常）
```

## 最佳实践

### 1. 预处理拼音字段

```csharp
// 推荐：入库时预先计算拼音
user.NamePinyin = PinYin.Get(user.Name);
user.NameFirst = PinYin.GetFirst(user.Name);
db.Save(user);

// 不推荐：每次查询时实时计算
var users = db.Users.Where(u => PinYin.GetFirst(u.Name) == "ZS");
```

### 2. 组合搜索策略

```csharp
// 推荐：同时支持原文和拼音搜索
public List<User> Search(String keyword)
{
    return users.Where(u =>
        u.Name.Contains(keyword) ||
        u.NamePinyin.Contains(keyword.ToUpper()) ||
        u.NameFirst.Contains(keyword.ToUpper())
    ).ToList();
}
```

### 3. 缓存常用转换

```csharp
// 对于高频转换的词汇，可考虑缓存
private static readonly ConcurrentDictionary<String, String> _cache = new();

public static String GetCached(String str)
{
    return _cache.GetOrAdd(str, s => PinYin.Get(s));
}
```

## 相关链接

- [字符串扩展 StringHelper](string_helper-字符串扩展StringHelper.md)
- [类型转换 Utility](utility-类型转换Utility.md)
