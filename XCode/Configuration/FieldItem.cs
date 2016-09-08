﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection;
using XCode.DataAccessLayer;

namespace XCode.Configuration
{
    /// <summary>数据属性元数据以及特性</summary>
    public class FieldItem
    {
        #region 属性
        /// <summary>属性元数据</summary>
        private PropertyInfo _Property;

        /// <summary>绑定列特性</summary>
        private BindColumnAttribute _Column;

        /// <summary>数据字段特性</summary>
        private DataObjectFieldAttribute _DataObjectField;

        private DescriptionAttribute _Description;

        private DisplayNameAttribute _DisplayName;

        /// <summary>备注</summary>
        public String Description { get; internal set; }

        private String _dis;
        /// <summary>说明</summary>
        public String DisplayName
        {
            get
            {
                if (!string.IsNullOrEmpty(_dis)) return _dis;
                var name = Description;
                if (String.IsNullOrEmpty(name)) return Name;

                var p = name.IndexOf("。");
                if (p > 0) name = name.Substring(0, p);

                return name;
            }
            internal set { _dis = value; }
        }

        /// <summary>顺序标识</summary>
        internal Int32 _ID;
        #endregion

        #region 扩展属性
        /// <summary>属性名</summary>
        public String Name { get; internal set; }

        /// <summary>属性类型</summary>
        public Type Type { get; internal set; }

        private Type _DeclaringType;
        /// <summary>声明类型</summary>
        internal Type DeclaringType
        {
            get
            {
                if (_DeclaringType != null) { return _DeclaringType; }
                // 确保动态增加的数据字段得到实体类型
                return Table.EntityType;
            }
            set { _DeclaringType = value; }
        }

        /// <summary>是否标识列</summary>
        public Boolean IsIdentity { get; internal set; }

        /// <summary>是否主键</summary>
        public Boolean PrimaryKey { get; internal set; }

        /// <summary>是否主字段。主字段作为业务主要字段，代表当前数据行意义</summary>
        public Boolean Master { get; private set; }

        /// <summary>是否允许空</summary>
        public Boolean IsNullable { get; internal set; }

        /// <summary>长度</summary>
        public Int32 Length { get; internal set; }

        /// <summary>是否数据绑定列</summary>
        public Boolean IsDataObjectField { get; set; }

        /// <summary>是否动态字段</summary>
        public Boolean IsDynamic { get { return _Property == null; } }

        ///// <summary>显示名。如果备注不为空则采用备注，否则采用属性名</summary>
        //public String DisplayName
        //{
        //    get
        //    {
        //        var name = Description;
        //        if (String.IsNullOrEmpty(name)) return Name;

        //        var p = name.IndexOf("。");
        //        if (p > 0) name = name.Substring(0, p);

        //        return name;
        //    }
        //}

        /// <summary>字段名要过滤掉的标识符，考虑MSSQL、MySql、SQLite、Oracle等</summary>
        static Char[] COLUMNNAME_FLAG = new Char[] { '[', ']', '\'', '"', '`' };

        private String _ColumnName;
        /// <summary>用于数据绑定的字段名</summary>
        /// <remarks>
        /// 默认使用BindColumn特性中指定的字段名，如果没有指定，则使用属性名。
        /// 字段名可能两边带有方括号等标识符
        /// </remarks>
        public String ColumnName { get { return _ColumnName; } set { if (value != null) _ColumnName = value.Trim(COLUMNNAME_FLAG); } }

        /// <summary>默认值</summary>
        public String DefaultValue { get; set; }

        /// <summary>是否只读</summary>
        /// <remarks>set { _ReadOnly = value; } 放出只读属性的设置，比如在编辑页面的时候，有的字段不能修改 如修改用户时  不能修改用户名</remarks>
        public Boolean ReadOnly { get; set; }

        /// <summary>表</summary>
        public TableItem Table { get; internal protected set; }

        /// <summary>字段</summary>
        public IDataColumn Field { get; private set; }

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

        /// <summary>已格式化的字段名，可字节用于SQL中。主要用于处理关键字，比如MSSQL里面的[User]</summary>
        public String FormatedName { get { return Factory.FormatName(ColumnName); } }

        /// <summary>跟当前字段有关系的原始字段</summary>
        public FieldItem OriField { get; internal set; }

        /// <summary>获取映射特性</summary>
        public MapAttribute Map { get; private set; }

        ///// <summary>宽度</summary>
        ///// <remarks>主要用于界面上的控件宽度</remarks>
        //public int Width { get; set; }

