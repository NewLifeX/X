using System;
using NewLife.Reflection;
using NewLife.Log;
using NewLife.Configuration;
using XCode.DataAccessLayer;

namespace XCode.Cache
{
    /// <summary>
    /// 缓存基类
    /// </summary>
    public abstract class CacheBase<TEntity> where TEntity : Entity<TEntity>, new()
    {
        #region 属性
        private String _ConnName;
        /// <summary>连接名</summary>
        public String ConnName
        {
            get { return _ConnName; }
            set { _ConnName = value; }
        }

        private String _TableName;
        /// <summary>表名</summary>
        public String TableName
        {
            get { return _TableName; }
            set { _TableName = value; }
        }
        #endregion

        /// <summary>
        /// 调用填充方法前设置连接名和表名，调用后还原
        /// </summary>
        internal void InvokeFill(Func callback)
        {
            String cn = Entity<TEntity>.Meta.ConnName;
            String tn = Entity<TEntity>.Meta.TableName;

            if (cn != ConnName) Entity<TEntity>.Meta.ConnName = ConnName;
            if (tn != TableName) Entity<TEntity>.Meta.TableName = TableName;

            try
            {
                callback();
            }
            catch (Exception ex)
            {
                //if (Config.GetConfig<Boolean>("XCode.Debug")) XTrace.WriteLine(ex.ToString());
                if (DAL.Debug) DAL.WriteLog(ex.ToString());
            }
            finally
            {
                if (cn != ConnName) Entity<TEntity>.Meta.ConnName = cn;
                if (tn != TableName) Entity<TEntity>.Meta.TableName = tn;
            }
        }
    }
}
