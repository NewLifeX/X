using System;
using System.Collections.Generic;
using System.Text;
using NewLife.Collections;

namespace XCode
{
    /// <summary>实体处理器。用于扩展实体类</summary>
    public interface IEntityHandler
    {
        ///// <summary>添加</summary>
        ///// <param name="entity"></param>
        ///// <returns></returns>
        //Int32 OnInsert(IEntity entity);

        ///// <summary>更新</summary>
        ///// <param name="entity"></param>
        ///// <returns></returns>
        //Int32 OnUpdate(IEntity entity);

        ///// <summary>删除</summary>
        ///// <param name="entity"></param>
        ///// <returns></returns>
        //Int32 OnDelete(IEntity entity);

        /// <summary>数据改变</summary>
        /// <param name="entityType"></param>
        /// <param name="entity"></param>
        /// <param name="isnew">是否添加新数据，null表示删除</param>
        /// <returns></returns>
        Boolean OnChange(Type entityType, IEntity entity, Boolean? isnew);

        /// <summary>加载前</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Int32 OnLoading(IEntity entity);

        /// <summary>加载后</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        Int32 OnLoaded(IEntity entity);
    }

    /// <summary>实体处理器基类</summary>
    public abstract class EntityHandler : IEntityHandler
    {
        ///// <summary>添加</summary>
        ///// <param name="entity"></param>
        ///// <returns></returns>
        //public virtual Int32 OnInsert(IEntity entity) { return 0; }

        ///// <summary>更新</summary>
        ///// <param name="entity"></param>
        ///// <returns></returns>
        //public virtual Int32 OnUpdate(IEntity entity) { return 0; }

        ///// <summary>删除</summary>
        ///// <param name="entity"></param>
        ///// <returns></returns>
        //public virtual Int32 OnDelete(IEntity entity) { return 0; }

        /// <summary>数据改变</summary>
        /// <param name="entityType">实体类型</param>
        /// <param name="entity">实体对象</param>
        /// <param name="isnew">是否添加新数据，null表示删除</param>
        /// <returns></returns>
        public virtual Boolean OnChange(Type entityType, IEntity entity, Boolean? isnew) { return true; }

        /// <summary>加载前</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual Int32 OnLoading(IEntity entity) { return 0; }

        /// <summary>加载后</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        public virtual Int32 OnLoaded(IEntity entity) { return 0; }
    }

    /// <summary>实体处理器管理</summary>
    public class EntityHandlerManager
    {
        private List<IEntityHandler> _Handlers;
        /// <summary>实体处理器集合</summary>
        public List<IEntityHandler> Handlers { get { return _Handlers ?? (_Handlers = new List<IEntityHandler>()); } }

        /// <summary>注册</summary>
        /// <param name="handler"></param>
        /// <returns></returns>
        public EntityHandlerManager Register(IEntityHandler handler)
        {
            if (handler != null) Handlers.Add(handler);
            return this;
        }

        /// <summary>数据改变</summary>
        /// <param name="entityType"></param>
        /// <param name="entity"></param>
        /// <param name="isnew">是否添加新数据，null表示删除</param>
        /// <returns></returns>
        public Boolean OnChange(Type entityType, IEntity entity, Boolean? isnew)
        {
            foreach (var handler in Handlers)
            {
                if (!handler.OnChange(entityType, entity, isnew)) return false;
            }

            return true;
        }
    }
}