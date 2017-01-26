using System;
using System.ComponentModel;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Threading;
using NewLife.Collections;
using XCode.Cache;

namespace XCode.DataAccessLayer
{
    partial class DAL
    {
        #region 统计属性
        [ThreadStatic]
        private static Int32 _QueryTimes;
        /// <summary>查询次数</summary>
        public static Int32 QueryTimes { get { return _QueryTimes; } }

        [ThreadStatic]
        private static Int32 _ExecuteTimes;
        /// <summary>执行次数</summary>
        public static Int32 ExecuteTimes { get { return _ExecuteTimes; } }
        #endregion

        #region 使用缓存后的数据操作方法
        /// <summary>根据条件把普通查询SQL格式化为分页SQL。</summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        public SelectBuilder PageSplit(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows)
        {
            //2016年7月2日 HUIYUE 取消分页SQL缓存，此部分缓存提升性能不多，但有可能会造成分页数据不准确，感觉得不偿失
            return Db.PageSplit(builder, startRowIndex, maximumRows);
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="tableNames">所依赖的表的表名</param>
        /// <returns></returns>
        [DebuggerHidden]
        public DataSet Select(String sql, params String[] tableNames)
        {
            CheckBeforeUseDatabase();

            Interlocked.Increment(ref _QueryTimes);
            return Session.Query(sql);
        }

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="builder">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="tableNames">所依赖的表的表名</param>
        /// <returns></returns>
        [DebuggerHidden]
        public DataSet Select(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows, params String[] tableNames)
        {
            builder = PageSplit(builder, startRowIndex, maximumRows);
            if (builder == null) return null;

            return Select(builder.ToString(), tableNames);
        }

        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="sb">查询生成器</param>
        /// <param name="tableNames">所依赖的表的表名</param>
        /// <returns></returns>
        [DebuggerHidden]
        public Int32 SelectCount(SelectBuilder sb, params String[] tableNames)
        {
            CheckBeforeUseDatabase();

            Interlocked.Increment(ref _QueryTimes);
            return (Int32)Session.QueryCount(sb);
        }

        /// <summary>执行SQL语句，返回受影响的行数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="tableNames">受影响的表的表名</param>
        /// <returns></returns>
        [DebuggerHidden]
        public Int32 Execute(String sql, params String[] tableNames)
        {
            CheckBeforeUseDatabase();

            Interlocked.Increment(ref _ExecuteTimes);

            return Session.Execute(sql);
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql"></param>
        /// <param name="tableNames">受影响的表的表名</param>
        /// <returns>新增行的自动编号</returns>
        [DebuggerHidden]
        public Int64 InsertAndGetIdentity(String sql, params String[] tableNames)
        {
            CheckBeforeUseDatabase();

            Interlocked.Increment(ref _ExecuteTimes);

            return Session.InsertAndGetIdentity(sql);
        }

        /// <summary>执行SQL语句，返回受影响的行数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <param name="tableNames">受影响的表的表名</param>
        /// <returns></returns>
        [DebuggerHidden]
        public Int32 Execute(String sql, CommandType type, DbParameter[] ps, params String[] tableNames)
        {
            CheckBeforeUseDatabase();

            Interlocked.Increment(ref _ExecuteTimes);

            return Session.Execute(sql, type, ps);
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql"></param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <param name="tableNames">受影响的表的表名</param>
        /// <returns>新增行的自动编号</returns>
        [DebuggerHidden]
        public Int64 InsertAndGetIdentity(String sql, CommandType type, DbParameter[] ps, params String[] tableNames)
        {
            CheckBeforeUseDatabase();

            Interlocked.Increment(ref _ExecuteTimes);

            return Session.InsertAndGetIdentity(sql, type, ps);
        }

        /// <summary>执行CMD，返回记录集</summary>
        /// <param name="cmd">CMD</param>
        /// <param name="tableNames">所依赖的表的表名</param>
        /// <returns></returns>
        [DebuggerHidden]
        public DataSet Select(DbCommand cmd, String[] tableNames)
        {
            CheckBeforeUseDatabase();

            Interlocked.Increment(ref _QueryTimes);
            return Session.Query(cmd);
        }

        /// <summary>执行CMD，返回受影响的行数</summary>
        /// <param name="cmd"></param>
        /// <param name="tableNames"></param>
        /// <returns></returns>
        [DebuggerHidden]
        public Int32 Execute(DbCommand cmd, String[] tableNames)
        {
            CheckBeforeUseDatabase();

            Interlocked.Increment(ref _ExecuteTimes);
            return Session.Execute(cmd);
        }
        #endregion

        #region 事务
        /// <summary>开始事务</summary>
        /// <remarks>
        /// Read Uncommitted: 允许读取脏数据，一个事务能看到另一个事务还没有提交的数据。（不会阻止其它操作）
        /// Read Committed: 确保事务读取的数据都必须是已经提交的数据。它限制了读取中间的，没有提交的，脏的数据。
        /// 但是它不能确保当事务重新去读取的时候，读的数据跟上次读的数据是一样的，也就是说当事务第一次读取完数据后，
        /// 该数据是可能被其他事务修改的，当它再去读取的时候，数据可能是不一样的。（数据隐藏，不阻止）
        /// Repeatable Read: 是一个更高级别的隔离级别，如果事务再去读取同样的数据，先前的数据是没有被修改过的。（阻止其它修改）
        /// Serializable: 它做出了最有力的保证，除了每次读取的数据是一样的，它还确保每次读取没有新的数据。（阻止其它添删改）
        /// </remarks>
        /// <param name="level">事务隔离等级</param>
        /// <returns>剩下的事务计数</returns>
        public Int32 BeginTransaction(IsolationLevel level = IsolationLevel.Unspecified)
        {
            CheckBeforeUseDatabase();

            return Session.BeginTransaction(level);
        }

        /// <summary>提交事务</summary>
        /// <returns>剩下的事务计数</returns>
        public Int32 Commit() { return Session.Commit(); }

        /// <summary>回滚事务，忽略异常</summary>
        /// <returns>剩下的事务计数</returns>
        public Int32 Rollback() { return Session.Rollback(); }
        #endregion

        #region 队列
        /// <summary>实体队列</summary>
        public EntityQueue Queue { get; private set; }
        #endregion
    }
}