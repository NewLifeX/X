# 经纬度哈希GeoHash

## 概述

`GeoHash` 是 NewLife.Core 提供的地理位置编码工具，将经纬度坐标转换为 Base32 字符串。编码后相邻字符串具有接近的地理位置，前缀越长匹配精度越高，适用于位置索引、附近查询、区域划分等 LBS 场景。

**命名空间**：`NewLife`  
**文档地址**：/core/geohash

## 编码精度对照表

| 长度 | 宽度误差 | 高度误差 | 面积约 |
|------|---------|---------|--------|
| 1 | 2500 km | 2500 km | 25000000 km² |
| 2 | 630 km | 630 km | 1600000 km² |
| 3 | 78 km | 78 km | 12000 km² |
| 4 | 20 km | 20 km | 400 km² |
| 5 | 2.4 km | 2.4 km | 2500 m²  4 |
| 6 | 610 m | 610 m | ~1.2 km² |
| 7 | 76 m | 76 m | ~12300 m² |
| 8 | 19 m | 19 m | ~780 m² |
| **9** | **2.4 m** | **2.4 m** | **~14 m²** |（默认精度）|
| 12 | 37 mm | 19 mm | 亚毫米级 |

## 快速开始

```csharp
using NewLife;

// 编码（默认 9 位）
var hash = GeoHash.Encode(116.3912f, 39.9067f);  // 北京天安门
Console.WriteLine(hash);  // 输出类似：wx4g0s624

// 解码
var (lon, lat) = GeoHash.Decode(hash);
Console.WriteLine($"经度: {lon:F6}，纬度: {lat:F6}");

// 获取边界框
var box = GeoHash.GetBoundingBox(hash);
Console.WriteLine($"[{box.MinLon:F6},{box.MinLat:F6}] ~ [{box.MaxLon:F6},{box.MaxLat:F6}]");
```

## API 参考

### Encode - 编码

```csharp
/// <summary>将经纬度编码为 GeoHash 字符串</summary>
/// <param name="longitude">经度，范围 [-180, 180]</param>
/// <param name="latitude">纬度，范围 [-90, 90]</param>
/// <param name="charCount">字符数（精度），范围 1~12，默认 9</param>
/// <returns>GeoHash 字符串（Base32）</returns>
public static String Encode(Double longitude, Double latitude, Int32 charCount = 9)
```

```csharp
// 5位精度（约2.4km误差），适合城市级查询
var hash5 = GeoHash.Encode(116.3912, 39.9067, 5);  // "wx4g0"

// 9位精度（约2.4m误差），适合精确定位（默认）
var hash9 = GeoHash.Encode(116.3912, 39.9067, 9);  // "wx4g0s624"

// 12位精度，厘米级定位
var hash12 = GeoHash.Encode(116.3912, 39.9067, 12);
```

### Decode - 解码

```csharp
/// <summary>将 GeoHash 字符串解码为经纬度（中心点坐标）</summary>
/// <param name="geoHash">GeoHash 字符串</param>
/// <returns>（经度, 纬度）元组</returns>
public static (Double Longitude, Double Latitude) Decode(String geoHash)
```

```csharp
var (lon, lat) = GeoHash.Decode("wx4g0s624");
Console.WriteLine($"经度: {lon:F6}，纬度: {lat:F6}");
```

### TryDecode - 安全解码

```csharp
/// <summary>尝试解码 GeoHash，失败返回 false 而不抛出异常</summary>
public static Boolean TryDecode(String? geoHash, out Double longitude, out Double latitude)
```

```csharp
if (GeoHash.TryDecode(userInput, out var lon, out var lat))
{
    // 有效的 GeoHash
}
else
{
    // 无效输入，无需处理异常
}
```

### IsValid - 验证

```csharp
/// <summary>验证字符串是否为有效的 GeoHash</summary>
public static Boolean IsValid(String? geoHash)
```

### GetBoundingBox - 获取边界框

```csharp
/// <summary>获取 GeoHash 覆盖的地理矩形范围</summary>
/// <returns>边界框，包含 MinLon/MaxLon/MinLat/MaxLat</returns>
public static (Double MinLon, Double MinLat, Double MaxLon, Double MaxLat) GetBoundingBox(String geoHash)
```

