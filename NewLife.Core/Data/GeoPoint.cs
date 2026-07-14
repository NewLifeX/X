namespace NewLife.Data;

/// <summary>经纬度坐标。值类型，栈分配，零GC压力</summary>
/// <remarks>
/// 默认值为 (0,0)，即几内亚湾。需要表示"无坐标"时使用 GeoPoint?。
/// </remarks>
public readonly record struct GeoPoint(Double Longitude, Double Latitude);