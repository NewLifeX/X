using System;

namespace XCode.Accessors
{
    /// <summary>实体数据访问器接口</summary>
    public interface IEntityAccessor
    {
        /// <summary>设置参数。返回自身，方便链式写法。</summary>>
        /// <param name="name">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        IEntityAccessor SetConfig(String name, Object value);

        /// <summary>设置参数。返回自身，方便链式写法。</summary>>
        /// <param name="option">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        IEntityAccessor SetConfig(EntityAccessorOptions option, Object value);

        /// <summary>是否支持从外部读取信息</summary>
        Boolean CanRead { get; }

        /// <summary>是否支持把信息写入到外部</summary>
        Boolean CanWrite { get; }

        /// <summary>外部=>实体，从外部读取信息并写入到实体对象</summary>>
        /// <param name="entity">实体对象</param>
        /// <param name="eop">实体操作。为空时由内部构建，但可在遍历调用访问器时由外部构造一次传入，以提高性能。</param>
        void Read(IEntity entity, IEntityOperate eop = null);

        /// <summary>实体=>外部，从实体对象读取信息并写入外部</summary>>
        /// <param name="entity">实体对象</param>
        /// <param name="eop">实体操作。为空时由内部构建，但可在遍历调用访问器时由外部构造一次传入，以提高性能。</param>
        void Write(IEntity entity, IEntityOperate eop = null);

        /// <summary>从实体对象读取指定实体字段的信息后触发</summary>
        event EventHandler<EntityAccessorEventArgs> OnReadItem;

        /// <summary>把指定实体字段的信息写入到实体对象后触发</summary>
        event EventHandler<EntityAccessorEventArgs> OnWriteItem;

        /// <summary>读写异常发生时触发</summary>
        event EventHandler<EntityAccessorEventArgs> OnError;
    }

    ///// <summary>实体数据存取器接口</summary>
    //public interface IEntityAccessor<TEntity> : IEntityAccessor where TEntity : Entity<TEntity>, new()
    //{
    //    /// <summary>
    //    /// 从实体对象读取信息
    //    /// </summary>
    //    /// <param name="entity">实体对象</param>
    //    void Read(TEntity entity);

    //    /// <summary>
    //    /// 把信息写入到实体对象
    //    /// </summary>
    //    /// <param name="entity">实体对象</param>
    //    void Write(TEntity entity);
    //}
}