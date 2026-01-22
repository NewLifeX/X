# 字符串扩展 StringHelper

## 概述

`StringHelper` 是 NewLife.Core 中的字符串处理工具类，提供了丰富的字符串扩展方法，包括比较、截取、拆分、拼接、编辑距离搜索等功能，极大简化了日常开发中的字符串操作。

**命名空间**：`NewLife`  
**文档地址**：https://newlifex.com/core/string_helper

## 核心特性

- **空值安全**：所有方法都能正确处理 null 和空字符串
- **忽略大小写**：提供多种忽略大小写的比较方法
- **高效实现**：使用 `StringBuilder` 池化、`Span<T>` 等技术优化性能
- **模糊匹配**：内置 Levenshtein 编辑距离和 LCS 最长公共子序列算法

## 快速开始

```csharp
using NewLife;

// 空值判断
var isEmpty = "".IsNullOrEmpty();           // true
var isBlank = "  ".IsNullOrWhiteSpace();    // true

// 忽略大小写比较
var equal = "Hello".EqualIgnoreCase("hello");  // true

// 字符串拆分
var arr = "1,2,3".SplitAsInt();             // [1, 2, 3]
var dic = "a=1;b=2".SplitAsDictionary();    // {a:1, b:2}

// 字符串拼接
var str = new[] { 1, 2, 3 }.Join(",");      // "1,2,3"
```

## API 参考

### 空值判断

#### IsNullOrEmpty

```csharp
public static Boolean IsNullOrEmpty(this String? value)
```

判断字符串是否为 null 或空字符串。

**示例**：
```csharp
String? s1 = null;
s1.IsNullOrEmpty()               // true
"".IsNullOrEmpty()               // true
" ".IsNullOrEmpty()              // false（空格不算空）
"hello".IsNullOrEmpty()          // false
```

#### IsNullOrWhiteSpace

```csharp
public static Boolean IsNullOrWhiteSpace(this String? value)
```

判断字符串是否为 null、空字符串或仅包含空白字符。

**示例**：
```csharp
String? s1 = null;
s1.IsNullOrWhiteSpace()          // true
"".IsNullOrWhiteSpace()          // true
"   ".IsNullOrWhiteSpace()       // true
"\t\n".IsNullOrWhiteSpace()      // true
"hello".IsNullOrWhiteSpace()     // false
```

### 字符串比较

#### EqualIgnoreCase

```csharp
public static Boolean EqualIgnoreCase(this String? value, params String?[] strs)
```

忽略大小写比较字符串是否与任意一个候选字符串相等。

**示例**：
```csharp
"Hello".EqualIgnoreCase("hello")                    // true
"Hello".EqualIgnoreCase("HELLO", "World")           // true
"Hello".EqualIgnoreCase("World", "Test")            // false
```

#### StartsWithIgnoreCase

```csharp
public static Boolean StartsWithIgnoreCase(this String? value, params String?[] strs)
```

忽略大小写判断字符串是否以任意一个候选前缀开始。

**示例**：
```csharp
"HelloWorld".StartsWithIgnoreCase("hello")          // true
"HelloWorld".StartsWithIgnoreCase("HELLO", "Hi")    // true
```

#### EndsWithIgnoreCase

```csharp
public static Boolean EndsWithIgnoreCase(this String? value, params String?[] strs)
```

忽略大小写判断字符串是否以任意一个候选后缀结束。

**示例**：
```csharp
"HelloWorld".EndsWithIgnoreCase("world")            // true
"HelloWorld".EndsWithIgnoreCase("WORLD", "Test")    // true
```

### 通配符匹配

#### IsMatch

```csharp
public static Boolean IsMatch(this String pattern, String input, StringComparison comparisonType = StringComparison.CurrentCulture)
```

使用通配符模式匹配字符串，支持 `*`（匹配任意长度）和 `?`（匹配单个字符）。

**特点**：
- 比正则表达式更简单、更高效
- 时间复杂度 O(n) ~ O(n*m)
- 无需构造正则对象

**示例**：
```csharp
"*.txt".IsMatch("document.txt")                     // true
"*.txt".IsMatch("document.doc")                     // false
"file?.txt".IsMatch("file1.txt")                    // true
"file?.txt".IsMatch("file12.txt")                   // false
"*".IsMatch("anything")                             // true（匹配所有）
"test*end".IsMatch("test123end")                    // true
```

### 字符串拆分

#### Split（扩展重载）

```csharp
public static String[] Split(this String? value, params String[] separators)
```

按指定分隔符拆分字符串，自动过滤空条目。

**示例**：
```csharp
"a,b,,c".Split(",")              // ["a", "b", "c"]（自动过滤空项）
"a;b,c".Split(",", ";")          // ["a", "b", "c"]
```

#### SplitAsInt

```csharp
public static Int32[] SplitAsInt(this String? value, params String[] separators)
```

拆分字符串并转换为整数数组，默认使用逗号和分号作为分隔符。

