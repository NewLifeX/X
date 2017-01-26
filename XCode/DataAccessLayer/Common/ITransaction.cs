using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NewLife;

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
            Level = level;

            Trans = conn.BeginTransaction(level);
            Count = 1;
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
                Trans.Rollback();

                Completed?.Invoke(this, new TransactionEventArgs { Success = false });
            }

            return this;
        }
        #endregion
    }
}