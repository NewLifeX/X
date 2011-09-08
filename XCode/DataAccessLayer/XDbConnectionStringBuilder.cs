using System;
using System.Collections.Generic;
using System.Text;
using System.Collections.Specialized;
using System.Collections;

namespace XCode.DataAccessLayer
{
    /// <summary>
    /// 连接字符串构造器
    /// </summary>
    /// <remarks>未稳定，仅供XCode内部使用，不建议外部使用</remarks>
    public class XDbConnectionStringBuilder : StringDictionary
    {
        #region 属性
        private Dictionary<String, String> _Keys = new Dictionary<string, string>();
        /// <summary>
        /// 已重载。
        /// </summary>
        public override ICollection Keys
        {
            get
            {
                return _Keys.Keys;
            }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        public override void Add(string key, string value)
        {
            base.Add(key, value);

            _Keys.Add(key, key.ToLower());
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="key"></param>
        public override void Remove(string key)
        {
            base.Remove(key);

            _Keys.Remove(key);
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        public override void Clear()
        {
            base.Clear();

            _Keys.Clear();
        }

        /// <summary>
        /// 已重载。
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public override string this[string key]
        {
            get
            {
                return base[key];
            }
            set
            {
                if (ContainsKey(key))
                    base[key] = value;
                else
                    Add(key, value);
            }
        }
        #endregion

        #region 连接字符串
        /// <summary>连接字符串</summary>
        public String ConnectionString
        {
            get
            {
                if (Count <= 0) return null;

                StringBuilder sb = new StringBuilder();
                foreach (String item in Keys)
                {
                    if (sb.Length > 0) sb.Append(";");
                    sb.AppendFormat("{0}={1}", item, base[item]);
                }

                return sb.ToString();
            }
            set
            {
                Clear();
                if (String.IsNullOrEmpty(value)) return;

                String[] kvs = value.Split(new String[] { ";" }, StringSplitOptions.RemoveEmptyEntries);
                if (kvs == null || kvs.Length <= 0) return;
                foreach (String item in kvs)
                {
                    Int32 p = item.IndexOf("=");
                    // 没有等号，或者等号在第一位，都不合法
                    if (p <= 0) continue;

                    String name = item.Substring(0, p);
                    String val = "";
                    if (p < item.Length - 1) val = item.Substring(p + 1);
                    Add(name.Trim(), val.Trim());
                }
            }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 尝试获取值
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryGetValue(String key, out String value)
        {
            value = null;

            if (!ContainsKey(key)) return false;

            value = this[key];

            return true;
        }

        /// <summary>
        /// 获取并删除连接字符串中的项
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public Boolean TryGetAndRemove(String key, out String value)
        {
            value = null;

            if (!ContainsKey(key)) return false;

            value = this[key];
            Remove(key);
            return true;
        }
        #endregion

        #region 辅助
        /// <summary>
        /// 已重载。
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return ConnectionString;
        }
        #endregion
    }
}