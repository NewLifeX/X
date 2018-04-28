using System;
using System.Collections.Generic;

namespace NewLife.Data
{
    /// <summary>具有扩展数据的接口</summary>
    public interface IExtend
    {
        /// <summary>数据项</summary>
        IDictionary<String, Object> Items { get; }

        /// <summary>设置 或 获取 数据项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Object this[String key] { get; set; }
    }
}