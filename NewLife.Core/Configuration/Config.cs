using System;
using System.Collections.Generic;
using System.Text;
using System.Configuration;
using System.Collections.Specialized;

namespace NewLife.Configuration
{
    /// <summary>
    /// 通用配置辅助类
    /// </summary>
    public static class Config
    {
        #region 属性
        private static List<String> hasLoad = new List<String>();

        private static NameValueCollection _AppSettings;
        /// <summary>应用设置</summary>
        public static NameValueCollection AppSettings
        {
            get
            {
                if (_AppSettings == null && !hasLoad.Contains("AppSettings"))
                {
                    _AppSettings = ConfigurationManager.AppSettings;
                    hasLoad.Add("AppSettings");
                }
                return _AppSettings;
            }
            //set { _AppSettings = value; }
        }

        private static ConnectionStringSettingsCollection _ConnectionStrings;
        /// <summary>连接字符串设置</summary>
        public static ConnectionStringSettingsCollection ConnectionStrings
        {
            get
            {
                if (_ConnectionStrings == null && !hasLoad.Contains("ConnectionStrings"))
                {
                    _ConnectionStrings = ConfigurationManager.ConnectionStrings;
                    hasLoad.Add("ConnectionStrings");
                }
                return _ConnectionStrings;
            }
            //set { _ConnectionStrings = value; }
        }
        #endregion

        #region 方法
        /// <summary>
        /// 是否包含指定项的设置
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public static Boolean Contain(String name)
        {
            if (AppSettings == null || AppSettings.Count < 1) return false;

            return Array.IndexOf(AppSettings.AllKeys, name) >= 0;
        }

        /// <summary>
        /// 取得指定名称的设置项，并转为指定类型
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <returns></returns>
        public static T GetConfig<T>(String name)
        {
            if (AppSettings == null || AppSettings.Count < 1) return default(T);

            return GetConfig<T>(name, default(T));
        }

        /// <summary>
        /// 取得指定名称的设置项，并转为指定类型。如果设置不存在，则返回默认值
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="name"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static T GetConfig<T>(String name, T defaultValue)
        {
            if (AppSettings == null || AppSettings.Count < 1) return defaultValue;

            String str = AppSettings[name];
            if (String.IsNullOrEmpty(name)) return defaultValue;

            Type type = typeof(T);
            TypeCode code = Type.GetTypeCode(type);

            if (code == TypeCode.String) return (T)(Object)str;

            if (code == TypeCode.Int32)
            {
                return (T)(Object)Convert.ToInt32(str);
            }

            if (code == TypeCode.Boolean)
            {
                if (str == "1" || str.Equals(Boolean.TrueString, StringComparison.OrdinalIgnoreCase))
                    return (T)(Object)true;
                else if (str == "0" || str.Equals(Boolean.FalseString, StringComparison.OrdinalIgnoreCase))
                    return (T)(Object)false;

                Boolean b = false;
                if (Boolean.TryParse(str.ToLower(), out b)) return (T)(Object)b;


            }

            T value = (T)Convert.ChangeType(str, type);

            return value;
        }

        /// <summary>
        /// 根据指定前缀，获取设置项
        /// </summary>
        /// <param name="prefix"></param>
        /// <returns></returns>
        public static String[] GetConfigByPrefix(String prefix)
        {
            if (AppSettings == null || AppSettings.Count < 1) return null;

            List<String> list = new List<String>();
            foreach (String item in AppSettings.Keys)
            {
                if (item.StartsWith(prefix, StringComparison.Ordinal)) list.Add(AppSettings[item]);
            }
            return list.Count > 0 ? list.ToArray() : null;
        }
        #endregion
    }
}
