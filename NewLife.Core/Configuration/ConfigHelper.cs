using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using NewLife.Reflection;
using NewLife.Serialization;

namespace NewLife.Configuration
{
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
            if (section.Childs == null) section.Childs = new List<IConfigSection>();
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
            if (section.Childs == null) section.Childs = new List<IConfigSection>();
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
        internal static void MapTo(this IConfigSection section, Object model)
        {
            if (section == null || section.Childs == null || section.Childs.Count == 0 || model == null) return;

            // 反射公有实例属性
            foreach (var pi in model.GetType().GetProperties(true))
            {
                if (!pi.CanRead || !pi.CanWrite) continue;
                //if (pi.GetIndexParameters().Length > 0) continue;
                //if (pi.GetCustomAttribute<IgnoreDataMemberAttribute>(false) != null) continue;
                //if (pi.GetCustomAttribute<XmlIgnoreAttribute>() != null) continue;

                var name = SerialHelper.GetName(pi);
                if (name.EqualIgnoreCase("ConfigFile", "IsNew")) continue;

                var cfg = section.Childs?.FirstOrDefault(e => e.Key.EqualIgnoreCase(name));
                if (cfg == null) continue;

                // 分别处理基本类型、数组类型、复杂类型
                if (pi.PropertyType.GetTypeCode() != TypeCode.Object)
                {
                    pi.SetValue(model, cfg.Value.ChangeType(pi.PropertyType), null);
                }
                else if (cfg.Childs != null)
                {
                    if (pi.PropertyType.As<IList>())
                    {
                        if (pi.PropertyType.IsArray)
                            MapArray(cfg, model, pi);
                        else
                            MapList(cfg, model, pi);
                    }
                    else
                    {
                        // 复杂类型需要递归处理
                        var val = pi.GetValue(model, null);
                        if (val == null)
                        {
                            // 如果有无参构造函数，则实例化一个
                            var ctor = pi.PropertyType.GetConstructor(new Type[0]);
                            if (ctor != null)
                            {
                                val = ctor.Invoke(null);
                                pi.SetValue(model, val, null);
                            }
                        }

                        // 递归映射
                        if (val != null) MapTo(cfg, val);
                    }
                }
            }
        }

        private static void MapArray(IConfigSection section, Object model, PropertyInfo pi)
        {
            var elementType = pi.PropertyType.GetElementTypeEx();

            // 实例化数组
            if (pi.GetValue(model, null) is not Array arr)
            {
                arr = Array.CreateInstance(elementType, section.Childs.Count);
                pi.SetValue(model, arr, null);
            }

            // 逐个映射
            for (var i = 0; i < section.Childs.Count; i++)
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
                    MapTo(sec, val);
                    arr.SetValue(val, i);
                }
            }
        }

        private static void MapList(IConfigSection section, Object model, PropertyInfo pi)
        {
            var elementType = pi.PropertyType.GetElementTypeEx();

            // 实例化列表
            if (pi.GetValue(model, null) is not IList list)
            {
                var obj = !pi.PropertyType.IsInterface ?
                    pi.PropertyType.CreateInstance() :
                    typeof(List<>).MakeGenericType(elementType).CreateInstance();

                list = obj as IList;
                if (list == null) return;

                pi.SetValue(model, list, null);
            }

            // 逐个映射
            for (var i = 0; i < section.Childs.Count; i++)
            {
                var val = elementType.CreateInstance();
                MapTo(section.Childs[i], val);
                list[i] = val;
            }
        }

        /// <summary>从实例公有属性映射到配置树</summary>
        /// <param name="section"></param>
        /// <param name="model"></param>
        internal static void MapFrom(this IConfigSection section, Object model)
        {
            if (section == null) return;

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
                var val = pi.GetValue(model, null);
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
                if (pi.PropertyType.GetTypeCode() != TypeCode.Object)
                {
                    cfg.SetValue(val);
                }
                else if (pi.PropertyType.As<IList>())
                {
                    if (val is IList list) MapArray(section, cfg, list, pi.PropertyType.GetElementTypeEx());
                }
                else
                {
                    // 递归映射
                    MapFrom(cfg, val);
                }
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
}