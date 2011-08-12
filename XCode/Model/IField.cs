//using System;
//using System.Collections.Generic;
//using System.Text;
//using XCode.Expressions;
//using XCode.Configuration;

//namespace XCode.Model
//{
//    /// <summary>
//    /// 字段接口
//    /// </summary>
//    public interface IField
//    {
//        #region 属性
//        /// <summary>名称</summary>
//        String Name { get; }

//        FieldItem Field { get; }
//        #endregion

//        #region 方法
//        FieldExpression Equal(Object value);
//        FieldExpression NotEqual(Object value);
//        FieldExpression Greater(Object value);
//        FieldExpression Less(Object value);
//        FieldExpression GreaterOrEqual(Object value);
//        FieldExpression LessOrEqual(Object value);
//        FieldExpression StartWith(Object value);
//        FieldExpression EndWith(Object value);
//        FieldExpression Contain(Object value);
//        FieldExpression In(Object value);
//        #endregion
//    }
//}