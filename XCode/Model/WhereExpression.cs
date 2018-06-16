using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Collections;

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
        /// <summary>左节点</summary>
        public Expression Left { get; set; }

        /// <summary>右节点</summary>
        public Expression Right { get; set; }

        /// <summary>是否And</summary>
        public Operator Operator { get; set; }

        /// <summary>是否为空</summary>
        public Boolean Empty { get { return Left == null && Right == null; } }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public WhereExpression() { }

        /// <summary>实例化</summary>
        /// <param name="left"></param>
        /// <param name="op"></param>
        /// <param name="right"></param>
        public WhereExpression(Expression left, Operator op, Expression right)
        {
            Left = Flatten(left);
            Operator = op;
            Right = Flatten(right);
        }
        #endregion

        #region 方法
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

            // 右侧表达式
            var sb = Pool.StringBuilder.Get();
            GetString(sb, ps, Right);

            // 中间运算符
            if (builder.Length > len && sb.Length > 0)
            {
                switch (Operator)
                {
                    case Operator.And: builder.Append(" And "); break;
                    case Operator.Or: builder.Append(" Or "); break;
                    case Operator.Space: builder.Append(" "); break;
                    default: break;
                }
            }

            builder.Append(sb.Put(true));
        }

        private void GetString(StringBuilder builder, IDictionary<String, Object> ps, Expression exp)
        {
            exp = Flatten(exp);
            if (exp == null) return;

            // 递归构建，下级运算符优先级较低时加括号
            var bracket = false;
            if (exp is WhereExpression where)
            {
                if (where.Empty) return;

                if (where.Operator > Operator) bracket = true;
            }

            if (bracket) builder.Append("(");
            exp.GetString(builder, ps);
            if (bracket) builder.Append(")");
        }

        /// <summary>拉平表达式，避免空子项</summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        private Expression Flatten(Expression exp)
        {
            if (exp == null) return null;

            if (exp is WhereExpression where)
            {
                // 左右为空，返回空
                if (where.Left == null && where.Right == null) return null;

                // 其中一边为空，递归拉平另一边
                if (where.Left == null) return Flatten(where.Right);
                if (where.Right == null) return Flatten(where.Left);
            }

            return exp;
        }
        #endregion

        #region 分组
        ///// <summary>按照指定若干个字段分组。没有条件时使用分组请用FieldItem的GroupBy</summary>
        ///// <param name="names"></param>
        ///// <returns>返回条件语句加上分组语句</returns>
        //public String GroupBy(params String[] names)
        //{
        //    var where = GetString(null);

        //    var sb = new StringBuilder();
        //    foreach (var item in names)
        //    {
        //        sb.Separate(",").Append(item);
        //    }

        //    if (where.IsNullOrWhiteSpace())
        //        return "Group By {0}".F(sb.ToString());
        //    else
        //        return "{1} Group By {0}".F(sb.ToString(), where);
        //}
        #endregion
    }
}