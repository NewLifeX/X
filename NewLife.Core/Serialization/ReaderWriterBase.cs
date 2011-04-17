using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using System.ComponentModel;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读写器基类
    /// </summary>
    public abstract class ReaderWriterBase : IReaderWriter
    {
        #region 属性
        private Encoding _Encoding;
        /// <summary>字符串编码</summary>
        public virtual Encoding Encoding
        {
            get { return _Encoding ?? (_Encoding = Encoding.UTF8); }
            set { _Encoding = value; }
        }

        //private Boolean _IsLittleEndian = true;
        ///// <summary>
        ///// 是否小端字节序。
        ///// </summary>
        ///// <remarks>
        ///// 网络协议都是Big-Endian；
        ///// Java编译的都是Big-Endian；
        ///// Motorola的PowerPC是Big-Endian；
        ///// x86系列则采用Little-Endian方式存储数据；
        ///// ARM同时支持 big和little，实际应用中通常使用Little-Endian。
        ///// </remarks>
        //public Boolean IsLittleEndian
        //{
        //    get { return _IsLittleEndian; }
        //    set { _IsLittleEndian = value; }
        //}

        /// <summary>是否序列化属性，默认序列化属性。主要影响GetMembers</summary>
        public virtual Boolean IsProperty { get { return true; } }
        #endregion

        #region 方法
        /// <summary>
        /// 获取需要序列化的成员（属性或字段）
        /// </summary>
        /// <returns></returns>
        public virtual MemberInfo[] GetMembers()
        {
            //TypeDescriptor td = new TypeDescriptor();
            return null;
        }
        #endregion
    }
}