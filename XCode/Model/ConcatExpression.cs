using System;
using System.Collections.Generic;
using System.Text;

namespace XCode
{
    /// <summary>逗号连接表达式</summary>
    public class ConcatExpression : Expression
    {
        #region 属性
        ///// <summary>内置字符串</summary>
        //public StringBuilder Builder { get; set; } = new StringBuilder();

        /// <summary>内置表达式集合</summary>
        public IList<Expression> Expressions { get; set; } = new List<Expression>();

        /// <summary>是否为空</summary>
        public override Boolean IsEmpty => Expressions.Count == 0;
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public ConcatExpression() { }

        /// <summary>实例化</summary>
        /// <param name="exp"></param>
        public ConcatExpression(String exp) => Expressions.Add(new Expression(exp));
        #endregion

        #region 方法
        /// <summary>增加</summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public ConcatExpression And(String exp)
        {
            if (String.IsNullOrEmpty(exp)) return this;

            //Builder.Separate(",").Append(exp);
            Expressions.Add(new Expression(exp));

            return this;
        }

        /// <summary>增加</summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public ConcatExpression And(Expression exp)
        {
            if (exp == null) return this;

            //Builder.Separate(",").Append(exp);
            Expressions.Add(exp);

            return this;
        }

        /// <summary>已重载。</summary>
        /// <param name="builder">字符串构建器</param>
        /// <param name="ps">参数字典</param>
        /// <returns></returns>
        public override void GetString(StringBuilder builder, IDictionary<String, Object> ps)
        {
            //if (Builder == null || Builder.Length <= 0) return;

            //builder.Append(Builder);

            var exps = Expressions;
            if (exps == null || exps.Count == 0) return;

            var first = true;
            foreach (var exp in exps)
            {
                if (!first) builder.Append(",");
                first = false;

                exp.GetString(builder, ps);
            }
        }
        #endregion

        #region 重载运算符
        /// <summary>重载运算符实现And操作，同时通过布尔型支持AndIf</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static ConcatExpression operator &(ConcatExpression exp, String value)
        {
            if (value == null) return exp;

            exp.And(value);

            return exp;
        }

        /// <summary>重载运算符实现And操作，同时通过布尔型支持AndIf</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static ConcatExpression operator &(ConcatExpression exp, Expression value)
        {
            if (value == null) return exp;

            exp.And(value);

            return exp;
        }
        #endregion
    }
}