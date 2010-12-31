using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.Serialization;
using System.Security.Permissions;
using XCode.DataAccessLayer;

namespace XCode.Exceptions
{
    /// <summary>
    /// 数据访问层SQL异常
    /// </summary>
    [Serializable]
    public class XSqlException : XDbException
    {
        #region 属性
        private String _Sql;
        /// <summary>SQL语句</summary>
        public String Sql
        {
            get { return _Sql; }
            private set { _Sql = value; }
        }
        #endregion

        #region 构造
        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="db"></param>
        public XSqlException(String sql, IDbSession db) : base(db) { Sql = sql; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="db"></param>
        /// <param name="message"></param>
        public XSqlException(String sql, IDbSession db, String message) : base(db, message + "[SQL:" + sql + "]") { Sql = sql; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="db"></param>
        /// <param name="message"></param>
        /// <param name="innerException"></param>
        public XSqlException(String sql, IDbSession db, String message, Exception innerException)
            : base(db, message + "[SQL:" + sql + "]", innerException)
        {
            Sql = sql;
        }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="sql"></param>
        /// <param name="db"></param>
        /// <param name="innerException"></param>
        public XSqlException(String sql, IDbSession db, Exception innerException)
            : base(db, (innerException != null ? innerException.Message : "") + "[SQL:" + sql + "]", innerException)
        {
            Sql = sql;
        }

        ///// <summary>
        ///// 初始化
        ///// </summary>
        ///// <param name="info"></param>
        ///// <param name="context"></param>
        //protected SqlException(SerializationInfo info, StreamingContext context)
        //    : base(info, context)
        //{
        //    Sql = (string)info.GetValue("sql", typeof(string));
        //}
        #endregion

        #region 方法
        /// <summary>
        /// 从序列化信息中读取Sql
        /// </summary>
        /// <param name="info"></param>
        /// <param name="context"></param>
        [SecurityPermission(SecurityAction.LinkDemand, Flags = SecurityPermissionFlag.SerializationFormatter)]
        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            info.AddValue("sql", Sql);
        }
        #endregion
    }
}