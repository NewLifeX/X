using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Configuration;
using System.Web.Configuration;
using NewLife.Reflection;

namespace NewLife.Configuration
{
    /// <summary>通用配置辅助类</summary>
    /// <remarks>
    /// 一定要注意的是：ConfigurationManager.AppSettings会获取当前应用的设置，如果子目录里面的web.config有设置，则会获取最近的设置。
    /// </remarks>
    public static class Config
    {
        #region 属性
        private static List<String> hasLoad = new List<String>();

        /// <summary>应用设置</summary>
        public static NameValueCollection AppSettings { get { return ConfigurationManager.AppSettings; } }

        /// <summary>连接字符串设置</summary>
        public static ConnectionStringSettingsCollection ConnectionStrings { get { return ConfigurationManager.ConnectionStrings; } }

        private static Object _httpHandlers;
        /// <summary>获取httphandlers</summary>
        public static Object httpHandlers
        {
            get
            {
                if (_httpHandlers == null && !hasLoad.Contains("httpHandlers"))
                {
                    _httpHandlers = ConfigurationManager.GetSection("system.web/httpHandlers");
                    hasLoad.Add("ConnectionStrings");
                }
                return _httpHandlers;
            }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 获取httpHandlers
        /// </summary>
        /// <returns></returns>
        public static List<HttpHandlerAction> GethttpHandlers()
        {
            List<HttpHandlerAction> value = new List<HttpHandlerAction>();

            HttpHandlersSection httphanset = httpHandlers as HttpHandlersSection;
            if (httphanset == null) return value;

            ConfigurationElementCollection cec = (ConfigurationElementCollection)httphanset.Handlers;

            for (Int32 i = 0; i < cec.Count; i++)
            {
                value.Add(httphanset.Handlers[i]);
            }

            return value;
        }

        /// <summary>
        /// 是否包含指定项的设置
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Boolean Contain(String name)
        {
            try
            {
                NameValueCollection nvs = AppSettings;
                if (nvs == null || nvs.Count < 1) return false;

                return Array.IndexOf(nvs.AllKeys, name) >= 0;
            }
            catch (ConfigurationErrorsException) { return false; }
        }

        /// <summary>依次尝试获取一批设置项，直到找到第一个为止</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="defaultValue"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        public static T GetMutilConfig<T>(T defaultValue, params String[] names)
        {
            T value;
            if (TryGetMutilConfig<T>(out value, names)) return value;
            return defaultValue;
        }

        /// <summary>依次尝试获取一批设置项，直到找到第一个为止</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="value"></param>
        /// <param name="names"></param>
        /// <returns></returns>
        public static Boolean TryGetMutilConfig<T>(out T value, params String[] names)
        {
            value = default(T);
            try
            {
                NameValueCollection nvs = AppSettings;
                if (nvs == null || nvs.Count < 1) return false;

                for (int i = 0; i < names.Length; i++)
                {
                    if (TryGetConfig<T>(names[i], out value)) return true;
                }

                return false;
            }
            catch (ConfigurationErrorsException) { return false; }
        }

        /// <summary>取得指定名称的设置项，并转为指定类型</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetConfig<T>(String name)
        {
            try
            {
                NameValueCollection nvs = AppSettings;
                if (nvs == null || nvs.Count < 1) return default(T);

                return GetConfig<T>(name, default(T));
            }
            catch (ConfigurationErrorsException) { return default(T); }
        }

        /// <summary>取得指定名称的设置项，并转为指定类型。如果设置不存在，则返回默认值</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetConfig<T>(String name, T defaultValue)
        {
            T value;
            if (TryGetConfig<T>(name, out value)) return value;
            return defaultValue;
        }

        /// <summary>尝试获取指定名称的设置项</summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Boolean TryGetConfig<T>(String name, out T value)
        {
            Object v = null;
            if (TryGetConfig(name, typeof(T), out v))
            {
                value = (T)v;
                return true;
            }

            value = default(T);
            return false;
        }

        /// <summary>尝试获取指定名称的设置项</summary>
        /// <param name="name"></param>
        /// <param name="type"></param>
        /// <param name="value"></param>
        /// <returns></returns>
        public static Boolean TryGetConfig(String name, Type type, out Object value)
        {
            value = null;
            try
            {
                NameValueCollection nvs = AppSettings;
                if (nvs == null || nvs.Count < 1) return false;

                String str = nvs[name];
                if (String.IsNullOrEmpty(str)) return false;

                TypeCode code = Type.GetTypeCode(type);

                if (code == TypeCode.String)
                    value = str;
                else if (code == TypeCode.Int32)
                    value = Convert.ToInt32(str);
                else if (code == TypeCode.Boolean)
                {
                    Boolean b = false;
                    if (str == "1" || str.EqualIgnoreCase(Boolean.TrueString))
                        value = true;
                    else if (str == "0" || str.EqualIgnoreCase(Boolean.FalseString))
                        value = false;
                    else if (Boolean.TryParse(str.ToLower(), out b))
                        value = b;
                }
                else
                    value = TypeX.ChangeType(str, type);

                return true;
            }
            catch (ConfigurationErrorsException) { return false; }
        }

        /// <summary>根据指定前缀，获取设置项。其中key不包含前缀</summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static IDictionary<String, String> GetConfigByPrefix(String prefix)
        {
            try
            {
                var nvs = AppSettings;
                if (nvs == null || nvs.Count < 1) return null;

                var nv = new Dictionary<String, String>(StringComparer.OrdinalIgnoreCase);
                foreach (String item in nvs)
                {
                    if (item.Length > prefix.Length && item.StartsWith(prefix, StringComparison.OrdinalIgnoreCase)) nv.Add(item.Substring(prefix.Length), nvs[item]);
                }
                //return list.Count > 0 ? list.ToArray() : null;
                return nv.Count > 0 ? nv : null;
            }
            catch (ConfigurationErrorsException) { return null; }
        }

        /// <summary>
        /// 取得指定名称的设置项，并分割为指定类型数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="split"></param>
        /// <returns></returns>
        public static T[] GetConfigSplit<T>(String name, String split)
        {
            try
            {
                NameValueCollection nvs = AppSettings;
                if (nvs == null || nvs.Count < 1) return new T[0];

                return GetConfigSplit<T>(name, split, new T[0]);
            }
            catch (ConfigurationErrorsException) { return new T[0]; }
        }

        /// <summary>
        /// 取得指定名称的设置项，并分割为指定类型数组
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="split"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T[] GetConfigSplit<T>(String name, String split, T[] defaultValue)
        {
            try
            {
                NameValueCollection nvs = AppSettings;
                if (nvs == null || nvs.Count < 1) return defaultValue;

                String str = GetConfig<String>(name);
                if (String.IsNullOrEmpty(str)) return defaultValue;

                String[] sps = String.IsNullOrEmpty(split) ? new String[] { ",", ";" } : new String[] { split };
                String[] ss = str.Split(sps, StringSplitOptions.RemoveEmptyEntries);
                if (ss == null || ss.Length < 1) return defaultValue;

                T[] arr = new T[ss.Length];
                for (int i = 0; i < ss.Length; i++)
                {
                    str = ss[i].Trim();
                    if (String.IsNullOrEmpty(str)) continue;

                    arr[i] = TypeX.ChangeType<T>(str);
                }

                return arr;
            }
            catch (ConfigurationErrorsException) { return defaultValue; }
        }
        #endregion
    }
}