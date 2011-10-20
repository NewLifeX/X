using System;
using NewLife;
using XCode.Configuration;

namespace XCode.Accessors
{
    /// <summary>实体访问器基类</summary>
    public abstract class EntityAccessorBase
    {
        #region 事件
        /// <summary>
        /// 从实体对象读取指定实体字段的信息后触发
        /// </summary>
        public virtual event EventHandler<EventArgs<IEntity, FieldItem>> OnRead;

        /// <summary>
        /// 把指定实体字段的信息写入到实体对象后触发
        /// </summary>
        public virtual event EventHandler<EventArgs<IEntity, FieldItem>> OnWrite;
        #endregion

        #region IEntityAccessor 成员
        /// <summary>是否支持从实体对象读取信息</summary>
        public virtual bool CanRead { get { return true; } }

        /// <summary>是否支持把信息写入到实体对象</summary>
        public virtual bool CanWrite { get { return true; } }

        /// <summary>
        /// 从实体对象读取信息
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="eop">实体操作。为空时由内部构建，但可在遍历调用访问器时由外部构造一次传入，以提高性能。</param>
        public virtual void Read(IEntity entity, IEntityOperate eop = null)
        {
            if (!CanRead) return;

            if (entity == null) throw new ArgumentNullException("entity");

            if (eop == null) eop = EntityFactory.CreateOperate(entity.GetType());
            foreach (FieldItem item in eop.AllFields)
            {
                OnWriteItem(entity, item);

                if (OnRead != null) OnRead(this, new EventArgs<IEntity, FieldItem>(entity, item));
            }
        }

        /// <summary>
        /// 从实体对象读取指定实体字段的信息
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="item">实体字段</param>
        protected virtual void OnReadItem(IEntity entity, FieldItem item) { }

        /// <summary>
        /// 把信息写入到实体对象
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="eop">实体操作。为空时由内部构建，但可在遍历调用访问器时由外部构造一次传入，以提高性能。</param>
        public virtual void Write(IEntity entity, IEntityOperate eop = null)
        {
            if (!CanWrite) return;

            if (entity == null) throw new ArgumentNullException("entity");

            if (eop == null) eop = EntityFactory.CreateOperate(entity.GetType());
            foreach (FieldItem item in eop.AllFields)
            {
                OnWriteItem(entity, item);

                if (OnWrite != null) OnWrite(this, new EventArgs<IEntity, FieldItem>(entity, item));
            }
        }

        /// <summary>
        /// 把指定实体字段的信息写入到实体对象
        /// </summary>
        /// <param name="entity">实体对象</param>
        /// <param name="item">实体字段</param>
        protected virtual void OnWriteItem(IEntity entity, FieldItem item) { }
        #endregion
    }
}