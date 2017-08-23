using System;
using System.Collections.Generic;
using System.Diagnostics;
using NewLife.Log;
using XCode.Membership;

namespace XCode.Transform
{
    /// <summary>数据同步</summary>
    /// <typeparam name="TSource">源实体类</typeparam>
    /// <typeparam name="TTarget">目标实体类</typeparam>
    public class Sync<TSource, TTarget> : Sync
        where TSource : Entity<TSource>, new()
        where TTarget : Entity<TTarget>, new()
    {
        #region 构造
        /// <summary>实例化数据抽取器</summary>
        public Sync() : base(Entity<TSource>.Meta.Factory, Entity<TTarget>.Meta.Factory) { }
        #endregion

        /// <summary>处理单行数据</summary>
        /// <param name="source">源实体</param>
        /// <param name="target">目标实体</param>
        /// <param name="isNew">是否新增</param>
        protected override IEntity ProcessItem(IEntity source, IEntity target, Boolean isNew)
        {
            return ProcessItem(source as TSource, target as TTarget, isNew);
        }

        /// <summary>处理单行数据</summary>
        /// <param name="source">源实体</param>
        /// <param name="target">目标实体</param>
        /// <param name="isNew">是否新增</param>
        protected virtual IEntity ProcessItem(TSource source, TTarget target, Boolean isNew)
        {
            target.CopyFrom(source, true);

            return target;
        }
    }

    public class Sync : ETL
    {
        #region 属性
        /// <summary>目标实体工厂。分批统计时不需要设定</summary>
        public IEntityOperate Target { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化数据抽取器</summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public Sync(IEntityOperate source, IEntityOperate target) : base(source)
        {
            Target = target;
        }
        #endregion

        #region 数据同步
        /// <summary>处理列表，批量事务提交</summary>
        /// <param name="list"></param>
        protected override void ProcessList(IList<IEntity> list)
        {
            // 批量事务提交
            var fact = Target;
            fact?.BeginTransaction();
            try
            {
                foreach (var source in list)
                {
                    try
                    {
                        // 有目标跟没有目标处理方式不同
                        if (fact != null)
                        {
                            var target = GetItem(source, out var isNew);
                            target = ProcessItem(source, target, isNew);
                            SaveItem(target, isNew);
                        }
                        else
                        {
                            ProcessItem(source, null, false);
                        }
                    }
                    catch (Exception ex)
                    {
                        ex = OnError(source, ex);
                        if (ex != null) throw ex;
                    }
                }
                fact?.Commit();
            }
            catch
            {
                fact?.Rollback();
                throw;
            }
        }

        /// <summary>处理单行数据</summary>
        /// <remarks>打开AutoSave时，上层ProcessList会自动保存数据</remarks>
        /// <param name="source">源实体</param>
        /// <param name="target">目标实体</param>
        /// <param name="isNew">是否新增</param>
        /// <returns></returns>
        protected virtual IEntity ProcessItem(IEntity source, IEntity target, Boolean isNew)
        {
            // 同名字段对拷
            target?.CopyFrom(source, true);

            return target;
        }

        /// <summary>根据源实体获取目标实体</summary>
        /// <param name="source">源实体</param>
        /// <param name="isNew">是否新增</param>
        /// <returns></returns>
        protected virtual IEntity GetItem(IEntity source, out Boolean isNew)
        {
            var key = source[Extracter.Factory.Unique.Name];

            // 查找目标，如果不存在则创建
            isNew = false;
            var fact = Target;
            var target = fact.FindByKey(key);
            if (target == null)
            {
                target = fact.Create();
                target[fact.Unique.Name] = key;
                isNew = true;
            }

            return target;
        }

        /// <summary>保存目标实体</summary>
        /// <param name="target"></param>
        /// <param name="isNew"></param>
        protected virtual void SaveItem(IEntity target, Boolean isNew)
        {
            // 自动保存
            //if (AutoSave)
            //{
            var st = Stat;
            if (isNew)
            {
                target.Insert();
                st.Total++;
            }
            else
            {
                target.Update();
                st.TotalUpdate++;
            }
            //}
        }
        #endregion
    }
}