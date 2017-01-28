using System;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
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
        DbTransaction Tran { get; }

        /// <summary>事务完成事件</summary>
        event EventHandler<TransactionEventArgs> Completed;

        /// <summary>增加事务计数</summary>
        /// <returns></returns>
        ITransaction Begin();

        /// <summary>提交事务</summary>
        /// <returns></returns>
        ITransaction Commit();

        /// <summary>回滚事务</summary>
        /// <returns></returns>
        ITransaction Rollback();
    }

    /// <summary>事务完成事件参数</summary>
    class TransactionEventArgs : EventArgs
    {
        /// <summary>事务是否成功</summary>
        public Boolean Success { get; set; }
    }

    class Transaction : ITransaction
    {
        #region 属性
        public IsolationLevel Level { get; private set; }

        public Int32 Count { get; set; }

        /// <summary>数据库事务</summary>
        public DbTransaction Tran { get; private set; }

        public event EventHandler<TransactionEventArgs> Completed;

        private static Int32 _gid = 1;
        /// <summary>事务唯一编号</summary>
        private Int32 ID { get; set; } = _gid++;

        DbConnection _Conn;
        #endregion

        #region 构造
        public Transaction(DbConnection conn, IsolationLevel level)
        {
            _Conn = conn;
            Level = level;

            Log = Setting.Current.TransactionDebug ? XTrace.Log : Logger.Null;

            Begin();
        }
        #endregion

        #region 方法
        public ITransaction Begin()
        {
            Debug.Assert(Count >= 0, "Tran{0}.Begin".F(ID));

            Count++;

            if (Count == 1)
            {
                using (var ct = new TimeCost("Tran{0}.Begin".F(ID), 1000))
                {
                    ct.Log = Log;
                    Tran = _Conn.BeginTransaction(Level);
                }

                Level = Tran.IsolationLevel;
                Log.Debug("Tran{0}.Begin {1}", ID, Level);
            }

            return this;
        }

        public ITransaction Commit()
        {
            Debug.Assert(Count >= 1, "Tran{0}.Commit".F(ID));

            Count--;

            if (Count == 0)
            {
                Log.Debug("Tran{0}.Commit {1}", ID, Level);

                try
                {
                    Tran.Commit();
                }
                finally
                {
                    Completed?.Invoke(this, new TransactionEventArgs { Success = true });
                }
            }

            return this;
        }

        public ITransaction Rollback()
        {
            Debug.Assert(Count >= 1, "Tran{0}.Rollback".F(ID));

            Count--;

            if (Count == 0)
            {
                Log.Debug("Tran{0}.Rollback {1}", ID, Level);

                try
                {
                    Tran.Rollback();
                }
                finally
                {
                    Completed?.Invoke(this, new TransactionEventArgs { Success = false });
                }
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