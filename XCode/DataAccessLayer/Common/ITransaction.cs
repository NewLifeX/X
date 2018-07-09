using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using NewLife.Collections;
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

        /// <summary>事务依赖对象</summary>
        IList<Object> Attachs { get; }

        /// <summary>事务完成事件</summary>
        event EventHandler<TransactionEventArgs> Completed;

        /// <summary>获取事务</summary>
        /// <param name="cmd">命令</param>
        /// <param name="execute">是否执行增删改</param>
        /// <returns></returns>
        DbTransaction Check(DbCommand cmd, Boolean execute);

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

        /// <summary>事务依赖对象</summary>
        public IList<Object> Attachs { get; } = new List<Object>();

        public event EventHandler<TransactionEventArgs> Completed;

        private static Int32 _gid;
        /// <summary>事务唯一编号</summary>
        private Int32 ID { get; set; }

        IDbSession _Session;

        /// <summary>连接对象</summary>
        public DbConnection Conn { get; private set; }
        #endregion

        #region 构造
        public Transaction(IDbSession session, IsolationLevel level)
        {
            _Session = session;
            Level = level;
            Count = 1;

            // 打开事务后，由事务管理连接
            Conn = _Session.Database.Pool.Get();
        }
        #endregion

        #region 延迟打开事务
        private DbTransaction _Tran;
        /// <summary>数据库事务。首次使用打开事务</summary>
        public DbTransaction Tran { get { return _Tran; } }

        /// <summary>获取事务</summary>
        /// <param name="cmd">命令</param>
        /// <param name="execute">是否执行增删改</param>
        /// <returns></returns>
        public DbTransaction Check(DbCommand cmd, Boolean execute)
        {
            if (cmd.Transaction != null) return cmd.Transaction;

            cmd.Transaction = _Tran;
            if (cmd.Connection == null) cmd.Connection = Conn;

            // 不要为查询打开事务
            if (!execute) return _Tran;

            Executes++;

            if (_Tran != null) return _Tran;

            //var ss = _Session;
            //if (!ss.Opened) ss.Open();

            _Tran = Conn.BeginTransaction(Level);
            cmd.Transaction = _Tran;
            if (cmd.Connection == null) cmd.Connection = Conn;

            Level = _Tran.IsolationLevel;
            ID = ++_gid;
            Log.Debug("Tran.Begin {0} {1}", ID, Level);

            return _Tran;
        }
        #endregion

        #region 方法
        public ITransaction Begin()
        {
            if (Count <= 0) throw new ArgumentOutOfRangeException(nameof(Count), $"事务[{ID}]不能重新开始");

            Count++;

            return this;
        }

        public ITransaction Commit()
        {
            if (Count <= 0) throw new ArgumentOutOfRangeException(nameof(Count), $"事务[{ID}]未开始或已结束");

            Count--;

            if (Count == 0)
            {
                var tr = _Tran;
                try
                {
                    if (tr != null)
                    {
                        Log.Debug("Tran.Commit {0} {1} Executes={2}", ID, Level, Executes);

                        tr.Commit();
                    }
                }
                finally
                {
                    _Tran = null;
                    Completed?.Invoke(this, new TransactionEventArgs(true, Executes));

                    // 把连接归还给对象池
                    _Session.Database.Pool.Put(Conn);
                }
            }

            return this;
        }

        public ITransaction Rollback()
        {
            if (Count <= 0) throw new ArgumentOutOfRangeException(nameof(Count), $"事务[{ID}]未开始或已结束");

            Count--;

            if (Count == 0)
            {
                var tr = _Tran;
                try
                {
                    if (tr != null)
                    {
                        Log.Debug("Tran.Rollback {0} {1} Executes={2}", ID, Level, Executes);

                        tr.Rollback();
                    }
                }
                finally
                {
                    _Tran = null;
                    Completed?.Invoke(this, new TransactionEventArgs(false, Executes));

                    // 把连接归还给对象池
                    _Session.Database.Pool.Put(Conn);
                }
            }

            return this;
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;
        #endregion
    }
}