using System;
using System.Text;

namespace XCode
{
    /// <summary>排序表达式</summary>
    public class OrderExpression
    {
        #region 属性
        private StringBuilder _Builder = new StringBuilder();
        /// <summary>内置字符串</summary>
        public StringBuilder Builder { get { return _Builder; } set { _Builder = value; } }
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

        /// <summary>升序</summary>
        /// <returns></returns>
        public OrderExpression Asc(String exp) { return And(exp); }

        /// <summary>降序</summary>
        /// <returns></returns>
        public OrderExpression Desc(String exp)
        {
            if (String.IsNullOrEmpty(exp)) return this;

            return And(exp + " Desc");
        }

        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override string ToString()
        {
            if (Builder == null || Builder.Length <= 0) return null;

            return Builder.ToString();
        }

        /// <summary>类型转换</summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        public static implicit operator String(OrderExpression obj)
        {
            return obj != null ? obj.ToString() : null;
        }
        #endregion

        #region 重载运算符
        /// <summary>重载运算符实现And操作，同时通过布尔型支持AndIf</summary>
        /// <param name="exp"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static OrderExpression operator &(OrderExpression exp, Object value)
        {
            if (value == null) return exp;

            exp.And(value.ToString());
            return exp;
        }
        #endregion
    }
}