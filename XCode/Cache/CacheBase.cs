using System;
using NewLife;
using NewLife.Configuration;
using NewLife.Reflection;
using XCode.DataAccessLayer;

namespace XCode.Cache
{
    /// <summary>缓存基类</summary>
    public abstract class CacheBase<TEntity> : CacheBase where TEntity : Entity<TEntity>, new()
    {
        #region 属性
        private String _ConnName;
        /// <summary>连接名</summary>
        public String ConnName { get { return _ConnName; } set { _ConnName = value; } }

        private String _TableName;
        /// <summary>表名</summary>
        public String TableName { get { return _TableName; } set { _TableName = value; } }
        #endregion

        /// <summary>调用填充方法前设置连接名和表名，调用后还原</summary>
        internal void InvokeFill(Func callback)
        {
            var cn = Entity<TEntity>.Meta.ConnName;
            var tn = Entity<TEntity>.Meta.TableName;

            if (cn != ConnName) Entity<TEntity>.Meta.ConnName = ConnName;
            if (tn != TableName) Entity<TEntity>.Meta.TableName = TableName;

            try
            {
                callback();
            }
            // 屏蔽对象销毁异常
            catch (ObjectDisposedException) { }
            catch (Exception ex)
            {
                // 无效操作，句柄未初始化，不用出现
                if (ex is InvalidOperationException && ex.Message.Contains("句柄未初始化")) return;
                if (DAL.Debug) DAL.WriteLog(ex.ToString());
            }
            finally
            {
                if (cn != ConnName) Entity<TEntity>.Meta.ConnName = cn;
                if (tn != TableName) Entity<TEntity>.Meta.TableName = tn;
            }
        }
    }

    /// <summary>缓存基类</summary>
    public abstract class CacheBase : DisposeBase
    {
        #region 设置
        /// <summary>是否调试缓存模块</summary>
        public static Boolean Debug { get { return CacheSetting.Debug; } }
        #endregion
    }
}