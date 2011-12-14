using System;

namespace NewLife.Serialization
{
    /// <summary>二进制序列化设置</summary>
    public class BinarySettings : ReaderWriterSetting
    {
        #region 属性
        private Boolean _IsLittleEndian = true;
        /// <summary>是否小端字节序。</summary>
        /// <remarks>
        /// 网络协议都是Big-Endian；
        /// Java编译的都是Big-Endian；
        /// Motorola的PowerPC是Big-Endian；
        /// x86系列则采用Little-Endian方式存储数据；
        /// ARM同时支持 big和little，实际应用中通常使用Little-Endian。
        /// </remarks>
        public Boolean IsLittleEndian { get { return _IsLittleEndian; } set { _IsLittleEndian = value; } }

        private Boolean _EncodeInt = true;
        /// <summary>编码整数。打开后将使用7位编码写入所有16、32和64位整数，节省空间。打开后字节序设置将会无效。默认打开。</summary>
        public Boolean EncodeInt { get { return _EncodeInt; } set { _EncodeInt = value; } }

        private Boolean _IgnoreType = true;
        /// <summary>忽略类型。打开后将不输出对象类型，按照读取时指定的类型读取。默认打开。</summary>
        public Boolean IgnoreType { get { return _IgnoreType; } set { _IgnoreType = value; } }

        private Boolean _IgnoreName = true;
        /// <summary>忽略名称。打开后将不输出成员名称，按照读取时指定的类型读取。默认打开。</summary>
        public Boolean IgnoreName { get { return _IgnoreName; } set { _IgnoreName = value; } }
        #endregion

        #region 类型
        private Boolean _SplitComplexType;
        /// <summary>是否拆分复杂类型。将拆分数组、内嵌和泛型</summary>
        public Boolean SplitComplexType { get { return _SplitComplexType; } set { _SplitComplexType = value; } }

        /// <summary>类型样式</summary>
        public enum TypeKinds
        {
            /// <summary>普通类型</summary>
            Normal,

            /// <summary>数组类型</summary>
            Array,

            /// <summary>内嵌类型</summary>
            Nested,

            /// <summary>泛型类型</summary>
            Generic
        }
        #endregion
    }
}