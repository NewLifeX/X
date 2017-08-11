using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace XCode
{
    /// <summary>条件表达式</summary>
    public class WhereExpression : Expression
    {
        #region 属性
        /// <summary>表达式集合</summary>
        public List<Expression> Exps { get; set; } = new List<Expression>();

        /// <summary>是否为空</summary>
        public Boolean Empty { get { return Exps.Count == 0; } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public WhereExpression() { }

        /// <summary>把普通表达式包装为条件表达式表达式</summary>
        /// <param name="exp"></param>
        public WhereExpression(Expression exp) { And(exp); }

        /// <summary>拼接表达式</summary>
        /// <param name="exps"></param>
        public WhereExpression(params Expression[] exps)
        {
            foreach (var item in exps)
            {
                if (item != null) Exps.Add(item);
            }
        }

        /// <summary>拼接表达式</summary>
        /// <param name="exps"></param>
        public WhereExpression(IEnumerable<Expression> exps)
        {
            foreach (var item in exps)
            {
                if (item != null) Exps.Add(item);
            }
        }
        #endregion

        #region 方法
        private WhereExpression Add(OperatorExpression op, Expression exp)
        {
            if (exp == null) return null;

            if (Exps.Count > 0)
            {
                if (op == null) throw new ArgumentNullException(nameof(op));
                Exps.Add(op);
            }

            Exps.Add(exp);

            return this;
        }

        /// <summary>And操作</summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public WhereExpression And(Expression exp)
        {
            if (exp == null) return this;

            // 如果前面有Or，则整体推入下一层
            if (Exps.Any(e => e == OperatorExpression.Or))
            {
                var agg = new WhereExpression(Exps);

                Exps.Clear();
                Exps.Add(agg);
            }

            return Add(OperatorExpression.And, exp);
        }

        /// <summary>Or操作</summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public WhereExpression Or(Expression exp)
        {
            if (exp == null) return this;

            return Add(OperatorExpression.Or, exp);
        }

        /// <summary>当前表达式作为子表达式</summary>
        /// <returns></returns>
        public WhereExpression AsChild() { return new WhereExpression(this); }

        /// <summary>输出条件表达式的字符串表示，遍历表达式集合并拼接起来</summary>
        /// <param name="needBracket">外部是否需要括号。如果外部要求括号，而内部又有Or，则加上括号</param>
        /// <param name="ps">参数字典</param>
        /// <returns></returns>
        public override String GetString(Boolean needBracket, IDictionary<String, Object> ps)
        {
            var exps = Exps;
            if (exps.Count == 0) return null;

            //// 重整表达式
            //var list = new List<ExpItem>();
            //var sub = new List<ExpItem>();

            //var hasOr = false;
            //// 优先计算And，所有And作为一个整体表达式进入内层，处理完以后当前层要么全是And，要么全是Or
            //for (Int32 i = 0; i < exps.Count; i++)
            //{
            //    sub.Add(exps[i]);
            //    // 如果下一个是Or，或者已经是最后一个，则合并sub到list
            //    if (i < exps.Count - 1 && !exps[i + 1].IsAnd || i == exps.Count - 1)
            //    {
            //        // sub创建新exp加入list
            //        // 一个就不用创建了
            //        if (sub.Count == 1)
            //        {
            //            list.Add(sub[0]);
            //            if (list.Count > 0 && !sub[0].IsAnd) hasOr = true;
            //        }
            //        else if (i == exps.Count - 1 && list.Count == 0)
            //            list.AddRange(sub);
            //        else
            //        {
            //            // 这一片And凑成一个子表达式
            //            var where = new WhereExpression();
            //            where.Exps.AddRange(sub);
            //            list.Add(new ExpItem(false, where));
            //            hasOr = true;
            //        }

            //        sub.Clear();
            //    }
            //}
            //// 第一个表达式的And/Or必须正确代表本层所有表达式
            //list[0].IsAnd = !hasOr;

            var list = Exps;
            //var list = (Flatten() as WhereExpression).Exps;

            // 开始计算
            var sb = new StringBuilder();
            for (var i = 0; i < list.Count; i += 2)
            {
                var exp = list[i];
                var op = i > 0 ? list[i - 1] : null;

                var str = exp.GetString(true, ps);
                // 跳过没有返回的表达式
                if (str.IsNullOrWhiteSpace()) continue;

                if (sb.Length > 0) sb.Append(op.Text);
                sb.Append(str);
            }

            if (sb.Length == 0) return null;
            if (needBracket) return "({0})".F(sb.ToString());
            return sb.ToString();
        }

        /// <summary>拉平表达式</summary>
        /// <returns></returns>
        public override Expression Flatten()
        {
            /*
             * 1，非条件表达式，直接返回
             * 2，条件表达式只有一个子项，返回子项拉平
             * 3，多个子项
             */

            if (Exps.Count == 0) return null;
            if (Exps.Count == 1) return Exps[0].Flatten();

            var where = new WhereExpression(Exps);
            var list = where.Exps;
            var and = false;

            for (var i = 0; i < list.Count; i += 2)
            {
                var exp = list[i];
                var op = i > 0 ? list[i - 1] as OperatorExpression : null;
                if (op != null && op == OperatorExpression.And) and = true;

                // 特殊处理where子表达式
                if (exp is WhereExpression w)
                {
                    if (w.Exps.Count == 1)
                        list[i] = exp.Flatten();
                    else if (w.Exps.Count > 1)
                    {
                        // 全与，拉上来
                        if (w.Exps.All(e => !(e is OperatorExpression) || e == OperatorExpression.And))
                        {
                            var k = i;
                            foreach (var elm in w.Exps)
                            {
                                if (k == i)
                                    list[i] = elm.Flatten();
                                else
                                    list.Insert(k, elm.Flatten());
                                k++;
                            }
                        }
                        // 全或，并且当前层也是或
                        else if ((i == 0 || op == OperatorExpression.Or) &&
                            w.Exps.All(e => !(e is OperatorExpression) || e == OperatorExpression.Or) &&
                            (i == list.Count - 1 || list[i + 1] == OperatorExpression.Or))
                        {
                            var k = i;
                            foreach (var elm in w.Exps)
                            {
                                if (k == i)
                                    list[i] = exp.Flatten();
                                else
                                    list.Insert(k, elm.Flatten());
                                k++;
                            }
                        }
                        else
                            list[i] = exp.Flatten();
                    }
                }
                else
                {
                    list[i] = exp.Flatten();
                }
            }

            // 只有一个
            if (list.Count == 1) return list[0].Flatten();

            return where;
        }
        #endregion

        #region 分组
        /// <summary>按照指定若干个字段分组。没有条件时使用分组请用FieldItem的GroupBy</summary>
        /// <param name="names"></param>
        /// <returns>返回条件语句加上分组语句</returns>
        public String GroupBy(params String[] names)
        {
            var where = GetString(false, null);
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