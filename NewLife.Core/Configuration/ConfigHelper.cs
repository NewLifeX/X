using System.Collections;
using System.ComponentModel;
using System.Reflection;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Configuration;

/// <summary>配置助手</summary>
public static class ConfigHelper
{
    #region 扩展
    /// <summary>查找配置项。可得到子级和配置</summary>
    /// <param name="section"></param>
    /// <param name="key"></param>
    /// <param name="createOnMiss"></param>
    /// <returns></returns>
    public static IConfigSection Find(this IConfigSection section, String key, Boolean createOnMiss = false)
    {
        if (key.IsNullOrEmpty()) return section;

        // 分层
        var ss = key.Split(':');

        var sec = section;

        // 逐级下钻
        for (var i = 0; i < ss.Length; i++)
        {
            var cfg = sec.Childs?.FirstOrDefault(e => e.Key.EqualIgnoreCase(ss[i]));
            if (cfg == null)
            {
                if (!createOnMiss) return null;

                cfg = sec.AddChild(ss[i]);
            }

            sec = cfg;
        }

        return sec;
    }

    /// <summary>添加子节点</summary>
    /// <param name="section"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static IConfigSection AddChild(this IConfigSection section, String key)
    {
        if (section == null) return null;

        var cfg = new ConfigSection { Key = key };
        section.Childs ??= new List<IConfigSection>();
        section.Childs.Add(cfg);

        return cfg;
    }

    /// <summary>查找或添加子节点</summary>
    /// <param name="section"></param>
    /// <param name="key"></param>
    /// <returns></returns>
    public static IConfigSection GetOrAddChild(this IConfigSection section, String key)
    {
        if (section == null) return null;

        var cfg = section.Childs?.FirstOrDefault(e => e.Key.EqualIgnoreCase(key));
        if (cfg != null) return cfg;

        cfg = new ConfigSection { Key = key };
        section.Childs ??= new List<IConfigSection>();
        section.Childs.Add(cfg);

        return cfg;
    }

    /// <summary>设置节点值。格式化友好字符串</summary>
    /// <param name="section"></param>
    /// <param name="value"></param>
    internal static void SetValue(this IConfigSection section, Object value)
    {
        if (value is DateTime dt)
            section.Value = dt.ToFullString();
        else if (value is Boolean b)
            section.Value = b.ToString().ToLower();
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
        var childs = section?.Childs?.ToArray();
        if (childs == null || childs.Length == 0 || model == null) return;

        // 支持字典
        if (model is IDictionary<String, Object> dic)
        {
            foreach (var cfg in childs)
            {
                dic[cfg.Key] = cfg.Value;

                if (cfg.Childs != null && cfg.Childs.Count > 0)
                    dic[cfg.Key] = cfg.Childs;
            }

            return;
        }

        var prv = provider as ConfigProvider;

        // 反射公有实例属性
        foreach (var pi in model.GetType().GetProperties(true))
        {
            if (!pi.CanRead || !pi.CanWrite) continue;
            //if (pi.GetIndexParameters().Length > 0) continue;
            //if (pi.GetCustomAttribute<IgnoreDataMemberAttribute>(false) != null) continue;
            //if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;

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

    private static void MapToObject(IConfigSection cfg, Object model, PropertyInfo pi, IConfigProvider provider)
    {
        // 分别处理基本类型、数组类型、复杂类型
        if (pi.PropertyType.GetTypeCode() != TypeCode.Object)
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
                // 复杂类型需要递归处理
                var val = model.GetValue(pi);
                if (val == null)
                {
                    // 如果有无参构造函数，则实例化一个
                    var ctor = pi.PropertyType.GetConstructor(new Type[0]);
                    if (ctor != null)
                    {
                        val = ctor.Invoke(null);
                        model.SetValue(pi, val);
                    }
                }

                // 递归映射
                if (val != null) MapTo(cfg, val, provider);
            }
        }
    }

    private static void MapToArray(IConfigSection section, Object model, PropertyInfo pi, IConfigProvider provider)
    {
        var elementType = pi.PropertyType.GetElementTypeEx();
        var count = section.Childs.Count;

        // 实例化数组
        if (model.GetValue(pi) is not Array arr || arr.Length == 0)
        {
            arr = Array.CreateInstance(elementType, count);
            model.SetValue(pi, arr);
        }

        // 逐个映射
        for (var i = 0; i < count && i < arr.Length; i++)
        {
            var sec = section.Childs[i];

            // 基元类型
            if (elementType.GetTypeCode() != TypeCode.Object)
            {
                if (sec.Key == elementType.Name)
                {
                    arr.SetValue(sec.Value.ChangeType(elementType), i);
                }
            }
            else
            {
                var val = elementType.CreateInstance();
                MapTo(sec, val, provider);
                arr.SetValue(val, i);
            }
        }
    }

