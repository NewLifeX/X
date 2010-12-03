using System;
using System.Collections.Generic;
using System.Text;
using System.Reflection;
using NewLife.Serialization.Protocol;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 协议序列化头部
    /// </summary>
    /// <remarks>
    /// 默认情况下，协议序列化包含头部信息，记录着本次序列化的配置信息。
    /// 使用特性来控制序列化过程优先级更高。
    /// </remarks>
    [ProtocolSerialProperty]
    public class ProtocolFormatterHead : ICloneable
    {
        #region 属性
        //[NonSerialized]
        private String _Magic = typeof(ProtocolFormatter).Name;
        /// <summary>标识</summary>
        //[ProtocolElement]
        public String Magic
        {
            get { return _Magic; }
            set { _Magic = value; }
        }

        //[NonSerialized]
        private String _Version;
        /// <summary>版本</summary>
        //[ProtocolElement]
        public String Version
        {
            get
            {
                if (_Version == null)
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    _Version = asm.GetName().Version.ToString();
                }
                return _Version;
            }
        }

        //[NonSerialized]
        private String _FileVersion;
        /// <summary>文件版本</summary>
        //[ProtocolElement]
        public String FileVersion
        {
            get
            {
                if (_FileVersion == null)
                {
                    Assembly asm = Assembly.GetExecutingAssembly();
                    AssemblyFileVersionAttribute att = Attribute.GetCustomAttribute(asm, typeof(AssemblyFileVersionAttribute)) as AssemblyFileVersionAttribute;
                    _FileVersion = att.Version;
                }
                return _FileVersion;
            }
        }

        //[NonSerialized]
        private FormatterConfig _Config;
        /// <summary>配置信息</summary>
        [ProtocolNotNull(true)]
        public FormatterConfig Config
        {
            get
            {
                //if (_Config == null) _Config = new FormatterConfig();
                if (_Config == null) _Config = FormatterConfig.Default;
                return _Config;
            }
            set { _Config = value; }
        }
        #endregion

        #region 序列对象信息
        private String _AssemblyName;
        /// <summary>汇编名</summary>
        public String AssemblyName
        {
            get { return _AssemblyName; }
            set { _AssemblyName = value; }
        }

        private String _TypeName;
        /// <summary>类型名</summary>
        public String TypeName
        {
            get { return _TypeName; }
            set { _TypeName = value; }
        }
        #endregion

        #region 扩展属性
        private List<Type> _Types;
        /// <summary>类型集合</summary>
        /// <remarks>支持序列化具体数据之前指定类型，存储的时候使用压缩编码的整数表示，也就是类型集合中的序号</remarks>
        public List<Type> Types
        {
            get { return _Types; }
            set { _Types = value; }
        }

        private List<Object> _RefObjects;
        /// <summary>引用对象集合</summary>
        /// <remarks>对于多次引用的对象，集中存储</remarks>
        public List<Object> RefObjects
        {
            get { return _RefObjects; }
            set { _RefObjects = value; }
        }
        #endregion

        #region 克隆
        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        object ICloneable.Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        public ProtocolFormatterHead Clone()
        {
            return (this as ICloneable).Clone() as ProtocolFormatterHead;
        }
        #endregion

        #region 默认
        private static ProtocolFormatterHead _Default;
        /// <summary>默认头部信息</summary>
        public static ProtocolFormatterHead Default
        {
            get
            {
                if (_Default == null)
                {
                    _Default = new ProtocolFormatterHead();
                }
                return _Default.Clone();
            }
        }
        #endregion
    }
}
