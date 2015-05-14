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

        /// <summary>树形节点名，根据深度带全角空格前缀</summary>
        String TreeNodeText { get; }

        /// <summary>获取完整树，包含根节点，排除指定分支。多用于树节点父级选择</summary>
        /// <param name="exclude"></param>
        /// <returns></returns>
        IEntityList FindAllChildsExcept(IEntityTree exclude);
        #endregion
    }
}