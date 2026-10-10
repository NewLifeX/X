using System;
using System.Collections.Generic;
using System.Reflection;

namespace NewLife.Reflection;

/// <summary>成员访问表。源生成器为标记类型预生成成员读写委托，序列化与配置绑定等场景优先使用，避免反射与表达式编译，NativeAOT 友好</summary>
/// <remarks>
/// 由 NewLife.Core 源生成器在编译期扫描标记（如 <c>[JsonSerializable(typeof(T))]</c>、继承 <c>Config&lt;T&gt;</c> 的配置类）后生成注册代码。
/// 表按"运行时类型 + 成员名"索引，命中时零反射；未命中（派生类实例、未标记类型等）自动回退原有反射路径，行为不变。
/// </remarks>
public static class MemberAccessorTable
{
    #region 属性
    private static readonly Object _lock = new();
    private static volatile Dictionary<(Type, String), Accessor> _map = [];
    #endregion

    #region 方法
    /// <summary>注册成员访问器</summary>
    /// <param name="type">成员所属类型（生成器标记的类型）</param>
    /// <param name="name">成员名</param>
    /// <param name="getter">取值委托，null 表示不可读</param>
    /// <param name="setter">赋值委托，null 表示不可写</param>
    public static void Register(Type type, String name, Func<Object, Object?>? getter, Action<Object, Object?>? setter)
    {
        if (type == null) throw new ArgumentNullException(nameof(type));
        if (name.IsNullOrEmpty()) throw new ArgumentNullException(nameof(name));

        // 写时复制发布（lock 保证唯一写入，volatile 保证所有读者可见）
        lock (_lock)
        {
            var key = (type, name);
            if (!_map.ContainsKey(key))
                _map = new Dictionary<(Type, String), Accessor>(_map) { [key] = new Accessor(getter, setter) };
        }
    }

    /// <summary>尝试获取成员取值委托</summary>
    /// <param name="type">成员所属类型</param>
    /// <param name="name">成员名</param>
    /// <returns>取值委托，未注册或不可读时返回 null</returns>
    public static Func<Object, Object?>? TryGetGetter(Type type, String name)
        => _map.TryGetValue((type, name), out var accessor) ? accessor.Getter : null;

    /// <summary>尝试获取成员赋值委托</summary>
    /// <param name="type">成员所属类型</param>
    /// <param name="name">成员名</param>
    /// <returns>赋值委托，未注册或不可写时返回 null</returns>
    public static Action<Object, Object?>? TryGetSetter(Type type, String name)
        => _map.TryGetValue((type, name), out var accessor) ? accessor.Setter : null;
    #endregion

    #region 辅助
    /// <summary>读取成员值。命中生成表时零反射，否则回退反射路径</summary>
    /// <param name="obj">目标对象</param>
    /// <param name="type">目标对象类型</param>
    /// <param name="member">成员</param>
    /// <returns>成员值</returns>
    internal static Object? GetValue(Object obj, Type type, MemberInfo member)
        => TryGetGetter(type, member.Name) is { } getter ? getter(obj) : obj.GetValue(member);

    /// <summary>写入成员值。命中生成表时零反射，否则回退反射路径</summary>
    /// <param name="obj">目标对象</param>
    /// <param name="type">目标对象类型</param>
    /// <param name="member">成员</param>
    /// <param name="value">成员值</param>
    internal static void SetValue(Object obj, Type type, MemberInfo member, Object? value)
    {
        if (TryGetSetter(type, member.Name) is { } setter)
            setter(obj, value);
        else
            obj.SetValue(member, value);
    }

    private sealed class Accessor
    {
        public Func<Object, Object?>? Getter { get; }
        public Action<Object, Object?>? Setter { get; }

        public Accessor(Func<Object, Object?>? getter, Action<Object, Object?>? setter)
        {
            Getter = getter;
            Setter = setter;
        }
    }
    #endregion
}
