using System.Collections.Generic;
using System.Reflection;
using NewLife.Reflection;

namespace System
{
    /// <summary>特性辅助类</summary>
    public static class AttributeX
    {
        #region 静态方法
        private static Dictionary<MemberInfo, Object> _micache1 = new Dictionary<MemberInfo, Object>();
        private static Dictionary<MemberInfo, Object> _micache2 = new Dictionary<MemberInfo, Object>();

        /// <summary>获取自定义特性，带有缓存功能，避免因.Net内部GetCustomAttributes没有缓存而带来的损耗</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="member"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static TAttribute[] GetCustomAttributes<TAttribute>(this MemberInfo member, Boolean inherit)
        {
            if (member == null) return new TAttribute[0];

            // 根据是否可继承，分属两个缓存集合
            var cache = inherit ? _micache1 : _micache2;

            Object obj = null;
            if (cache.TryGetValue(member, out obj)) return (TAttribute[])obj;
            lock (cache)
            {
                if (cache.TryGetValue(member, out obj)) return (TAttribute[])obj;

                var atts = member.GetCustomAttributes(typeof(TAttribute), inherit) as TAttribute[];
                var att = atts == null ? new TAttribute[0] : atts;
                cache[member] = att;
                return att;
            }
        }

        /// <summary>获取自定义属性</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="member"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static TAttribute GetCustomAttribute<TAttribute>(this MemberInfo member, Boolean inherit)
        {
            var atts = member.GetCustomAttributes<TAttribute>(inherit);
            if (atts == null || atts.Length < 1) return default(TAttribute);

            return atts[0];
        }

        private static Dictionary<Assembly, Object> _micache3 = new Dictionary<Assembly, Object>();

        /// <summary>获取自定义属性，带有缓存功能，避免因.Net内部GetCustomAttributes没有缓存而带来的损耗</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static TAttribute[] GetCustomAttributes<TAttribute>(this Assembly assembly)
        {
            if (assembly == null) return new TAttribute[0];

            // 根据是否可继承，分属两个缓存集合
            var cache = _micache3;

            Object obj = null;
            if (cache.TryGetValue(assembly, out obj)) return (TAttribute[])obj;
            lock (cache)
            {
                if (cache.TryGetValue(assembly, out obj)) return (TAttribute[])obj;

                // GetCustomAttributes的第二参数会被忽略
                var atts = assembly.GetCustomAttributes(typeof(TAttribute), true) as TAttribute[];
                var att = atts == null ? new TAttribute[0] : atts;
                cache[assembly] = att;
                return att;
            }
        }

        /// <summary>获取自定义属性</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static TAttribute GetCustomAttribute<TAttribute>(this Assembly assembly)
        {
            var avs = assembly.GetCustomAttributes<TAttribute>();
            if (avs == null || avs.Length < 1) return default(TAttribute);

            return avs[0];
        }

        /// <summary>获取自定义属性的值。可用于ReflectionOnly加载的程序集</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static TResult GetCustomAttributeValue<TAttribute, TResult>(this Assembly target)
        {
            if (target == null) return default(TResult);

            var list = CustomAttributeData.GetCustomAttributes(target);
            if (list == null || list.Count < 1) return default(TResult);

            foreach (var item in list)
            {
                if (typeof(TAttribute) != item.Constructor.DeclaringType) continue;

                var args = item.ConstructorArguments;
                if (args != null && args.Count > 0) return (TResult)args[0].Value;
            }

            return default(TResult);
        }

        /// <summary>获取自定义属性的值。可用于ReflectionOnly加载的程序集</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="target"></param>
        /// <param name="inherit">是否递归</param>
        /// <returns></returns>
        public static TResult GetCustomAttributeValue<TAttribute, TResult>(this Type target, Boolean inherit)
        {
            if (target == null) return default(TResult);

            var list = CustomAttributeData.GetCustomAttributes(target);

            if (list != null && list.Count > 0)
            {
                foreach (var item in list)
                {
                    if (!TypeX.Equal(typeof(TAttribute), item.Constructor.DeclaringType)) continue;

                    var args = item.ConstructorArguments;
                    if (args != null && args.Count > 0) return (TResult)args[0].Value;
                }
            }
            if (inherit)
            {
                target = target.BaseType;
                if (target != null && target != typeof(Object))
                    return GetCustomAttributeValue<TAttribute, TResult>(target, inherit);
            }

            return default(TResult);
        }
        #endregion
    }
}