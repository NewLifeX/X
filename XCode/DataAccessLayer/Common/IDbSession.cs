using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据库会话接口。
    /// 对应于与数据库的一次会话连接。
    /// </summary>
    public interface IDbSession : IDisposable
    {
        #region 属性
        /// <summary>
        /// 数据库
        /// </summary>
        IDatabase Database { get; }

        /// <summary>
        /// 链接字符串
        /// </summary>
        String ConnectionString { get; set; }

        /// <summary>
        /// 数据库链接
        /// </summary>
        DbConnection Conn { get; }

        /// <summary>
        /// 数据库名
        /// </summary>
        String DatabaseName { get; set; }

        /// <summary>
        /// 查询次数
        /// </summary>
        Int32 QueryTimes { get; set; }

        /// <summary>
        /// 执行次数
        /// </summary>
        Int32 ExecuteTimes { get; set; }
        #endregion

        #region 打开/关闭
        /// <summary>
        /// 连接是否已经打开
        /// </summary>
        bool Opened { get; }

        /// <summary>
        /// 打开
        /// </summary>
        void Open();

        /// <summary>
        /// 关闭
        /// </summary>
        void Close();

        /// <summary>
        /// 自动关闭。
        /// 启用事务后，不关闭连接。
        /// 在提交或回滚事务时，如果IsAutoClose为true，则会自动关闭
        /// </summary>
        void AutoClose();
        #endregion

        #region 事务
        /// <summary>
        /// 开始事务
        /// </summary>
        /// <returns></returns>
        Int32 BeginTransaction();

        /// <summary>
        /// 提交事务
        /// </summary>
        /// <returns></returns>
        Int32 Commit();

        /// <summary>
        /// 回滚事务
        /// </summary>
        /// <returns></returns>
        Int32 Rollback();
        #endregion

        #region 基本方法 查询/执行
        /// <summary>
        /// 执行SQL查询，返回记录集
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="ps">参数</param>
        /// <returns>记录集</returns>
        DataSet Query(String sql, params DbParameter[] ps);

        /// <summary>
        /// 执行SQL查询，返回记录集
        /// </summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>记录集</returns>
        DataSet Query(SelectBuilder builder, Int32 startRowIndex, Int32 maximumRows);

        /// <summary>
        /// 执行DbCommand，返回记录集
        /// </summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns>记录集</returns>
        DataSet Query(DbCommand cmd);

        /// <summary>
        /// 执行SQL查询，返回总记录数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>总记录数</returns>
        Int64 QueryCount(String sql);

        /// <summary>
        /// 执行SQL查询，返回总记录数
        /// </summary>
        /// <param name="builder">查询生成器</param>
        /// <returns>总记录数</returns>
        Int64 QueryCount(SelectBuilder builder);

        /// <summary>
        /// 快速查询单表记录数，稍有偏差
        /// </summary>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        Int64 QueryCountFast(String tableName);

        /// <summary>
        /// 执行SQL语句，返回受影响的行数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        Int32 Execute(String sql);

        /// <summary>
        /// 执行DbCommand，返回受影响的行数
        /// </summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        Int32 Execute(DbCommand cmd);

        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        Int64 InsertAndGetIdentity(String sql);

        /// <summary>
        /// 执行SQL语句，返回结果中的第一行第一列
        /// </summary>
        /// <typeparam name="T">返回类型</typeparam>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        T ExecuteScalar<T>(String sql);

        /// <summary>
        /// 获取一个DbCommand。
        /// 配置了连接，并关联了事务。
        /// 连接已打开。
        /// 使用完毕后，必须调用AutoClose方法，以使得在非事务及设置了自动关闭的情况下关闭连接
        /// </summary>
        /// <returns></returns>
        DbCommand CreateCommand();
        #endregion

        #region 构架
        /// <summary>
        /// 返回数据源的架构信息
        /// </summary>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        DataTable GetSchema(string collectionName, string[] restrictionValues);
        #endregion
    }
}