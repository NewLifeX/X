using System.Reflection;
using NewLife.Collections;
using NewLife.Reflection;

namespace System
{
    /// <summary>特性辅助类</summary>
    public static class AttributeX
    {
        #region 静态方法
        private static DictionaryCache<String, Object> _miCache = new DictionaryCache<String, Object>();

        /// <summary>获取自定义特性，带有缓存功能，避免因.Net内部GetCustomAttributes没有缓存而带来的损耗</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="member"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static TAttribute[] GetCustomAttributes<TAttribute>(this MemberInfo member, Boolean inherit = true)
        {
            if (member == null) return new TAttribute[0];

            var key = "";
            var type = (member as Type) ?? member.DeclaringType ?? member.ReflectedType;
            if (type != null)
                key = String.Format("{0}_{1}", type.FullName, member.Name);
            else
                key = String.Format("{0}_{1}", member.Module.Assembly.FullName, member.MetadataToken);

            key = String.Format("{0}_{1}_{2}", key, typeof(TAttribute).FullName, inherit);

            return (TAttribute[])_miCache.GetItem<MemberInfo, Boolean>(key, member, inherit, (k, m, h) =>
            {
                var atts = m.GetCustomAttributes(typeof(TAttribute), h) as TAttribute[];
                return atts == null ? new TAttribute[0] : atts;
            });
        }

        /// <summary>获取自定义属性</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="member"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static TAttribute GetCustomAttribute<TAttribute>(this MemberInfo member, Boolean inherit = true)
        {
            var atts = member.GetCustomAttributes<TAttribute>(inherit);
            if (atts == null || atts.Length < 1) return default(TAttribute);

            return atts[0];
        }

        private static DictionaryCache<String, Object> _asmCache = new DictionaryCache<String, Object>();

        /// <summary>获取自定义属性，带有缓存功能，避免因.Net内部GetCustomAttributes没有缓存而带来的损耗</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static TAttribute[] GetCustomAttributes<TAttribute>(this Assembly assembly)
        {
            if (assembly == null) return new TAttribute[0];

            var key = String.Format("{0}_{1}", assembly.FullName, typeof(TAttribute).FullName);

            return (TAttribute[])_asmCache.GetItem<Assembly>(key, assembly, (k, m) =>
            {
                var atts = m.GetCustomAttributes(typeof(TAttribute), true) as TAttribute[];
                return atts == null ? new TAttribute[0] : atts;
            });
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
        public static TResult GetCustomAttributeValue<TAttribute, TResult>(this MemberInfo target, Boolean inherit = true)
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
            if (inherit && target is Type)
            {
                target = (target as Type).BaseType;
                if (target != null && target != typeof(Object))
                    return GetCustomAttributeValue<TAttribute, TResult>(target, inherit);
            }

            return default(TResult);
        }
        #endregion
    }
}