using System;
using System.Collections.Generic;
using System.Text;

namespace XCode.DataAccessLayer
{
    /// <summary>连接字符串构造器</summary>
    public class ConnectionStringBuilder /*: Dictionary<String, String>*/
    {
        #region 属性
        private IDictionary<String, String> _dic;

        /// <summary>获取 或 设置 设置项</summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public String this[String key] { get => _dic?[key]; set => _dic[key] = value; }
        #endregion

        #region 构造
        /// <summary>实例化</summary>
        public ConnectionStringBuilder(String connStr) => _dic = connStr.SplitAsDictionary("=", ";", true);
        #endregion

        #region 连接字符串
        /// <summary>连接字符串</summary>
        public String ConnectionString => _dic?.Join(";", kv => $"{kv.Key}={kv.Value}");
        #endregion

        #region 方法
        /// <summary>获取连接字符串中的项</summary>
        /// <param name="key"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Boolean TryGetValue(String key, out String value) => _dic.TryGetValue(key, out value);

        /// <summary>获取并删除连接字符串中的项</summary>
        /// <param name="key"></param>
        /// <param name="value">数值</param>
        /// <returns></returns>
        public Boolean TryGetAndRemove(String key, out String value)
        {
            value = null;

            if (_dic == null || !_dic.TryGetValue(key, out value)) return false;

            _dic.Remove(key);

            return true;
        }

        /// <summary>尝试添加项，如果存在则失败</summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryAdd(String key,String value)
        {
            if (_dic.ContainsKey(key)) return false;

            _dic[key] = value;

            return true;
        }
        #endregion

        #region 辅助
        /// <summary>已重载。</summary>
        /// <returns></returns>
        public override String ToString() => ConnectionString;
        #endregion
    }
}