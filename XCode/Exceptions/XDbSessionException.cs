using System;
using XCode.DataAccessLayer;

namespace XCode.Exceptions
{
    /// <summary>数据访问层异常</summary>
    [Serializable]
    public class XDbSessionException : XDbException
    {
        /// <summary>数据库会话</summary>
        public IDbSession Session { get; }

        #region 构造
        /// <summary>初始化</summary>
        /// <param name="session"></param>
        public XDbSessionException(IDbSession session) : base(session?.Database) { Session = session; }

        /// <summary>初始化</summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        public XDbSessionException(IDbSession session, String message) : base(session?.Database, message) { Session = session; }

        /// <summary>初始化</summary>
        /// <param name="session"></param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XDbSessionException(IDbSession session, String message, Exception innerException)
            : base(session.Database, message, innerException)
        {
            Session = session;
        }

        /// <summary>初始化</summary>
        /// <param name="session"></param>
        /// <param name="innerException"></param>
        public XDbSessionException(IDbSession session, Exception innerException)
            : base(session.Database, innerException)
        {
            Session = session;
        }
        #endregion
    }
}