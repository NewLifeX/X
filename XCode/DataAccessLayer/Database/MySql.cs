using System;
using System.Collections.Generic;
using System.Text;
using System.Data.Common;
using System.Reflection;
using NewLife.Log;

namespace XCode.DataAccessLayer
{
    class MySql : DbBase
    {
        #region 属性
        /// <summary>
        /// 返回数据库类型。
        /// </summary>
        public override DatabaseType DbType
        {
            get { return DatabaseType.MySql; }
        }

        private static DbProviderFactory _dbProviderFactory;
        /// <summary>
        /// 静态构造函数
        /// </summary>
        static DbProviderFactory dbProviderFactory
        {
            get
            {
                if (_dbProviderFactory == null)
                {
                    //反射实现获取数据库工厂
                    Assembly asm = Assembly.LoadFile("MySql.Data.dll");
                    Type type = asm.GetType("MySqlClientFactory");
                    FieldInfo field = type.GetField("Instance");
                    _dbProviderFactory = field.GetValue(null) as DbProviderFactory;
                }
                return _dbProviderFactory;
            }
        }

        /// <summary>工厂</summary>
        public override DbProviderFactory Factory
        {
            get { return _dbProviderFactory; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 创建数据库会话
        /// </summary>
        /// <returns></returns>
        protected override IDbSession OnCreateSession()
        {
            return new MySqlSession();
        }

        /// <summary>
        /// 创建元数据对象
        /// </summary>
        /// <returns></returns>
        protected override IMetaData OnCreateMetaData()
        {
            return new MySqlMetaData();
        }
        #endregion

        #region 分页
        /// <summary>
        /// 已重写。获取分页
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <param name="startRowIndex">开始行，0开始</param>
        /// <param name="maximumRows">最大返回行数</param>
        /// <param name="keyColumn">主键列。用于not in分页</param>
        /// <returns></returns>
        public override string PageSplit(string sql, Int32 startRowIndex, Int32 maximumRows, string keyColumn)
        {
            // 从第一行开始，不需要分页
            if (startRowIndex <= 0)
            {
                if (maximumRows < 1)
                    return sql;
                else
                    return String.Format("{0} limit {1}", sql, maximumRows);
            }
            if (maximumRows < 1)
                throw new NotSupportedException("不支持取第几条数据之后的所有数据！");
            else
                sql = String.Format("{0} limit {1}, {2}", sql, startRowIndex, maximumRows);
            return sql;
        }

        public override string PageSplit(SelectBuilder builder, int startRowIndex, int maximumRows, string keyColumn)
        {
            return PageSplit(builder.ToString(), startRowIndex, maximumRows, keyColumn);
        }
        #endregion
    }

    /// <summary>
    /// MySql数据库
    /// </summary>
    internal class MySqlSession : DbSession
    {
        #region 属性
        ///// <summary>
        ///// 返回数据库类型。
        ///// </summary>
        //public override DatabaseType DbType
        //{
        //    get { return DatabaseType.MySql; }
        //}

        //private static DbProviderFactory _dbProviderFactory;
        ///// <summary>
        ///// 静态构造函数
        ///// </summary>
        //static DbProviderFactory dbProviderFactory
        //{
        //    get
        //    {
        //        if (_dbProviderFactory == null)
        //        {
        //            //反射实现获取数据库工厂
        //            Assembly asm = Assembly.LoadFile("MySql.Data.dll");
        //            Type type = asm.GetType("MySqlClientFactory");
        //            FieldInfo field = type.GetField("Instance");
        //            _dbProviderFactory = field.GetValue(null) as DbProviderFactory;
        //        }
        //        return _dbProviderFactory;
        //    }
        //}

        ///// <summary>工厂</summary>
        //public override DbProviderFactory Factory
        //{
        //    get { return _dbProviderFactory; }
        //}
        #endregion

        #region 基本方法 查询/执行
        /// <summary>
        /// 执行插入语句并返回新增行的自动编号
        /// </summary>
        /// <param name="sql">SQL语句</param>
        /// <returns>新增行的自动编号</returns>
        public override Int64 InsertAndGetIdentity(string sql)
        {
            ExecuteTimes++;
            //SQLServer写法
            sql = "SET NOCOUNT ON;" + sql + ";Select LAST_INSERT_ID()";
            if (Debug) WriteLog(sql);
            try
            {
                DbCommand cmd = PrepareCommand();
                cmd.CommandText = sql;
                return Int64.Parse(cmd.ExecuteScalar().ToString());
            }
            catch (DbException ex)
            {
                throw OnException(ex, sql);
            }
            finally { AutoClose(); }
        }
        #endregion
    }

    /// <summary>
    /// MySql元数据
    /// </summary>
    class MySqlMetaData : DbMetaData
    {

    }
}