**特点**：
- 自动过滤空格
- 自动过滤无效数字
- 保留重复项

**示例**：
```csharp
"1,2,3".SplitAsInt()             // [1, 2, 3]
"1, 2, 3".SplitAsInt()           // [1, 2, 3]（自动去除空格）
"1;2;3".SplitAsInt()             // [1, 2, 3]（支持分号）
"1,abc,3".SplitAsInt()           // [1, 3]（过滤无效项）
"1,1,2".SplitAsInt()             // [1, 1, 2]（保留重复）
```

#### SplitAsDictionary

```csharp
public static IDictionary<String, String> SplitAsDictionary(
    this String? value, 
    String nameValueSeparator = "=", 
    String separator = ";", 
    Boolean trimQuotation = false)
```

将字符串拆分为键值对字典。

**参数说明**：
- `nameValueSeparator`：键值分隔符，默认 `=`
- `separator`：条目分隔符，默认 `;`
- `trimQuotation`：是否去除值两端的引号

**示例**：
```csharp
// 基本用法
"a=1;b=2".SplitAsDictionary()
// { "a": "1", "b": "2" }

// 自定义分隔符
"a:1,b:2".SplitAsDictionary(":", ",")
// { "a": "1", "b": "2" }

// 去除引号
"name='test';value=\"123\"".SplitAsDictionary("=", ";", true)
// { "name": "test", "value": "123" }

// 无键名时使用序号
"value1;key=value2".SplitAsDictionary()
// { "[0]": "value1", "key": "value2" }
```

> **提示**：返回的字典不区分大小写（`StringComparer.OrdinalIgnoreCase`）

### 字符串拼接

#### Join

```csharp
public static String Join(this IEnumerable value, String separator = ",")
public static String Join<T>(this IEnumerable<T> value, String separator = ",", Func<T, Object?>? func = null)
```

将集合元素拼接为字符串。

**示例**：
```csharp
// 基本用法
new[] { 1, 2, 3 }.Join()         // "1,2,3"
new[] { 1, 2, 3 }.Join(";")      // "1;2;3"

// 使用转换函数
var users = new[] { new { Name = "张三" }, new { Name = "李四" } };
users.Join(",", u => u.Name)     // "张三,李四"
```

#### Separate

```csharp
public static StringBuilder Separate(this StringBuilder sb, String separator)
```

向 `StringBuilder` 追加分隔符，但会忽略开头（第一次调用不追加）。

**示例**：
```csharp
var sb = new StringBuilder();
sb.Separate(",").Append("a");    // "a"
sb.Separate(",").Append("b");    // "a,b"
sb.Separate(",").Append("c");    // "a,b,c"
```

### 字符串截取

#### Substring（扩展重载）

```csharp
public static String Substring(this String str, String? after, String? before = null, Int32 startIndex = 0, Int32[]? positions = null)
```

从字符串中截取指定标记之间的内容。

**示例**：
```csharp
// 截取标记之后的内容
"Hello[World]End".Substring("[")            // "World]End"

// 截取两个标记之间的内容
"Hello[World]End".Substring("[", "]")       // "World"

// 截取标记之前的内容
"Hello[World]End".Substring(null, "[")      // "Hello"

// 获取匹配位置
var positions = new Int32[2];
"Hello[World]End".Substring("[", "]", 0, positions);
// positions[0] = 6（内容起始位置）
// positions[1] = 11（内容结束位置）
```

#### Cut

```csharp
public static String Cut(this String str, Int32 maxLength, String? pad = null)
```

按最大长度截取字符串，可指定填充字符。

**示例**：
```csharp
"HelloWorld".Cut(8)              // "HelloWor"
"HelloWorld".Cut(8, "...")       // "Hello..."（总长度不超过8）
"Hi".Cut(8)                      // "Hi"（不足长度原样返回）
```

#### TrimStart / TrimEnd

```csharp
public static String TrimStart(this String str, params String[] starts)
public static String TrimEnd(this String str, params String[] ends)
```

从字符串开头/结尾移除指定的子字符串，不区分大小写，支持多次匹配。

**示例**：
```csharp
"HelloHelloWorld".TrimStart("Hello")     // "World"（移除所有匹配的前缀）
"WorldEndEnd".TrimEnd("End")             // "World"
```

#### CutStart / CutEnd

```csharp
public static String CutStart(this String str, params String[] starts)
public static String CutEnd(this String str, params String[] ends)
```

移除指定子字符串及其之前/之后的所有内容。

**示例**：
```csharp
"path/to/file.txt".CutStart("/")         // "file.txt"
"path/to/file.txt".CutEnd("/")           // "path/to"
```

#### EnsureStart / EnsureEnd

```csharp
public static String EnsureStart(this String? str, String start)
public static String EnsureEnd(this String? str, String end)
```

确保字符串以指定内容开始/结束。

**示例**：
```csharp
"world".EnsureStart("Hello")     // "Helloworld"
"Hello".EnsureStart("Hello")     // "Hello"（已存在则不添加）

"/api/users".EnsureEnd("/")      // "/api/users/"
"/api/users/".EnsureEnd("/")     // "/api/users/"
```

