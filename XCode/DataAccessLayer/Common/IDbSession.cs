using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using NewLife;
using NewLife.Data;
using NewLife.Reflection;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据库会话接口。
    /// 对应于与数据库的一次会话连接。
    /// </summary>
    public interface IDbSession : IDisposable2
    {
        #region 属性
        /// <summary>数据库</summary>
        IDatabase Database { get; }

        /// <summary>链接字符串</summary>
        String ConnectionString { get; set; }

        ///// <summary>数据库链接</summary>
        //DbConnection Conn { get; }

        /// <summary>查询次数</summary>
        Int32 QueryTimes { get; set; }

        /// <summary>执行次数</summary>
        Int32 ExecuteTimes { get; set; }

        /// <summary>是否输出SQL</summary>
        Boolean ShowSQL { get; set; }
        #endregion

        #region 打开/关闭
        ///// <summary>连接是否已经打开</summary>
        //Boolean Opened { get; }

        ///// <summary>打开</summary>
        //void Open();

        ///// <summary>关闭</summary>
        //void Close();

        ///// <summary>
        ///// 自动关闭。
        ///// 启用事务后，不关闭连接。
        ///// 在提交或回滚事务时，如果IsAutoClose为true，则会自动关闭
        ///// </summary>
        //void AutoClose();

        ///// <summary>设置自动关闭。启用、禁用、继承</summary>
        ///// <param name="enable"></param>
        //void SetAutoClose(Boolean? enable);
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
        Int32 BeginTransaction(IsolationLevel level);

        /// <summary>提交事务</summary>
        /// <returns>剩下的事务计数</returns>
        Int32 Commit();

        /// <summary>回滚事务</summary>
        /// <param name="ignoreException">是否忽略异常</param>
        /// <returns>剩下的事务计数</returns>
        Int32 Rollback(Boolean ignoreException = true);
        #endregion

        #region 基本方法 查询/执行
        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>记录集</returns>
        DataSet Query(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps);

        /// <summary>执行DbCommand，返回记录集</summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns>记录集</returns>
        DataSet Query(DbCommand cmd);

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        DbTable Query(String sql, IDataParameter[] ps);

        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>总记录数</returns>
        Int64 QueryCount(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps);

        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="builder">查询生成器</param>
        /// <returns>总记录数</returns>
        Int64 QueryCount(SelectBuilder builder);

        /// <summary>快速查询单表记录数，稍有偏差</summary>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        Int64 QueryCountFast(String tableName);

        /// <summary>执行SQL语句，返回受影响的行数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        Int32 Execute(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps);

        /// <summary>执行DbCommand，返回受影响的行数</summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        Int32 Execute(DbCommand cmd);

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps);

        /// <summary>执行SQL语句，返回结果中的第一行第一列</summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        T ExecuteScalar<T>(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps);

        /// <summary>创建DbCommand</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        DbCommand CreateCommand(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps);
        #endregion

        #region 批量操作
        /// <summary>批量插入</summary>
        /// <param name="tableName">表名</param>
        /// <param name="columns">要插入的字段，默认所有字段</param>
        /// <param name="list">实体列表</param>
        /// <returns></returns>
        Int32 Insert(String tableName, IDataColumn[] columns, IEnumerable<IIndexAccessor> list);

        /// <summary>批量更新</summary>
        /// <param name="tableName">表名</param>
        /// <param name="columns">要更新的字段，默认所有字段</param>
        /// <param name="updateColumns">要更新的字段，默认脏数据</param>
        /// <param name="addColumns">要累加更新的字段，默认累加</param>
        /// <param name="list">实体列表</param>
        /// <returns></returns>
        Int32 Update(String tableName, IDataColumn[] columns, ICollection<String> updateColumns, ICollection<String> addColumns, IEnumerable<IIndexAccessor> list);

        /// <summary>批量插入或更新</summary>
        /// <param name="tableName">表名</param>
        /// <param name="columns">要插入的字段，默认所有字段</param>
        /// <param name="updateColumns">主键已存在时，要更新的字段</param>
        /// <param name="addColumns">主键已存在时，要累加更新的字段</param>
        /// <param name="list">实体列表</param>
        /// <returns></returns>
        Int32 InsertOrUpdate(String tableName, IDataColumn[] columns, ICollection<String> updateColumns, ICollection<String> addColumns, IEnumerable<IIndexAccessor> list);
        #endregion

        #region 异步操作
#if !NET4
        ///// <summary>异步打开</summary>
        ///// <returns></returns>
        //Task OpenAsync();

        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        Task<DbTable> QueryAsync(String sql, params IDataParameter[] ps);
#endif
        #endregion

        #region 高级
        /// <summary>清空数据表，标识归零</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        Int32 Truncate(String tableName);
        #endregion

        #region 构架
        /// <summary>返回数据源的架构信息</summary>
        /// <param name="conn">连接</param>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        DataTable GetSchema(DbConnection conn, String collectionName, String[] restrictionValues);
        #endregion
    }
}