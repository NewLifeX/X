using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using XCode.DataAccessLayer;

namespace XCode.Configuration
{
    /// <summary>数据属性元数据以及特性</summary>
    public class FieldItem
    {
        #region 属性
        private PropertyInfo _Property;
        /// <summary>属性元数据</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("该成员在后续版本中将不再被支持！")]
        public PropertyInfo Property
        {
            get { return _Property; }
            set { _Property = value; }
        }

        private BindColumnAttribute _Column;
        /// <summary>绑定列特性</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("该成员在后续版本中将不再被支持！")]
        public BindColumnAttribute Column
        {
            get { return _Column; }
            set { _Column = value; }
        }

        private DataObjectFieldAttribute _DataObjectField;
        /// <summary>数据字段特性</summary>
        [EditorBrowsable(EditorBrowsableState.Never)]
        [Obsolete("该成员在后续版本中将不再被支持！")]
        public DataObjectFieldAttribute DataObjectField
        {
            get { return _DataObjectField; }
            set { _DataObjectField = value; }
        }

        private DescriptionAttribute _Description;
        /// <summary>备注</summary>
        public String Description
        {
            get
            {
                if (_Description != null && !String.IsNullOrEmpty(_Description.Description)) return _Description.Description;
                if (_Column != null && !String.IsNullOrEmpty(_Column.Description)) return _Column.Description;
                return null;
            }
        }
        #endregion

        #region 扩展属性
        /// <summary>属性名</summary>
        public String Name { get { return _Property == null ? null : _Property.Name; } }

        /// <summary>属性类型</summary>
        public Type Type { get { return _Property == null ? null : _Property.PropertyType; } }

        /// <summary>属性类型</summary>
        internal Type DeclaringType { get { return _Property == null ? null : _Property.DeclaringType; } }

        /// <summary>是否标识列</summary>
        public Boolean IsIdentity { get { return _DataObjectField == null ? false : _DataObjectField.IsIdentity; } }

        /// <summary>是否主键</summary>
        public Boolean PrimaryKey { get { return _DataObjectField == null ? false : _DataObjectField.PrimaryKey; } }

        /// <summary>是否允许空</summary>
        public Boolean IsNullable { get { return _DataObjectField == null ? false : _DataObjectField.IsNullable; } }

        /// <summary>长度</summary>
        public Int32 Length { get { return _DataObjectField == null ? 0 : _DataObjectField.Length; } }

        /// <summary>是否数据绑定列</summary>
        internal Boolean IsDataObjectField { get { return _DataObjectField != null; } }

        /// <summary>显示名。如果备注不为空则采用备注，否则采用属性名</summary>
        //[Obsolete("请改为使用Description属性！")]
        public String DisplayName
        {
            get
            {
                //if (Description != null && !String.IsNullOrEmpty(Description.Description)) return Description.Description;
                //if (_Column != null && !String.IsNullOrEmpty(_Column.Description)) return _Column.Description;

                return !String.IsNullOrEmpty(Description) ? Description : Name;
            }
        }

        /// <summary>字段名要过滤掉的标识符，考虑MSSQL、MySql、SQLite、Oracle等</summary>
        static Char[] COLUMNNAME_FLAG = new Char[] { '[', ']', '\'', '"', '`' };

        private String _ColumnName;
        /// <summary>用于数据绑定的字段名。
        /// 默认使用BindColumn特性中指定的字段名，如果没有指定，则使用属性名。
        /// </summary>
        public String ColumnName
        {
            get
            {
                if (_ColumnName != null) return _ColumnName;

                // 字段名可能两边带有方括号等标识符
                if (_Column != null && !String.IsNullOrEmpty(_Column.Name))
                    _ColumnName = _Column.Name.Trim(COLUMNNAME_FLAG);
                else
                    _ColumnName = _Property.Name;

                return _ColumnName;
            }
        }

        /// <summary>默认值</summary>
        public String DefaultValue { get { return _Column == null ? null : _Column.DefaultValue; } }

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

        /// <summary>实体操作者</summary>
        public IEntityOperate Factory
        {
            get
            {
                Type type = Table.EntityType;
                if (type.IsInterface) return null;

                return EntityFactory.CreateOperate(type);
            }
        }
        #endregion

