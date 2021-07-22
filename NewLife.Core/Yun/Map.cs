using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Log;
using NewLife.Security;
using NewLife.Serialization;

#nullable enable
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
        Task<IDictionary<String, Object>> GetGeocoderAsync(String address, String? city = null);

        /// <summary>查询地址获取坐标</summary>
        /// <param name="address">地址</param>
        /// <param name="city">城市</param>
        /// <param name="formatAddress">是否格式化地址</param>
        /// <returns></returns>
        Task<GeoAddress> GetGeoAsync(String address, String? city = null, Boolean formatAddress = false);
        #endregion

        #region 逆地址编码
        /// <summary>根据坐标获取地址</summary>
        /// <param name="point"></param>
        /// <returns></returns>
        Task<IDictionary<String, Object>> GetGeocoderAsync(GeoPoint point);

        /// <summary>根据坐标获取地址</summary>
        /// <param name="point"></param>
        /// <returns></returns>
        Task<GeoAddress> GetGeoAsync(GeoPoint point);
        #endregion

        #region 路径规划
        /// <summary>计算距离和驾车时间</summary>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <param name="type">路径计算的方式和方法</param>
        /// <returns></returns>
        Task<Driving> GetDistanceAsync(GeoPoint origin, GeoPoint destination, Int32 type = 0);
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
        /// <summary>应用密钥。多个key逗号分隔</summary>
        public String? AppKey { get; set; }

        /// <summary>应用密码参数名</summary>
        protected String KeyName { get; set; } = "key";

        /// <summary>最后密钥</summary>
        public String? LastKey { get; private set; }

        /// <summary>坐标系</summary>
        public String? CoordType { get; set; }

        /// <summary>最后网址</summary>
        public String? LastUrl { get; private set; }

        /// <summary>最后响应</summary>
        public String? LastString { get; private set; }

        /// <summary>最后结果</summary>
        public IDictionary<String, Object>? LastResult { get; private set; }

        /// <summary>收到异常响应时是否抛出异常</summary>
        public Boolean ThrowException { get; set; }
        #endregion

        #region 构造
        /// <summary>销毁</summary>
        /// <param name="disposing"></param>
        protected override void Dispose(Boolean disposing)
        {
            base.Dispose(disposing);

            _Client.TryDispose();
        }
        #endregion

        #region 方法
        private HttpClient? _Client;

        /// <summary>异步获取字符串</summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public virtual async Task<String> GetStringAsync(String url)
        {
            var key = AcquireKey();
            if (key.IsNullOrEmpty()) throw new ArgumentNullException(nameof(AppKey), "没有可用密钥");

            if (_Client == null) _Client = DefaultTracer.Instance.CreateHttpClient();

            if (url.Contains("?"))
                url += "&";
            else
                url += "?";

            url += KeyName + "=" + key;

            LastUrl = url;
            LastString = null;
            LastKey = key;

            var rs = await _Client.GetStringAsync(url).ConfigureAwait(false);

            //// 删除无效密钥
            //if (IsValidKey(rs)) RemoveKey(key);

            return LastString = rs;
        }

        /// <summary>远程调用</summary>
        /// <param name="url">目标Url</param>
        /// <param name="result">结果字段</param>
        /// <returns></returns>
        protected virtual async Task<T?> InvokeAsync<T>(String url, String result) where T : class
        {
            LastResult = null;

            var html = await GetStringAsync(url).ConfigureAwait(false);
            if (html.IsNullOrEmpty()) return default;

            var rs = JsonParser.Decode(html);

            LastResult = rs;

            return (T)rs;
        }
        #endregion

        #region 密钥管理
        private String[]? _Keys;
        //private Int32 _KeyIndex;

        /// <summary>申请密钥</summary>
        /// <returns></returns>
        protected String AcquireKey()
        {
            if (AppKey.IsNullOrEmpty()) return String.Empty;

            var ks = _Keys;
            if (ks == null) ks = _Keys = AppKey?.Split(",");
            if (ks == null) return String.Empty;

            //var key = _Keys[_KeyIndex++];
            //if (_KeyIndex >= _Keys.Length) _KeyIndex = 0;

            // 使用本地变量保存数据，避免多线程冲突
            var idx = Rand.Next(ks.Length);
            var key = ks[idx];

            return key;
        }

        /// <summary>移除不可用密钥</summary>
        /// <param name="key"></param>
        protected void RemoveKey(String key)
        {
            // 使用本地变量保存数据，避免多线程冲突
            var ks = _Keys;
            if (ks == null || ks.Length == 0) return;

            var list = new List<String>(ks);
            if (list.Contains(key)) list.Remove(key);

            _Keys = list.ToArray();
        }

        private readonly String[] _KeyWords = new[] { "INVALID", "LIMIT" };
        /// <summary>是否无效Key。可能禁用或超出限制</summary>
        /// <param name="result"></param>
        /// <returns></returns>
        protected virtual Boolean IsValidKey(String result)
        {
            if (result.IsNullOrEmpty()) return false;

            return _KeyWords.Any(e => result.Contains(e));
        }
        #endregion

        #region 日志
        /// <summary>日志</summary>
        public ILog Log { get; set; } = Logger.Null;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args) => Log?.Info(format, args);
        #endregion
    }
}
#nullable restore