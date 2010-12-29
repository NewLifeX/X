using System;
using System.Web;
using System.Web.Caching;
using NewLife.Reflection;

namespace NewLife.Web
{
    /// <summary>
    /// Http状态，经常用于登录用户的Current
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class HttpState<T> where T : class
    {
        #region 属性
        private String _Key;
        /// <summary>键值</summary>
        public String Key
        {
            get { return _Key; }
            set { _Key = value; }
        }

        private Boolean _EnableSession = true;
        /// <summary>使用Session</summary>
        public Boolean EnableSession
        {
            get { return _EnableSession; }
            set { _EnableSession = value; }
        }

        private Boolean _EnableHttpItems = true;
        /// <summary>使用HttpItems</summary>
        public Boolean EnableHttpItems
        {
            get { return _EnableHttpItems; }
            set { _EnableHttpItems = value; }
        }

        private Boolean _EnableCache = false;
        /// <summary>使用Cache</summary>
        public Boolean EnableCache
        {
            get { return _EnableCache; }
            set { _EnableCache = value; }
        }

        //private TimeSpan _CacheTime;
        ///// <summary>Cache缓存的时间</summary>
        //public TimeSpan CacheTime
        //{
        //    get { return _CacheTime; }
        //    set { _CacheTime = value; }
        //}

        private Boolean _EnableCookie = true;
        /// <summary>使用Cookie</summary>
        public Boolean EnableCookie
        {
            get { return _EnableCookie; }
            set { _EnableCookie = value; }
        }

        /// <summary>
        /// 实体转为Cookie的方法
        /// </summary>
        public Converter<T, HttpCookie> EntityToCookie;

        /// <summary>
        /// Cookie转为实体的方法
        /// </summary>
        public Converter<HttpCookie, T> CookieToEntity;

        /// <summary>
        /// 自定义保存
        /// </summary>
        public Converter<T, Boolean> Save;

        /// <summary>
        /// 自定义加载
        /// </summary>
        public Converter<HttpState<T>, T> Load;
        #endregion

        #region 扩展属性
        /// <summary>
        /// Http上下文
        /// </summary>
        static HttpContext Http { get { return HttpContext.Current; } }
        #endregion

        #region 创建
        /// <summary>
        /// 初始化
        /// </summary>
        public HttpState() { Key = typeof(T).Name + "_HttpStateKey"; }

        /// <summary>
        /// 初始化
        /// </summary>
        /// <param name="key"></param>
        public HttpState(String key) { Key = key; }
        #endregion

        #region 数据
        /// <summary>
        /// 获取当前Http状态保存的对象
        /// </summary>
        public T Current
        {
            get { return Get(CookieToEntity, Load); }
            set { Set(value, EntityToCookie, Save); }
        }
        #endregion

        #region 核心
        /// <summary>
        /// 获取Http状态
        /// </summary>
        /// <param name="conv">把Cookie转为实体的转换器</param>
        /// <returns></returns>
        [Obsolete("该方法在将来版本中将不再支持，请使用T Get(Converter<HttpCookie, T> conv, Converter<HttpState<T>, T> load)")]
        public T Get(Converter<HttpCookie, T> conv)
        {
            return Get(conv, null);
        }

        /// <summary>
        /// 获取Http状态
        /// </summary>
        /// <param name="conv">把Cookie转为实体的转换器</param>
        /// <param name="load">自定义加载方法</param>
        /// <returns></returns>
        public T Get(Converter<HttpCookie, T> conv, Converter<HttpState<T>, T> load)
        {
            T entity = default(T);

            HttpContext http = Http;
            if (http == null) return entity;

            String key = Key;

            String sessionID = String.Empty;

            // 尝试Items
            if (EnableHttpItems && http.Items != null)
            {
                entity = http.Items[key] as T;
                if (entity != null) return entity;
            }
            // 尝试Session
            if (EnableSession && http.Session != null)
            {
                sessionID = http.Session.SessionID;

                entity = http.Session[key] as T;
                if (entity != null) return entity;
            }
            // 尝试全局Cache
            if (EnableCache && !String.IsNullOrEmpty(sessionID))
            {
                //// SessionID作为全局缓存的key，使得非当前会话可以修改当前会话的数据
                //sessionID += "_" + key;

                entity = GetCache(sessionID);
                if (entity != null)
                {
                    //Set(entity, null, null);
                    return entity;
                }
            }
            // 尝试Cookie
            if (EnableCookie && http.Request != null && http.Request.Cookies != null)
            {
                //if (func != null)
                //{
                //    // 防止重入
                //    String key2 = key + "_Getting";
                //    if (!http.Items.Contains(key2))
                //    {
                //        http.Items[key2] = true;
                //        //try
                //        //{
                //        entity = func();
                //        //}
                //        //finally
                //        //{
                //        //    http.Items.Remove(key2);
                //        //}
                //    }
                //}
                if (conv != null && Array.IndexOf(http.Request.Cookies.AllKeys, key) >= 0)
                {
                    entity = conv(http.Request.Cookies[key]);
                }
            }
            // 自定义加载
            if (load != null)
            {
                entity = load(this);
            }
            return entity;
        }

        /// <summary>
        /// 设置Http状态
        /// </summary>
        /// <param name="entity"></param>
        /// <param name="conv">把实体转换为Cookie的转换器</param>
        [Obsolete("该方法在将来版本中将不再支持，请使用void Set(T entity, Converter<T, HttpCookie> conv, Converter<T, Boolean> save)")]
        public void Set(T entity, Converter<T, HttpCookie> conv)
        {
            Set(entity, conv, null);
        }

        /// <summary>
        /// 设置Http状态
        /// </summary>
        /// <param name="entity">实体</param>
        /// <param name="conv">把实体转换为Cookie的转换器</param>
        /// <param name="save">自定义保存</param>
        public void Set(T entity, Converter<T, HttpCookie> conv, Converter<T, Boolean> save)
        {
            HttpContext http = Http;
            if (http == null) return;

            String key = Key;

            String sessionID = String.Empty;

            // 尝试Items
            if (EnableHttpItems && http.Items != null)
            {
                if (entity != null)
                    http.Items[key] = entity;
                else
                    http.Items.Remove(key);
            }
            // 尝试Session
            if (EnableSession && http.Session != null)
            {
                sessionID = http.Session.SessionID;

                if (entity != null)
                    http.Session[key] = entity;
                else
                    http.Session.Remove(key);
            }
            // 尝试全局Cache
            if (EnableCache && !String.IsNullOrEmpty(sessionID))
            {
                //sessionID += "_" + key;

                //if (entity != null)
                //    http.Cache.Insert(sessionID, entity, null, Cache.NoAbsoluteExpiration, new TimeSpan(0, 20, 0));
                //else
                //    http.Cache.Remove(sessionID);
                SetCache(sessionID, entity);
            }
            // 尝试Cookie
            #region 尝试Cookie
            if (EnableCookie && http.Response != null && http.Response.Cookies != null)
            {
                if (conv != null)
                {
                    HttpCookie cookie = conv(entity);
                    if (cookie != null)
                    {
                        cookie.Name = key;

                        // 使用顶级域，最大程度共享
                        String host = http.Request.Url.Host;
                        // 不含圆点的可能是主机名
                        if (host.Contains("."))
                        {
                            // 取最后一段判断是不是IP地址
                            String last = host.Substring(host.LastIndexOf(".") + 1);
                            Int32 n = 0;
                            if (!Int32.TryParse(last, out n))
                            {
                                String[] ss = host.Split('.');
                                host = ss[ss.Length - 2] + "." + ss[ss.Length - 1];
                                cookie.Domain = host;
                            }
                        }

                        http.Response.SetCookie(cookie);
                    }
                }
                //if (entity == null) http.Response.Cookies.Remove(key);
            }
            //if (http.Response != null && http.Response.Cookies != null)
            //{
            //    if (entity == null)
            //    {
            //        http.Response.Cookies.Remove(key);
            //    }
            //    else if (action != null)
            //    {
            //        HttpCookie cookie = null;
            //        if (Array.IndexOf(http.Response.Cookies.AllKeys, key) >= 0)
            //            cookie = http.Response.Cookies[key];
            //        else
            //        {
            //            // 仅仅New一个cookie，是否加入响应集合，由外部决定
            //            cookie = new HttpCookie(key);

            //            //// 虽然Response.Cookie里面没有key，但是这样取的时候，它会自动增加一个，并添加到集合
            //            //cookie = http.Response.Cookies[key];

            //            //// 取得请求时的Cookie
            //            //HttpCookie cookie2 = null;
            //            //if (Array.IndexOf(http.Request.Cookies.AllKeys, key) >= 0)
            //            //    cookie2 = http.Request.Cookies[key];

            //            //// 设置新的授权
            //            //if (cookie2 != null)
            //            //{

            //            //}
            //        }

            //        // 使用顶级域，最大程度共享
            //        String host = http.Request.Url.Host;
            //        // 不含圆点的可能是主机名
            //        if (host.Contains("."))
            //        {
            //            // 取最后一段判断是不是IP地址
            //            String last = host.Substring(host.LastIndexOf(".") + 1);
            //            Int32 n = 0;
            //            if (!Int32.TryParse(last, out n))
            //            {
            //                String[] ss = host.Split('.');
            //                host = ss[ss.Length - 2] + "." + ss[ss.Length - 1];
            //                cookie.Domain = host;
            //            }
            //        }

            //        // 防止重入
            //        String key2 = key + "_Setting";
            //        if (!http.Items.Contains(key2))
            //        {
            //            http.Items[key2] = true;
            //            try
            //            {
            //                String v = cookie.Value;
            //                action(cookie);

            //                //// 如果没有改变Cookie值，则不设置Cookie
            //                //if (cookie.Value != v) http.Response.Cookies.Set(cookie);
            //                //http.Response.Cookies.Add(cookie);
            //            }
            //            finally
            //            {
            //                http.Items.Remove(key2);
            //            }
            //        }
            //    }
            //}
            #endregion
            // 自定义保存
            if (save != null)
            {
                save(entity);
            }
        }
        #endregion

        #region Cache操作
        /// <summary>
        /// 从全局Cache中获取数据
        /// </summary>
        /// <param name="sessionID">使用SessionID作为数据在全局Cache中的标识</param>
        /// <returns></returns>
        public T GetCache(String sessionID)
        {
            // SessionID作为全局缓存的key，使得非当前会话可以修改当前会话的数据
            sessionID += "_" + Key;

            T entity = Http.Cache[sessionID] as T;
            if (entity != null)
            {
                //Set(entity, null, null);
                return entity;
            }

            return default(T);
        }

        /// <summary>
        /// 设置数据到全局Cache
        /// </summary>
        /// <param name="sessionID">使用SessionID作为数据在全局Cache中的标识</param>
        /// <param name="entity"></param>
        public void SetCache(String sessionID, T entity)
        {
            sessionID += "_" + Key;

            if (entity != null)
                Http.Cache.Insert(sessionID, entity, null, Cache.NoAbsoluteExpiration, new TimeSpan(0, 20, 0));
            else
                Http.Cache.Remove(sessionID);
        }
        #endregion
    }
}