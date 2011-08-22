using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Data;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 网络数据库
    /// </summary>
    class Network : DbBase
    {
        #region 属性
        /// <summary>
        /// 返回数据库类型。
        /// </summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.Network; }
        }

        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get { throw new NotSupportedException(); }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 创建数据库会话
        /// </summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession()
        {
            return new NetworkSession();
        }

        /// <summary>
        /// 创建元数据对象
        /// </summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData()
        {
            return new NetworkMetaData();
        }
        #endregion

        #region 网络操作
        private IDatabase _Server;
        /// <summary>服务端数据库对象，该对象不可以使用与会话相关的功能</summary>
        public IDatabase Server
        {
            get { return _Server; }
            private set { _Server = value; }
        }

        /// <summary>
        /// 请求服务器，更新基本信息到本地
        /// </summary>
        void UpdateInfo()
        {

        }
        #endregion
    }

    /// <summary>
    /// 网络数据库会话
    /// </summary>
    class NetworkSession : DbSession
    {
        #region 重载
        /// <summary>
        /// 查询记录集
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public override DataSet Query(string sql)
        {
            return base.Query(sql);
        }

        /// <summary>
        /// 带主键信息查询记录集
        /// </summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public override DataSet QueryWithKey(string sql)
        {
            return base.QueryWithKey(sql);
        }

        /// <summary>
        /// 不支持
        /// </summary>
        /// <param name="cmd"></param>
        /// <returns></returns>
        public override DataSet Query(DbCommand cmd)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 执行SQL查询，返回总记录数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        protected override Int64 QueryCountInternal(string sql)
        {
            return base.QueryCountInternal(sql);
        }

        /// <summary>
        /// 快速查询单表记录数，稍有偏差
        /// </summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override Int64 QueryCountFast(string tableName)
        {
            return base.QueryCountFast(tableName);
        }

        /// <summary>
        /// 执行SQL语句，返回受影响的行数
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns></returns>
        public override int Execute(string sql)
        {
            return base.Execute(sql);
        }

        /// <summary>
        /// 执行DbCommand，返回受影响的行数
        /// </summary>
        /// <param name="cmd">DbCommand</param>
        /// <returns></returns>
        public override int Execute(DbCommand cmd)
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public override long InsertAndGetIdentity(string sql)
        {
            return base.InsertAndGetIdentity(sql);
        }

        /// <summary>
        /// 不支持
        /// </summary>
        /// <returns></returns>
        public override DbCommand CreateCommand()
        {
            throw new NotSupportedException();
        }

        /// <summary>
        /// 返回数据源的架构信息
        /// </summary>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        public override DataTable GetSchema(string collectionName, string[] restrictionValues)
        {
            return base.GetSchema(collectionName, restrictionValues);
        }
        #endregion
    }

    /// <summary>
    /// 网络数据库元数据
    /// </summary>
    class NetworkMetaData : DbMetaData
    {

    }

    /// <summary>
    /// 网络数据库服务器，处理客户端发来的数据库请求
    /// </summary>
    class NetworkDbServer
    {

    }
}
