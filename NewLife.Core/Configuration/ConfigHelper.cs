using System.Collections;
using System.ComponentModel;
using System.Reflection;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Configuration;

/// <summary>配置助手</summary>
/// <remarks>提供配置树的查找、添加和映射等扩展方法</remarks>
public static class ConfigHelper
{
    #region 扩展
    /// <summary>查找配置项</summary>
    /// <param name="section">起始配置节</param>
    /// <param name="key">键路径，冒号分隔</param>
    /// <param name="createOnMiss">当不存在时是否自动创建</param>
    /// <returns>返回匹配配置节；未找到且不创建时返回 null</returns>
    public static IConfigSection? Find(this IConfigSection section, String key, Boolean createOnMiss = false)
    {
        if (key.IsNullOrEmpty()) return section;

        // 分层
        var ss = key.Split(':');

        var sec = section;

        // 逐级下钻
        for (var i = 0; i < ss.Length; i++)
        {
            var part = ss[i];
            if (part.IsNullOrEmpty()) continue;

            var cfg = sec.Childs?.FirstOrDefault(e => e.Key.EqualIgnoreCase(part));
            if (cfg == null)
            {
                if (!createOnMiss) return null;

                cfg = sec.AddChild(part);
            }

            sec = cfg;
        }

        return sec;
    }

    /// <summary>添加子节点</summary>
    /// <param name="section">父配置节</param>
    /// <param name="key">子节点键名</param>
    /// <returns>创建的子配置节</returns>
    public static IConfigSection AddChild(this IConfigSection section, String key)
    {
        //if (section == null) return null;

        var cfg = new ConfigSection { Key = key };
        section.Childs ??= [];
        section.Childs.Add(cfg);

        return cfg;
    }

    /// <summary>查找或添加子节点</summary>
    /// <param name="section">父配置节</param>
    /// <param name="key">子节点键名</param>
    /// <returns>已存在或新建的子配置节</returns>
    public static IConfigSection GetOrAddChild(this IConfigSection section, String key)
    {
        //if (section == null) return null;

        var cfg = section.Childs?.FirstOrDefault(e => e.Key.EqualIgnoreCase(key));
        if (cfg != null) return cfg;

        cfg = new ConfigSection { Key = key };
        section.Childs ??= [];
        section.Childs.Add(cfg);

        return cfg;
    }

    /// <summary>设置节点值</summary>
    /// <remarks>格式化友好字符串，DateTime、Boolean、Enum 等特殊处理</remarks>
    /// <param name="section">目标配置节</param>
    /// <param name="value">待设置的值</param>
    internal static void SetValue(this IConfigSection section, Object? value)
    {
        if (value is DateTime dt)
            section.Value = dt.ToFullString();
        else if (value is Boolean b)
            section.Value = b.ToString().ToLowerInvariant();
        else if (value is Enum)
            section.Value = value.ToString();
        else
            section.Value = value?.ToString();
    }
    #endregion

    #region 映射
    /// <summary>映射配置树到实例公有属性</summary>
    /// <param name="section">数据源</param>
    /// <param name="model">模型</param>
    /// <param name="provider">提供者</param>
    public static void MapTo(this IConfigSection section, Object model, IConfigProvider provider)
    {
        var childs = section.Childs?.ToArray();
        if (childs == null || childs.Length == 0 || model == null) return;

        // 支持字典
        if (model is IDictionary<String, Object?> dic)
        {
            MapToDictionary(childs, dic);
            return;
        }

        var prv = provider as ConfigProvider;

        // 反射公有实例属性
        foreach (var pi in model.GetType().GetProperties(true))
        {
            if (!pi.CanRead || !pi.CanWrite) continue;

            var name = SerialHelper.GetName(pi);
            if (name.EqualIgnoreCase("ConfigFile", "IsNew")) continue;

            prv?.UseKey(name);
            var cfg = childs.FirstOrDefault(e => e.Key.EqualIgnoreCase(name));
            if (cfg == null)
            {
                prv?.MissKey(name);
                continue;
            }

            // 分别处理基本类型、数组类型、复杂类型
            MapToObject(cfg, model, pi, provider);
        }
    }

