using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 协议特性基类
    /// </summary>
    public abstract class ProtocolAttribute : Attribute
    {
        #region 特性设置合并到设置信息
        /// <summary>
        /// 特性设置合并到设置信息
        /// </summary>
        /// <param name="config"></param>
        public virtual void MergeTo(FormatterConfig config) { }
        #endregion

        #region 相等
        /// <summary>
        /// 比较两个特性是否相等
        /// </summary>
        /// <param name="config"></param>
        /// <returns></returns>
        public virtual bool Equals(FormatterConfig config) { return true; }
        #endregion

        #region 获取特性
        /// <summary>
        /// 获取自定义特性
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static TAttribute[] GetCustomAttributes<TAttribute>(MemberInfo member)
            where TAttribute : Attribute
        {
            if (member == null) return null;

            Attribute[] atts = Attribute.GetCustomAttributes(member);
            if (atts == null || atts.Length < 1) return null;

            List<TAttribute> list = new List<TAttribute>();
            foreach (Attribute item in atts)
            {
                if (item is TAttribute) list.Add(item as TAttribute);
            }

            if (list == null || list.Count < 1) return null;

            return list.ToArray();
        }

        /// <summary>
        /// 获取自定义特性
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public static TAttribute GetCustomAttribute<TAttribute>(MemberInfo member)
            where TAttribute : Attribute
        {
            if (member == null) return null;

            return Attribute.GetCustomAttribute(member, typeof(TAttribute)) as TAttribute;
        }
        #endregion
    }
}
