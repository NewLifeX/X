using System;
using System.Collections.Generic;
using NewLife.Reflection;

namespace NewLife.CommonEntity
{
    /// <summary>
    /// 实体资格提供者，如果一个泛型实体类有多个子类，应该实现自己的提供者，以指定使用哪一个子类作为默认类型
    /// </summary>
    public interface IEntityShipProvider
    {
        /// <summary>
        /// 获取实体类型。默认扫描所有程序集，包括未加载程序集，优先返回非默认类型的实体类型。
        /// </summary>
        /// <param name="pluginType">约束插件类型</param>
        /// <param name="defaultType">默认类型</param>
        /// <returns></returns>
        Type GetEntityType(Type pluginType, Type defaultType);
    }

    /// <summary>
    /// 实体资格提供者，如果一个泛型实体类有多个子类，应该实现自己的提供者，以指定使用哪一个子类作为默认类型
    /// </summary>
    public class EntityShipProvider : IEntityShipProvider
    {
        /// <summary>
        /// 获取实体类型。默认扫描所有程序集，包括未加载程序集，优先返回非默认类型的实体类型。
        /// </summary>
        /// <param name="pluginType">约束插件类型</param>
        /// <param name="defaultType">默认类型</param>
        /// <returns></returns>
        public virtual Type GetEntityType(Type pluginType, Type defaultType)
        {
            List<Type> list = AssemblyX.FindAllPlugins(pluginType, true);
            if (list == null || list.Count < 1) return null;

            // 一个，直接返回
            if (list.Count == 1) return list[0];

            // 多个，且设置了默认值，排除掉默认值
            if (defaultType != null) list.RemoveAll(item => item != defaultType);

            // 一个，直接返回
            if (list.Count == 1) return list[0];

            // 还有多个，尝试排除同名者
            foreach (Type type in list)
            {
                // 实体类和基类名字相同
                String name = type.BaseType.Name;
                Int32 p = name.IndexOf('`');
                if (p > 0 && type.Name == name.Substring(0, p)) continue;

                return type;
            }

            return list[0];
        }
    }
}