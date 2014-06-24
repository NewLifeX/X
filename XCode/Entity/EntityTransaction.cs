using System;
using NewLife;
using XCode.DataAccessLayer;

namespace XCode
{
    /// <summary>实体事务区域。配合using使用，进入区域事务即开始，直到<see cref="EntityTransaction.Commit"/>提交，否则离开区域时回滚。</summary>
    /// <typeparam name="TEntity"></typeparam>
    /// <example>
    /// <code>
    /// using (var et = new EntityTransaction&lt;Administrator&gt;())
    /// {
    ///     var admin = Administrator.FindByName("admin");
    ///     admin.Logins++;
    ///     admin.Update();
    /// 
    ///     et.Commit();
    /// }
    /// </code>
    /// </example>
    public class EntityTransaction<TEntity> : EntityTransaction where TEntity : Entity<TEntity>, new()
    {
        /// <summary>为实体类实例化一个事务区域</summary>
        public EntityTransaction() : base(null as IDbSession) { Entity<TEntity>.Meta.Session.BeginTrans(); }

        /// <summary>提交事务</summary>
        public override void Commit()
        {
            Entity<TEntity>.Meta.Session.Commit();

            hasFinish = true;
        }

        /// <summary>回滚事务</summary>
        protected override void Rollback()
        {
            // 回滚时忽略异常
            Entity<TEntity>.Meta.Session.Rollback();

            hasFinish = true;
        }
    }

    /// <summary>实体事务区域。配合using使用，进入区域事务即开始，直到<see cref="Commit"/>提交，否则离开区域时回滚。</summary>
    /// <example>
    /// <code>
    /// using (var et = new EntityTransaction(DAL.Create("Common")))
    /// {
    ///     var admin = Administrator.FindByName("admin");
    ///     admin.Logins++;
    ///     admin.Update();
    /// 
    ///     et.Commit();
    /// }
    /// </code>
    /// </example>
    public class EntityTransaction : DisposeBase
    {
        #region 属性
        private Boolean hasStart;
        /// <summary>是否已完成事务</summary>
        protected Boolean hasFinish;

        private IDbSession _Session;
        /// <summary>会话</summary>
        public IDbSession Session { get { return _Session; } private set { _Session = value; } }
        #endregion

        #region 构造
        /// <summary>用数据库会话来实例化一个事务区域</summary>
        /// <param name="session"></param>
        public EntityTransaction(IDbSession session)
        {
            Session = session;
            if (session != null)
            {
                session.BeginTransaction();
                hasStart = true;
            }
        }

        /// <summary>用数据访问对象来实例化一个事务区域</summary>
        /// <param name="dal"></param>
        public EntityTransaction(DAL dal) : this(dal.Session) { }

        /// <summary>用实体操作接口来实例化一个事务区域</summary>
        /// <param name="eop"></param>
        public EntityTransaction(IEntityOperate eop) : this(DAL.Create(eop.ConnName).Session) { }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）。
        /// 因为该方法只会被调用一次，所以该参数的意义不太大。</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (hasStart && !hasFinish) Rollback();
        }
        #endregion

        #region 方法
        /// <summary>提交事务</summary>
        public virtual void Commit()
        {
            Session.Commit();

            hasFinish = true;
        }

        /// <summary>回滚事务</summary>
        protected virtual void Rollback()
        {
            // 回滚时忽略异常
            Session.Rollback(true);

            hasFinish = true;
        }
        #endregion
    }
}