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

        /// <summary>调用填充方法前设置连接名和表名，调用后还原</summary>
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
        private static Boolean? _Debug;
        /// <summary>是否调试缓存模块</summary>
        public static Boolean Debug
        {
            get
            {
                if (_Debug != null) return _Debug.Value;

                _Debug = Config.GetConfig<Boolean>("XCode.Cache.Debug", false);

                return _Debug.Value;
            }
            set { _Debug = value; }
        }
        #endregion
    }
}