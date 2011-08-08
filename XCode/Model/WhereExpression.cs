//using System;
//using System.Collections.Generic;
//using System.Text;
//using XCode;

//namespace XCode
//{
//    /// <summary>
//    /// Where表达式
//    /// </summary>
//    /// <typeparam name="TEntity"></typeparam>
//    public class WhereExpression<TEntity> where TEntity : Entity<TEntity>, new()
//    {
//        private StringBuilder _Builder = new StringBuilder();
//        /// <summary>内置字符串</summary>
//        public StringBuilder Builder
//        {
//            get { return _Builder; }
//            set { _Builder = value; }
//        }

//        private String MakeCondition(String name, Object value, String action)
//        {
//            return Entity<TEntity>.MakeCondition(name, value, action);
//        }

//        /// <summary>
//        /// And操作
//        /// </summary>
//        /// <param name="name"></param>
//        /// <param name="action"></param>
//        /// <param name="value"></param>
//        public void And(String name, String action, Object value)
//        {
//            And(MakeCondition(name, value, action));
//        }

//        /// <summary>
//        /// And操作
//        /// </summary>
//        /// <param name="name"></param>
//        /// <param name="value"></param>
//        public void And(String name, Object value)
//        {
//            And(MakeCondition(name, value, "="));
//        }

//        /// <summary>
//        /// And操作
//        /// </summary>
//        /// <param name="express"></param>
//        public void And(String express)
//        {
//            if (Builder.Length > 0 && Builder[Builder.Length - 1] != '(') Builder.Append(" And ");
//            Builder.Append(express);
//        }

//        /// <summary>
//        /// Or操作
//        /// </summary>
//        /// <param name="name"></param>
//        /// <param name="action"></param>
//        /// <param name="value"></param>
//        public void Or(String name, String action, Object value)
//        {
//            Or(MakeCondition(name, value, action));
//        }

//        /// <summary>
//        /// Or操作
//        /// </summary>
//        /// <param name="name"></param>
//        /// <param name="value"></param>
//        public void Or(String name, Object value)
//        {
//            Or(MakeCondition(name, value, "="));
//        }

//        /// <summary>
//        /// Or操作
//        /// </summary>
//        /// <param name="express"></param>
//        public void Or(String express)
//        {
//            if (Builder.Length > 0 && Builder[Builder.Length - 1] != '(') Builder.Append(" Or ");
//            Builder.Append(express);
//        }

//        /// <summary>
//        /// 已重载。
//        /// </summary>
//        /// <returns></returns>
//        public override string ToString()
//        {
//            if (Builder == null || Builder.Length <= 0)
//                return null;
//            else
//                return Builder.ToString();
//        }
//    }
//}
