using System;
using System.Collections.Generic;
using System.Diagnostics;
using NewLife.Log;
using XCode.Membership;

namespace XCode.Transform
{
    /// <summary>数据同步，不同实体类之间</summary>
    /// <typeparam name="TSource">源实体类</typeparam>
    /// <typeparam name="TTarget">目标实体类</typeparam>
    public class Sync<TSource, TTarget> : ETL
        where TSource : Entity<TSource>, new()
        where TTarget : Entity<TTarget>, new()
    {
        #region 构造
        /// <summary>实例化数据抽取器</summary>
        public Sync() : base(Entity<TSource>.Meta.Factory)
        {
            Target = Entity<TTarget>.Meta.Factory;
        }
        #endregion

        /// <summary>启动时检查参数</summary>
        public override void Start()
        {
            // 如果目标表为空，则使用仅插入
            if (!InsertOnly)
            {
                if (Target.Count == 0) InsertOnly = true;
            }

            base.Start();
        }

        /// <summary>处理单行数据</summary>
        /// <param name="source">源实体</param>
        protected override IEntity SyncItem(IEntity source)
        {
            var target = GetItem(source, out var isNew);

            SyncItem(source as TSource, target as TTarget, isNew);

            SaveItem(target, isNew);

            return target;
        }

        /// <summary>处理单行数据</summary>
        /// <param name="source">源实体</param>
        /// <param name="target">目标实体</param>
        /// <param name="isNew">是否新增</param>
        protected virtual IEntity SyncItem(TSource source, TTarget target, Boolean isNew)
        {
            // 同名字段对拷
            target?.CopyFrom(source, true);

            return target;
        }
    }

    /// <summary>数据同步，相同实体类不同表和库</summary>
    /// <typeparam name="TSource">源实体类</typeparam>
    public class Sync<TSource> : ETL
        where TSource : Entity<TSource>, new()
    {
        #region 属性
        ///// <summary>源连接</summary>
        //public String SourceConn { get; set; }

        ///// <summary>源表名</summary>
        //public String SourceTable { get; set; }

        /// <summary>目标连接</summary>
        public String TargetConn { get; set; }

        /// <summary>目标表名</summary>
        public String TargetTable { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化数据抽取器</summary>
        public Sync() : base(Entity<TSource>.Meta.Factory)
        {
            var fact = Entity<TSource>.Meta.Factory;

            Target = fact;

            //SourceConn = fact.Table.ConnName;
            //SourceTable = fact.Table.TableName;
        }
        #endregion

        /// <summary>启动时检查参数</summary>
        public override void Start()
        {
            if (TargetConn.IsNullOrEmpty()) throw new ArgumentNullException(nameof(TargetConn));
            if (TargetTable.IsNullOrEmpty()) throw new ArgumentNullException(nameof(TargetTable));

            // 如果目标表为空，则使用仅插入
            if (!InsertOnly)
            {
                var count = Target.Split(TargetConn, TargetTable, () => Target.Count);
                if (count == 0) InsertOnly = true;
            }

            base.Start();
        }

        /// <summary>同步数据列表时，在目标表上执行</summary>
        /// <param name="list"></param>
        /// <param name="set"></param>
        /// <returns></returns>
        protected override Int32 OnSync(IList<IEntity> list, IExtractSetting set)
        {
            return Target.Split(TargetConn, TargetTable, () => base.OnSync(list, set));
        }

        /// <summary>处理单行数据</summary>
        /// <param name="source">源实体</param>
        protected override IEntity SyncItem(IEntity source)
        {
            var isNew = InsertOnly;
            var target = InsertOnly ? source : GetItem(source, out isNew);

            SyncItem(source as TSource, target as TSource, isNew);

            SaveItem(target, isNew);

            return target;
        }

        /// <summary>处理单行数据</summary>
        /// <param name="source">源实体</param>
        /// <param name="target">目标实体</param>
        /// <param name="isNew">是否新增</param>
        protected virtual IEntity SyncItem(TSource source, TSource target, Boolean isNew)
        {
            // 同名字段对拷
            target?.CopyFrom(source, true);

            return target;
        }
    }
}