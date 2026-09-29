namespace NewLife.Http;

/// <summary>参数化HTTP路由器，支持 {param}、{param?}、{*path} 路由模式</summary>
/// <remarks>
/// 路由优先级：精确匹配 &gt; 参数化匹配 &gt; 通配符匹配。
/// 
/// 路由模式示例：
/// - /api/users/{id}          → 匹配 /api/users/123，参数 id=123
/// - /api/users/{id?}         → 匹配 /api/users 或 /api/users/123
/// - /api/files/{*path}       → 匹配 /api/files/a/b/c，参数 path=a/b/c
/// - /api/{controller}/{action} → 匹配 /api/user/info，参数 controller=user, action=info
/// </remarks>
public class HttpRouter
{
    private readonly List<RouteEntry> _routes = [];

    /// <summary>注册路由</summary>
    /// <param name="pattern">路由模式，如 /api/users/{id}</param>
    /// <param name="handler">关联的处理器</param>
    public void Register(String pattern, IHttpHandler handler)
    {
        if (pattern.IsNullOrEmpty()) throw new ArgumentNullException(nameof(pattern));
        if (handler == null) throw new ArgumentNullException(nameof(handler));

        // 确保以 / 开头
        pattern = pattern.EnsureStart("/");

        // 解析路由段
        var segments = ParsePattern(pattern);

        _routes.Add(new RouteEntry(pattern, segments, handler));
    }

    /// <summary>匹配路径并提取参数</summary>
    /// <param name="path">请求路径（不含查询字符串）</param>
    /// <param name="parameters">输出参数字典（不为null）</param>
    /// <returns>匹配到的处理器；未匹配返回null</returns>
    public IHttpHandler? Match(String path, IDictionary<String, Object?> parameters)
    {
        if (path.IsNullOrEmpty()) return null;

        // 确保以 / 开头
        if (!path.StartsWith("/")) path = "/" + path;

        var pathSegs = path.Split('/');

        foreach (var entry in _routes)
        {
            if (TryMatch(entry, pathSegs, parameters))
                return entry.Handler;
        }

        return null;
    }

    /// <summary>解析路由模式为段列表</summary>
    private static List<RouteSegment> ParsePattern(String pattern)
    {
        var segments = new List<RouteSegment>();
        var parts = pattern.Split('/');

        // 模式与请求路径都按 '/' 切分、都带前导空段（Match 里 path.Split('/') 不裁剪），
        // 这里同样保留，两侧段数才能对齐；裁掉会让模式段数恒比请求少 1，所有参数化路由匹配不上
        foreach (var part in parts)
        {
            if (part.StartsWith("{*"))
            {
                // 通配参数 {*path}
                var name = part[2..].TrimEnd('}');
                segments.Add(RouteSegment.Wildcard(name));
            }
            else if (part.StartsWith("{"))
            {
                // 参数化段 {param} 或 {param?}
                var inner = part[1..].TrimEnd('}');
                var optional = inner.EndsWith("?");
                var name = optional ? inner[..^1] : inner;
                segments.Add(RouteSegment.Parameter(name, optional));
            }
            else
            {
                segments.Add(RouteSegment.Literal(part));
            }
        }

        return segments;
    }

    /// <summary>尝试匹配路由条目</summary>
    private static Boolean TryMatch(RouteEntry entry, String[] pathSegs, IDictionary<String, Object?> parameters)
    {
        var routeSegs = entry.Segments;
        var hasWildcard = routeSegs.Count > 0 && routeSegs[^1].IsWildcard;

        // 非通配路由：路径段数必须在 [最少段数, 路由段数] 范围内。
        // 尾部可选段可省略，因此最少段数 = 路由段数 - 尾部可选段数，使 /api/users/{id?} 也能匹配 /api/users
        if (!hasWildcard)
        {
            var minSegs = routeSegs.Count;
            for (var i = routeSegs.Count - 1; i >= 0 && routeSegs[i].IsOptional; i--)
                minSegs--;

            if (pathSegs.Length < minSegs || pathSegs.Length > routeSegs.Count)
                return false;
        }

        // 通配路由：路径段数必须 >= 路由段数
        if (hasWildcard && pathSegs.Length < routeSegs.Count)
            return false;

        for (var i = 0; i < routeSegs.Count; i++)
        {
            var seg = routeSegs[i];

            if (seg.IsWildcard)
            {
                // 捕获剩余所有段
                var remaining = String.Join("/", pathSegs, i, pathSegs.Length - i);
                parameters[seg.Name] = remaining;
                return true;
            }

            if (i >= pathSegs.Length)
            {
                // 路径段数不足，且该段可选
                if (seg.IsOptional)
                {
                    parameters[seg.Name] = null;
                    continue;
                }
                return false;
            }

            var pathSeg = pathSegs[i];

            if (seg.IsParameter)
            {
                // 捕获参数值
                parameters[seg.Name] = pathSeg.Length > 0 ? pathSeg : null;
            }
            else
            {
                // 精确匹配（大小写不敏感）
                if (!seg.Name.EqualIgnoreCase(pathSeg))
                    return false;
            }
        }

        // 非通配路由需确认全部段匹配完毕
        if (!hasWildcard && pathSegs.Length > routeSegs.Count)
            return false;

        return true;
    }

    #region 内部类型
    private sealed class RouteEntry
    {
        public String Pattern { get; }
        public List<RouteSegment> Segments { get; }
        public IHttpHandler Handler { get; }

        public RouteEntry(String pattern, List<RouteSegment> segments, IHttpHandler handler)
        {
            Pattern = pattern;
            Segments = segments;
            Handler = handler;
        }
    }

    private readonly struct RouteSegment
    {
        public String Name { get; }
        public Boolean IsParameter { get; }
        public Boolean IsOptional { get; }
        public Boolean IsWildcard { get; }

        private RouteSegment(String name, Boolean isParameter, Boolean isOptional, Boolean isWildcard)
        {
            Name = name;
            IsParameter = isParameter;
            IsOptional = isOptional;
            IsWildcard = isWildcard;
        }

        public static RouteSegment Literal(String name) => new(name, false, false, false);
        public static RouteSegment Parameter(String name, Boolean optional) => new(name, true, optional, false);
        public static RouteSegment Wildcard(String name) => new(name, true, false, true);
    }
    #endregion
}
