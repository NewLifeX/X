using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;
using System.Reflection;
using NewLife.Reflection;
using NewLife.Linq;

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
            if (value.GetType() != flag.GetType()) throw new ArgumentException("item", "枚举标识判断必须是相同的类型！");

            UInt64 num = Convert.ToUInt64(flag);
            return (Convert.ToUInt64(value) & num) == num;
        }

        /// <summary>
        /// 获取枚举字段的注释
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public static String GetDescription(this Enum value)
        {
            Type type = value.GetType();
            FieldInfo item = type.GetField(value.ToString(), BindingFlags.Public | BindingFlags.Static);

            DescriptionAttribute att = AttributeX.GetCustomAttribute<DescriptionAttribute>(item, false);
            if (att != null && !String.IsNullOrEmpty(att.Description)) return att.Description;

            return null;
        }

        /// <summary>获取枚举类型的所有字段注释</summary>
        /// <typeparam name="TEnum"></typeparam>
        /// <returns></returns>
        public static Dictionary<TEnum, String> GetDescriptions<TEnum>()
        {
            Dictionary<TEnum, String> dic = new Dictionary<TEnum, string>();

            //TypeX type = typeof(TEnum);
            //foreach (FieldInfo item in type.BaseType.GetFields(BindingFlags.Public | BindingFlags.Static))
            //{
            //    if (!item.IsStatic) continue;

            //    // 这里的快速访问方法会报错
            //    //FieldInfoX fix = FieldInfoX.Create(item);
            //    //PermissionFlags value = (PermissionFlags)fix.GetValue(null);
            //    TEnum value = (TEnum)item.GetValue(null);

            //    String des = item.Name;
            //    DescriptionAttribute att = AttributeX.GetCustomAttribute<DescriptionAttribute>(item, false);
            //    if (att != null && !String.IsNullOrEmpty(att.Description)) des = att.Description;
            //    dic.Add(value, des);
            //}

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
            foreach (FieldInfo item in enumType.BaseType.GetFields(BindingFlags.Public | BindingFlags.Static))
            {
                if (!item.IsStatic) continue;

                // 这里的快速访问方法会报错
                //FieldInfoX fix = FieldInfoX.Create(item);
                //PermissionFlags value = (PermissionFlags)fix.GetValue(null);
                Int32 value = Convert.ToInt32(item.GetValue(null));

                String des = item.Name;

                DisplayNameAttribute dna = AttributeX.GetCustomAttribute<DisplayNameAttribute>(item, false);
                if (dna != null && !String.IsNullOrEmpty(dna.DisplayName)) des = dna.DisplayName;

                DescriptionAttribute att = AttributeX.GetCustomAttribute<DescriptionAttribute>(item, false);
                if (att != null && !String.IsNullOrEmpty(att.Description)) des = att.Description;
                dic.Add(value, des);
            }

            return dic;
        }
    }
}