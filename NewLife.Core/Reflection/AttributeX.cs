using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NewLife.Reflection
{
    /// <summary>
    /// 特性辅助类
    /// </summary>
    public class AttributeX
    {
        #region 静态方法
        /// <summary>
        /// 获取自定义属性
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <param name="member"></param>
        /// <returns></returns>
        public static TAttribute GetCustomAttribute<TAttribute>(MemberInfo member)
        {
            TAttribute[] avs = member.GetCustomAttributes(typeof(TAttribute), true) as TAttribute[];
            if (avs == null || avs.Length < 1) return default(TAttribute);

            return avs[0];
        }

        /// <summary>
        /// 获取自定义属性的值。可用于ReflectionOnly加载的程序集
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <returns></returns>
        public static TResult GetCustomAttributeValue<TAttribute, TResult>(Assembly target)
        {
            IList<CustomAttributeData> list = CustomAttributeData.GetCustomAttributes(target);
            if (list == null || list.Count < 1) return default(TResult);

            foreach (CustomAttributeData item in list)
            {
                if (typeof(TAttribute) != item.Constructor.DeclaringType) continue;

                if (item.ConstructorArguments != null && item.ConstructorArguments.Count > 0)
                    return (TResult)item.ConstructorArguments[0].Value;
            }

            return default(TResult);
        }

        /// <summary>
        /// 获取自定义属性的值。可用于ReflectionOnly加载的程序集
        /// </summary>
        /// <typeparam name="TAttribute"></typeparam>
        /// <typeparam name="TResult"></typeparam>
        /// <param name="target"></param>
        /// <param name="isRecursion">是否递归</param>
        /// <returns></returns>
        public static TResult GetCustomAttributeValue<TAttribute, TResult>(Type target, Boolean isRecursion)
        {
            IList<CustomAttributeData> list = CustomAttributeData.GetCustomAttributes(target);
            if (list == null || list.Count < 1)
            {
                target = target.BaseType;
                if (target != null && target != typeof(Object))
                    return GetCustomAttributeValue<TAttribute, TResult>(target, isRecursion);
                else
                    return default(TResult);
            }

            foreach (CustomAttributeData item in list)
            {
                if (!TypeX.Equal(typeof(TAttribute), item.Constructor.DeclaringType)) continue;

                if (item.ConstructorArguments != null && item.ConstructorArguments.Count > 0)
                    return (TResult)item.ConstructorArguments[0].Value;
            }

            return default(TResult);
        }
        #endregion
    }
}