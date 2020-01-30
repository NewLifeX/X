using System;
using System.Collections;
using System.Collections.Generic;
using NewLife.Reflection;
using XCode.Configuration;

namespace XCode.Model
{
    /// <summary>查询条件构建器。主要用于构建数据权限等扩展性查询</summary>
    /// <remarks>
    /// 输入文本型变量表达式，分析并计算得到条件表达式。
    /// 例如：
    /// 输入 CreateUserID={$User.ID}， 输出 _.CreateUserID==Data["User"].GetValue("ID")
    /// 输入 StartSiteId in {#SiteIds} or CityId={#CityId}，输出 _.StartSiteId.In(Data2["SiteIds"]) | _.CityId==Data2["CityId"]
    /// </remarks>
    public class WhereBuilder
    {
        #region 属性
        /// <summary>实体工厂</summary>
        public IEntityOperate Factory { get; set; }

        /// <summary>表达式语句</summary>
        public String Expression { get; set; }

        /// <summary>数据源。{$name}访问</summary>
        public IDictionary<String, Object> Data { get; set; }

        /// <summary>第二数据源。{#name}访问</summary>
        public IDictionary<String, Object> Data2 { get; set; }
        #endregion

        #region 构造
        #endregion

        #region 方法
        /// <summary>计算获取表达式</summary>
        /// <returns></returns>
        public Expression GetExpression()
        {
            var fact = Factory;
            if (fact == null) throw new ArgumentNullException(nameof(Factory));

            var exp = Expression;
            if (exp.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Expression));

            // 分解表达式。不支持括号
            return Parse(exp);
        }

        /// <summary>递归分解表达式</summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        protected internal virtual Expression Parse(String exp)
        {
            // StartSiteId in {#SiteIds} or CityId={#CityId}

            // 与 运算
            var p = exp.IndexOf(" and ", StringComparison.OrdinalIgnoreCase);
            if (p > 0)
            {
                var left = Create(exp.Substring(0, p));
                var right = Parse(exp.Substring(p + 5));

                return new WhereExpression(left, Operator.And, right);
            }

            // 或 运算
            p = exp.IndexOf(" or ", StringComparison.OrdinalIgnoreCase);
            if (p > 0)
            {
                var left = Create(exp.Substring(0, p));
                var right = Parse(exp.Substring(p + 4));

                return new WhereExpression(left, Operator.Or, right);
            }

            // 作为字符串表达式，无法细化分解
            return Create(exp);
        }

        private Expression Create(String exp)
        {
            // CreateUserID={$User.ID}
            // StartSiteId in {#SiteIds} or CityId={#CityId}

            // 等号运算
            var p = exp.IndexOf('=');
            if (p >= 0)
            {
                var name = exp.Substring(0, p).Trim();
                var value = exp.Substring(p + 1).Trim();
                if (!name.EndsWithIgnoreCase("<", ">", "!"))
                {
                    var fi = Factory.Table.FindByName(name) as FieldItem;
                    if (fi == null) throw new XCodeException($"无法识别表达式[{exp}]中的字段[{name}]，实体类[{Factory.EntityType.FullName}]中没有该字段");

                    var val = GetValue(value);
                    return fi.Equal(val);
                }
            }

            // 集合运算
            p = exp.IndexOf(" in", StringComparison.OrdinalIgnoreCase);
            if (p >= 0)
            {
                var name = exp.Substring(0, p).Trim();
                var value = exp.Substring(p + 3).Trim();

                var fi = Factory.Table.FindByName(name) as FieldItem;
                if (fi == null) throw new XCodeException($"无法识别表达式[{exp}]中的字段[{name}]，实体类[{Factory.EntityType.FullName}]中没有该字段");

                var val = GetValue(value.TrimStart('(').TrimEnd(')'));
                if (val is String s) return fi.In(s);
                if (val is IEnumerable e) return fi.In(e);

                throw new XCodeException($"无法识别表达式[{exp}]中的字段[{name}]的数据序列[{val}]");
            }

            // 其它无法识别的运算，只要替换变量
            p = 0;
            var str = exp;
            while (true)
            {
                var s = str.IndexOf("{$", p);
                if (s < 0) s = str.IndexOf("{#", p);
                if (s < 0) break;

                var e = str.IndexOf('}', s);
                if (e < 0) break;

                var name = str.Substring(s, e - s + 1);
                var val = GetValue(name);

                // 替换
                exp = exp.Replace(name, val + "");

                p = e + 1;
            }

            return new Expression(exp);
        }

        private Object GetValue(String exp)
        {
            if (exp.IsNullOrEmpty()) return null;

            if (exp[0] == '{' && exp[exp.Length - 1] == '}')
            {
                var dt = Data;
                var source = "Data";
                if (exp.StartsWith("{#"))
                {
                    dt = Data2;
                    source = "Data2";
                }

                if (dt == null) throw new ArgumentException("缺少数据源", source);

                var key = exp.Substring(2, exp.Length - 3);
                if (!key.Contains("."))
                {
                    // 普通变量
                    if (!dt.TryGetValue(key, out var value)) throw new ArgumentException($"数据源中缺少数据[{key}]", source);

                    return value;
                }
                else
                {
                    // 多层变量
                    var ss = key.Split('.');
                    if (!dt.TryGetValue(ss[0], out var value)) throw new ArgumentException($"数据源中缺少数据[{key}]", source);

                    for (var i = 1; i < ss.Length; i++)
                    {
                        value = value.GetValue(ss[i]);
                    }

                    return value;
                }
            }

            return exp;
        }
        #endregion
    }
}