        #region 构造
        /// <summary>构造函数</summary>
        /// <param name="table"></param>
        /// <param name="property">属性</param>
        public FieldItem(TableItem table, PropertyInfo property)
        {
            if (property == null) throw new ArgumentNullException("property");

            _Table = table;

            _Property = property;
            _Column = BindColumnAttribute.GetCustomAttribute(_Property);
            _DataObjectField = DataObjectAttribute.GetCustomAttribute(_Property, typeof(DataObjectFieldAttribute)) as DataObjectFieldAttribute;
            _Description = DescriptionAttribute.GetCustomAttribute(_Property, typeof(DescriptionAttribute)) as DescriptionAttribute;
        }
        #endregion

        #region 方法
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            //if (String.IsNullOrEmpty(Description))
            //    return Name;
            //else
            //    return String.Format("{1}（{0}）", Name, Description);

            // 为了保持兼容旧的_.Name等代码，必须只能返回字段名
            return ColumnName;
        }

        /// <summary>填充到XField中去</summary>
        /// <param name="field">字段</param>
        public void Fill(IDataColumn field)
        {
            _Field = field;

            if (field == null) return;

            IDataColumn xf = field;
            if (xf == null) return;

            xf.ColumnName = ColumnName;
            xf.Name = Name;
            xf.DataType = _Property.PropertyType;
            xf.Description = Description;

            if (_Column != null)
            {
                xf.ID = _Column.Order;
                xf.RawType = _Column.RawType;
                xf.Precision = _Column.Precision;
                xf.Scale = _Column.Scale;
                xf.IsUnicode = _Column.IsUnicode;
                xf.Default = _Column.DefaultValue;

                // 特别处理，兼容旧版本
                if (xf.DataType == typeof(Decimal))
                {
                    if (xf.Precision == 0) xf.Precision = 18;
                }
            }
            if (_DataObjectField != null)
            {
                xf.Length = _DataObjectField.Length;
                xf.Identity = _DataObjectField.IsIdentity;
                xf.PrimaryKey = _DataObjectField.PrimaryKey;
                xf.Nullable = _DataObjectField.IsNullable;
            }
        }

        /// <summary>建立表达式</summary>
        /// <param name="action"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public FieldExpression CreateExpression(String action, Object value)
        {
            return new FieldExpression { Field = this, Action = action, Value = value };
            //var op = Factory;
            //String sql = null;
            //String name = op.FormatName(ColumnName);
            //if (!String.IsNullOrEmpty(action) && action.Contains("{0}"))
            //{
            //    if (action.Contains("%"))
            //        sql = name + " Like " + op.FormatValue(this, String.Format(action, value));
            //    else
            //        sql = name + String.Format(action, op.FormatValue(this, value));
            //}
            //else
            //{
            //    // 右值本身就是FieldItem，属于对本表字段进行操作
            //    //if (value is FieldItem)
            //    //    sql = String.Format("{0}{1}{2}", name, action, op.FormatName((value as FieldItem).ColumnName));
            //    // 减少一步类型转换
            //    var fi = value as FieldItem;
            //    if (fi != null)
            //        sql = String.Format("{0}{1}{2}", name, action, op.FormatName(fi.ColumnName));
            //    else
            //        sql = String.Format("{0}{1}{2}", name, action, op.FormatValue(this, value));
            //}
            //return new WhereExpression(sql);
        }
        #endregion

        #region 基本运算
        /// <summary>等于</summary>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression Equal(object value) { return MakeCondition(this, value, "="); }

        /// <summary>不等于</summary>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression NotEqual(object value) { return MakeCondition(this, value, "<>"); }

        /// <summary>以某个字符串开始,{0}%操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression StartsWith(Object value)
        {
            if (value == null || value + "" == "") return new Expression();

            return CreateExpression("{0}%", value);
        }

        /// <summary>以某个字符串结束，%{0}操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression EndsWith(Object value)
        {
            if (value == null || value + "" == "") return new Expression();

            return CreateExpression("%{0}", value);
        }

        /// <summary>包含某个字符串，%{0}%操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression Contains(Object value)
        {
            if (value == null || value + "" == "") return new Expression();

            return CreateExpression("%{0}%", value);
        }

        /// <summary>In操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="value">逗号分割的数据</param>
        /// <returns></returns>
        public Expression In(String value)
        {
            if (String.IsNullOrEmpty(value)) return new Expression();

            //return new WhereExpression(String.Format("{0} In({1})", Factory.FormatName(ColumnName), value));
            return CreateExpression(" In({0})", value);
        }

