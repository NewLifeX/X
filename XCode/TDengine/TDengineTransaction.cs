using System;
using System.Data;
using System.Data.Common;

namespace XCode.TDengine
{
    /// <summary>事务</summary>
    public class TDengineTransaction : DbTransaction
    {
        private TDengineConnection _connection;
        private readonly IsolationLevel _isolationLevel;
        //private Boolean _completed;

        internal TDengineTransaction(TDengineConnection connection, IsolationLevel isolationLevel)
        {
            _connection = connection;
            _isolationLevel = isolationLevel;

            Begin();
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            if (disposing && _connection.State == ConnectionState.Open)
            {
                if (_connection._transactionLevel > 0)
                {
                    _connection.Execute("ROLLBACK;");
                    _connection.Transaction = null;
                }
            }
        }

        /// <summary>数据库连接</summary>
        protected override DbConnection DbConnection => _connection;

        /// <summary>等级</summary>
        public override IsolationLevel IsolationLevel => _isolationLevel;

        /// <summary>开始事务</summary>
        public virtual void Begin()
        {
            if (_connection._transactionLevel++ > 0) return;

            _connection.Execute("BEGIN;");
        }

        /// <summary>提交事务</summary>
        public override void Commit()
        {
            if (--_connection._transactionLevel > 0) return;

            _connection.Execute("COMMIT;");
            _connection.Transaction = null;
        }

        /// <summary>回滚事务</summary>
        public override void Rollback()
        {
            if (--_connection._transactionLevel > 0) return;

            _connection.Execute("ROLLBACK;");
            _connection.Transaction = null;
        }
    }
}