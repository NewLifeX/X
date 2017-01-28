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

        /// <summary>执行次数。其决定是否更新缓存</summary>
        Int32 Executes { get; }

        /// <summary>数据库事务</summary>
        DbTransaction Tran { get; }

        /// <summary>事务完成事件</summary>
        event EventHandler<TransactionEventArgs> Completed;

        /// <summary>获取事务</summary>
        /// <param name="execute">是否执行增删改</param>
        /// <returns></returns>
        DbTransaction Check(Boolean execute);

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
        public Boolean Success { get; }

        /// <summary>执行次数。其决定是否更新缓存</summary>
        public Int32 Executes { get; }

        public TransactionEventArgs(Boolean success, Int32 executes)
        {
            Success = success;
            Executes = executes;
        }
    }

    class Transaction : ITransaction
    {
        #region 属性
        public IsolationLevel Level { get; private set; }

        public Int32 Count { get; set; }

        /// <summary>执行次数。其决定是否更新缓存</summary>
        public Int32 Executes { get; set; }

        public event EventHandler<TransactionEventArgs> Completed;

        private static Int32 _gid = 1;
        /// <summary>事务唯一编号</summary>
        private Int32 ID { get; set; } = _gid++;

        IDbSession _Session;
        #endregion

        #region 构造
        public Transaction(IDbSession session, IsolationLevel level)
        {
            _Session = session;
            Level = level;
            Count = 1;

            Log = Setting.Current.TransactionDebug ? XTrace.Log : Logger.Null;
        }
        #endregion

        #region 延迟打开事务
        private DbTransaction _Tran;
        /// <summary>数据库事务。首次使用打开事务</summary>
        public DbTransaction Tran { get { return _Tran; } }

        /// <summary>获取事务</summary>
        /// <param name="execute">是否执行增删改</param>
        /// <returns></returns>
        public DbTransaction Check(Boolean execute)
        {
            // 不要为查询打开事务
            if (!execute) return _Tran;

            Executes++;

            if (_Tran != null) return _Tran;

            var ss = _Session;
            if (!ss.Opened) ss.Open();

            _Tran = ss.Conn.BeginTransaction(Level);

            Level = _Tran.IsolationLevel;
            Log.Debug("Tran.Begin {0} {1}", ID, Level);

            return _Tran;
        }
        #endregion

        #region 方法
        public ITransaction Begin()
        {
            Debug.Assert(Count >= 1, "Tran.Begin {0}".F(ID));

            Count++;

            return this;
        }

        public ITransaction Commit()
        {
            Debug.Assert(Count >= 1, "Tran.Commit {0}".F(ID));

            Count--;

            if (Count == 0)
            {
                var tr = _Tran;
                try
                {
                    if (tr != null)
                    {
                        Log.Debug("Tran.Commit {0} {1}", ID, Level);

                        tr.Commit();
                    }
                }
                finally
                {
                    _Tran = null;
                    Completed?.Invoke(this, new TransactionEventArgs(true, Executes));
                }
            }

            return this;
        }

        public ITransaction Rollback()
        {
            Debug.Assert(Count >= 1, "Tran.Rollback {0}".F(ID));

            Count--;

            if (Count == 0)
            {
                var tr = _Tran;
                try
                {
                    if (tr != null)
                    {
                        Log.Debug("Tran.Rollback {0} {1}", ID, Level);

                        tr.Rollback();
                    }
                }
                finally
                {
                    _Tran = null;
                    Completed?.Invoke(this, new TransactionEventArgs(false, Executes));
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