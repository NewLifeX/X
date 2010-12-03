using System;
using System.Reflection;
using NewLife.IO;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 写入上下文
    /// </summary>
    public class WriteContext : ReadWriteContext
    {
        #region 属性
        private BinaryWriterX _Writer;
        /// <summary>写入器</summary>
        public BinaryWriterX Writer
        {
            get { return _Writer; }
            set { _Writer = value; }
        }
        #endregion

        #region 重载
        /// <summary>
        /// 创建当前对象的新实例
        /// </summary>
        /// <returns></returns>
        protected internal override ReadWriteContext Create()
        {
            return new WriteContext();
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
            (context as WriteContext).Writer = Writer;
            return context;
        }
        #endregion
    }
}
