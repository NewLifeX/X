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
        private PropertyInfo Property
        {
            get { return _Property; }
            set { _Property = value; }
        }

        private BindColumnAttribute _Column;
        /// <summary>绑定列特性</summary>
        private BindColumnAttribute Column
        {
            get { return _Column; }
            set { _Column = value; }
        }

        private DataObjectFieldAttribute _DataObjectField;
        /// <summary>数据字段特性</summary>
        private DataObjectFieldAttribute DataObjectField
        {
            get { return _DataObjectField; }
            set { _DataObjectField = value; }
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

        /// <summary>属性类型</summary>
        internal Type DeclaringType { get { return Property == null ? null : Property.DeclaringType; } }

        /// <summary>是否标识列</summary>
        public Boolean IsIdentity { get { return DataObjectField == null ? false : DataObjectField.IsIdentity; } }

        /// <summary>是否主键</summary>
        public Boolean PrimaryKey { get { return DataObjectField == null ? false : DataObjectField.PrimaryKey; } }

        /// <summary>是否允许空</summary>
        public Boolean IsNullable { get { return DataObjectField == null ? false : DataObjectField.IsNullable; } }

        /// <summary>是否数据绑定列</summary>
        internal Boolean IsDataObjectField { get { return DataObjectField != null; } }

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

        private String _ColumnName;
        /// <summary>
        /// 用于数据绑定的字段名。
        /// 默认使用BindColumn特性中指定的字段名，如果没有指定，则使用属性名。
        /// </summary>
        public String ColumnName
        {
            get
            {
                if (_ColumnName != null) return _ColumnName;

                // 字段名可能两边带有方括号等标识符
                if (Column != null && !String.IsNullOrEmpty(Column.Name))
                    _ColumnName = Column.Name.Trim(COLUMNNAME_FLAG);
                else
                    _ColumnName = Property.Name;

                return _ColumnName;
            }
        }

        /// <summary>默认值</summary>
        public String DefaultValue { get { return Column == null ? null : Column.DefaultValue; } }

        private TableItem _Table;
        /// <summary>表</summary>
        public TableItem Table
        {
            get { return _Table; }
            //set { _Field = value; }
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
        /// <param name="table"></param>
        /// <param name="property"></param>
        public FieldItem(TableItem table, PropertyInfo property)
        {
            if (property == null) throw new ArgumentNullException("property");

            _Table = table;

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

            IDataColumn xf = field;
            if (xf == null) return;

            xf.Name = ColumnName;
            xf.Alias = Name;
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

        /// <summary>
        /// 建立表达式
        /// </summary>
        /// <param name="action"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public WhereExpression CreateExpression(String action, Object value)
        {
            IEntityOperate op = EntityFactory.CreateOperate(Table.EntityType);
            String sql = null;
            if (!String.IsNullOrEmpty(action) && action.Contains("{0}"))
            {
                if (action.Contains("%"))
                    sql = op.FormatName(ColumnName) + " Like " + op.FormatValue(this, String.Format(action, value));
                else
                    sql = op.FormatName(ColumnName) + String.Format(action, op.FormatValue(this, value));
            }
            else
                sql = String.Format("{0}{1}{2}", op.FormatName(ColumnName), action, op.FormatValue(this, value));
            return new WhereExpression(sql);
        }
        #endregion

        #region 重载运算符
        /// <summary>
        /// 等于
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public WhereExpression Equal(object value) { return MakeCondition(this, value, "=="); }

        /// <summary>
        /// 不等于
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public WhereExpression NotEqual(object value) { return MakeCondition(this, value, "<>"); }

        //public static String operator ==(FieldItem field, Object value) { return MakeCondition(field, value, "=="); }
        //public static String operator !=(FieldItem field, Object value) { return MakeCondition(field, value, "<>"); }

        /// <summary>
        /// 以某个字符串开始
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public WhereExpression StartsWith(Object value) { return CreateExpression("{0}%", value); }

        /// <summary>
        /// 以某个字符串结束
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public WhereExpression EndsWith(Object value) { return CreateExpression("%{0}", value); }

        /// <summary>
        /// 包含某个字符串
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        public WhereExpression Contains(Object value) { return CreateExpression("%{0}%", value); }

        /// <summary>
        /// 大于
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static WhereExpression operator >(FieldItem field, Object value) { return MakeCondition(field, value, ">"); }

        /// <summary>
        /// 小于
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static WhereExpression operator <(FieldItem field, Object value) { return MakeCondition(field, value, "<"); }

        /// <summary>
        /// 大于等于
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static WhereExpression operator >=(FieldItem field, Object value) { return MakeCondition(field, value, ">="); }

        /// <summary>
        /// 小于等于
        /// </summary>
        /// <param name="field"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static WhereExpression operator <=(FieldItem field, Object value) { return MakeCondition(field, value, "<="); }

        static WhereExpression MakeCondition(FieldItem field, Object value, String action)
        {
            //IEntityOperate op = EntityFactory.CreateOperate(field.Table.EntityType);
            //return new WhereExpression(String.Format("{0}{1}{2}", op.FormatName(field.ColumnName), action, op.FormatValue(field, value)));
            return field == null ? new WhereExpression() : field.CreateExpression(action, value);
        }
        #endregion

        #region 类型转换
        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator String(FieldItem obj)
        {
            return !obj.Equals(null) ? obj.ColumnName : null;
        }
        #endregion
    }
}