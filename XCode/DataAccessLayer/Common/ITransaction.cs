using System;
using System.Data;
using System.Data.Common;
using NewLife.Log;

namespace XCode.DataAccessLayer
{
    /// <summary>事务对象</summary>
    interface ITransaction
    {
        /// <summary>事务隔离级别</summary>
        IsolationLevel Level { get; }

        /// <summary>事务计数。当且仅当事务计数等于1时，才提交或回滚。</summary>
        Int32 Count { get; }

        /// <summary>数据库事务</summary>
        DbTransaction Trans { get; }

        //void Add(Action<Boolean> callback);
        event EventHandler<TransactionEventArgs> Completed;

        ITransaction Begin();

        ITransaction Commit();

        ITransaction Rollback();
    }

    class TransactionEventArgs : EventArgs
    {
        public Boolean Success { get; set; }
    }

    class Transaction : ITransaction
    {
        #region 属性
        public IsolationLevel Level { get; }

        public Int32 Count { get; set; }

        /// <summary>数据库事务</summary>
        public DbTransaction Trans { get; }

        public event EventHandler<TransactionEventArgs> Completed;
        #endregion

        #region 构造
        public Transaction(DbConnection conn, IsolationLevel level)
        {
            Log = Setting.Current.TransactionDebug ? XTrace.Log : Logger.Null;

            using (var ct = new TimeCost("BeginTransaction", 1000))
            {
                ct.Log = Log;
                Trans = conn.BeginTransaction(level);
            }

            Count = 1;

            Level = Trans.IsolationLevel;
            Log.Debug("Transaction.Begin {0}", Level);
        }
        #endregion

        #region 方法
        public ITransaction Begin()
        {
            Count++;

            return this;
        }

        public ITransaction Commit()
        {
            Count--;

            if (Count == 0)
            {
                Log.Debug("Transaction.Commit {0}", Level);
                Trans.Commit();

                Completed?.Invoke(this, new TransactionEventArgs { Success = true });
            }

            return this;
        }

        public ITransaction Rollback()
        {
            Count--;

            if (Count == 0)
            {
                Log.Debug("Transaction.Rollback {0}", Level);
                Trans.Rollback();

                Completed?.Invoke(this, new TransactionEventArgs { Success = false });
            }

            return this;
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; }
        #endregion
    }
}