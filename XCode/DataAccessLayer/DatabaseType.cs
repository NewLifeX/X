using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 数据库类型
    /// </summary>
    public enum DatabaseType
    {
        /// <summary>
        /// MS的Access文件数据库
        /// </summary>
        Access = 0,

        /// <summary>
        /// MS的SqlServer数据库
        /// </summary>
        SqlServer = 1,

        /// <summary>
        /// Oracle数据库
        /// </summary>
        Oracle = 2,

        /// <summary>
        /// MySql数据库
        /// </summary>
        MySql = 3,

        /// <summary>
        /// MS的SqlServer2005数据库
        /// </summary>
        SqlServer2005 = 4,

        /// <summary>
        /// SQLite数据库
        /// </summary>
        SQLite = 5
    }
}