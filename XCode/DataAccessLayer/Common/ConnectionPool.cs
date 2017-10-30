using System;
using System.Data;
using System.Data.Common;
using NewLife.Collections;

namespace XCode.DataAccessLayer
{
    /// <summary>连接池</summary>
    /// <remarks>
    /// 默认设置：
    /// 1，最小连接为CPU个数，最小2个最大8个
    /// 2，最大连接1000
    /// 3，空闲时间10s
    /// 4，完全空闲时间60s
    /// </remarks>
    public class ConnectionPool : Pool<DbConnection>
    {
        #region 属性
        /// <summary>工厂</summary>
        public DbProviderFactory Factory { get; set; }

        /// <summary>连接字符串</summary>
        public String ConnectionString { get; set; }
        #endregion

        /// <summary>实例化一个连接池</summary>
        public ConnectionPool()
        {
            Min = Environment.ProcessorCount;
            if (Min < 2) Min = 2;
            if (Min > 8) Min = 8;

            Max = 1000;

            IdleTime = 10;
            AllIdleTime = 60;
        }

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
            catch (DbException ex)
            {
                DAL.WriteLog("Open错误：[{0}]{1}", ex?.GetTrue()?.Message, conn.ConnectionString);
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