        /// <summary>In操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接。只有一项时转为等于</remarks>
        /// <param name="value">枚举数据，会转化为字符串</param>
        /// <returns></returns>
        public Expression In(IEnumerable value) { return _In(value, true); }

        Expression _In(IEnumerable value, Boolean flag)
        {
            if (value == null) return new Expression();
            //if (value == null) throw new ArgumentNullException("value");

            var op = Factory;
            var name = op.FormatName(ColumnName);

            var vs = new List<Object>();
            var list = new List<String>();
            foreach (var item in value)
            {
                // 避免重复项
                if (vs.Contains(item)) continue;
                vs.Add(item);

                // 格式化数值
                var str = op.FormatValue(this, item);
                list.Add(str);
            }
            if (list.Count <= 0) return new Expression();
            //if (list.Count <= 0) throw new ArgumentNullException("value");

            // 如果In操作且只有一项，修改为等于
            if (list.Count == 1)
            {
                if (flag)
                    return CreateExpression("=", vs[0]);
                else
                    return CreateExpression("<>", vs[0]);
            }

            //return new WhereExpression(String.Format("{0}{2} In({1})", name, String.Join(",", list.ToArray()), flag ? "" : " Not"));
            if (flag)
                return CreateExpression(" In({0})", list.Join(","));
            else
                return CreateExpression(" Not In({0})", list.Join(","));
        }

        /// <summary>NotIn操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression NotIn(String value)
        {
            if (String.IsNullOrEmpty(value)) return new Expression();

            //return new WhereExpression(String.Format("{0} Not In({1})", Factory.FormatName(ColumnName), value));
            return CreateExpression(" Not In({0})", value);
        }

        /// <summary>NotIn操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接。只有一项时修改为不等于</remarks>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression NotIn(IEnumerable value) { return _In(value, false); }

        /// <summary>IsNull操作，不为空，一般用于字符串，但不匹配0长度字符串</summary>
        /// <returns></returns>
        public Expression IsNull()
        {
            //return new WhereExpression(String.Format("{0} Is Null", Factory.FormatName(ColumnName)));
            return new Expression("{0} Is Null".F(Factory.FormatName(ColumnName)));
        }

        /// <summary>NotIn操作</summary>
        /// <returns></returns>
        public Expression NotIsNull()
        {
            //return new WhereExpression(String.Format("Not {0} Is Null", Factory.FormatName(ColumnName)));
            return new Expression("Not {0} Is Null".F(Factory.FormatName(ColumnName)));
        }
        #endregion

        #region 复杂运算
        /// <summary>IsNullOrEmpty操作，用于空或者0长度字符串</summary>
        /// <returns></returns>
        public Expression IsNullOrEmpty() { return IsNull() | (Equal("")); }

        /// <summary>NotIsNullOrEmpty操作</summary>
        /// <returns></returns>
        public Expression NotIsNullOrEmpty() { return NotIsNull() & (NotEqual("")); }

        /// <summary>是否True或者False/Null，参数决定两组之一</summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public Expression IsTrue(Boolean? flag)
        {
            if (flag == null) return null;

            var f = flag.Value;
            if (f) return Equal(true);

            if (this.Type == typeof(Boolean) && !IsNullable) return Equal(false);

            return NotEqual(true) | IsNull();
        }

        /// <summary>是否False或者True/Null，参数决定两组之一</summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public Expression IsFalse(Boolean? flag)
        {
            if (flag == null) return null;

            var f = flag.Value;
            if (!f) return Equal(false);

            if (this.Type == typeof(Boolean) && !IsNullable) return Equal(true);

            return NotEqual(false) | IsNull();
        }

        /// <summary>时间专用区间函数</summary>
        /// <param name="start">起始时间，大于等于</param>
        /// <param name="end">结束时间，小于。如果是日期，则加一天</param>
        /// <returns></returns>
        public Expression Between(DateTime start, DateTime end)
        {
            if (start <= DateTime.MinValue)
            {
                if (end <= DateTime.MinValue) return null;

                // 如果只有日期，则加一天，表示包含这一天
                if (end == end.Date) end = end.AddDays(1);

                return this < end;
            }
            else
            {
                var exp = this >= start;
                if (end <= DateTime.MinValue) return exp;

                // 如果只有日期，则加一天，表示包含这一天
                if (end == end.Date) end = end.AddDays(1);

                return exp & this < end;
            }
        }
        #endregion

