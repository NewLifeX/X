using System;
using System.Reflection;

namespace XCode
{
    /// <summary>指定实体类属性所绑定数据字段信息。</summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
    public sealed class BindColumnAttribute : Attribute
    {
        #region 属性
        /// <summary>字段名</summary>
        public String Name { get; set; }

        /// <summary>描述</summary>
        public String Description { get; set; }

        /// <summary>
        /// 原始数据类型。
        /// 当且仅当目标数据库同为该数据库类型时，采用实体属性信息上的RawType作为反向工程的目标字段类型，以期获得开发和生产的最佳兼容。
        /// </summary>
        public String RawType { get; set; }

        /// <summary>元素类型</summary>
        public String ItemType { get; set; }

        /// <summary>精度</summary>
        public Int32 Precision { get; set; }

        /// <summary>位数</summary>
        public Int32 Scale { get; set; }

        /// <summary>是否主字段。主字段作为业务主要字段，代表当前数据行意义</summary>
        public Boolean Master { get; set; }
        #endregion

        #region 构造
        /// <summary>构造函数</summary>
        public BindColumnAttribute() { }

        /// <summary>构造函数</summary>
        /// <param name="name">字段名</param>
        public BindColumnAttribute(String name)
        {
            Name = name;
        }

        /// <summary>构造函数</summary>
        /// <param name="name">名称</param>
        /// <param name="description"></param>
        /// <param name="rawType"></param>
        public BindColumnAttribute(String name, String description, String rawType)
        {
            Name = name;
            Description = description;
            RawType = rawType;
        }
        #endregion

        #region 方法
        /// <summary>检索应用于类型成员的自定义属性。</summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static BindColumnAttribute GetCustomAttribute(MemberInfo element)
        {
            return GetCustomAttribute(element, typeof(BindColumnAttribute)) as BindColumnAttribute;
        }
        #endregion
    }
}