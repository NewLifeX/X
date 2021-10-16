using System;
using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Runtime.InteropServices;
using NewLife;
using NewLife.Collections;
using NewLife.Log;
using NewLife.Net;
using TDengineDriver;
using XCode.DataAccessLayer;
using TD = TDengineDriver.TDengine;

namespace XCode.TDengine
{
    /// <summary>数据库连接</summary>
    public partial class TDengineConnection : DbConnection
    {
        #region 属性
        internal IntPtr _handler;
        internal Int32 _transactionLevel;

        /// <summary>连接字符串</summary>
        public override String ConnectionString { get; set; }

        private String _version = String.Empty;
        /// <summary>服务器版本</summary>
        public override String ServerVersion
        {
            get
            {
                if (_handler == IntPtr.Zero) throw new XCodeException("连接未打开");

                if (_version.IsNullOrEmpty())
                    _version = Marshal.PtrToStringAnsi(TD.GetServerInfo(_handler));

                return _version;
            }
        }

        private ConnectionState _state;
        /// <summary>状态</summary>
        public override ConnectionState State => _state;

        /// <summary>数据提供者工厂</summary>
        protected override DbProviderFactory DbProviderFactory => TDengineFactory.Instance;

        /// <summary>事务</summary>
        protected internal virtual TDengineTransaction Transaction { get; set; }

        private readonly String _Database;
        /// <summary>数据库</summary>
        public override String Database => _Database;

        private String _DataSource;
        /// <summary>数据源</summary>
        public override String DataSource => _DataSource;
        #endregion

        #region 构造
        static TDengineConnection()
        {
            var configPath = "C:/TDengine/cfg";

#if !(NET40 || NET45)
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
                configPath = "/etc/taos";
#endif

            TD.Options((Int32)TDengineInitOption.TDDB_OPTION_CONFIGDIR, configPath);
            TD.Options((Int32)TDengineInitOption.TDDB_OPTION_SHELL_ACTIVITY_TIMER, "60");
            TD.Init();

            var h = TD.GetClientInfo();
            if (h != IntPtr.Zero)
            {
                var str = Marshal.PtrToStringAnsi(h);
                XTrace.WriteLine("TDengine v{0}", str);
            }

            AppDomain.CurrentDomain.DomainUnload += (s, e) => TD.Cleanup();
        }

        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            if (disposing) Close();

            base.Dispose(disposing);
        }
        #endregion

        #region 核心方法
        private void SetState(ConnectionState value)
        {
            var originalState = _state;
            if (originalState != value)
            {
                _state = value;
                OnStateChange(new StateChangeEventArgs(originalState, value));
            }
        }

        /// <summary>打开连接</summary>
        public override void Open()
        {
            if (State == ConnectionState.Open) return;

            var connStr = ConnectionString;
            if (connStr.IsNullOrEmpty()) throw new InvalidOperationException("未设置连接字符串");

            var pool = GetPool(ConnectionString);
            _handler = pool.Get();

            SetState(ConnectionState.Open);

            //ChangeDatabase(db);
        }

        /// <summary>关闭连接</summary>
        public override void Close()
        {
#if DEBUG
            //XTrace.WriteLine("State={1} 断开TDengine：server={0}", _DataSource, State);
#endif

            //if (State != ConnectionState.Closed) TD.Close(_handler);

            var pool = GetPool(ConnectionString);
            pool.Put(_handler);
            _handler = IntPtr.Zero;

            Transaction?.Dispose();

            SetState(ConnectionState.Closed);
        }

        /// <summary>执行</summary>
        /// <param name="sql"></param>
        /// <returns></returns>
        public Int32 Execute(String sql)
        {
            using var cmd = CreateCommand();
            cmd.CommandText = sql;
            return cmd.ExecuteNonQuery();
        }
        #endregion

        #region 连接池
        private class MyPool : ObjectPool<IntPtr>
        {
            public String ConnectionString { get; set; }

            protected override IntPtr OnCreate()
            {
                var connStr = ConnectionString;
                if (connStr.IsNullOrEmpty()) throw new InvalidOperationException("未设置连接字符串");

                var builder = new ConnectionStringBuilder(connStr);
                var dataSource = builder["DataSource"] ?? builder["Server"];
                var port = builder["Port"].ToInt();
                //if (port <= 0) port = 6030;

                var user = builder["username"] ?? builder["user"] ?? builder["uid"];
                var pass = builder["password"] ?? builder["pass"] ?? builder["pwd"];
                var db = builder["database"] ?? builder["db"];

                var uri = new NetUri(dataSource);
                if (port > 0) uri.Port = port;
#if DEBUG
                XTrace.WriteLine("连接TDengine：server={0};user={1};pass={2};db={3}", dataSource, user, pass, db);
#endif

                var handler = TD.Connect(uri.Address + "", user, pass, db, (Int16)uri.Port);
                if (handler == IntPtr.Zero) throw new XCodeException("打开数据库连接失败！");

                return handler;
            }

            protected override void OnDispose(IntPtr value)
            {
#if DEBUG
                XTrace.WriteLine("断开TDengine");
#endif
                TD.Close(value);
            }
        }

        private static ConcurrentDictionary<String, IPool<IntPtr>> _cache = new();
        internal static IPool<IntPtr> GetPool(String connStr)
        {
            return _cache.GetOrAdd(connStr, k => new MyPool
            {
                Name = "TDPool",
                ConnectionString = k,
                Min = 1,
                Max = 1000,
                IdleTime = 20,
                AllIdleTime = 120,
#if DEBUG
                Log = XTrace.Log,
#endif
            });
        }
        #endregion

        #region 辅助方法
        /// <summary>创建命令</summary>
        /// <returns></returns>
        protected override DbCommand CreateDbCommand() => new TDengineCommand { Connection = this, Transaction = Transaction };

        /// <summary>开始事务</summary>
        /// <param name="isolationLevel"></param>
        /// <returns></returns>
        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel) => Transaction = new TDengineTransaction(this, isolationLevel);

        /// <summary>改变数据库</summary>
        /// <param name="databaseName"></param>
        public override void ChangeDatabase(String databaseName)
        {
            if (_Database.IsNullOrEmpty() || _Database != databaseName)
            {
                //Int32 result = TD.SelectDatabase(_handler, databaseName);
                //if (result == 0) _Database = databaseName;
            }
        }
        #endregion
    }
}