    /// <summary>映射配置子节点到字典</summary>
    /// <param name="childs">子节点数组</param>
    /// <param name="dic">目标字典</param>
    private static void MapToDictionary(IConfigSection[] childs, IDictionary<String, Object?> dic)
    {
        foreach (var cfg in childs)
        {
            if (cfg.Key.IsNullOrEmpty()) continue;

            // 如有子级，则优先返回子级集合，否则返回值
            dic[cfg.Key] = (cfg.Childs != null && cfg.Childs.Count > 0) ? cfg.Childs : cfg.Value;
        }
    }

    /// <summary>映射配置节到对象属性</summary>
    /// <param name="cfg">源配置节</param>
    /// <param name="model">目标模型</param>
    /// <param name="pi">属性信息</param>
    /// <param name="provider">配置提供者</param>
    private static void MapToObject(IConfigSection cfg, Object model, PropertyInfo pi, IConfigProvider provider)
    {
        // 分别处理基本类型、数组类型、复杂类型
        if (pi.PropertyType.IsBaseType())
        {
            model.SetValue(pi, cfg.Value);
        }
        else if (cfg.Childs != null)
        {
            if (pi.PropertyType.As<IList>() || pi.PropertyType.As(typeof(IList<>)))
            {
                if (pi.PropertyType.IsArray)
                    MapToArray(cfg, model, pi, provider);
                else
                    MapToList(cfg, model, pi, provider);
            }
            else
            {
                MapToComplexObject(cfg, model, pi, provider);
            }
        }
    }

    /// <summary>映射配置节到复杂对象属性</summary>
    /// <param name="cfg">源配置节</param>
    /// <param name="model">目标模型</param>
    /// <param name="pi">属性信息</param>
    /// <param name="provider">配置提供者</param>
    private static void MapToComplexObject(IConfigSection cfg, Object model, PropertyInfo pi, IConfigProvider provider)
    {
        var val = model.GetValue(pi);
        if (val == null)
        {
            // 如果有无参构造函数，则实例化一个
            var ctor = pi.PropertyType.GetConstructor(Type.EmptyTypes);
            if (ctor != null)
            {
                val = ctor.Invoke(null);
                model.SetValue(pi, val);
            }
        }

        // 递归映射
        if (val != null) MapTo(cfg, val, provider);
    }

    /// <summary>映射配置节到数组属性</summary>
    /// <param name="section">源配置节</param>
    /// <param name="model">目标模型</param>
    /// <param name="pi">属性信息</param>
    /// <param name="provider">配置提供者</param>
    private static void MapToArray(IConfigSection section, Object model, PropertyInfo pi, IConfigProvider provider)
    {
        if (section.Childs == null) return;

        var elementType = pi.PropertyType.GetElementTypeEx();
        if (elementType == null) return;

        var count = section.Childs.Count;

        // 实例化或调整数组，按配置元素数量精确创建，避免保留旧数据
        if (model.GetValue(pi) is not Array arr || arr.Length != count)
        {
            arr = Array.CreateInstance(elementType, count);
            model.SetValue(pi, arr);
        }

        // 逐个映射
        for (var i = 0; i < count && i < arr.Length; i++)
        {
            var sec = section.Childs[i];

            // 基元类型或可直接转换的简单类型
            if (elementType.IsBaseType())
            {
                // 放宽 Key 限制，优先使用值进行转换
                arr.SetValue(sec.Value?.ChangeType(elementType), i);
            }
            else
            {
                var val = elementType.CreateInstance();
                if (val != null) MapTo(sec, val, provider);
                arr.SetValue(val, i);
            }
        }
    }

