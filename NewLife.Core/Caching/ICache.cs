using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NewLife.Caching
{
    /// <summary>缓存接口</summary>
    public interface ICache
    {
        /// <summary>名称</summary>
        String Name { get; }

        /// <summary>获取和设置缓存，永不过期</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Object this[String key] { get;set; }

        /// <summary>是否包含缓存项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        Boolean Contains(String key);

        /// <summary>添加缓存项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒</param>
        /// <returns></returns>
        Boolean Add(String key, Object value, Int32 expire = 0);

        /// <summary>添加获取缓存项</summary>
        /// <param name="key">键</param>
        /// <param name="value">值</param>
        /// <param name="expire">过期时间，秒</param>
        /// <returns></returns>
        Object AddOrGet(String key, Object value, Int32 expire = 0);

        /// <summary>获取缓存项</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        Object Get(String key);

        /// <summary>移除缓存项</summary>
        /// <param name="key">键</param>
        /// <returns></returns>
        Object Remove(String key);
    }
}