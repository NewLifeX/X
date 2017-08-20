using System;
using XCode.DataAccessLayer;

namespace XCode.Exceptions
{
    /// <summary>数据访问层异常</summary>
    [Serializable]
    public class XDbException : XCodeException
    {
        /// <summary>数据库</summary>
        public IDatabase Database { get; }

        #region 构造
        /// <summary>初始化</summary>
        /// <param name="db"></param>
        public XDbException(IDatabase db) { Database = db; }

        /// <summary>初始化</summary>
        /// <param name="db"></param>
        /// <param name="message"></param>
        public XDbException(IDatabase db, String message) : base(message) { Database = db; }

        /// <summary>初始化</summary>
        /// <param name="db"></param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XDbException(IDatabase db, String message, Exception innerException)
            : base(message + (db != null ? "[DB:" + db.ConnName + "/" + db.Type.ToString() + "]" : null), innerException)
        {
            Database = db;
        }

        /// <summary>初始化</summary>
        /// <param name="db"></param>
        /// <param name="innerException"></param>
        public XDbException(IDatabase db, Exception innerException)
            : base((innerException?.Message) + (db != null ? "[DB:" + db.ConnName + "/" + db.Type.ToString() + "]" : null), innerException)
        {
            Database = db;
        }
        #endregion
    }
}