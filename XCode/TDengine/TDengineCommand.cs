using System;
using System.Data;
using System.Data.Common;
using System.Threading.Tasks;
using NewLife;
using TD = TDengineDriver.TDengine;

namespace XCode.TDengine
{
    /// <summary>命令</summary>
    public class TDengineCommand : DbCommand
    {
        #region 属性
        /// <summary>命令类型</summary>
        public override CommandType CommandType { get; set; }

        /// <summary>命令文本</summary>
        public override String CommandText { get; set; }

        /// <summary>命令超时</summary>
        public override Int32 CommandTimeout { get; set; } = 30;

        /// <summary>数据库连接</summary>
        protected override DbConnection DbConnection { get; set; }

        /// <summary>数据库事务</summary>
        protected override DbTransaction DbTransaction { get; set; }

        /// <summary>参数集合</summary>
        protected override DbParameterCollection DbParameterCollection { get; } = new TDengineParameterCollection();

        /// <summary>设计时可见</summary>
        public override Boolean DesignTimeVisible { get; set; } = true;

        /// <summary>更新行来源</summary>
        public override UpdateRowSource UpdatedRowSource { get; set; }
        #endregion

        #region 方法
        /// <summary>创建参数</summary>
        /// <returns></returns>
        protected override DbParameter CreateDbParameter() => new TDengineParameter();

        /// <summary>准备执行命令</summary>
        public override void Prepare()
        {
            var conn = DbConnection;
            if (conn?.State != ConnectionState.Open) throw new InvalidOperationException("数据库连接未打开");

            if (CommandText.IsNullOrEmpty()) throw new InvalidOperationException("未设置命令文本");
        }

        /// <summary>执行查询</summary>
        /// <param name="behavior"></param>
        /// <returns></returns>
        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            var conn = DbConnection as TDengineConnection;
            if (conn?.State != ConnectionState.Open) conn.Open();

            Prepare();

            var sql = CommandText;
            foreach (TDengineParameter tp in DbParameterCollection)
            {
                var val = Type.GetTypeCode(tp.Value?.GetType()) switch
                {
                    TypeCode.Boolean => (tp.Value + "").ToLower(),
                    TypeCode.Byte or TypeCode.Char or TypeCode.SByte => tp.Value,
                    TypeCode.DateTime => tp.Value.ToDateTime().ToLong(),
                    TypeCode.DBNull => "",
                    TypeCode.Single or TypeCode.Decimal or TypeCode.Double => tp.Value,
                    TypeCode.Int16 => tp.Value,
                    TypeCode.Int32 => tp.Value,
                    TypeCode.Int64 => tp.Value,
                    TypeCode.UInt16 => tp.Value,
                    TypeCode.UInt32 => tp.Value,
                    TypeCode.UInt64 => tp.Value,
                    _ => tp.Value,
                };
                sql = sql.Replace(tp.ParameterName, val + "");
            }

            var task = Task.Factory.StartNew(() => TD.Query(conn._handler, sql));
            if (!task.Wait(TimeSpan.FromSeconds(CommandTimeout))) throw new XCodeException("执行超时");

            var handler = task.Result;
            var code = TD.ErrorNo(handler);
            if (handler == IntPtr.Zero) code = TD.ErrorNo(conn._handler);
            if (code != 0) throw new XCodeException(TD.Error(handler) ?? $"执行出错[{code}]。");

            return new TDengineDataReader(this, behavior, handler);
        }

        /// <summary>执行命令</summary>
        /// <returns></returns>
        public override Int32 ExecuteNonQuery()
        {
            Prepare();

            using var reader = ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult);
            while (reader.NextResult()) ;

            return reader.RecordsAffected;
        }

        /// <summary>执行查询</summary>
        /// <returns></returns>
        public override Object ExecuteScalar()
        {
            Prepare();

            using var reader = ExecuteReader(CommandBehavior.SingleRow | CommandBehavior.SingleResult);
            return reader.Read() && reader.FieldCount > 0 ? reader.GetValue(0) : null;
        }

        /// <summary>取消</summary>
        public override void Cancel() { }
        #endregion
    }
}