#### TrimInvisible

```csharp
public static String? TrimInvisible(this String? value)
```

移除字符串中的不可见 ASCII 控制字符（0-31 和 127）。

**示例**：
```csharp
"Hello\x00World\x1F".TrimInvisible()     // "HelloWorld"
```

### 编码转换

#### GetBytes

```csharp
public static Byte[] GetBytes(this String? value, Encoding? encoding = null)
```

将字符串转换为字节数组，默认使用 UTF-8 编码。

**示例**：
```csharp
"Hello".GetBytes()                        // UTF-8 编码的字节数组
"Hello".GetBytes(Encoding.ASCII)          // ASCII 编码
"你好".GetBytes(Encoding.UTF8)            // UTF-8 编码中文
```

### 模糊搜索

#### Levenshtein 编辑距离

```csharp
public static Int32 LevenshteinDistance(String str1, String str2)
public static String[] LevenshteinSearch(String key, String[] words)
```

计算两个字符串之间的编辑距离（插入、删除、替换操作的最少次数）。

**示例**：
```csharp
// 计算编辑距离
StringHelper.LevenshteinDistance("kitten", "sitting")  // 3

// 模糊搜索
var words = new[] { "apple", "application", "banana", "apply" };
StringHelper.LevenshteinSearch("appl", words)
// ["apple", "application", "apply"]
```

#### LCS 最长公共子序列

```csharp
public static Int32 LCSDistance(String word, String[] keys)
public static String[] LCSSearch(String key, String[] words)
public static IEnumerable<T> LCSSearch<T>(this IEnumerable<T> list, String keys, Func<T, String> keySelector, Int32 count = -1)
```

基于最长公共子序列的模糊搜索，适合用于搜索建议、自动补全等场景。

**示例**：
```csharp
var words = new[] { "HelloWorld", "HelloKitty", "GoodBye" };
StringHelper.LCSSearch("Hello", words)
// ["HelloKitty", "HelloWorld"]

// 泛型搜索
var users = new[] { 
    new { Id = 1, Name = "张三" },
    new { Id = 2, Name = "张小三" },
    new { Id = 3, Name = "李四" }
};
users.LCSSearch("张", u => u.Name, 2)
// 返回张三、张小三
```

#### Match 模糊匹配

```csharp
public static IList<KeyValuePair<T, Double>> Match<T>(this IEnumerable<T> list, String keys, Func<T, String> keySelector)
public static IEnumerable<T> Match<T>(this IEnumerable<T> list, String keys, Func<T, String> keySelector, Int32 count, Double confidence = 0.5)
```

基于命中率和跳过惩罚的模糊匹配算法。

**示例**：
```csharp
var products = new[] { "iPhone 15", "iPhone 15 Pro", "Samsung Galaxy" };
products.Match("iPhone", s => s, 2, 0.3)
// ["iPhone 15", "iPhone 15 Pro"]
```

### 文字转语音

```csharp
public static void Speak(this String value)
public static void SpeakAsync(this String value)
```

调用系统语音引擎朗读文本（仅 Windows 平台）。

**示例**：
```csharp
"你好，世界".Speak();       // 同步朗读
"你好，世界".SpeakAsync();  // 异步朗读
```

## 最佳实践

### 1. 使用空值安全的方法

```csharp
// 推荐：使用扩展方法
if (str.IsNullOrEmpty()) return;

// 不推荐：需要处理 null
if (str == null || str.Length == 0) return;
```

### 2. 解析配置字符串

```csharp
// 连接字符串解析
var connStr = "Server=localhost;Database=test;User=root;Password=123456";
var dic = connStr.SplitAsDictionary();
var server = dic["Server"];      // "localhost"
var database = dic["Database"];  // "test"
```

### 3. URL 参数解析

```csharp
var query = "name=test&age=18&tags=a,b,c";
var dic = query.SplitAsDictionary("=", "&");
var name = dic["name"];          // "test"
var tags = dic["tags"].Split(",");  // ["a", "b", "c"]
```

### 4. 使用 StringBuilder 池

```csharp
using NewLife.Collections;

// 从池中获取 StringBuilder
var sb = Pool.StringBuilder.Get();
sb.Append("Hello");
sb.Separate(",").Append("World");

// 返回字符串并归还到池
var result = sb.Return(true);    // "Hello,World"
```

## 性能说明

- `IsNullOrEmpty` 和 `IsNullOrWhiteSpace` 使用内联优化
- 字符串拆分和拼接使用 `StringBuilder` 池，减少内存分配
- 通配符匹配使用单指针回溯算法，避免正则表达式开销
- 编辑距离算法针对短字符串优化

## 相关链接

- [类型转换 Utility](utility-类型转换Utility.md)
- [数据扩展 IOHelper](io_helper-数据扩展IOHelper.md)
- [路径扩展 PathHelper](path_helper-路径扩展PathHelper.md)
