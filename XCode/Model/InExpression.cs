using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NewLife;
using NewLife.Reflection;
using XCode.Configuration;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>格式化表达式。通过字段、格式化字符串和右值去构建表达式</summary>
    /// <remarks>右值可能为空，比如{0} Is Null</remarks>
    public class InExpression : Expression
    {
        #region 属性
        /// <summary>字段</summary>
        public FieldItem Field { get; set; }

        /// <summary>格式化字符串</summary>
        public String Format { get; set; }

        /// <summary>操作数</summary>
        public Object Value { get; set; }

        /// <summary>是否为空</summary>
        public override Boolean IsEmpty => Field == null || Format.IsNullOrWhiteSpace();
        #endregion

        #region 构造
        /// <summary>构造格式化表达式</summary>
        /// <param name="field"></param>
        /// <param name="format"></param>
        /// <param name="value"></param>
        public InExpression(FieldItem field, String format, Object value)
        {
            Field = field;
            Format = format;
            Value = value;
        }
        #endregion

        #region 输出
        /// <summary>已重载。输出字段表达式的字符串形式</summary>
        /// <param name="db">数据库</param>
        /// <param name="builder">字符串构建器</param>
        /// <param name="ps">参数字典</param>
        /// <returns></returns>
        public override void GetString(IDatabase db, StringBuilder builder, IDictionary<String, Object> ps)
        {
            if (Field == null || Format.IsNullOrWhiteSpace()) return;

            // 部分场景外部未能传入数据库，此时内部尽力获取
            if (db == null) db = Field?.Factory.Session.Dal.Db;

            var column = Field.Field;
            var columnName = db.FormatName(Field.Field);

            // 非参数化
            if (ps == null)
            {
                // 可能不需要参数，比如 Is Null
                var val = "";
                if (Format.Contains("{1}"))
                {
                    if (Value is SelectBuilder sb)
                        val = sb;
                    else if (Value is IList<Object> ems)
                        val = ems.Join(",", e => db.FormatValue(column, e));
                    else if (Value is String)
                    {
                        var list = (Value + "").Split(',', StringSplitOptions.RemoveEmptyEntries).ToList();
                        list.RemoveAll(e => (e + "").Trim().IsNullOrEmpty() || e.Contains("%")); //处理类似 in("xxx,xxx,xxx"),和 like "%,xxxx,%" 这两种情况下无法正常格式化查询字符串
                        val = list.Count > 1 ? list.Join(",", e => db.FormatValue(column, e)) : db.FormatValue(column, Value);
                    }
                    else
                        val = db.FormatValue(column, Value);
                }

                builder.AppendFormat(Format, columnName, val);
                return;
            }

            var type = Field.Type;
            if (type.IsEnum) type = typeof(Int32);

            // 特殊处理In操作
            if (Format.Contains(" In("))
            {
                // String/SelectBuilder 不走参数化
                if (Value is String)
                {
                    var val = db.FormatValue(column, Value);
                    builder.AppendFormat(Format, columnName, val);
                    return;
                }
                if (Value is SelectBuilder)
                {
                    builder.AppendFormat(Format, columnName, Value);
                    return;
                }

                // 序列需要多参数
                if (Value is IEnumerable ems)
                {
                    var k = 1;
                    var pns = new List<String>();
                    foreach (var item in ems)
                    {
                        var name = Field.Name + k;
                        var i = 2;
                        while (ps.ContainsKey(name)) name = Field.Name + k + i++;
                        k++;

                        ps[name] = item.ChangeType(type);

                        pns.Add(db.FormatParameterName(name));
                    }
                    builder.AppendFormat(Format, columnName, pns.Join());

                    return;
                }
            }

            // 可能不需要参数，比如 Is Null
            if (Format.Contains("{1}"))
            {
                // 参数化处理
                var name = Field.Name;

                var i = 2;
                while (ps.ContainsKey(name)) name = Field.Name + i++;

                // 数值留给字典
                ps[name] = Value.ChangeType(type);

                builder.AppendFormat(Format, columnName, db.FormatParameterName(name));
            }
            else
            {
                builder.AppendFormat(Format, columnName);
            }
        }
        #endregion
    }
}