using System;
using System.Collections.Generic;
using NewLife.Collections;

namespace XCode.Cache
{
    class CacheItem
    {
        /// <summary>所依赖的表的表名</summary>
        private ICollection<String> _TableNames;

        /// <summary>到期时间</summary>
        public DateTime ExpireTime;

        /// <summary>构造函数</summary>
        /// <param name="tableNames"></param>
        public CacheItem(String[] tableNames) : this(tableNames, 0) { }

        /// <summary>构造函数</summary>
        /// <param name="tableNames"></param>
        /// <param name="time">缓存时间，单位秒</param>
        public CacheItem(String[] tableNames, Int32 time)
        {
            if (tableNames != null && tableNames.Length > 0) _TableNames = new HashSet<String>(tableNames);

            if (time > 0) ExpireTime = DateTime.Now.AddSeconds(time);
        }

        /// <summary>是否依赖于某个表</summary>
        /// <param name="tableName">表名</param>
        /// <returns></returns>
        public Boolean IsDependOn(String tableName)
        {
            // 空表名，不匹配
            if (String.IsNullOrEmpty(tableName)) return false;

            // *表示全局匹配
            if (tableName == "*") return true;

            //hxw add 2015-04-23 修改
            if (_TableNames == null)
            {
                //缓存时没有传入表名，等同于关联所有表，任何表的更新都会导致它失效
                return true;
            }

            // 包含完整表名，匹配
            return _TableNames.Contains(tableName);
        }
    }

    class CacheItem<T> : CacheItem
    {
        private T _Value;
        /// <summary>缓存的数据</summary>
        public T Value { get { return _Value; } }

        /// <summary>构造函数</summary>
        /// <param name="tableNames"></param>
        /// <param name="value">数值</param>
        public CacheItem(String[] tableNames, T value) : this(tableNames, value, 0) { }

        /// <summary>构造函数</summary>
        /// <param name="tableNames"></param>
        /// <param name="value">数值</param>
        /// <param name="time">缓存时间，单位秒</param>
        public CacheItem(String[] tableNames, T value, Int32 time) : base(tableNames, time) { _Value = value; }
    }
}