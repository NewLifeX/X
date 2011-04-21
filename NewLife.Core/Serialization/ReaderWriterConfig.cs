using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Serialization
{
    /// <summary>
    /// 读写器配置。
    /// </summary>
    /// <remarks>
    /// 之所以把读写器配置写在一个独立的类里面，然后在各个方法之间传递，是为了实现精确控制，
    /// 因为某个类可能采用全局的设置，但是它的某些成员采用别的设置，这个时候可以通过修改配置来实现
    /// </remarks>
    public class ReaderWriterConfig : ICloneable
    {
        #region 属性
        private Boolean _Required;
        /// <summary>必须的</summary>
        public Boolean Required
        {
            get { return _Required; }
            set { _Required = value; }
        }
        #endregion

        #region ICloneable 成员

        object ICloneable.Clone()
        {
            return MemberwiseClone();
        }

        /// <summary>
        /// 克隆
        /// </summary>
        /// <returns></returns>
        public virtual ReaderWriterConfig Clone()
        {
            ReaderWriterConfig config = (this as ICloneable).Clone() as ReaderWriterConfig;
            config.Required = this.Required;
            return config;
        }
        #endregion
    }
}