using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using NewLife.Log;
using XCode.Membership;

namespace XCode.Transform
{
    /// <summary>数据同步，不同实体类之间</summary>
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

        /// <summary>每一轮启动时</summary>
        /// <param name="set"></param>
        /// <returns></returns>
        protected override Boolean Init(IExtractSetting set)
        {
            // 如果目标表为空，则使用仅插入
            var count = Target.Count;
            InsertOnly = count == 0;

            return base.Init(set);
        }

        /// <summary>处理单行数据</summary>
        /// <param name="ctx">数据上下文</param>
        /// <param name="source">源实体</param>
        protected override IEntity ProcessItem(DataContext ctx, IEntity source)
        {
            var isNew = InsertOnly;
            //var target = GetItem(source, ref isNew);
            var target = isNew && source is TTarget ? source : GetItem(source, ref isNew);

            var rs = SyncItem(source as TSource, target as TTarget, isNew);

            if (rs != null) SaveItem(rs, isNew);

            return rs;
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
    public class Sync<TSource> : Sync
        where TSource : Entity<TSource>, new()
    {
        #region 属性
        /// <summary>来源连接</summary>
        public String SourceConn { get; set; }

        /// <summary>来源表名</summary>
        public String SourceTable { get; set; }

        /// <summary>目标连接</summary>
        public String TargetConn { get; set; }

        /// <summary>目标表名</summary>
        public String TargetTable { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化数据抽取器</summary>
        public Sync() : base(Entity<TSource>.Meta.Factory, Entity<TSource>.Meta.Factory)
        {
            var fact = Entity<TSource>.Meta.Factory;
            SourceConn = fact.ConnName;
            SourceTable = fact.TableName;
        }
        #endregion

        /// <summary>启动时检查参数</summary>
        public override void Start()
        {
            if (TargetConn.IsNullOrEmpty()) throw new ArgumentNullException(nameof(TargetConn));
            if (TargetTable.IsNullOrEmpty()) throw new ArgumentNullException(nameof(TargetTable));

            base.Start();
        }

        /// <summary>每一轮启动时</summary>
        /// <param name="set"></param>
        /// <returns></returns>
        protected override Boolean Init(IExtractSetting set)
        {
            // 如果目标表为空，则使用仅插入
            var count = Target.Split(TargetConn, TargetTable, () => Target.Count);
            InsertOnly = count == 0;

            return base.Init(set);
        }

        /// <summary>从来源表查数据</summary>
        /// <param name="ctx">数据上下文</param>
        /// <param name="extracter"></param>
        /// <param name="set">设置</param>
        /// <returns></returns>
        internal protected override IList<IEntity> Fetch(DataContext ctx, IExtracter extracter, IExtractSetting set)
        {
            return Target.Split(SourceConn, SourceTable, () => base.Fetch(ctx, extracter, set));
        }

        /// <summary>同步数据列表时，在目标表上执行</summary>
        /// <param name="ctx">数据上下文</param>
        /// <returns></returns>
        protected override IList<IEntity> OnProcess(DataContext ctx)
        {
            return Target.Split(TargetConn, TargetTable, () => base.OnProcess(ctx));
        }

        /// <summary>处理单行数据</summary>
        /// <param name="ctx">数据上下文</param>
        /// <param name="source">源实体</param>
        protected override IEntity ProcessItem(DataContext ctx, IEntity source)
        {
            var isNew = InsertOnly;
            var target = isNew && Target.EntityType == Extracter.Factory.EntityType ? source : GetItem(source, ref isNew);

            var rs = SyncItem(source as TSource, target as TSource, isNew);

            if (rs != null) SaveItem(rs, isNew);

            return rs;
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

    /// <summary>数据同步</summary>
    public class Sync : ETL
    {
        #region 属性
        /// <summary>目标实体工厂。分批统计时不需要设定</summary>
        public IEntityOperate Target { get; set; }

        /// <summary>仅插入，不用判断目标是否已有数据</summary>
        public Boolean InsertOnly { get; set; }
        #endregion

        #region 构造
        /// <summary>实例化数据抽取器</summary>
        public Sync() : base() { }

        /// <summary>实例化数据同步</summary>
        /// <param name="source"></param>
        /// <param name="target"></param>
        public Sync(IEntityOperate source, IEntityOperate target) : base(source) { Target = target; }
        #endregion

        #region 数据处理
        /// <summary>抽取数据</summary>
        /// <param name="ctx">数据上下文</param>
        /// <param name="extracter"></param>
        /// <param name="set">设置</param>
        /// <returns></returns>
        internal protected override IList<IEntity> Fetch(DataContext ctx, IExtracter extracter, IExtractSetting set)
        {
            var list = base.Fetch(ctx, extracter, set);

            // 如果一批数据为空，可能是追到了尽头
            if (list == null || list.Count == 0) InsertOnly = false;

            return list;
        }
        #endregion

        #region 数据同步
        /// <summary>处理列表，带事务保护，传递批次配置，支持多线程</summary>
        /// <param name="ctx">数据上下文</param>
        protected override IList<IEntity> OnProcess(DataContext ctx)
        {
            // 批量事务提交
            var fact = Target;
            if (fact == null) throw new ArgumentNullException(nameof(Target));

            using (var tran = fact.CreateTrans())
            {
                var rs = base.OnProcess(ctx);

                tran.Commit();

                return rs;
            }
        }

        /// <summary>同步单行数据</summary>
        /// <param name="ctx">数据上下文</param>
        /// <param name="source">源实体</param>
        /// <returns></returns>
        protected override IEntity ProcessItem(DataContext ctx, IEntity source)
        {
            var isNew = InsertOnly;
            var target = isNew ? source : GetItem(source, ref isNew);

            // 同名字段对拷
            target?.CopyFrom(source, true);

            SaveItem(target, isNew);

            return target;
        }

        /// <summary>根据源实体获取目标实体</summary>
        /// <param name="source">源实体</param>
        /// <param name="isNew">是否新增</param>
        /// <returns></returns>
        protected virtual IEntity GetItem(IEntity source, ref Boolean isNew)
        {
            var key = source[Extracter.Factory.Unique.Name];

            // 查找目标，如果不存在则创建
            var fact = Target;
            var target = isNew ? null : fact.FindByKey(key);
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
            if (target == null) return;

            var st = Stat;
            if (isNew)
                target.Insert();
            else if (target.HasDirty)
            {
                target.Update();
                st.Changes++;
            }
        }
        #endregion
    }
}