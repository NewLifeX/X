using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NewLife.Data;
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
        public IExtend Data { get; set; }

        /// <summary>第二数据源。{#name}访问</summary>
        public IExtend Data2 { get; set; }
        #endregion

        #region 构造
        #endregion

        #region 方法
        /// <summary>设置数据源</summary>
        /// <param name="dictionary"></param>
        public void SetData(IDictionary<String, Object> dictionary) => Data = dictionary.ToExtend();

        /// <summary>设置第二数据源</summary>
        /// <param name="dictionary"></param>
        public void SetData2(IDictionary<String, Object> dictionary) => Data2 = dictionary.ToExtend();
        #endregion

        #region 表达式
        /// <summary>计算获取表达式</summary>
        /// <returns></returns>
        public Expression GetExpression()
        {
            var fact = Factory;
            if (fact == null) throw new ArgumentNullException(nameof(Factory));

            var exp = Expression;
            if (exp.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Expression));

            // 分解表达式。不支持括号
            return ParseExpression(exp);
        }

        /// <summary>递归分解表达式</summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected virtual Expression ParseExpression(String value)
        {
            // StartSiteId in {#SiteIds} or CityId={#CityId}

            // 与 运算
            var p = value.IndexOf(" and ", StringComparison.OrdinalIgnoreCase);
            if (p > 0)
            {
                var left = ParseField(value.Substring(0, p));
                var right = ParseExpression(value.Substring(p + 5));

                return new WhereExpression(left, Operator.And, right);
            }

            // 或 运算
            p = value.IndexOf(" or ", StringComparison.OrdinalIgnoreCase);
            if (p > 0)
            {
                var left = ParseField(value.Substring(0, p));
                var right = ParseExpression(value.Substring(p + 4));

                return new WhereExpression(left, Operator.Or, right);
            }

            // 作为字符串表达式，无法细化分解
            return ParseField(value);
        }

        private Expression ParseField(String exp)
        {
            // CreateUserID={$User.ID}
            // StartSiteId in {#SiteIds} or CityId={#CityId}

            // 解析表达式
            var model = Find(exp, new[] { "==", "!=", "<>", "=", " in" });
            if (model != null)
            {
                switch (model.Action)
                {
                    case "==":
                    case "=": return model.Field.Equal(model.Value);
                    case "!=":
                    case "<>": return model.Field.NotEqual(model.Value);
                    case " in":
                        {
                            if (model.Value is String s) return model.Field.In(s);
                            if (model.Value is IEnumerable e) return model.Field.In(e);
                            break;
                        }
                }
            }

            throw new XCodeException($"无法解析表达式[{exp}]");
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
                    //if (!dt.TryGetValue(key, out var value)) throw new ArgumentException($"数据源中缺少数据[{key}]", source);
                    var value = dt[key];

                    return value;
                }
                else
                {
                    // 多层变量
                    var ss = key.Split('.');
                    //if (!dt.TryGetValue(ss[0], out var value)) throw new ArgumentException($"数据源中缺少数据[{key}]", source);
                    var value = dt[ss[0]];

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

        #region 实体评估
        /// <summary>评估指定实体是否满足表达式要求</summary>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        public Boolean Eval(IEntity entity)
        {
            var fact = Factory;
            if (fact == null) throw new ArgumentNullException(nameof(Factory));

            var exp = Expression;
            if (exp.IsNullOrEmpty()) throw new ArgumentNullException(nameof(Expression));

            return EvalParse(Expression, entity);
        }

        /// <summary>递归分解表达式</summary>
        /// <param name="value"></param>
        /// <param name="entity">实体对象</param>
        /// <returns></returns>
        protected virtual Boolean EvalParse(String value, IEntity entity)
        {
            // StartSiteId in {#SiteIds} or CityId={#CityId}

            // 与 运算
            var p = value.IndexOf(" and ", StringComparison.OrdinalIgnoreCase);
            if (p > 0)
            {
                var left = EvalField(value.Substring(0, p), entity);
                if (!left) return false;

                return EvalParse(value.Substring(p + 5), entity);
            }

            // 或 运算
            p = value.IndexOf(" or ", StringComparison.OrdinalIgnoreCase);
            if (p > 0)
            {
                var left = EvalField(value.Substring(0, p), entity);
                if (left) return true;

                return EvalParse(value.Substring(p + 4), entity);
            }

            return EvalField(value, entity);
        }

        private Boolean EvalField(String exp, IEntity entity)
        {
            // CreateUserID={$User.ID}
            // StartSiteId in {#SiteIds} or CityId={#CityId}

            // 等号运算
            // 解析表达式
            var model = Find(exp, new[] { "==", "!=", "<>", "=", " in" });
            if (model != null)
            {
                var eval = entity[model.Field.Name];
                var val = model.Value;
                switch (model.Action)
                {
                    case "==":
                    case "=": return Equals(eval, val);
                    case "!=":
                    case "<>": return !Equals(eval, val);
                    case " in":
                        {
                            if (val is String s)
                            {
                                return s.Split(",").Contains(eval);
                            }
                            if (val is IEnumerable e)
                            {
                                foreach (var item in e)
                                {
                                    if (Equals(item, eval)) return true;
                                }
                                return false;
                            }
                            break;
                        }
                }
            }

            return false;
        }
        #endregion

        #region 辅助
        private Model Find(String exp, String[] seps)
        {
            foreach (var item in seps)
            {
                var p = exp.IndexOf(item, StringComparison.OrdinalIgnoreCase);
                if (p >= 0)
                {
                    var name = exp.Substring(0, p).Trim();
                    var value = exp.Substring(p + item.Length).Trim();

                    var fi = Factory.Table.FindByName(name) as FieldItem;
                    if (fi == null) throw new XCodeException($"无法识别表达式[{exp}]中的字段[{name}]，实体类[{Factory.EntityType.FullName}]中没有该字段");

                    var val = GetValue(value);

                    return new Model
                    {
                        Action = item,
                        //Name = name,
                        Value = val,
                        Field = fi,
                    };
                }
            }

            return null;
        }

        class Model
        {
            public String Action { get; set; }
            //public String Name { get; set; }
            public Object Value { get; set; }
            public FieldItem Field { get; set; }
        }

        //private Int32 IndexOfAny(String str, String[] seps)
        //{
        //    foreach (var item in seps)
        //    {
        //        var p = str.IndexOf(item);
        //        if (p >= 0) return p;
        //    }

        //    return -1;
        //}
        #endregion
    }
}