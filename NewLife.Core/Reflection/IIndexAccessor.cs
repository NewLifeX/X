using System;

namespace NewLife.Reflection
{
    /// <summary>
    /// 索引器接访问口。
    /// 该接口用于通过名称快速访问对象属性或字段（属性优先）。
    /// </summary>
    [Obsolete("=>IIndex")]
    public interface IIndexAccessor
    {
        /// <summary>获取/设置 指定名称的属性或字段的值</summary>
        /// <param name="name"></param>
        /// <returns></returns>
        Object this[String name] { get; set; }
    }
}