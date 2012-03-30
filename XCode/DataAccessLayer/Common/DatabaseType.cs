using System;
using System.Collections.Generic;
using System.Text;
using System.ComponentModel;

namespace XCode.DataAccessLayer
{
    /// <summary>数据库类型</summary>
    public enum DatabaseType
    {
        /// <summary>MS的Access文件数据库</summary>
        [Description("Access文件数据库")]
        Access = 0,

        /// <summary>MS的SqlServer数据库</summary>
        [Description("SqlServer数据库")]
        SqlServer = 1,

        /// <summary>Oracle数据库</summary>
        [Description("Oracle数据库")]
        Oracle = 2,

        /// <summary>MySql数据库</summary>
        [Description("MySql数据库")]
        MySql = 3,

        /// <summary>SqlCe数据库</summary>
        [Description("SqlCe数据库")]
        SqlCe = 4,

        /// <summary>SQLite数据库</summary>
        [Description("SQLite数据库")]
        SQLite = 5,

        /// <summary>Firebird数据库</summary>
        [Description("Firebird数据库")]
        Firebird = 6,

        /// <summary>SqlCe数据库</summary>
        [Description("PostgreSQL数据库")]
        PostgreSQL = 7,

        /// <summary>网络虚拟数据库</summary>
        [Description("网络虚拟数据库")]
        Network = 100,

        /// <summary>分布式数据库</summary>
        [Description("分布式数据库")]
        Distributed = 888,

        /// <summary>外部数据库</summary>
        [Description("外部数据库")]
        Other = 999
    }
}