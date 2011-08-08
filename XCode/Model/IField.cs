using System;
using System.Collections.Generic;
using System.Text;
using XCode.Expressions;

namespace XCode.Model
{
    /// <summary>
    /// 字段接口
    /// </summary>
    public interface IField
    {
        #region 属性
        #endregion

        #region 方法
        FieldExpression Equal(Object value);
        FieldExpression LargeThan(Object value);
        FieldExpression LessThan(Object value);
        FieldExpression LargeOrEqual(Object value);
        FieldExpression LessOrEqual(Object value);
        FieldExpression StartWith(Object value);
        FieldExpression EndWith(Object value);
        FieldExpression Contain(Object value);
        FieldExpression In(Object value);
        #endregion
    }
}