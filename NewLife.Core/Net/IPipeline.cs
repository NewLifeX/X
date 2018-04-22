using System;
using System.Collections;
using System.Collections.Generic;

namespace NewLife.Net
{
    /// <summary>管道</summary>
    public interface IPipeline : IEnumerable<IHandler>
    {
        ///// <summary>服务提供者</summary>
        //IServiceProvider Service { get; }

        /// <summary>添加处理器到开头</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        IPipeline AddFirst(IHandler handler);

        /// <summary>添加处理器到末尾</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        IPipeline AddLast(IHandler handler);

        /// <summary>添加处理器到指定名称之前</summary>
        /// <param name="baseHandler">基准处理器</param>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        IPipeline AddBefore(IHandler baseHandler, IHandler handler);

        /// <summary>添加处理器到指定名称之后</summary>
        /// <param name="baseHandler">基准处理器</param>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        IPipeline AddAfter(IHandler baseHandler, IHandler handler);

        /// <summary>删除处理器</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        IPipeline Remove(IHandler handler);
    }

    /// <summary>管道</summary>
    public class Pipeline : IPipeline
    {
        #region 属性
        ///// <summary>服务提供者</summary>
        //public IServiceProvider Service { get; set; }

        /// <summary>处理器集合</summary>
        public IList<IHandler> Handlers { get; } = new List<IHandler>();
        #endregion

        #region 构造
        #endregion

        #region 方法
        /// <summary>添加处理器到开头</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual IPipeline AddFirst(IHandler handler)
        {
            Handlers.Insert(0, handler);
            return this;
        }

        /// <summary>添加处理器到末尾</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual IPipeline AddLast(IHandler handler)
        {
            Handlers.Add(handler);
            return this;
        }
        
        /// <summary>添加处理器到指定名称之前</summary>
        /// <param name="baseHandler">基准处理器</param>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual IPipeline AddBefore(IHandler baseHandler, IHandler handler)
        {
            var idx = Handlers.IndexOf(baseHandler);
            if (idx > 0) Handlers.Insert(idx, handler);
            return this;
        }

        /// <summary>添加处理器到指定名称之后</summary>
        /// <param name="baseHandler">基准处理器</param>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual IPipeline AddAfter(IHandler baseHandler, IHandler handler)
        {
            var idx = Handlers.IndexOf(baseHandler);
            if (idx > 0) Handlers.Insert(idx + 1, handler);
            return this;
        }
        
        /// <summary>删除处理器</summary>
        /// <param name="handler">处理器</param>
        /// <returns></returns>
        public virtual IPipeline Remove(IHandler handler)
        {
            Handlers.Remove(handler);
            return this;
        }

        /// <summary>枚举器</summary>
        /// <returns></returns>
        public IEnumerator<IHandler> GetEnumerator() => Handlers.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        #endregion
    }
}