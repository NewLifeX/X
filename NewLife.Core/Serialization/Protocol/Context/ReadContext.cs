using System;
using System.Reflection;
using NewLife.IO;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 读取上下文
    /// </summary>
    public class ReadContext : ReadWriteContext
    {
        #region 属性
        private BinaryReaderX _Reader;
        /// <summary>读取器</summary>
        public BinaryReaderX Reader
        {
            get { return _Reader; }
            set { _Reader = value; }
        }
        #endregion

        #region 重载
        /// <summary>
        /// 创建当前对象的新实例
        /// </summary>
        /// <returns></returns>
        protected internal override ReadWriteContext Create()
        {
            return new ReadContext();
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="data"></param>
        /// <param name="type"></param>
        /// <param name="member"></param>
        /// <returns></returns>
        public override ReadWriteContext Clone(object data, Type type, MemberInfo member)
        {
            ReadWriteContext context = base.Clone(data, type, member);
            (context as ReadContext).Reader = Reader;
            return context;
        }
        #endregion
    }
}
