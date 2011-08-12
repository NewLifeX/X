using System;
using System.Text;

namespace XCode
{
    /// <summary>
    /// 条件表达式
    /// </summary>
    public class WhereExpression
    {
        #region 属性
        private StringBuilder _Builder = new StringBuilder();
        /// <summary>内置字符串</summary>
        public StringBuilder Builder
        {
            get { return _Builder; }
            set { _Builder = value; }
        }
        #endregion

        #region 构造
        ///// <summary>
        ///// 新建一个条件表达式对象
        ///// </summary>
        ///// <returns></returns>
        //public static WhereExpression New()
        //{
        //    return new WhereExpression();
        //}
        #endregion

        #region 方法
        /// <summary>
        /// And操作
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public WhereExpression And(String exp)
        {
            if (Builder.Length > 0 && Builder[Builder.Length - 1] != '(') Builder.Append(" And ");
            Builder.Append(exp);

            return this;
        }

        /// <summary>
        /// Or操作
        /// </summary>
        /// <param name="exp"></param>
        /// <returns></returns>
        public WhereExpression Or(String exp)
        {
            if (Builder.Length > 0 && Builder[Builder.Length - 1] != '(') Builder.Append(" Or ");
            Builder.Append(exp);

            return this;
        }

        /// <summary>
        /// 有条件And操作
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="exp"></param>
        /// <returns></returns>
        public WhereExpression AndIf(Boolean condition, String exp)
        {
            return condition ? And(exp) : this;
        }

        /// <summary>
        /// 有条件Or操作
        /// </summary>
        /// <param name="condition"></param>
        /// <param name="exp"></param>
        /// <returns></returns>
        public WhereExpression OrIf(Boolean condition, String exp)
        {
            return condition ? Or(exp) : this;
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Builder == null || Builder.Length <= 0)
                return null;
            else
                return Builder.ToString();
        }

        /// <summary>
        /// 类型转换
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator String(WhereExpression obj)
        {
            return obj != null ? obj.ToString() : null;
        }
        #endregion

        #region 重载运算符
        private Boolean skipNext = false;

        /// <summary>
        /// 重载运算符实现And操作
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static WhereExpression operator &(WhereExpression exp, Object value)
        {
            // 如果是布尔型，表明是下一段的条件语句
            if (value is Boolean)
            {
                exp.skipNext = !(Boolean)value;
                return exp;
            }
            // 如果上一个要求这里跳过，则跳过
            if (exp.skipNext)
            {
                exp.skipNext = false;
                return exp;
            }

            exp.And(value.ToString());
            return exp;
        }

        /// <summary>
        /// 重载运算符实现Or操作
        /// </summary>
        /// <param name="exp"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static WhereExpression operator |(WhereExpression exp, Object value)
        {
            // 如果是布尔型，表明是下一段的条件语句
            if (value is Boolean)
            {
                exp.skipNext = !(Boolean)value;
                return exp;
            }
            // 如果上一个要求这里跳过，则跳过
            if (exp.skipNext)
            {
                exp.skipNext = false;
                return exp;
            }

            exp.Or(value.ToString());
            return exp;
        }
        #endregion
    }
}