    /// <summary>映射配置节到列表属性</summary>
    /// <param name="section">源配置节</param>
    /// <param name="model">目标模型</param>
    /// <param name="pi">属性信息</param>
    /// <param name="provider">配置提供者</param>
    private static void MapToList(IConfigSection section, Object model, PropertyInfo pi, IConfigProvider provider)
    {
        var elementType = pi.PropertyType.GetElementTypeEx();
        if (elementType == null) return;

        // 确保存在列表实例
        IList list;
        var current = model.GetValue(pi);
        if (current is IList l)
        {
            list = l;
            // 映射前清空原有数据
            list.Clear();
        }
        else
        {
            var obj = !pi.PropertyType.IsInterface ?
                pi.PropertyType.CreateInstance() :
                typeof(List<>).MakeGenericType(elementType).CreateInstance();

            if (obj is not IList newList) return;

            model.SetValue(pi, newList);
            list = newList;
        }

        if (section.Childs == null) return;

        // 逐个映射
        var childs = section.Childs.ToArray();
        for (var i = 0; i < childs.Length; i++)
        {
            Object? val = null;
            if (elementType.IsBaseType())
            {
                // 将字符串值转换为目标元素类型
                val = childs[i].Value?.ChangeType(elementType);
            }
            else
            {
                val = elementType.CreateInstance();
                if (val != null) MapTo(childs[i], val, provider);
            }
            list.Add(val);
        }
    }

    /// <summary>从实例公有属性映射到配置树</summary>
    /// <param name="section">目标配置节</param>
    /// <param name="model">模型实例</param>
    public static void MapFrom(this IConfigSection section, Object model)
    {
        if (section == null) return;

        // 支持字典
        if (model is IDictionary<String, Object?> dic)
        {
            MapFromDictionary(section, dic);
            return;
        }

        // 反射公有实例属性
        foreach (var pi in model.GetType().GetProperties(true))
        {
            if (!pi.CanRead || !pi.CanWrite) continue;

            var name = SerialHelper.GetName(pi);
            if (name.EqualIgnoreCase("ConfigFile", "IsNew")) continue;

            // 名称前面加上命名空间
            var cfg = section.GetOrAddChild(name);

            // 反射获取属性值
            var value = model.GetValue(pi);
            cfg.Comment = pi.GetCustomAttribute<DescriptionAttribute>()?.Description;
            if (cfg.Comment.IsNullOrEmpty())
                cfg.Comment = pi.GetCustomAttribute<DisplayNameAttribute>()?.DisplayName;

            // 即使模型字段值为空，也必须拷贝，否则修改设置时，无法清空某字段
            // 分别处理基本类型、数组类型、复杂类型
            MapObject(section, cfg, value, pi.PropertyType);
        }
    }

    /// <summary>从字典映射到配置节</summary>
    /// <param name="section">目标配置节</param>
    /// <param name="dic">源字典</param>
    private static void MapFromDictionary(IConfigSection section, IDictionary<String, Object?> dic)
    {
        foreach (var item in dic)
        {
            var cfg = section.GetOrAddChild(item.Key);
            var value = item.Value;

            // 分别处理基本类型、数组类型、复杂类型
            if (value != null) MapObject(section, cfg, value, value.GetType());
        }
    }

    /// <summary>映射单个对象到配置节</summary>
    /// <param name="section">父配置节</param>
    /// <param name="cfg">目标配置节</param>
    /// <param name="val">源值</param>
    /// <param name="type">值类型</param>
    private static void MapObject(IConfigSection section, IConfigSection cfg, Object? val, Type type)
    {
        // 分别处理基本类型、数组类型、复杂类型
        if (type.IsBaseType())
        {
            cfg.SetValue(val);
        }
        else if (type.As<IList>() || type.As(typeof(IList<>)))
        {
            if (val is IList list)
            {
                var elementType = type.GetElementTypeEx();
                if (elementType != null) MapArray(cfg, list, elementType);
            }
        }
        else if (val != null)
        {
            // 递归映射
            MapFrom(cfg, val);
        }
    }

    /// <summary>映射列表到配置节</summary>
    /// <param name="cfg">目标配置节</param>
    /// <param name="list">源列表</param>
    /// <param name="elementType">元素类型</param>
    private static void MapArray(IConfigSection cfg, IList list, Type elementType)
    {
        // 直接重用并清空当前配置节的子节点，避免顺序漂移并保留注释
        cfg.Childs ??= [];
        if (cfg.Childs.Count > 0) cfg.Childs.Clear();

        // 数组元素是没有key的集合
        foreach (var item in list)
        {
            if (item == null) continue;

            var cfg2 = cfg.AddChild(elementType.Name);

            // 分别处理基本类型和复杂类型
            if (item.GetType().IsBaseType())
                cfg2.SetValue(item);
            else
                MapFrom(cfg2, item);
        }
    }
    #endregion
}