using System;
using System.IO;
using NewLife.Collections;

namespace NewLife.Net.Http
{
    /// <summary>Http缓存。以Url作为缓存键</summary>
    class HttpCache
    {
        #region 属性
        private Int32 _Expriod = 600;
        /// <summary>过期时间。单位是秒，默认0秒，表示永不过期</summary>
        public Int32 Expriod { get { return _Expriod; } set { _Expriod = value; } }

        private DictionaryCache<String, HttpCacheItem> _Items;
        /// <summary>缓存项</summary>
        private DictionaryCache<String, HttpCacheItem> Items
        {
            get
            {
                if (_Items == null) _Items = new DictionaryCache<string, HttpCacheItem>(StringComparer.OrdinalIgnoreCase) { Expriod = Expriod };
                return _Items;
            }
            set { _Items = value; }
        }
        #endregion

        #region 方法
        public HttpCacheItem GetItem(String url)
        {
            return Items[url];
            //HttpCacheItem item = null;
            //if (!Items.TryGetValue(url, out item)) return null;
            //lock (Items)
            //{
            //    if (!Items.TryGetValue(url, out item)) return null;

            //    // 移除过期
            //    if (item.ExpiredTime < DateTime.Now)
            //    {
            //        Items.Remove(url);
            //        item = null;
            //    }

            //    return item;
            //}
        }

        public HttpCacheItem Add(HttpHeader request, HttpHeader response)
        {
            String url = request.RawUrl;
            var item = new HttpCacheItem() { Url = url, Request = request, Response = response };
            item.Stream = response.GetStream();
            //lock (Items)
            //{
            //    Items[url] = item;
            //}
            Items[url] = item;

            return item;
        }
        #endregion
    }

    /// <summary>Http缓存项。</summary>
    class HttpCacheItem
    {
        #region 属性
        private String _Url;
        /// <summary>网址</summary>
        public String Url { get { return _Url; } set { _Url = value; } }

        private HttpHeader _Request;
        /// <summary>请求</summary>
        public HttpHeader Request { get { return _Request; } set { _Request = value; } }

        private HttpHeader _Response;
        /// <summary>响应</summary>
        public HttpHeader Response { get { return _Response; } set { _Response = value; } }

        private Stream _Stream;
        /// <summary>数据流</summary>
        public Stream Stream { get { return _Stream ?? (_Stream = new MemoryStream()); } set { _Stream = value; } }

        //private DateTime _StartTime = DateTime.Now;
        ///// <summary>开始时间</summary>
        //public DateTime StartTime { get { return _StartTime; } set { _StartTime = value; } }

        //private DateTime _ExpiredTime = DateTime.Now.AddMinutes(10);
        ///// <summary>到期时间</summary>
        //public DateTime ExpiredTime { get { return _ExpiredTime; } set { _ExpiredTime = value; } }
        #endregion
    }
}