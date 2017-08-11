using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XCode
{
    /// <summary>操作符</summary>
    public enum Operator
    {
        /// <summary>与，交集</summary>
        And,

        /// <summary>或，并集</summary>
        Or,

        /// <summary>空格</summary>
        Space
    };

    /// <summary>条件表达式</summary>
    public class WhereExpression : Expression
    {
        #region 属性
        ///// <summary>表达式集合</summary>
        //public List<Expression> Exps { get; set; } = new List<Expression>();

        /// <summary>是否为空</summary>
        public Boolean Empty { get { return Left == null && Right == null; } }

        /// <summary>左节点</summary>
        public Expression Left { get; set; }

        /// <summary>右节点</summary>
        public Expression Right { get; set; }

        /// <summary>是否And</summary>
        public Operator Operator { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public WhereExpression() { }

        ///// <summary>把普通表达式包装为条件表达式表达式</summary>
        ///// <param name="exp"></param>
        //public WhereExpression(Expression exp) { And(exp); }

        /// <summary>实例化</summary>
        /// <param name="left"></param>
        /// <param name="op"></param>
        /// <param name="right"></param>
        public WhereExpression(Expression left, Operator op, Expression right)
        {
            Left = left;
            Operator = op;
            Right = right;
        }

        ///// <summary>拼接表达式</summary>
        ///// <param name="exps"></param>
        //public WhereExpression(params Expression[] exps)
        //{
        //    foreach (var item in exps)
        //    {
        //        if (item != null) Exps.Add(item);
        //    }
        //}

        ///// <summary>拼接表达式</summary>
        ///// <param name="exps"></param>
        //public WhereExpression(IEnumerable<Expression> exps)
        //{
        //    foreach (var item in exps)
        //    {
        //        if (item != null) Exps.Add(item);
        //    }
        //}
        #endregion

        #region 方法
        //private WhereExpression Add(OperatorExpression op, Expression exp)
        //{
        //    if (exp == null) return null;

        //    if (Exps.Count > 0)
        //    {
        //        if (op == null) throw new ArgumentNullException(nameof(op));
        //        Exps.Add(op);
        //    }

        //    Exps.Add(exp);

        //    return this;
        //}

        ///// <summary>And操作</summary>
        ///// <param name="exp"></param>
        ///// <returns></returns>
        //public WhereExpression And(Expression exp)
        //{
        //    if (exp == null) return this;

        //    // 如果前面有Or，则整体推入下一层
        //    if (Exps.Any(e => e == OperatorExpression.Or))
        //    {
        //        var agg = new WhereExpression(Exps);

        //        Exps.Clear();
        //        Exps.Add(agg);
        //    }

        //    return Add(OperatorExpression.And, exp);
        //}

        ///// <summary>Or操作</summary>
        ///// <param name="exp"></param>
        ///// <returns></returns>
        //public WhereExpression Or(Expression exp)
        //{
        //    if (exp == null) return this;

        //    return Add(OperatorExpression.Or, exp);
        //}

        ///// <summary>当前表达式作为子表达式</summary>
        ///// <returns></returns>
        //public WhereExpression AsChild() { return new WhereExpression(this); }

        /// <summary>输出条件表达式的字符串表示，遍历表达式集合并拼接起来</summary>
        /// <param name="builder"></param>
        /// <param name="ps">参数字典</param>
        /// <returns></returns>
        public override void GetString(StringBuilder builder, IDictionary<String, Object> ps)
        {
            if (Empty) return;

            // 递归构建，下级运算符优先级较低时加括号

            var len = builder.Length;

            // 左侧表达式
            GetString(builder, ps, Left);

            // 中间运算符
            if (Left != null && builder.Length > len)
            {
                switch (Operator)
                {
                    case Operator.And: builder.Append(" And "); break;
                    case Operator.Or: builder.Append(" Or "); break;
                    case Operator.Space: builder.Append(" "); break;
                    default: break;
                }
            }

            // 右侧表达式
            GetString(builder, ps, Right);
        }

        private void GetString(StringBuilder builder, IDictionary<String, Object> ps, Expression exp)
        {
            if (exp == null) return;

            var bracket = false;
            if (exp is WhereExpression where && !where.Empty && where.Operator > Operator) bracket = true;

            if (bracket) builder.Append("(");
            exp.GetString(builder, ps);
            if (bracket) builder.Append(")");
        }
        #endregion

        #region 脱括号
        ///// <summary>拉平表达式</summary>
        ///// <returns></returns>
        //public override Expression Flatten()
        //{
        //    /*
        //     * 1，非条件表达式，直接返回
        //     * 2，条件表达式只有一个子项，返回子项拉平
        //     * 3，多个子项
        //     */

        //    if (Exps.Count == 0) return null;
        //    if (Exps.Count == 1) return Exps[0].Flatten();

        //    var where = new WhereExpression(Exps);
        //    var list = where.Exps;
        //    var and = false;

        //    for (var i = 0; i < list.Count; i += 2)
        //    {
        //        var exp = list[i];
        //        var op = i > 0 ? list[i - 1] as OperatorExpression : null;
        //        if (op != null && op == OperatorExpression.And) and = true;

        //        // 特殊处理where子表达式
        //        if (exp is WhereExpression w)
        //        {
        //            if (w.Exps.Count == 1)
        //                list[i] = exp.Flatten();
        //            else if (w.Exps.Count > 1)
        //            {
        //                // 全与，拉上来
        //                if (w.Exps.All(e => !(e is OperatorExpression) || e == OperatorExpression.And))
        //                {
        //                    var k = i;
        //                    foreach (var elm in w.Exps)
        //                    {
        //                        if (k == i)
        //                            list[i] = elm.Flatten();
        //                        else
        //                            list.Insert(k, elm.Flatten());
        //                        k++;
        //                    }
        //                }
        //                // 全或，并且当前层也是或
        //                else if ((i == 0 || op == OperatorExpression.Or) &&
        //                    w.Exps.All(e => !(e is OperatorExpression) || e == OperatorExpression.Or) &&
        //                    (i == list.Count - 1 || list[i + 1] == OperatorExpression.Or))
        //                {
        //                    var k = i;
        //                    foreach (var elm in w.Exps)
        //                    {
        //                        if (k == i)
        //                            list[i] = exp.Flatten();
        //                        else
        //                            list.Insert(k, elm.Flatten());
        //                        k++;
        //                    }
        //                }
        //                else
        //                    list[i] = exp.Flatten();
        //            }
        //        }
        //        else
        //        {
        //            list[i] = exp.Flatten();
        //        }
        //    }

        //    // 只有一个
        //    if (list.Count == 1) return list[0].Flatten();

        //    return where;
        //}
        #endregion

        #region 分组
        /// <summary>按照指定若干个字段分组。没有条件时使用分组请用FieldItem的GroupBy</summary>
        /// <param name="names"></param>
        /// <returns>返回条件语句加上分组语句</returns>
        public String GroupBy(params String[] names)
        {
            var where = GetString(null);
            var sb = new StringBuilder();
            foreach (var item in names)
            {
                sb.Separate(",").Append(item);
            }

            if (where.IsNullOrWhiteSpace())
                return "Group By {0}".F(sb.ToString());
            else
                return "{1} Group By {0}".F(sb.ToString(), where);
        }
        #endregion
    }
}