using System;
using System.ComponentModel;
using System.Reflection;
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
        public String Description
        {
            get
            {
                if (_Description != null && !String.IsNullOrEmpty(_Description.Description)) return _Description.Description;
                if (Column != null && !String.IsNullOrEmpty(Column.Description)) return Column.Description;
                return null;
            }
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
        [Obsolete("请改为使用Description属性！")]
        public String DisplayName
        {
            get
            {
                //if (Description != null && !String.IsNullOrEmpty(Description.Description)) return Description.Description;
                //if (Column != null && !String.IsNullOrEmpty(Column.Description)) return Column.Description;

                return Description;
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

        private IDataColumn _Field;
        /// <summary>字段</summary>
        public IDataColumn Field
        {
            get { return _Field; }
            //set { _Field = value; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="property"></param>
        public FieldItem(PropertyInfo property)
        {
            if (property == null) throw new ArgumentNullException("property");

            Property = property;
            Column = BindColumnAttribute.GetCustomAttribute(Property);
            DataObjectField = DataObjectAttribute.GetCustomAttribute(Property, typeof(DataObjectFieldAttribute)) as DataObjectFieldAttribute;
            _Description = DescriptionAttribute.GetCustomAttribute(Property, typeof(DescriptionAttribute)) as DescriptionAttribute;
        }
        #endregion

        #region 方法
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (String.IsNullOrEmpty(Description))
                return Name;
            else
                return String.Format("{1}（{0}）", Name, Description);
        }

        /// <summary>
        /// 填充到XField中去
        /// </summary>
        /// <param name="field"></param>
        public void Fill(IDataColumn field)
        {
            _Field = field;

            if (field == null) return;

            XField xf = field as XField;
            if (xf == null) return;

            xf.Name = ColumnName;
            xf.DataType = Property.PropertyType;
            xf.Description = Description;

            if (Column != null)
            {
                xf.ID = Column.Order;
                xf.RawType = Column.RawType;
                xf.Precision = Column.Precision;
                xf.Scale = Column.Scale;
                xf.IsUnicode = Column.IsUnicode;
                xf.Default = Column.DefaultValue;
            }
            if (DataObjectField != null)
            {
                xf.Length = DataObjectField.Length;
                xf.Identity = DataObjectField.IsIdentity;
                xf.PrimaryKey = DataObjectField.PrimaryKey;
                xf.Nullable = DataObjectField.IsNullable;
            }
        }
        #endregion
    }
}