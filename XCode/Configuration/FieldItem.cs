using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using XCode.DataAccessLayer;

namespace XCode.Configuration
{
    /// <summary>
    /// 数据属性元数据以及特性
    /// </summary>
    public class FieldItem
    {
        #region 属性
        private PropertyInfo _Property;
        /// <summary>属性元数据</summary>
        public PropertyInfo Property
        {
            get { return _Property; }
            private set { _Property = value; }
        }

        private BindColumnAttribute _Column;
        /// <summary>绑定列特性</summary>
        public BindColumnAttribute Column
        {
            get { return _Column; }
            private set { _Column = value; }
        }

        private DataObjectFieldAttribute _DataObjectField;
        /// <summary>数据字段特性</summary>
        public DataObjectFieldAttribute DataObjectField
        {
            get { return _DataObjectField; }
            private set { _DataObjectField = value; }
        }

        private DescriptionAttribute _Description;
        /// <summary>数据字段特性</summary>
        public DescriptionAttribute Description
        {
            get { return _Description; }
            private set { _Description = value; }
        }
        #endregion

        #region 扩展属性
        /// <summary>属性名</summary>
        public String Name { get { return Property == null ? null : Property.Name; } }

        /// <summary>属性类型</summary>
        public Type Type { get { return Property == null ? null : Property.PropertyType; } }

        /// <summary>是否标识列</summary>
        public Boolean IsIdentity { get { return DataObjectField == null ? false : DataObjectField.IsIdentity; } }

        /// <summary>是否主键</summary>
        public Boolean PrimaryKey { get { return DataObjectField == null ? false : DataObjectField.PrimaryKey; } }

        /// <summary>是否允许空</summary>
        public Boolean IsNullable { get { return DataObjectField == null ? false : DataObjectField.IsNullable; } }

        /// <summary>显示名</summary>
        public String DisplayName
        {
            get
            {
                //if (Column == null || String.IsNullOrEmpty(Column.Description)) return "";
                //return Column.Description;

                if (Description != null && !String.IsNullOrEmpty(Description.Description)) return Description.Description;
                if (Column != null && !String.IsNullOrEmpty(Column.Description)) return Column.Description;

                return null;
            }
        }

        /// <summary>字段名要过滤掉的标识符，考虑MSSQL、MySql、SQLite、Oracle等</summary>
        static Char[] COLUMNNAME_FLAG = new Char[] { '[', ']', '\'', '"', '`' };

        /// <summary>
        /// 用于数据绑定的字段名。
        /// 默认使用BindColumn特性中指定的字段名，如果没有指定，则使用属性名。
        /// </summary>
        public String ColumnName
        {
            get
            {
                // 字段名可能两边带有方括号等标识符
                if (Column != null && !String.IsNullOrEmpty(Column.Name))
                    return Column.Name.Trim(COLUMNNAME_FLAG);
                else
                    return Property.Name;
            }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="pi"></param>
        public FieldItem(PropertyInfo pi)
        {
            Property = pi;
            Column = BindColumnAttribute.GetCustomAttribute(Property);
            DataObjectField = DataObjectAttribute.GetCustomAttribute(Property, typeof(DataObjectFieldAttribute)) as DataObjectFieldAttribute;
            Description = DescriptionAttribute.GetCustomAttribute(Property, typeof(DescriptionAttribute)) as DescriptionAttribute;
        }
        #endregion

        #region 方法
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Name;
        }

        /// <summary>
        /// 填充到XField中去
        /// </summary>
        /// <param name="field"></param>
        public void Fill(XField field)
        {
            field.ID = Column.Order;
            field.Name = ColumnName;
            field.RawType = Column.RawType;
            field.DataType = Property.PropertyType;
            field.Description = Column.Description;
            field.Length = DataObjectField.Length;
            field.Precision = Column.Precision;
            field.Scale = Column.Scale;
            field.IsUnicode = Column.IsUnicode;
            field.Identity = DataObjectField.IsIdentity;
            field.PrimaryKey = DataObjectField.PrimaryKey;
            field.Nullable = DataObjectField.IsNullable;
            field.Default = Column.DefaultValue;
        }
        #endregion
    }
}