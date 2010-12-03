using System;
using System.Reflection;

namespace NewLife.Serialization.Protocol
{
    /// <summary>
    /// 协议序列化配置
    /// </summary>
    public class FormatterConfig : ICloneable
    {
        #region 属性
        private Int32 _Size;
        /// <summary>大小</summary>
        /// <remarks>默认为0时，首先序列化一个压缩整数作为数组元素个数，再序列化每一个项；设置了大小后，不再压缩元素个数，而是以特性指定的大小为准</remarks>
        public Int32 Size
        {
            get { return _Size; }
            set { _Size = value; }
        }
        #endregion

        #region 标识
        private ConfigFlags _Flag;
        /// <summary>标识</summary>
        public ConfigFlags Flag
        {
            get { return _Flag; }
            set { _Flag = value; }
        }

        /// <summary>
        /// 是否没有头部
        /// </summary>
        [ProtocolNonSerialized]
        public Boolean NoHead
        {
            get { return GetFlag(ConfigFlags.NoHead); }
            set { SetFlag(ConfigFlags.NoHead, value); }
        }

        /// <summary>是否仅序列化属性</summary>
        [ProtocolNonSerialized]
        public Boolean SerialProperty
        {
            get { return GetFlag(ConfigFlags.SerialProperty); }
            set { SetFlag(ConfigFlags.SerialProperty, value); }
        }

        /// <summary>
        /// 是否非空
        /// </summary>
        [ProtocolNonSerialized]
        public Boolean NotNull
        {
            get { return GetFlag(ConfigFlags.NotNull); }
            set { SetFlag(ConfigFlags.NotNull, value); }
        }

        /// <summary>
        /// 是否压缩整数
        /// </summary>
        [ProtocolNonSerialized]
        public Boolean EncodeInt
        {
            get { return GetFlag(ConfigFlags.EncodeInt); }
            set { SetFlag(ConfigFlags.EncodeInt, value); }
        }

        /// <summary>是否使用对象引用</summary>
        [ProtocolNonSerialized]
        public Boolean UseRefObject
        {
            get { return GetFlag(ConfigFlags.UseRefObject); }
            set { SetFlag(ConfigFlags.UseRefObject, value); }
        }

        /// <summary>
        /// 获取标识位
        /// </summary>
        /// <param name="flag"></param>
        /// <returns></returns>
        public Boolean GetFlag(ConfigFlags flag)
        {
            return (Flag & flag) == flag;
        }

        /// <summary>
        /// 设置标识位
        /// </summary>
        /// <param name="flag"></param>
        /// <param name="value"></param>
        public void SetFlag(ConfigFlags flag, Boolean value)
        {
            // 或操作加上标识
            // 异或操作让指定标识位取反
            // 在异或中，所有与0的异或不变，与1的异或取反

            if (value)
                Flag |= flag;
            else
            {
                // 必须先检查是否包含这个标识位，因为异或的操作仅仅是取反
                if ((Flag & flag) == flag) Flag ^= flag;
            }
        }
        #endregion

        #region 默认
        private static FormatterConfig _Default;
        /// <summary>默认配置信息</summary>
        public static FormatterConfig Default
        {
            get
            {
                if (_Default == null)
                {
                    _Default = new FormatterConfig();
                }
                return _Default.Clone();
            }
        }
        #endregion

        #region 克隆
        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        object ICloneable.Clone()
        {
            return Clone();
        }

        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        public FormatterConfig Clone()
        {
            //return (this as ICloneable).Clone() as FormatterConfig;
            FormatterConfig config = new FormatterConfig();
            config.Flag = this.Flag;
            return config;
        }
        #endregion

        #region 合并
        /// <summary>
        /// 合并特性
        /// </summary>
        /// <param name="atts"></param>
        public void Merge(ProtocolAttribute[] atts)
        {
            if (atts == null || atts.Length < 1) return;

            // 从设置过的特性来更新标识信息
            foreach (ProtocolAttribute item in atts)
            {
                item.MergeTo(this);
            }
        }

        /// <summary>
        /// 克隆自身，合并指定成员的协议特性
        /// </summary>
        /// <param name="member"></param>
        /// <returns></returns>
        public FormatterConfig CloneAndMerge(MemberInfo member)
        {
            if (member == null) return this;

            // 对系统类型不做处理
            if (member is Type && IsSystemType(member as Type)) return this;

            // 有差异时才克隆，减少对象创建
            ProtocolAttribute[] atts = ProtocolAttribute.GetCustomAttributes<ProtocolAttribute>(member);
            if (atts == null || atts.Length < 1) return this;

            Boolean b = true;
            foreach (ProtocolAttribute item in atts)
            {
                if (!item.Equals(this))
                {
                    b = false;
                    break;
                }
            }
            if (b) return this;

            FormatterConfig config = Clone();
            config.Merge(atts);
            return config;
        }
        #endregion

        #region 辅助方法
        /// <summary>
        /// 是否系统类型
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static Boolean IsSystemType(Type type)
        {
            return type.Assembly.FullName.EndsWith("PublicKeyToken=b77a5c561934e089");
        }
        #endregion

        #region 重载
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return Flag.ToString();
        }
        #endregion
    }
}
