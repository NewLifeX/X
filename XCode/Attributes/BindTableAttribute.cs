using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace XCode
{
	/// <summary>
	/// 用于指定数据类所绑定到的数据表的表名
	/// </summary>
    [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
	public sealed class BindTableAttribute : Attribute
	{
        private String _Name;
        /// <summary>表名</summary>
        public String Name
        {
            get { return _Name; }
            set { _Name = value; }
        }

        private String _Description;
        /// <summary>描述</summary>
        public String Description
        {
            get { return _Description; }
            set { _Description = value; }
        }

        private String _ConnName;
        /// <summary>连接名</summary>
        public String ConnName
        {
            get { return _ConnName; }
            set { _ConnName = value; }
        }

		/// <summary>
		/// 构造函数
		/// </summary>
        /// <param name="name">表名</param>
		public BindTableAttribute(String name)
		{
			Name = name;
		}
		/// <summary>
		/// 构造函数
		/// </summary>
        /// <param name="name">表名</param>
		/// <param name="description">描述</param>
        public BindTableAttribute(String name, String description)
		{
            Name = name;
			Description = description;
		}

        /// <summary>
        /// 检索应用于类型成员的自定义属性。
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static BindTableAttribute GetCustomAttribute(MemberInfo element)
        {
            return GetCustomAttribute(element, typeof(BindTableAttribute)) as BindTableAttribute;
        }
    }
}