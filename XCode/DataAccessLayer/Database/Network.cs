using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using NewLife.Data;
using XCode.Service;

namespace XCode.DataAccessLayer
{
    /// <summary>网络数据库</summary>
    class Network : DbBase
    {
        #region 属性
        /// <summary>返回数据库类型。</summary>
        public override DatabaseType Type => DatabaseType.Network;

        /// <summary>服务端数据库对象，该对象不可以使用与会话相关的功能</summary>
        public IDatabase Server { get; private set; }

        /// <summary>原始数据库类型。</summary>
        public DatabaseType RawType { get; private set; }

        /// <summary>工厂</summary>
        public override DbProviderFactory Factory => Server.Factory;

        protected override void OnSetConnectionString(ConnectionStringBuilder builder)
        {
            base.OnSetConnectionString(builder);

            _ConnectionString = builder.ToString();

            // 打开连接
            GetClient();
        }
        #endregion

        #region 方法
        /// <summary>创建数据库会话</summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession() => new NetworkSession(this);

        /// <summary>创建元数据对象</summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData() => new NetworkMetaData();

        public override Boolean Support(String providerName)
        {
            providerName = providerName.ToLower();
            if (providerName.Contains("net")) return true;

            return base.Support(providerName);
        }
        #endregion

        #region 网络通信
        private DbClient _Client;

        public DbClient GetClient()
        {
            if (_Client == null)
            {
                lock (this)
                {
                    if (_Client == null)
                    {
                        var builder = new ConnectionStringBuilder(ConnectionString);
                        var uri = builder["Server"];

                        var tc = new DbClient(uri);
                        //var rs = tc.LoginAsync().Result;
                        tc.Open();
                        var rs = tc.Info;

                        _ServerVersion = rs.Version;
                        RawType = rs.DbType;

                        Server = DbFactory.GetDefault(RawType);

                        _Client = tc;
                    }
                }
            }

            return _Client;
        }
        #endregion

        #region 分页
        /// <summary>构造分页SQL</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <param name="keyColumn">唯一键。用于not in分页</param>
        /// <returns>分页SQL</returns>
        public override String PageSplit(String sql, Int64 startRowIndex, Int64 maximumRows, String keyColumn) => Server.PageSplit(sql, startRowIndex, maximumRows, keyColumn);

        /// <summary>构造分页SQL</summary>
        /// <param name="builder">查询生成器</param>
        /// <param name="startRowIndex">开始行，0表示第一行</param>
        /// <param name="maximumRows">最大返回行数，0表示所有行</param>
        /// <returns>分页SQL</returns>
        public override SelectBuilder PageSplit(SelectBuilder builder, Int64 startRowIndex, Int64 maximumRows) => Server.PageSplit(builder, startRowIndex, maximumRows);
        #endregion

        #region 数据库特性
        /// <summary>长文本长度</summary>
        public override Int32 LongTextLength => Server.LongTextLength;

        /// <summary>格式化时间为SQL字符串</summary>
        /// <param name="dateTime">时间值</param>
        /// <returns></returns>
        public override String FormatDateTime(DateTime dateTime) => Server.FormatDateTime(dateTime);

        /// <summary>格式化名称，如果不是关键字，则原样返回</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public override String FormatName(String name) => Server.FormatName(name);

        /// <summary>格式化数据为SQL数据</summary>
        /// <param name="field">字段</param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public override String FormatValue(IDataColumn field, Object value) => Server.FormatValue(field, value);

        /// <summary>格式化参数名</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public override String FormatParameterName(String name) => Server.FormatParameterName(name);

        /// <summary>字符串相加</summary>
        /// <param name="left"></param>
        /// <param name="right"></param>
        /// <returns></returns>
        public override String StringConcat(String left, String right) => Server.StringConcat(left, right);

        /// <summary>创建参数</summary>
        /// <param name="name">名称</param>
        /// <param name="value">值</param>
        /// <param name="field">字段</param>
        /// <returns></returns>
        public override IDataParameter CreateParameter(String name, Object value, IDataColumn field = null) => Server.CreateParameter(name, value, field);

        /// <summary>创建参数数组</summary>
        /// <param name="ps"></param>
        /// <returns></returns>
        public override IDataParameter[] CreateParameters(IDictionary<String, Object> ps) => Server.CreateParameters(ps);
        #endregion
    }

    /// <summary>网络数据库会话</summary>
    class NetworkSession : DbSession
    {
        #region 构造函数
        public NetworkSession(IDatabase db) : base(db) { }
        #endregion

        #region 重载
        /// <summary>执行SQL查询，返回记录集</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public override DbTable Query(String sql, IDataParameter[] ps)
        {
            var client = (Database as Network).GetClient();

            var dps = ps?.ToDictionary(e => e.ParameterName, e => e.Value);
            return client.QueryAsync(sql, dps).Result;
        }

        /// <summary>执行SQL查询，返回总记录数</summary>
        /// <param name="builder">查询生成器</param>
        /// <returns>总记录数</returns>
        public override Int64 QueryCount(SelectBuilder builder)
        {
            var ds = Query(builder.SelectCount().ToString(), builder.Parameters.ToArray());
            if (ds == null || ds.Rows == null || ds.Rows.Count == 0) return -1;

            return ds.Rows[0][0].ToLong();
        }

        /// <summary>快速查询单表记录数，稍有偏差</summary>
        /// <param name="tableName"></param>
        /// <returns></returns>
        public override Int64 QueryCountFast(String tableName)
        {
            var client = (Database as Network).GetClient();
            return client.QueryCountAsync(tableName).Result;
        }

        /// <summary>执行SQL语句，返回受影响的行数</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns></returns>
        public override Int32 Execute(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            var client = (Database as Network).GetClient();
            var dps = ps?.ToDictionary(e => e.ParameterName, e => e.Value);

            return (Int32)client.ExecuteAsync(sql, dps).Result;
        }

        /// <summary>执行插入语句并返回新增行的自动编号</summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="type">命令类型，默认SQL文本</param>
        /// <param name="ps">命令参数</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(String sql, CommandType type = CommandType.Text, params IDataParameter[] ps)
        {
            var client = (Database as Network).GetClient();
            var dps = ps?.ToDictionary(e => e.ParameterName, e => e.Value);

            return client.ExecuteAsync("@" + sql, dps).Result;
        }

        /// <summary>返回数据源的架构信息</summary>
        /// <param name="conn">连接</param>
        /// <param name="collectionName">指定要返回的架构的名称。</param>
        /// <param name="restrictionValues">为请求的架构指定一组限制值。</param>
        /// <returns></returns>
        public override DataTable GetSchema(DbConnection conn, String collectionName, String[] restrictionValues) => null;
        #endregion
    }

    /// <summary>网络数据库元数据</summary>
    class NetworkMetaData : DbMetaData
    {
        protected override void OnSetTables(IDataTable[] tables, Migration mode) { }
    }
}