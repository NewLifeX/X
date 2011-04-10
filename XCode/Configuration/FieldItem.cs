using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.ComponentModel;
using XCode.DataAccessLayer;
using XCode.Exceptions;

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

        #region 静态
        private static Dictionary<Type, ReadOnlyList<FieldItem>> _Fields = new Dictionary<Type, ReadOnlyList<FieldItem>>();
        /// <summary>
        /// 取得指定类的帮定到数据表字段的属性。
        /// 某些数据字段可能只是用于展现数据，并不帮定到数据表字段，
        /// 区分的方法就是，DataObjectField属性是否为空。
        /// 静态缓存。
        /// </summary>
        /// <param name="t">实体类型</param>
        /// <returns>帮定到数据表字段的属性对象列表</returns>
        public static List<FieldItem> Fields(Type t)
        {
            if (_Fields.ContainsKey(t) && !_Fields[t].Changed) return _Fields[t];
            lock (_Fields)
            {
                if (_Fields.ContainsKey(t))
                {
                    if (_Fields[t].Changed) _Fields[t] = _Fields[t].Keep();
                    return _Fields[t];
                }

                List<FieldItem> cFields = AllFields(t);
                cFields = cFields.FindAll(delegate(FieldItem item) { return item.DataObjectField != null; });
                ReadOnlyList<FieldItem> list = new ReadOnlyList<FieldItem>(cFields);
                _Fields.Add(t, list);
                return list;
            }
        }

        private static Dictionary<Type, ReadOnlyList<FieldItem>> _AllFields = new Dictionary<Type, ReadOnlyList<FieldItem>>();
        /// <summary>
        /// 取得指定类的所有数据属性。
        /// 静态缓存。
        /// </summary>
        /// <param name="t">实体类型</param>
        /// <returns>所有数据属性对象列表</returns>
        public static List<FieldItem> AllFields(Type t)
        {
            if (_AllFields.ContainsKey(t) && !_AllFields[t].Changed) return _AllFields[t];
            lock (_AllFields)
            {
                if (_AllFields.ContainsKey(t))
                {
                    if (_AllFields[t].Changed) _AllFields[t] = _AllFields[t].Keep();
                    return _AllFields[t];
                }

                List<FieldItem> list = new List<FieldItem>();
                PropertyInfo[] pis = t.GetProperties();
                List<String> names = new List<String>();
                foreach (PropertyInfo item in pis)
                {
                    // 排除索引器
                    if (item.GetIndexParameters().Length > 0) continue;

                    list.Add(new FieldItem(item));

                    if (names.Contains(item.Name)) throw new XCodeException(String.Format("{0}类中出现重复属性{1}", t.Name, item.Name));
                    names.Add(item.Name);
                }
                ReadOnlyList<FieldItem> list2 = new ReadOnlyList<FieldItem>(list);
                _AllFields.Add(t, list2);
                return list2;
            }
        }

        private static Dictionary<Type, ReadOnlyList<FieldItem>> _Unique = new Dictionary<Type, ReadOnlyList<FieldItem>>();
        /// <summary>
        /// 唯一键
        /// 如果有标识列，则返回标识列集合；
        /// 否则，返回主键集合。
        /// </summary>
        /// <param name="t">实体类型</param>
        /// <returns>唯一键数组</returns>
        public static List<FieldItem> Unique(Type t)
        {
            if (_Unique.ContainsKey(t) && !_Unique[t].Changed) return _Unique[t];
            lock (_Unique)
            {
                if (_Unique.ContainsKey(t))
                {
                    if (_Unique[t].Changed) _Unique[t] = _Unique[t].Keep();
                    return _Unique[t];
                }

                List<FieldItem> list = new List<FieldItem>();
                foreach (FieldItem fi in Fields(t))
                {
                    if (fi.IsIdentity)
                    {
                        list.Add(fi);
                    }
                }
                if (list.Count < 1) // 没有标识列，使用主键
                {
                    foreach (FieldItem fi in Fields(t))
                    {
                        if (fi.PrimaryKey)
                        {
                            list.Add(fi);
                        }
                    }
                }
                ReadOnlyList<FieldItem> list2 = new ReadOnlyList<FieldItem>(list);
                _Unique.Add(t, list2);
                return list2;
            }
        }
        #endregion
    }
}