        ///// <summary>排序</summary>
        ///// <remarks>主要用于界面上的控件显示排序</remarks>
        //public int Sort { get; set; }

        ///// <summary>跟当前字段有关系的原始字段</summary>
        ///// <remarks>（原始字段，主要是在列表或者是Form里替换字段后可以读取原来的绑定关系）,不敢用OriField，因为暂时没有去查OriField具体的用途。</remarks>
        //public FieldItem OldField { get;  set; }
        #endregion

        #region 构造
        internal FieldItem() { }

        /// <summary>构造函数</summary>
        /// <param name="table"></param>
        /// <param name="property">属性</param>
        public FieldItem(TableItem table, PropertyInfo property)
        {
            Table = table;

            if (property != null)
            {
                _Property = property;
                var dc = _Column = BindColumnAttribute.GetCustomAttribute(property);
                var df = _DataObjectField = property.GetCustomAttribute<DataObjectFieldAttribute>();
                var ds = _Description = property.GetCustomAttribute<DescriptionAttribute>();
                var di = _DisplayName = property.GetCustomAttribute<DisplayNameAttribute>();
                Map = property.GetCustomAttribute<MapAttribute>();
                Name = property.Name;
                Type = property.PropertyType;
                DeclaringType = property.DeclaringType;

                if (df != null)
                {
                    IsIdentity = df.IsIdentity;
                    PrimaryKey = df.PrimaryKey;
                    IsNullable = df.IsNullable;
                    Length = df.Length;

                    IsDataObjectField = true;
                }

                if (dc != null)
                {
                    _ID = dc.Order;
                    Master = dc.Master;
                }

                if (dc != null && !dc.Name.IsNullOrWhiteSpace())
                    ColumnName = dc.Name;
                else
                    ColumnName = Name;

                if (ds != null && !String.IsNullOrEmpty(ds.Description))
                    Description = ds.Description;
                else if (dc != null && !String.IsNullOrEmpty(dc.Description))
                    Description = dc.Description;
                if (di != null && !di.DisplayName.IsNullOrEmpty())
                    DisplayName = di.DisplayName;

                var map = Map;
                if (map == null || map.Provider == null) ReadOnly = !property.CanWrite;
                var ra = property.GetCustomAttribute<ReadOnlyAttribute>();
                if (ra != null) ReadOnly = ra.IsReadOnly;
            }
        }
        #endregion

        #region 方法
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            // 为了保持兼容旧的_.Name等代码，必须只能返回字段名
            return ColumnName;
        }

        /// <summary>填充到XField中去</summary>
        /// <param name="field">字段</param>
        public void Fill(IDataColumn field)
        {
            Field = field;

            if (field == null) return;

            IDataColumn dc = field;
            if (dc == null) return;

            dc.ID = _ID;
            dc.ColumnName = ColumnName;
            dc.Name = Name;
            dc.DataType = Type;
            dc.Description = Description;
            dc.Default = DefaultValue;

            var col = _Column;
            if (col != null)
            {
                dc.RawType = col.RawType;
                dc.Precision = col.Precision;
                dc.Scale = col.Scale;
                dc.IsUnicode = col.IsUnicode;
            }
            else
            {
                dc.IsUnicode = true;
            }

            // 特别处理，兼容旧版本
            if (dc.DataType == typeof(Decimal))
            {
                if (dc.Precision == 0) dc.Precision = 18;
            }

            dc.Length = Length;
            dc.Identity = IsIdentity;
            dc.PrimaryKey = PrimaryKey;
            dc.Nullable = IsNullable;
            dc.Master = Master;
        }

        /// <summary>建立表达式</summary>
        /// <param name="action"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        internal Expression CreateFormatExpression(String action, String value) { return new FormatExpression(this, action, value); }

        internal static Expression CreateFieldExpression(FieldItem field, String action, Object value)
        {
            return field == null ? new Expression() : new FieldExpression(field, action, value);
        }
        #endregion

        #region 基本运算
        /// <summary>等于</summary>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression Equal(object value) { return CreateFieldExpression(this, "=", value); }

        /// <summary>不等于</summary>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression NotEqual(object value) { return CreateFieldExpression(this, "<>", value); }

        Expression CreateLike(String value) { return CreateFormatExpression("{0} Like {1}", Factory.FormatValue(this, value)); }

        /// <summary>以某个字符串开始,{0}%操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression StartsWith(String value)
        {
            if (value == null || value + "" == "") return new Expression();

            return CreateLike("{0}%".F(value));
        }

        /// <summary>以某个字符串结束，%{0}操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression EndsWith(String value)
        {
            if (value == null || value + "" == "") return new Expression();

            return CreateLike("%{0}".F(value));
        }

