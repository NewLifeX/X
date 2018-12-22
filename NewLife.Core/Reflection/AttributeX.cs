using System.Collections.Concurrent;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using NewLife.Reflection;

namespace System
{
    /// <summary>特性辅助类</summary>
    public static class AttributeX
    {
        #region 静态方法
        private static ConcurrentDictionary<String, Object> _asmCache = new ConcurrentDictionary<String, Object>();

        /// <summary>获取自定义属性，带有缓存功能，避免因.Net内部GetCustomAttributes没有缓存而带来的损耗</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="assembly"></param>
        /// <returns></returns>
        public static TAttribute[] GetCustomAttributes<TAttribute>(this Assembly assembly)
        {
            if (assembly == null) return new TAttribute[0];

            var key = String.Format("{0}_{1}", assembly.FullName, typeof(TAttribute).FullName);

            return (TAttribute[])_asmCache.GetOrAdd(key, k =>
            {
                var atts = assembly.GetCustomAttributes(typeof(TAttribute), true) as TAttribute[];
                return atts ?? (new TAttribute[0]);
            });
        }

        /// <summary>获取成员绑定的显示名，优先DisplayName，然后Description</summary>
        /// <param name="member"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static String GetDisplayName(this MemberInfo member, Boolean inherit = true)
        {
            var att = member.GetCustomAttribute<DisplayNameAttribute>(inherit);
            if (att != null && !att.DisplayName.IsNullOrWhiteSpace()) return att.DisplayName;

            return null;
        }

        /// <summary>获取成员绑定的显示名，优先DisplayName，然后Description</summary>
        /// <param name="member"></param>
        /// <param name="inherit"></param>
        /// <returns></returns>
        public static String GetDescription(this MemberInfo member, Boolean inherit = true)
        {
            var att2 = member.GetCustomAttribute<DescriptionAttribute>(inherit);
            if (att2 != null && !att2.Description.IsNullOrWhiteSpace()) return att2.Description;

            return null;
        }

        /// <summary>获取自定义属性的值。可用于ReflectionOnly加载的程序集</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static TResult GetCustomAttributeValue<TAttribute, TResult>(this Assembly target) where TAttribute : Attribute
        {
            if (target == null) return default(TResult);

            // CustomAttributeData可能会导致只反射加载，需要屏蔽内部异常
            try
            {
                var list = CustomAttributeData.GetCustomAttributes(target);
                if (list == null || list.Count < 1) return default(TResult);

                foreach (var item in list)
                {
                    if (typeof(TAttribute) != item.Constructor.DeclaringType) continue;

                    var args = item.ConstructorArguments;
                    if (args != null && args.Count > 0) return (TResult)args[0].Value;
                }
            }
            catch { }

            return default(TResult);
        }

        /// <summary>获取自定义属性的值。可用于ReflectionOnly加载的程序集</summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="target">目标对象</param>
        /// <param name="inherit">是否递归</param>
        /// <returns></returns>
        public static TResult GetCustomAttributeValue<TAttribute, TResult>(this MemberInfo target, Boolean inherit = true) where TAttribute : Attribute
        {
            if (target == null) return default(TResult);

            try
            {
                var list = CustomAttributeData.GetCustomAttributes(target);
                if (list != null && list.Count > 0)
                {
                    foreach (var item in list)
                    {
                        if (typeof(TAttribute).FullName != item.Constructor.DeclaringType.FullName) continue;

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
            }
            catch
            {
                // 出错以后，如果不是仅反射加载，可以考虑正面来一次
                if (!target.Module.Assembly.ReflectionOnly)
                {
                    //var att = GetCustomAttribute<TAttribute>(target, inherit);
                    var att = target.GetCustomAttribute<TAttribute>(inherit);
                    if (att != null)
                    {
                        var pi = typeof(TAttribute).GetProperties().FirstOrDefault(p => p.PropertyType == typeof(TResult));
                        if (pi != null) return (TResult)att.GetValue(pi);
                    }
                }
            }

            return default(TResult);
        }

        #endregion
    }
}