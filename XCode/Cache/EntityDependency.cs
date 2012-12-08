using System;
using System.Web.Caching;
using NewLife.Threading;

namespace XCode.Cache
{
    /// <summary>实体依赖。用于HttpRuntime.Cache，一旦指定的实体类数据改变，马上让缓存过期。</summary>
    /// <typeparam name="TEntity"></typeparam>
    public class EntityDependency<TEntity> : CacheDependency where TEntity : Entity<TEntity>, new()
    {
        /// <summary>实例化一个实体依赖。</summary>
        public EntityDependency() : this(0) { }

        TimerX timer = null;
        Int64 count = 0;

        /// <summary>
        /// 通过指定一个检查周期实例化一个实体依赖。
        /// 利用线程池定期去检查该实体类的总记录数，一旦改变则让缓存过期。
        /// 这样子就避免了其它方式修改数据而没能及时更新缓存问题
        /// </summary>
        /// <param name="period">检查周期，单位毫秒。必须大于1000（1秒），以免误用。</param>
        public EntityDependency(Int32 period)
        {
            Entity<TEntity>.Meta.OnDataChange += new Action<Type>(Meta_OnDataChange);

            if (period > 1000)
            {
                count = Entity<TEntity>.Meta.LongCount;
                timer = new TimerX(d => CheckCount(), null, period, period);
            }
        }

        void Meta_OnDataChange(Type obj)
        {
            NotifyDependencyChanged(this, EventArgs.Empty);
        }

        void CheckCount()
        {
            if (Entity<TEntity>.Meta.LongCount != count)
            {
                NotifyDependencyChanged(this, EventArgs.Empty);

                if (timer != null) timer.Dispose();
            }
        }

        /// <summary>释放资源</summary>
        protected override void DependencyDispose()
        {
            base.DependencyDispose();

            if (timer != null) timer.Dispose();
        }

        //public override string GetUniqueID()
        //{
        //    return base.GetUniqueID();
        //}
    }
}
