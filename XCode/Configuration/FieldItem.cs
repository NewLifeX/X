using System;
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
        private readonly PropertyInfo _Property;

        /// <summary>绑定列特性</summary>
        private readonly BindColumnAttribute _Column;

        /// <summary>数据字段特性</summary>
        private readonly DataObjectFieldAttribute _DataObjectField;

        private readonly DescriptionAttribute _Description;

        private readonly DisplayNameAttribute _DisplayName;

        /// <summary>备注</summary>
        public String Description { get; internal set; }

        private String _dis;
        /// <summary>说明</summary>
        public String DisplayName
        {
            get
            {
                if (!_dis.IsNullOrEmpty()) return _dis;

                var name = Description;
                if (name.IsNullOrEmpty()) return Name;

                var p = name.IndexOf("。");
                if (p > 0) name = name.Substring(0, p);

                return name;
            }
            internal set { _dis = value; }
        }
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
        public Boolean IsDynamic => _Property == null;

        /// <summary>字段名要过滤掉的标识符，考虑MSSQL、MySql、SQLite、Oracle等</summary>
        static readonly Char[] COLUMNNAME_FLAG = new Char[] { '[', ']', '\'', '"', '`' };

        private String _ColumnName;
        /// <summary>用于数据绑定的字段名</summary>
        /// <remarks>
        /// 默认使用BindColumn特性中指定的字段名，如果没有指定，则使用属性名。
        /// 字段名可能两边带有方括号等标识符
        /// </remarks>
        public String ColumnName { get { return _ColumnName; } set { if (value != null) _ColumnName = value.Trim(COLUMNNAME_FLAG); } }

        ///// <summary>默认值</summary>
        //public String DefaultValue { get; set; }

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
                var type = Table.EntityType;
                if (type.IsInterface) return null;

                return EntityFactory.CreateOperate(type);
            }
        }

        /// <summary>已格式化的字段名，可字节用于SQL中。主要用于处理关键字，比如MSSQL里面的[User]</summary>
        public String FormatedName => Factory.FormatName(ColumnName);

        /// <summary>跟当前字段有关系的原始字段</summary>
        public FieldItem OriField { get; internal set; }

        /// <summary>获取映射特性</summary>
        public MapAttribute Map { get; private set; }
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
        public override String ToString() => ColumnName;

        /// <summary>填充到XField中去</summary>
        /// <param name="field">字段</param>
        public void Fill(IDataColumn field)
        {
            Field = field;

            if (field == null) return;

            var dc = field;
            if (dc == null) return;

            dc.ColumnName = ColumnName;
            dc.Name = Name;
            dc.DataType = Type;
            dc.Description = Description;

            var col = _Column;
            if (col != null)
            {
                dc.RawType = col.RawType;
                dc.Precision = col.Precision;
                dc.Scale = col.Scale;
            }

            // 特别处理，兼容旧版本
            if (dc.DataType == typeof(Decimal))
            {
                if (dc.Precision == 0) dc.Precision = 19;
                if (dc.Scale == 0) dc.Scale = 4;
            }

            dc.Length = Length;
            dc.Identity = IsIdentity;
            dc.PrimaryKey = PrimaryKey;
            dc.Nullable = IsNullable;
            dc.Master = Master;
        }

        /// <summary>建立表达式</summary>
        /// <param name="format"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        internal Expression CreateFormat(String format, Object value) => new FormatExpression(this, format, value);

        internal static Expression CreateField(FieldItem field, String action, Object value) => field == null ? new Expression() : new FieldExpression(field, action, value);
        #endregion

        #region 基本运算
        /// <summary>等于</summary>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression Equal(Object value) => CreateField(this, "=", value);

        /// <summary>不等于</summary>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression NotEqual(Object value) => CreateField(this, "<>", value);

        Expression CreateLike(String value) => CreateFormat("{0} Like {1}", value);

        /// <summary>以某个字符串开始,{0}%操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression StartsWith(String value)
        {
            if (Type != typeof(String)) throw new NotSupportedException($"[{nameof(StartsWith)}]函数仅支持字符串字段！");

            if (value == null || value + "" == "") return new Expression();

            return CreateLike("{0}%".F(value));
        }

        /// <summary>以某个字符串结束，%{0}操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression EndsWith(String value)
        {
            if (Type != typeof(String)) throw new NotSupportedException($"[{nameof(EndsWith)}]函数仅支持字符串字段！");

            if (value == null || value + "" == "") return new Expression();

            return CreateLike("%{0}".F(value));
        }

        /// <summary>包含某个字符串，%{0}%操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression Contains(String value)
        {
            if (Type != typeof(String)) throw new NotSupportedException($"[{nameof(Contains)}]函数仅支持字符串字段！");

            if (value == null || value + "" == "") return new Expression();

            return CreateLike("%{0}%".F(value));
        }

        /// <summary>包含某个字符串，%{0}%操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression NotContains(String value)
        {
            if (Type != typeof(String)) throw new NotSupportedException($"[{nameof(NotContains)}]函数仅支持字符串字段！");

            if (value == null || value + "" == "") return new Expression();

            return CreateFormat("{0} Not Like {1}", value);
        }

        /// <summary>In操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接。只有一项时转为等于</remarks>
        /// <param name="value">枚举数据，会转化为字符串</param>
        /// <returns></returns>
        public Expression In(IEnumerable value) => _In(value, true);

        Expression _In(IEnumerable value, Boolean flag)
        {
            if (value == null) return new Expression();

            var op = Factory;
            var name = op.FormatName(ColumnName);

            var vs = new List<Object>();
            var list = new List<Object>();
            foreach (var item in value)
            {
                // 避免重复项
                if (vs.Contains(item)) continue;
                vs.Add(item);

                // 格式化数值
                //var str = op.FormatValue(this, item);
                list.Add(item);
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
            if (list.Count == 1) return CreateField(this, flag ? "=" : "<>", vs[0]);

            return CreateFormat(flag ? "{0} In({1})" : "{0} Not In({1})", list);
        }

        /// <summary>NotIn操作</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接。只有一项时修改为不等于</remarks>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Expression NotIn(IEnumerable value) => _In(value, false);

        /// <summary>In操作。直接使用字符串可能有注入风险</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="child">逗号分割的数据。可能有注入风险</param>
        /// <returns></returns>
        public Expression In(String child)
        {
            if (child == null) return new Expression();

            return CreateFormat("{0} In({1})", child);
        }

        /// <summary>NotIn操作。直接使用字符串可能有注入风险</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="child">数值</param>
        /// <returns></returns>
        public Expression NotIn(String child)
        {
            if (child == null) return new Expression();

            return CreateFormat("{0} Not In({1})", child);
        }

        /// <summary>In操作。直接使用字符串可能有注入风险</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="builder">逗号分割的数据。可能有注入风险</param>
        /// <returns></returns>
        public Expression In(SelectBuilder builder)
        {
            if (builder == null) return new Expression();

            return CreateFormat("{0} In({1})", builder);
        }

        /// <summary>NotIn操作。直接使用字符串可能有注入风险</summary>
        /// <remarks>空参数不参与表达式操作，不生成该部分SQL拼接</remarks>
        /// <param name="builder">数值</param>
        /// <returns></returns>
        public Expression NotIn(SelectBuilder builder)
        {
            if (builder == null) return new Expression();

            return CreateFormat("{0} Not In ({1})", builder);
        }

        /// <summary>IsNull操作，不为空，一般用于字符串，但不匹配0长度字符串</summary>
        /// <returns></returns>
        public Expression IsNull() => CreateFormat("{0} Is Null", null);

        /// <summary>NotIn操作</summary>
        /// <returns></returns>
        public Expression NotIsNull() => CreateFormat("Not {0} Is Null", null);
        #endregion

        #region 复杂运算
        /// <summary>IsNullOrEmpty操作，用于空或者0长度字符串</summary>
        /// <returns></returns>
        public Expression IsNullOrEmpty()
        {
            if (Type != typeof(String)) throw new NotSupportedException($"[{nameof(IsNullOrEmpty)}]函数仅支持字符串字段！");

            return IsNull() | Equal("");
        }

        /// <summary>NotIsNullOrEmpty操作</summary>
        /// <returns></returns>
        public Expression NotIsNullOrEmpty()
        {
            if (Type != typeof(String)) throw new NotSupportedException($"[{nameof(NotIsNullOrEmpty)}]函数仅支持字符串字段！");

            return NotIsNull() & NotEqual("");
        }

        /// <summary>是否True或者False/Null，参数决定两组之一</summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public Expression IsTrue(Boolean? flag)
        {
            if (Type != typeof(Boolean)) throw new NotSupportedException($"[{nameof(IsTrue)}]函数仅支持布尔型字段！");

            if (flag == null) return null;

            var f = flag.Value;
            if (f) return Equal(true);

            // IsTrue/IsFalse 不再需要判空，因为那样还不如直接使用等于号
            //if (Type == typeof(Boolean) && !IsNullable) return Equal(false);

            return NotEqual(true) | IsNull();
        }

        /// <summary>是否False或者True/Null，参数决定两组之一</summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public Expression IsFalse(Boolean? flag)
        {
            if (Type != typeof(Boolean)) throw new NotSupportedException($"[{nameof(IsFalse)}]函数仅支持布尔型字段！");

            if (flag == null) return null;

            var f = flag.Value;
            if (!f) return Equal(false);

            // IsTrue/IsFalse 不再需要判空，因为那样还不如直接使用等于号
            //if (Type == typeof(Boolean) && !IsNullable) return Equal(true);

            return NotEqual(false) | IsNull();
        }
        #endregion

        #region 重载运算符
        /// <summary>大于</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator >(FieldItem field, Object value) => CreateField(field, ">", value);

        /// <summary>小于</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator <(FieldItem field, Object value) => CreateField(field, "<", value);

        /// <summary>大于等于</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator >=(FieldItem field, Object value) => CreateField(field, ">=", value);

        /// <summary>小于等于</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator <=(FieldItem field, Object value) => CreateField(field, "<=", value);
        #endregion

        #region 类型转换
        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator String(FieldItem obj) => !obj.Equals(null) ? obj.ColumnName : null;
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
        public static Expression operator ==(Field field, Object value) => field.Equal(value);

        /// <summary>不等于</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static Expression operator !=(Field field, Object value) => field.NotEqual(value);

        /// <summary>重写一下</summary>
        /// <returns></returns>
        public override Int32 GetHashCode() => base.GetHashCode();

        /// <summary>重写一下</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public override Boolean Equals(Object obj) => base.Equals(obj);

        #region 类型转换
        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator String(Field obj) => !obj.Equals(null) ? obj.ColumnName : null;
        #endregion
    }
}