using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;

namespace XCode
{
	/// <summary>
	/// 用于指定数据类属性所绑定到的数据表的字段名
	/// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false)]
	public sealed class BindColumnAttribute : Attribute
	{
        private String _Name;
        /// <summary>字段名</summary>
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

        private String _DefaultValue;
        /// <summary>默认值</summary>
        public String DefaultValue
        {
            get { return _DefaultValue; }
            set { _DefaultValue = value; }
        }

        private String _RawType;
        /// <summary>原始数据类型</summary>
        public String RawType
        {
            get { return _RawType; }
            set { _RawType = value; }
        }

        private Int32 _Precision;
        /// <summary>精度</summary>
        public Int32 Precision
        {
            get { return _Precision; }
            set { _Precision = value; }
        }

        private Int32 _Scale;
        /// <summary>位数</summary>
        public Int32 Scale
        {
            get { return _Scale; }
            set { _Scale = value; }
        }

        private Boolean _IsUnicode;
        /// <summary>是否Unicode</summary>
        public Boolean IsUnicode
        {
            get { return _IsUnicode; }
            set { _IsUnicode = value; }
        }

        private Int32 _Order;
        /// <summary>顺序</summary>
        public Int32 Order
        {
            get { return _Order; }
            set { _Order = value; }
        }

		/// <summary>
		/// 构造函数
		/// </summary>
        /// <param name="name">字段名</param>
        public BindColumnAttribute(String name)
		{
            Name = name;
		}

        /// <summary>
        /// 检索应用于类型成员的自定义属性。
        /// </summary>
        /// <param name="element"></param>
        /// <returns></returns>
        public static BindColumnAttribute GetCustomAttribute(MemberInfo element)
        {
            return GetCustomAttribute(element, typeof(BindColumnAttribute)) as BindColumnAttribute;
        }
    }
}