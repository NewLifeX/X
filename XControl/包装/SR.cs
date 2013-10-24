using System;
using System.Collections.Generic;
using System.Reflection;
using NewLife.Reflection;

namespace XControl
{
    /// <summary>System.Web的资源包装</summary>
    public static class SR
    {
        #region 属性
        private static Type _SRType;
        /// <summary>类型</summary>
        public static Type SRType
        {
            get
            {
                if (_SRType == null)
                {
                    Assembly asm = typeof(System.Web.UI.WebControls.TextBox).Assembly;
                    Type[] types = asm.GetTypes();
                    foreach (Type item in types)
                    {
                        if (item.Name == "SR")
                        {
                            _SRType = item;
                            break;
                        }
                    }
                }
                return _SRType;
            }
        }
        #endregion

        /// <summary>取得System.Web资源中的字符串</summary>
        /// <param name="name">名称</param>
        /// <param name="args"></param>
        /// <returns></returns>
        public static string GetString(string name, params object[] args)
        {
            //var method = SRType.GetMethod("GetString", new Type[] { typeof(String), typeof(Object[]) });
            var method = Reflect.GetMethod(SRType, "GetString", typeof(String), typeof(Object[]));
            var obj = Reflect.Invoke(method, name, args);
            if (obj == null)
                return null;
            else
                return obj.ToString();
        }

        static Dictionary<String, String> cache = new Dictionary<string, string>();
        /// <summary>取得System.Web资源中的字符串</summary>
        /// <param name="name">名称</param>
        /// <returns></returns>
        public static string GetString(string name)
        {
            if (cache.ContainsKey(name)) return cache[name];
            lock (cache)
            {
                if (cache.ContainsKey(name)) return cache[name];

                //MethodInfo method = SRType.GetMethod("GetString", new Type[] { typeof(String) });
                var method = Reflect.GetMethod(SRType, "GetString", typeof(String));
                var obj = Reflect.Invoke(method, name);
                var rs = String.Empty;
                if (obj != null) rs = obj.ToString();

                cache.Add(name, rs);

                return rs;
            }
        }
    }
}