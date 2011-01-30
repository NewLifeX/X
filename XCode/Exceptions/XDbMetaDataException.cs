using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;

namespace XCode.Exceptions
{
    /// <summary>
    /// 数据库元数据异常
    /// </summary>
    public class XDbMetaDataException : XDbException
    {
        private IMetaData _MetaData;
        /// <summary>数据库元数据</summary>
        public IMetaData MetaData
        {
            get { return _MetaData; }
            //set { _Database = value; }
        }

        #region 构造
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="metadata"></param>
        public XDbMetaDataException(IMetaData metadata) : base(metadata.Database) { _MetaData = metadata; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="message"></param>
        public XDbMetaDataException(IMetaData metadata, String message) : base(metadata.Database, message) { _MetaData = metadata; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XDbMetaDataException(IMetaData metadata, String message, Exception innerException)
            : base(metadata.Database, message, innerException)
        {
            _MetaData = metadata;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="metadata"></param>
        /// <param name="innerException"></param>
        public XDbMetaDataException(IMetaData metadata, Exception innerException)
            : base(metadata.Database, innerException)
        {
            _MetaData = metadata;
        }
        #endregion
    }
}
