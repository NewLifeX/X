using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;

namespace System
{
    /// <summary>枚举类型助手类</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public static class EnumHelper
    {
        /// <summary>枚举变量是否包含指定标识</summary>
        /// <param name="value">枚举变量</param>
        /// <param name="flag">要判断的标识</param>
        /// <returns></returns>
        public static Boolean Has(this Enum value, Enum flag)
        {
            if (value.GetType() != flag.GetType()) throw new ArgumentException("flag", "枚举标识判断必须是相同的类型！");

            UInt64 num = Convert.ToUInt64(flag);
            return (Convert.ToUInt64(value) & num) == num;
        }

        /// <summary>设置标识位</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="source"></param>
        /// <param name="flag"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static T Set<T>(this Enum source, T flag, Boolean value)
        {
            if (!(source is T)) throw new ArgumentException("source", "枚举标识判断必须是相同的类型！");

            UInt64 s = Convert.ToUInt64(source);
            UInt64 f = Convert.ToUInt64(flag);

            if (value)
            {
                s |= f;
            }
            else
            {
                s &= ~f;
            }

            return (T)Enum.ToObject(typeof(T), s);
        }

        /// <summary>获取枚举字段的注释</summary>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static String GetDescription(this Enum value)
        {
            var type = value.GetType();
            var item = type.GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);

            //var att = AttributeX.GetCustomAttribute<DescriptionAttribute>(item, false);
            var att = item.GetCustomAttribute<DescriptionAttribute>(false);
            if (att != null && !String.IsNullOrEmpty(att.Description)) return att.Description;

            return null;
        }

        /// <summary>获取枚举类型的所有字段注释</summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static Dictionary<TEnum, String> GetDescriptions<TEnum>()
        {
            var dic = new Dictionary<TEnum, String>();

            foreach (var item in GetDescriptions(typeof(TEnum)))
            {
                dic.Add((TEnum)(Object)item.Key, item.Value);
            }

            return dic;
        }

        /// <summary>获取枚举类型的所有字段注释</summary>
        /// <param name="enumType"></param>
        /// <returns></returns>
        public static Dictionary<Int32, String> GetDescriptions(Type enumType)
        {
            var dic = new Dictionary<Int32, String>();
            foreach (var item in enumType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!item.IsStatic) continue;

                // 这里的快速访问方法会报错
                //FieldInfoX fix = FieldInfoX.Create(item);
                //PermissionFlags value = (PermissionFlags)fix.GetValue(null);
                Int32 value = Convert.ToInt32(item.GetValue(null));

                String des = item.Name;

                //var dna = AttributeX.GetCustomAttribute<DisplayNameAttribute>(item, false);
                var dna = item.GetCustomAttribute<DisplayNameAttribute>(false);
                if (dna != null && !String.IsNullOrEmpty(dna.DisplayName)) des = dna.DisplayName;

                //var att = AttributeX.GetCustomAttribute<DescriptionAttribute>(item, false);
                var att = item.GetCustomAttribute<DescriptionAttribute>(false);
                if (att != null && !String.IsNullOrEmpty(att.Description)) des = att.Description;
                dic.Add(value, des);
            }

            return dic;
        }
    }
}