using System;
using System.Collections.Generic;

namespace XCode
{
    /// <summary>实体数据存取器接口</summary>
    public interface IEntityAccessor
    {
        /// <summary>
        /// 使用参数进行初始化
        /// </summary>
        /// <param name="ps"></param>
        void Init(IDictionary<String, Object> ps);

        /// <summary>是否支持从实体对象读取信息</summary>
        Boolean CanRead { get; }

        /// <summary>是否支持把信息写入到实体对象</summary>
        Boolean CanWrite { get; }

        /// <summary>
        /// 从实体对象读取信息
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="eop">实体操作。为空时由内部构建，但可在遍历调用访问器时由外部构造一次传入，以提高性能。</param>
        void Read(IEntity entity, IEntityOperate eop = null);

        /// <summary>
        /// 把信息写入到实体对象
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="eop">实体操作。为空时由内部构建，但可在遍历调用访问器时由外部构造一次传入，以提高性能。</param>
        void Write(IEntity entity, IEntityOperate eop = null);
    }

    /// <summary>实体数据存取器接口</summary>
    public interface IEntityAccessor<TEntity> : IEntityAccessor where TEntity : Entity<TEntity>, new()
    {
        /// <summary>
        /// 从实体对象读取信息
        /// </summary>
        /// <param name="entity">实体对象</param>
        void Read(TEntity entity);

        /// <summary>
        /// 把信息写入到实体对象
        /// </summary>
        /// <param name="entity">实体对象</param>
        void Write(TEntity entity);
    }
}