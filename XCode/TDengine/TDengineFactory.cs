using System.Data.Common;

namespace XCode.TDengine
{
    /// <summary>TDengine工厂</summary>
    /// <remarks>
    /// 参考SQLite驱动建立架构
    /// </remarks>
    public class TDengineFactory : DbProviderFactory
    {
        private TDengineFactory() { }

        /// <summary>实例</summary>
        public static readonly TDengineFactory Instance = new TDengineFactory();

        /// <summary>创建命令</summary>
        /// <returns></returns>
        public override DbCommand CreateCommand() => new TDengineCommand();

        /// <summary>创建连接</summary>
        /// <returns></returns>
        public override DbConnection CreateConnection() => new TDengineConnection();

        /// <summary>创建连接字符串生成器</summary>
        /// <returns></returns>
        public override DbConnectionStringBuilder CreateConnectionStringBuilder() => new DbConnectionStringBuilder();

        /// <summary>创建参数</summary>
        /// <returns></returns>
        public override DbParameter CreateParameter() => new TDengineParameter();
    }
}