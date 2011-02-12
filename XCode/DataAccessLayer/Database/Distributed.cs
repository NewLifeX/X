using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;

namespace XCode.DataAccessLayer.Database
{
    /// <summary>
    /// 分布式数据库。同时操作多个数据库，多读多写，读写分析。
    /// 支持：
    /// 1，一主多从写入（主库同步写入从库异步写入）；
    /// 2，多主多从写入（主库同步写入从库异步写入）；
    /// 3，按权重分布式读取；
    /// </summary>
    /// <remarks>
    /// 1，通过连接字符串配置读写服务器组，并加上权重，如“WriteServer='connA*1,connC*0' ReadServer='connB*8,connD'”；
    /// 2，对于写服务器，权重大于0表示作为主服务器，操作返回值取主服务器操作总和，等于0表示作为从服务器，采用异步方式写入，不设置权重表示0，全部不设置权重表示1；
    /// 3，对于读服务器，默认根据权重进行随机分配，不设置表示1；
    /// 4，对于读服务器，可优先考虑最近使用的数据库
    /// </remarks>
    class Distributed : DbBase
    {
        #region 属性
        /// <summary>
        /// 返回数据库类型。
        /// </summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.Distributed; }
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
            return new DistributedDbSession();
        }

        /// <summary>
        /// 创建元数据对象
        /// </summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData()
        {
            return new DistributedDbMetaData();
        }
        #endregion
    }

    class DistributedDbSession : DbSession
    {

    }

    class DistributedDbMetaData : DbMetaData
    {

    }
}