        /// <summary>包含某个字符串，%{0}%操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression Contains(String value)
        {
            if (value == null || value + "" == "") return new Expression();

            return CreateLike("%{0}%".F(value));
        }

        ///// <summary>In操作。直接使用字符串可能有注入风险</summary>
        ///// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        ///// <param name="value">逗号分割的数据。可能有注入风险</param>
        ///// <returns></returns>
        //[Obsolete("=>In(IEnumerable value)，直接使用字符串参数可能有注入风险")]
        //public Expression In(String value)
        //{
        //    if (String.IsNullOrEmpty(value)) return new Expression();

        //    return CreateFormatExpression("{0} In({1})", Factory.FormatValue(this, value));
        //}

        /// <summary>In操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接。只有一项时转为等于</remarks>
        /// <param name="value">枚举数据，会转化为字符串</param>
        /// <returns></returns>
        public Expression In(IEnumerable value) { return _In(value, true); }

        Expression _In(IEnumerable value, Boolean flag)
        {
            if (value == null) return new Expression();

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

            // 特殊处理枚举全选，如果全选了枚举的所有项，则跳过当前条件构造
            if (vs[0].GetType().IsEnum)
            {
                var es = Enum.GetValues(vs[0].GetType());
                if (es.Length == vs.Count)
                {
                    if (vs.SequenceEqual(es.Cast<Object>())) return new Expression();
                }
            }

            // 如果In操作且只有一项，修改为等于
            if (list.Count == 1) return CreateFieldExpression(this, flag ? "=" : "<>", vs[0]);

            return CreateFormatExpression(flag ? "{0} In({1})" : "{0} Not In({1})", list.Join(","));
        }

        ///// <summary>NotIn操作。直接使用字符串可能有注入风险</summary>
        ///// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        ///// <param name="value">数值</param>
        ///// <returns></returns>
        //[Obsolete("=>NotIn(IEnumerable value)，直接使用字符串参数可能有注入风险")]
        //public Expression NotIn(String value)
        //{
        //    if (String.IsNullOrEmpty(value)) return new Expression();

        //    return CreateFormatExpression("{0} Not In({1})", Factory.FormatValue(this, value));
        //}

        /// <summary>NotIn操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接。只有一项时修改为不等于</remarks>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression NotIn(IEnumerable value) { return _In(value, false); }

        /// <summary>In操作。直接使用字符串可能有注入风险</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="builder">逗号分割的数据。可能有注入风险</param>
        /// <returns></returns>
        public Expression In(SelectBuilder builder)
        {
            if (builder == null) return new Expression();

            return CreateFormatExpression("{0} In({1})", builder);
        }

        /// <summary>NotIn操作。直接使用字符串可能有注入风险</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="builder">数值</param>
        /// <returns></returns>
        public Expression NotIn(SelectBuilder builder)
        {
            if (builder == null) return new Expression();

            return CreateFormatExpression("{0} NOT IN ({1})", builder);
        }

        /// <summary>IsNull操作，不为空，一般用于字符串，但不匹配0长度字符串</summary>
        /// <returns></returns>
        public Expression IsNull() { return CreateFormatExpression("{0} Is Null", null); }

        /// <summary>NotIn操作</summary>
        /// <returns></returns>
        public Expression NotIsNull() { return CreateFormatExpression("Not {0} Is Null", null); }
        #endregion

        #region 复杂运算
        /// <summary>IsNullOrEmpty操作，用于空或者0长度字符串</summary>
        /// <returns></returns>
        public Expression IsNullOrEmpty() { return IsNull() | Equal(""); }

        /// <summary>NotIsNullOrEmpty操作</summary>
        /// <returns></returns>
        public Expression NotIsNullOrEmpty() { return NotIsNull() & NotEqual(""); }

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
        public static Expression operator >(FieldItem field, Object value) { return CreateFieldExpression(field, ">", value); }

        /// <summary>小于</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator <(FieldItem field, Object value) { return CreateFieldExpression(field, "<", value); }

        /// <summary>大于等于</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator >=(FieldItem field, Object value) { return CreateFieldExpression(field, ">=", value); }

        /// <summary>小于等于</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator <=(FieldItem field, Object value) { return CreateFieldExpression(field, "<=", value); }
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

        internal Field(TableItem table, String name, Type type, String description, Int32 length)
        {
            Table = table;

            _ID = table.Fields.Length + 1;

            Name = name;
            ColumnName = name;
            Type = type;
            Description = description;
            Length = length;

            IsNullable = true;
        }
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