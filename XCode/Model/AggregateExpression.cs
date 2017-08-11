using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace XCode.Model
{
    /// <summary>聚合表达式</summary>
    public class AggregateExpression : Expression
    {
        #region 属性
        /// <summary>表达式集合</summary>
        public List<Expression> Exps { get; set; } = new List<Expression>();

        /// <summary>是否为空</summary>
        public Boolean Empty { get { return Exps.Count == 0; } }
        #endregion

        #region 构造
        /// <summary>拼接表达式</summary>
        /// <param name="exps"></param>
        public AggregateExpression(params Expression[] exps)
        {
            foreach (var item in exps)
            {
                if (item != null) Exps.Add(item);
            }
        }

        /// <summary>拼接表达式</summary>
        /// <param name="exps"></param>
        public AggregateExpression(IEnumerable<Expression> exps)
        {
            foreach (var item in exps)
            {
                if (item != null) Exps.Add(item);
            }
        }
        #endregion

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

            var list = new List<Expression>();

            foreach (var item in Exps)
            {
                // 特殊处理where子表达式
                if (item is AggregateExpression agg)
                {
                    if (agg.Exps.Count == 1)
                        list.Add(agg.Flatten());
                    else if (agg.Exps.Count > 1)
                    {
                        // 全与，拉上来
                        if (agg.Exps.All(e => e == OperatorExpression.And))
                        {
                            foreach (var elm in agg.Exps)
                            {
                                list.Add(elm.Flatten());
                            }
                        }

                        // 全或，且当前前后也是或，拉上来
                        //if (where.Exps.Skip(1).All(e => !e.IsAnd))
                        //{
                        //    foreach (var elm in where.Exps)
                        //    {
                        //        list.Add(new ExpItem(true, elm.Exp.Flatten()));
                        //    }
                        //}
                    }
                }
                else
                    list.Add(item.Flatten());
            }

            // 只有一个
            if (list.Count == 1) return list[0].Flatten();

            return new WhereExpression { Exps = list };
        }
    }
}