```csharp
var (minLon, minLat, maxLon, maxLat) = GeoHash.GetBoundingBox("wx4g0");
Console.WriteLine($"西南角: ({minLon:F4}, {minLat:F4})");
Console.WriteLine($"东北角: ({maxLon:F4}, {maxLat:F4})");
```

### Neighbor - 获取相邻格子

```csharp
/// <summary>获取指定方向的相邻 GeoHash</summary>
/// <param name="geoHash">当前格子</param>
/// <param name="dLon">经度方向偏移（-1/0/1，负=西，正=东）</param>
/// <param name="dLat">纬度方向偏移（-1/0/1，负=南，正=北）</param>
public static String Neighbor(String geoHash, Int32 dLon, Int32 dLat)
```

```csharp
var center = "wx4g0s624";
var north  = GeoHash.Neighbor(center, 0, 1);   // 正北
var south  = GeoHash.Neighbor(center, 0, -1);  // 正南
var east   = GeoHash.Neighbor(center, 1, 0);   // 正东
var west   = GeoHash.Neighbor(center, -1, 0);  // 正西
var ne     = GeoHash.Neighbor(center, 1, 1);   // 东北
```

### Neighbors - 获取全部8个相邻格子

```csharp
/// <summary>获取周围 8 个相邻 GeoHash（顺时针：N/NE/E/SE/S/SW/W/NW）</summary>
public static String[] Neighbors(String geoHash)
```

```csharp
var center    = "wx4g0s624";
var neighbors = GeoHash.Neighbors(center);
// neighbors[0]=北, [1]=东北, [2]=东, [3]=东南
// neighbors[4]=南, [5]=西南, [6]=西, [7]=西北

// 9宫格查询（自身 + 8个邻格）
var cells = neighbors.Prepend(center).ToArray();
```

## 使用场景

### 场景一：附近位置查询（数据库索引）

```csharp
// 存储时计算并保存 GeoHash（需要适当精度）
var poi = new PointOfInterest
{
    Longitude = 116.3912,
    Latitude  = 39.9067,
    GeoHash5  = GeoHash.Encode(116.3912, 39.9067, 5),  // 城市级索引
};

// 查询附近 POI（获取 9 宫格内的所有 POI）
var userHash  = GeoHash.Encode(userLon, userLat, 5);
var searchSet = new HashSet<String>(GeoHash.Neighbors(userHash)) { userHash };

var nearbyPois = db.PointsOfInterest
    .Where(p => searchSet.Contains(p.GeoHash5))
    .ToList();
```

### 场景二：区域分发（物流/骑手分配）

```csharp
// 按行政区划配置区域码（5位  2.4km 区域）
var config = new Dictionary<String, String>
{
    { "wx4g0", "北京朝阳配送站A" },
    { "wx4g1", "北京朝阳配送站B" },
    // ...
};

// 下单时自动分配
var orderHash = GeoHash.Encode(order.Longitude, order.Latitude, 5);
if (config.TryGetValue(orderHash, out var station))
    order.Station = station;
```

### 场景三：精度降级（节省索引空间）

```csharp
// 精确坐标  不同精度的前缀
var precise = GeoHash.Encode(116.3912, 39.9067, 9);  // "wx4g0s624"
var city    = precise[..5];  // "wx4g0" 城市级
var block   = precise[..7];  // "wx4g0s6" 街区级

// 多精度同时索引，便于范围查询
```

## 注意事项

- **边界问题**：经度 180、纬度 90 附近的相邻格子计算可能跨越 Date Line，`Neighbor` 内部已处理，但业务逻辑仍需注意。
- **精度选择**：精度越高，9宫格覆盖面积越小，漏查附近点的可能性越大。建议查询用 5~7 位，存储用 9 位。
- **非制图用途**：GeoHash 不保证等面积，高纬度地区单个格子的实际面积比低纬度小，不适合地理统计用途。
- **数据库索引**：在 GeoHash 字符串字段上建立普通 B-Tree 字符串前缀索引，即可支持 `LIKE 'wx4g0%'` 的范围查询，无需地理索引插件。
