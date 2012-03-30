using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;

namespace XCode.Exceptions
{
    /// <summary>数据访问层异常</summary>
    public class XDbException : XCodeException
    {
        private IDatabase _Database;
        /// <summary>数据库</summary>
        public IDatabase Database
        {
            get { return _Database; }
            //set { _Database = value; }
        }

        #region 构造
        /// <summary>初始化</summary>
        /// <param name="db"></param>
        public XDbException(IDatabase db) { _Database = db; }

        /// <summary>初始化</summary>
        /// <param name="db"></param>
        /// <param name="message"></param>
        public XDbException(IDatabase db, String message) : base(message) { _Database = db; }

        /// <summary>初始化</summary>
        /// <param name="db"></param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XDbException(IDatabase db, String message, Exception innerException)
            : base(message + (db != null ? "[DB:" + db.ConnName + "/" + db.DbType.ToString() + "]" : null), innerException)
        {
            _Database = db;
        }

        /// <summary>初始化</summary>
        /// <param name="db"></param>
        /// <param name="innerException"></param>
        public XDbException(IDatabase db, Exception innerException)
            : base((innerException != null ? innerException.Message : null) + (db != null ? "[DB:" + db.ConnName + "/" + db.DbType.ToString() + "]" : null), innerException)
        {
            _Database = db;
        }
        #endregion
    }
}