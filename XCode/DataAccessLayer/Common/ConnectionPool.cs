using System;
using System.Data;
using System.Data.Common;
using NewLife.Collections;

namespace XCode.DataAccessLayer
{
    /// <summary>连接池</summary>
    public class ConnectionPool : Pool<DbConnection>
    {
        #region 属性
        /// <summary>工厂</summary>
        public DbProviderFactory Factory { get; set; }

        /// <summary>连接字符串</summary>
        public String ConnectionString { get; set; }
        #endregion

        /// <summary>创建时连接数据库</summary>
        /// <returns></returns>
        protected override DbConnection Create()
        {
            var conn = Factory.CreateConnection();
            conn.ConnectionString = ConnectionString;

            try
            {
                conn.Open();
            }
            catch (DbException)
            {
                DAL.WriteLog("Open错误：{0}", conn.ConnectionString);
                throw;
            }

            return conn;
        }

        /// <summary>申请时检查是否打开</summary>
        /// <param name="value"></param>
        protected override Boolean OnAcquire(DbConnection value)
        {
            try
            {
                if (value.State == ConnectionState.Closed) value.Open();

                return true;
            }
            catch { return false; }
        }

        /// <summary>释放时，返回是否有效。无效对象将会被抛弃</summary>
        /// <param name="value"></param>
        protected override Boolean OnRelease(DbConnection value)
        {
            try
            {
                return value.State == ConnectionState.Open;
            }
            catch { return false; }
        }

        ///// <summary>销毁时关闭连接</summary>
        ///// <param name="value"></param>
        //protected override void OnDestroy(DbConnection value)
        //{
        //    value?.Close();
        //}
    }
}