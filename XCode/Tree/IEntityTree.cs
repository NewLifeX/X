using System;

namespace XCode
{
    /// <summary>实体树接口</summary>
    public interface IEntityTree : IEntity
    {
        #region 属性
        /// <summary>父实体</summary>
        IEntity Parent { get; }

        /// <summary>子实体集合</summary>
        IEntityList Childs { get; }

        /// <summary>子孙实体集合。以深度层次树结构输出</summary>
        IEntityList AllChilds { get; }

        /// <summary>父亲实体集合。以深度层次树结构输出</summary>
        IEntityList AllParents { get; }

        /// <summary>深度</summary>
        Int32 Deepth { get; }
        #endregion
    }
}