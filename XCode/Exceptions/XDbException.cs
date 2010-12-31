using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;

namespace XCode.Exceptions
{
    /// <summary>
    /// 数据访问层异常
    /// </summary>
    public class XDbException : XCodeException
    {
        private IDbSession _Database;
        /// <summary>数据访问层</summary>
        public IDbSession Database
        {
            get { return _Database; }
            //set { _Database = value; }
        }

        #region 构造
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="db"></param>
        public XDbException(IDbSession db) { _Database = db; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="db"></param>
        /// <param name="message"></param>
        public XDbException(IDbSession db, String message) : base(message) { _Database = db; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="db"></param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XDbException(IDbSession db, String message, Exception innerException)
            : base(message + (db != null ? "[DB:" + db.Db.DbType.ToString() + "]" : null), innerException)
        {
            _Database = db;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="db"></param>
        /// <param name="innerException"></param>
        public XDbException(IDbSession db, Exception innerException)
            : base((innerException != null ? innerException.Message : null) + (db != null ? "[DB:" + db.Db.DbType.ToString() + "]" : null), innerException)
        {
            _Database = db;
        }
        #endregion
    }
}