    private static void MapToList(IConfigSection section, Object model, PropertyInfo pi, IConfigProvider provider)
    {
        var elementType = pi.PropertyType.GetElementTypeEx();

        // 实例化列表
        if (model.GetValue(pi) is not IList list)
        {
            var obj = !pi.PropertyType.IsInterface ?
                pi.PropertyType.CreateInstance() :
                typeof(List<>).MakeGenericType(elementType).CreateInstance();

            list = obj as IList;
            if (list == null) return;

            model.SetValue(pi, list);
        }

        // 映射前清空原有数据
        list.Clear();

        // 逐个映射
        var childs = section.Childs.ToArray();
        for (var i = 0; i < childs.Length; i++)
        {
            var val = elementType.CreateInstance();
            if (elementType.GetTypeCode() != TypeCode.Object)
            {
                val = childs[i].Value;
            }
            else
            {
                MapTo(childs[i], val, provider);
                //list[i] = val;
            }
            list.Add(val);
        }
    }

    /// <summary>从实例公有属性映射到配置树</summary>
    /// <param name="section"></param>
    /// <param name="model"></param>
    public static void MapFrom(this IConfigSection section, Object model)
    {
        if (section == null) return;

        // 支持字典
        if (model is IDictionary<String, Object> dic)
        {
            foreach (var item in dic)
            {
                var cfg = section.GetOrAddChild(item.Key);
                var value = item.Value;

                // 分别处理基本类型、数组类型、复杂类型
                if (value != null) MapObject(section, cfg, value, value.GetType());
            }

            return;
        }

        // 反射公有实例属性
        foreach (var pi in model.GetType().GetProperties(true))
        {
            if (!pi.CanRead || !pi.CanWrite) continue;
            //if (pi.GetIndexParameters().Length > 0) continue;
            //if (pi.GetCustomAttribute<IgnoreDataMemberAttribute>(false) != null) continue;
            //if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;

            var name = SerialHelper.GetName(pi);
            if (name.EqualIgnoreCase("ConfigFile", "IsNew")) continue;

            // 名称前面加上命名空间
            var cfg = section.GetOrAddChild(name);

            // 反射获取属性值
            var value = model.GetValue(pi);
            var att = pi.GetCustomAttribute<DescriptionAttribute>();
            cfg.Comment = att?.Description;
            if (cfg.Comment.IsNullOrEmpty())
            {
                var att2 = pi.GetCustomAttribute<DisplayNameAttribute>();
                cfg.Comment = att2?.DisplayName;
            }

            //!! 即使模型字段值为空，也必须拷贝，否则修改设置时，无法清空某字段
            //if (val == null) continue;

            // 分别处理基本类型、数组类型、复杂类型
            MapObject(section, cfg, value, pi.PropertyType);
        }
    }

    private static void MapObject(IConfigSection section, IConfigSection cfg, Object val, Type type)
    {
        // 分别处理基本类型、数组类型、复杂类型
        if (type.GetTypeCode() != TypeCode.Object)
        {
            cfg.SetValue(val);
        }
        else if (type.As<IList>() || type.As(typeof(IList<>)))
        {
            if (val is IList list) MapArray(section, cfg, list, type.GetElementTypeEx());
        }
        else if (val != null)
        {
            // 递归映射
            MapFrom(cfg, val);
        }
    }

    private static void MapArray(IConfigSection section, IConfigSection cfg, IList list, Type elementType)
    {
        // 为了避免数组元素叠加，干掉原来的
        section.Childs.Remove(cfg);
        cfg = new ConfigSection { Key = cfg.Key, Childs = new List<IConfigSection>(), Comment = cfg.Comment };
        section.Childs.Add(cfg);

        // 数组元素是没有key的集合
        foreach (var item in list)
        {
            if (item == null) continue;

            var cfg2 = cfg.AddChild(elementType.Name);

            // 分别处理基本类型和复杂类型
            if (item.GetType().GetTypeCode() != TypeCode.Object)
                cfg2.SetValue(item);
            else
                MapFrom(cfg2, item);
        }
    }
    #endregion
}