using System;
using System.Collections.Generic;
using System.Text;

namespace XCode
{
    /// <summary>
    /// 实体树扩展接口
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IEntityTreeExtend<TEntity> : IEntityTree<TEntity>
        where TEntity : Entity<TEntity>, IEntityTree<TEntity>, IEntityTreeExtend<TEntity>, new()
    {
        #region 属性


        /// <summary>
        /// 子孙实体集合。以深度层次树结构输出
        /// </summary>
        EntityList<TEntity> AllChilds { get; }

        /// <summary>
        /// 父亲实体集合。以深度层次树结构输出
        /// </summary>
        EntityList<TEntity> AllParents { get; }

        /// <summary>
        /// 深度
        /// </summary>
        Int32 Deepth { get; }
        #endregion
    }
}