using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Log;
using NewLife.Serialization;
using NewLife.Web;

namespace NewLife.Yun
{
    /// <summary>地图提供者接口</summary>
    public interface IMap
    {
        #region 属性
        /// <summary>应用密钥</summary>
        String AppKey { get; set; }
        #endregion

        #region 方法
        /// <summary>异步获取字符串</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        Task<String> GetStringAsync(String url);
        #endregion

        #region 地址编码
        /// <summary>查询地址的经纬度坐标</summary>
        /// <param name="address"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        Task<IDictionary<String, Object>> GetGeocoderAsync(String address, String city = null);

        ///// <summary>查询地址获取坐标</summary>
        ///// <param name="address"></param>
        ///// <param name="city"></param>
        ///// <returns></returns>
        //Task<GeoPoint> GetGeoAsync(String address, String city = null);
        #endregion

        #region 逆地址编码
        ///// <summary>根据坐标获取地址</summary>
        ///// <param name="point"></param>
        ///// <returns></returns>
        //Task<IDictionary<String, Object>> GetGeocoderAsync(GeoPoint point);
        #endregion

        #region 路径规划

        #endregion

        #region 日志
        /// <summary>日志</summary>
        ILog Log { get; set; }
        #endregion
    }

    /// <summary>地图提供者</summary>
    public class Map : DisposeBase
    {
        #region 属性
        /// <summary>应用密钥</summary>
        public String AppKey { get; set; }

        /// <summary>应用密码参数名</summary>
        protected String KeyName { get; set; } = "key";
        #endregion

        #region 构造
        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            _Client.TryDispose();
        }
        #endregion

        #region 方法
        private WebClientX _Client;

        /// <summary>异步获取字符串</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual async Task<String> GetStringAsync(String url)
        {
            if (AppKey.IsNullOrEmpty()) throw new ArgumentNullException(nameof(AppKey));

            if (_Client == null) _Client = new WebClientX { Log = Log };

            if (url.Contains("?"))
                url += "&";
            else
                url += "?";

            url += KeyName + "=" + AppKey;

            return await _Client.DownloadStringAsync(url);
        }

        /// <summary>远程调用</summary>
        /// <param name="url">目标Url</param>
        /// <param name="result">结果字段</param>
        /// <returns></returns>
        public virtual async Task<T> InvokeAsync<T>(String url, String result)
        {
            var html = await GetStringAsync(url);
            if (html.IsNullOrEmpty()) return default(T);

            return (T)new JsonParser(html).Decode();
        }
        #endregion


        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            Log?.Info(format, args);
        }
        #endregion
    }
}