using System;
using XCode.DataAccessLayer;

namespace XCode.Exceptions
{
    /// <summary>数据库元数据异常</summary>
    [Serializable]
    public class XDbMetaDataException : XDbException
    {
        /// <summary>数据库元数据</summary>
        public IMetaData MetaData { get; }

        #region 构造
        /// <summary>初始化</summary>
        /// <param name="metadata"></param>
        public XDbMetaDataException(IMetaData metadata) : base(metadata.Database) { MetaData = metadata; }

        /// <summary>初始化</summary>
        /// <param name="metadata"></param>
        /// <param name="message"></param>
        public XDbMetaDataException(IMetaData metadata, String message) : base(metadata.Database, message) { MetaData = metadata; }

        /// <summary>初始化</summary>
        /// <param name="metadata"></param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XDbMetaDataException(IMetaData metadata, String message, Exception innerException)
            : base(metadata.Database, message, innerException)
        {
            MetaData = metadata;
        }

        /// <summary>初始化</summary>
        /// <param name="metadata"></param>
        /// <param name="innerException"></param>
        public XDbMetaDataException(IMetaData metadata, Exception innerException)
            : base(metadata.Database, innerException)
        {
            MetaData = metadata;
        }
        #endregion
    }
}