        #region 重载运算符
        /// <summary>大于</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator >(FieldItem field, Object value) { return MakeCondition(field, value, ">"); }

        /// <summary>小于</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator <(FieldItem field, Object value) { return MakeCondition(field, value, "<"); }

        /// <summary>大于等于</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator >=(FieldItem field, Object value) { return MakeCondition(field, value, ">="); }

        /// <summary>小于等于</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator <=(FieldItem field, Object value) { return MakeCondition(field, value, "<="); }

        internal static Expression MakeCondition(FieldItem field, Object value, String action)
        {
            //IEntityOperate op = EntityFactory.CreateOperate(field.Table.EntityType);
            //return new WhereExpression(String.Format("{0}{1}{2}", op.FormatName(field.ColumnName), action, op.FormatValue(field, value)));
            return field == null ? new Expression() : field.CreateExpression(action, value);
        }
        #endregion

        #region 排序
        /// <summary>升序</summary>
        /// <returns></returns>
        public ConcatExpression Asc() { return new ConcatExpression(Factory.FormatName(ColumnName)); }

        /// <summary>降序</summary>
        /// <remarks>感谢 树懒（303409914）发现这里的错误</remarks>
        /// <returns></returns>
        public ConcatExpression Desc() { return new ConcatExpression(Factory.FormatName(ColumnName) + " Desc"); }
        #endregion

        #region 分组选择
        /// <summary>分组</summary>
        /// <returns></returns>
        public ConcatExpression GroupBy() { return new ConcatExpression(String.Format("Group By {0}", Factory.FormatName(ColumnName))); }

        public ConcatExpression Aggregate(String action, String newName)
        {
            var name = Factory.FormatName(ColumnName);
            if (String.IsNullOrEmpty(newName))
                newName = name;
            else
                newName = Factory.FormatName(newName);

            return new ConcatExpression(String.Format("{2}({0}) as {1}", name, newName, action));
        }

        /// <summary>数量</summary>
        /// <param name="newName">聚合后as的新名称，默认空，表示跟前面字段名一致</param>
        /// <returns></returns>
        public ConcatExpression Count(String newName = null) { return Aggregate("Count", newName); }

        /// <summary>求和</summary>
        /// <param name="newName">聚合后as的新名称，默认空，表示跟前面字段名一致</param>
        /// <returns></returns>
        public ConcatExpression Sum(String newName = null) { return Aggregate("Sum", newName); }

        /// <summary>最小值</summary>
        /// <param name="newName">聚合后as的新名称，默认空，表示跟前面字段名一致</param>
        /// <returns></returns>
        public ConcatExpression Min(String newName = null) { return Aggregate("Min", newName); }

        /// <summary>最大值</summary>
        /// <param name="newName">聚合后as的新名称，默认空，表示跟前面字段名一致</param>
        /// <returns></returns>
        public ConcatExpression Max(String newName = null) { return Aggregate("Max", newName); }
        #endregion

        #region 类型转换
        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator String(FieldItem obj)
        {
            return !obj.Equals(null) ? obj.ColumnName : null;
        }
        #endregion
    }

    /// <summary>继承FieldItem，仅仅为了重载==和!=运算符</summary>
    [EditorBrowsable(EditorBrowsableState.Never)]
    public class Field : FieldItem
    {
        #region 构造
        /// <summary>构造函数</summary>
        /// <param name="table"></param>
        /// <param name="property">属性</param>
        public Field(TableItem table, PropertyInfo property) : base(table, property) { }
        #endregion

        /// <summary>等于</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator ==(Field field, Object value) { return field.Equal(value); }

        /// <summary>不等于</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator !=(Field field, Object value) { return field.NotEqual(value); }

        /// <summary>重写一下</summary>
        /// <returns></returns>
        public override int GetHashCode() { return base.GetHashCode(); }

        /// <summary>重写一下</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override bool Equals(object obj) { return base.Equals(obj); }

        #region 类型转换
        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator String(Field obj)
        {
            return !obj.Equals(null) ? obj.ColumnName : null;
        }
        #endregion
    }
}