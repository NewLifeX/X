using System;
using System.Collections.Generic;
using System.Text;

namespace XCode
{
    /// <summary>排序表达式</summary>
    public class OrderExpression : Expression
    {
        #region 属性
        /// <summary>内置字符串</summary>
        public StringBuilder Builder { get; set; } = new StringBuilder();
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public OrderExpression() { }

        /// <summary>实例化</summary>
        /// <param name="exp"></param>
        public OrderExpression(String exp) { Builder.Append(exp); }
        #endregion

        #region 方法
        /// <summary>增加</summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public OrderExpression And(String exp)
        {
            if (String.IsNullOrEmpty(exp)) return this;

            if (Builder.Length > 0) Builder.Append(",");
            Builder.Append(exp);

            return this;
        }

        /// <summary>已重载。</summary>
        /// <param name="builder">字符串构建器</param>
        /// <param name="ps">参数字典</param>
        /// <returns></returns>
        public override void GetString(StringBuilder builder, IDictionary<String, Object> ps)
        {
            if (Builder == null || Builder.Length <= 0) return;

            builder.Append(Builder);
        }

        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator String(OrderExpression obj) => obj?.GetString(null);
        #endregion

        #region 重载运算符
        /// <summary>重载运算符实现And操作</summary>
        /// <param name="exp"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public static OrderExpression operator &(OrderExpression exp, Object value)
        {
            if (value == null) return exp;

            exp?.And(value + "");

            return exp;
        }
        #endregion
    }
}