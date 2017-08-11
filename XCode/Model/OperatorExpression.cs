using System;

namespace XCode
{
    /// <summary>操作符表达式</summary>
    public class OperatorExpression : Expression
    {
        #region 构造
        /// <summary>实例化</summary>
        /// <param name="op"></param>
        public OperatorExpression(String op) : base(op) { }
        #endregion

        #region 静态与或
        /// <summary>与运算</summary>
        public static OperatorExpression And { get; } = new OperatorExpression(" And ");

        /// <summary>或运算</summary>
        public static OperatorExpression Or { get; } = new OperatorExpression(" Or ");

        /// <summary>或运算</summary>
        public static OperatorExpression Blank { get; } = new OperatorExpression(" ");
        #endregion
    }
}