using System;
using System.Collections.Generic;
using System.Text;

namespace XCode
{
    /// <summary>
    /// 实体树接口
    /// </summary>
    /// <typeparam name="TEntity"></typeparam>
    public interface IEntityTree<TEntity> where TEntity : Entity<TEntity>, IEntityTree<TEntity>, new()
    {
        #region 属性
        /// <summary>
        /// 父实体
        /// </summary>
        TEntity Parent { get; }

        /// <summary>
        /// 子实体集合
        /// </summary>
        EntityList<TEntity> Childs { get; }
        #endregion
    }
}