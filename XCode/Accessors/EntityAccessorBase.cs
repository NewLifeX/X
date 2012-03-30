using System;
using System.Collections.Generic;
using XCode.Configuration;
using XCode.Exceptions;

namespace XCode.Accessors
{
    /// <summary>实体访问器基类</summary>
    abstract class EntityAccessorBase : IEntityAccessor
    {
        #region 属性
        private Boolean _AllFields = true;
        /// <summary>是否所有字段</summary>
        public virtual Boolean AllFields
        {
            get { return _AllFields; }
            set { _AllFields = value; }
        }
        #endregion

        #region 事件
        /// <summary>从实体对象读取指定实体字段的信息后触发</summary>
        public virtual event EventHandler<EntityAccessorEventArgs> OnReadItem;

        /// <summary>把指定实体字段的信息写入到实体对象后触发</summary>
        public virtual event EventHandler<EntityAccessorEventArgs> OnWriteItem;

        /// <summary>读写异常发生时触发</summary>
        public virtual event EventHandler<EntityAccessorEventArgs> OnError;
        #endregion

        #region IEntityAccessor 成员
        /// <summary>设置参数。返回自身，方便链式写法。</summary>>
        /// <param name="name">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        public virtual IEntityAccessor SetConfig(String name, Object value)
        {
            if (name.EqualIgnoreCase(EntityAccessorOptions.AllFields)) AllFields = (Boolean)value;

            return this;
        }

        /// <summary>设置参数。返回自身，方便链式写法。</summary>>
        /// <param name="option">参数名</param>
        /// <param name="value">参数值</param>
        /// <returns></returns>
        IEntityAccessor IEntityAccessor.SetConfig(EntityAccessorOptions option, Object value)
        {
            return SetConfig(option.ToString(), value);
        }

        /// <summary>是否支持从外部读取信息</summary>
        public virtual bool CanRead { get { return true; } }

        /// <summary>是否支持把信息写入到外部</summary>
        public virtual bool CanWrite { get { return true; } }

        /// <summary>外部=>实体，从外部读取信息并写入到实体对象</summary>>
        /// <param name="entity">实体对象</param>
        /// <param name="eop">实体操作。为空时由内部构建，但可在遍历调用访问器时由外部构造一次传入，以提高性能。</param>
        public virtual void Read(IEntity entity, IEntityOperate eop = null)
        {
            if (!CanRead) return;

            if (entity == null) throw new ArgumentNullException("entity");

            if (eop == null) eop = EntityFactory.CreateOperate(entity.GetType());
            foreach (FieldItem item in GetFields(eop))
            {
                try
                {
                    ReadItem(entity, item);

                    if (OnReadItem != null) OnReadItem(this, new EntityAccessorEventArgs { Entity = entity, Field = item });
                }
                catch (Exception ex)
                {
                    if (OnError != null)
                        OnError(this, new EntityAccessorEventArgs { Entity = entity, Field = item, Error = ex });
                    else
                        throw new XCodeException("读取" + item.Name + "的数据时出错！" + ex.Message, ex);
                }
            }
        }

        /// <summary>外部=>实体，从外部读取指定实体字段的信息</summary>>
        /// <param name="entity">实体对象</param>
        /// <param name="item">实体字段</param>
        protected virtual void ReadItem(IEntity entity, FieldItem item) { }

        /// <summary>实体=>外部，从实体对象读取信息并写入外部</summary>>
        /// <param name="entity">实体对象</param>
        /// <param name="eop">实体操作。为空时由内部构建，但可在遍历调用访问器时由外部构造一次传入，以提高性能。</param>
        public virtual void Write(IEntity entity, IEntityOperate eop = null)
        {
            if (!CanWrite) return;

            if (entity == null) throw new ArgumentNullException("entity");

            if (eop == null) eop = EntityFactory.CreateOperate(entity.GetType());
            foreach (FieldItem item in GetFields(eop))
            {
                try
                {
                    WriteItem(entity, item);

                    if (OnWriteItem != null) OnWriteItem(this, new EntityAccessorEventArgs { Entity = entity, Field = item });
                }
                catch (Exception ex)
                {
                    if (OnError != null)
                        OnError(this, new EntityAccessorEventArgs { Entity = entity, Field = item, Error = ex });
                    else
                        throw new XCodeException("设置" + item.Name + "的数据时出错！" + ex.Message, ex);
                }
            }
        }

        /// <summary>实体=>外部，把指定实体字段的信息写入到外部</summary>>
        /// <param name="entity">实体对象</param>
        /// <param name="item">实体字段</param>
        protected virtual void WriteItem(IEntity entity, FieldItem item) { }
        #endregion

        #region 辅助
        /// <summary>获取需要访问的字段</summary>>
        /// <param name="eop"></param>
        /// <returns></returns>
        protected virtual IEnumerable<FieldItem> GetFields(IEntityOperate eop)
        {
            return AllFields ? eop.AllFields : eop.Fields;
        }
        #endregion
    }
}