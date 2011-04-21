using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Reflection;

namespace NewLife.Serialization
{
    /// <summary>
    /// 二进制读取器
    /// </summary>
    public class BinaryReaderX : ReaderBase
    {
        #region 属性
        private BinaryReader _Reader;
        /// <summary>读取器</summary>
        public BinaryReader Reader
        {
            get { return _Reader; }
            set { _Reader = value; }
        }

        private Boolean _IsLittleEndian = true;
        /// <summary>
        /// 是否小端字节序。
        /// </summary>
        /// <remarks>
        /// 网络协议都是Big-Endian；
        /// Java编译的都是Big-Endian；
        /// Motorola的PowerPC是Big-Endian；
        /// x86系列则采用Little-Endian方式存储数据；
        /// ARM同时支持 big和little，实际应用中通常使用Little-Endian。
        /// </remarks>
        public Boolean IsLittleEndian
        {
            get { return _IsLittleEndian; }
            set { _IsLittleEndian = value; }
        }
        #endregion

        /// <summary>
        /// 读取字节
        /// </summary>
        /// <returns></returns>
        public override byte ReadByte()
        {
            return Reader.ReadByte();
        }

        /// <summary>
        /// 判断字节顺序
        /// </summary>
        /// <param name="count"></param>
        /// <returns></returns>
        protected override byte[] ReadIntBytes(int count)
        {
            Byte[] buffer = base.ReadIntBytes(count);

            // 如果不是小端字节顺序，则倒序
            if (!IsLittleEndian) Array.Reverse(buffer);

            return buffer;
        }

        #region 获取成员
        /// <summary>
        /// 已重载。序列化字段
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        protected override MemberInfo[] OnGetMembers(Type type)
        {
            return FilterMembers(FindFields(type), typeof(NonSerializedAttribute));
        }
        #endregion
    }
}