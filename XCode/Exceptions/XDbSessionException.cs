using System;
using System.Collections.Generic;
using System.Text;
using XCode.DataAccessLayer;

namespace XCode.Exceptions
{
    /// <summary>数据访问层异常</summary>
    public class XDbSessionException : XDbException
    {
        private IDbSession _Session;
        /// <summary>数据库会话</summary>
        public IDbSession Session
        {
            get { return _Session; }
            //set { _Database = value; }
        }

        #region 构造
        /// <summary>初始化</summary>
        /// <param name="session"></param>
        public XDbSessionException(IDbSession session) : base(session == null ? null : session.Database) { _Session = session; }

        /// <summary>初始化</summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        public XDbSessionException(IDbSession session, String message) : base(session == null ? null : session.Database, message) { _Session = session; }

        /// <summary>初始化</summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XDbSessionException(IDbSession session, String message, Exception innerException)
            : base(session.Database, message, innerException)
        {
            _Session = session;
        }

        /// <summary>初始化</summary>
        /// <param name="session"></param>
        /// <param name="innerException"></param>
        public XDbSessionException(IDbSession session, Exception innerException)
            : base(session.Database, innerException)
        {
            _Session = session;
        }
        #endregion
    }
}