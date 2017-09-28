using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Serialization;

namespace NewLife.Web
{
    /// <summary>百度地图</summary>
    public class BaiduMap
    {
        #region 属性
        /// <summary>应用密钥</summary>
        public String AppKey { get; set; } = "C73357a276668f8b0563d3f936475007";
        #endregion

        #region 方法
        private WebClientX _Client;
        private async Task<String> GetStringAsync(String url)
        {
            if (AppKey.IsNullOrEmpty()) throw new ArgumentNullException(nameof(AppKey));

            if (_Client == null) _Client = new WebClientX();

            //if (url.Contains("{appkey}")) url = url.Replace("{appkey}", AppKey);
            if (url.Contains("?"))
                url += "&ak=" + AppKey;
            else
                url += "?ak=" + AppKey;

            return await _Client.DownloadStringAsync(url);
        }
        #endregion

        #region 地址转坐标
        class Geocoder
        {
            /// <summary>返回结果状态值， 成功返回0，其他值请查看下方返回码状态表。</summary>
            public Int32 Status { get; set; }

            public result Result { get; set; } = new result();

            public class result
            {
                /// <summary>经纬度坐标</summary>
                public location Location { get; set; } = new location();

                /// <summary>位置的附加信息，是否精确查找。1为精确查找，即准确打点；0为不精确，即模糊打点（模糊打点无法保证准确度，不建议使用）。</summary>
                public Int32 precise { get; set; }

                /// <summary>可信度，描述打点准确度，大于80表示误差小于100m。该字段仅作参考，返回结果准确度主要参考precise参数。</summary>
                public Int32 confidence { get; set; }

                /// <summary>地址类型</summary>
                public String level { get; set; }
            }

            /// <summary>经纬度坐标</summary>
            public class location
            {
                /// <summary>纬度值</summary>
                public Double lng { get; set; }

                /// <summary>经度值</summary>
                public Double lat { get; set; }
            }
        }

        //private String GeoCoderUrl = "http://api.map.baidu.com/geocoder/v2/?address={0}&city={1}&output=json&ak={appkey}";
        private String GeoCoderUrl = "http://api.map.baidu.com/geocoder/v2/?address={0}&city={1}&output=json";
        /// <summary>查询地址的经纬度坐标</summary>
        /// <param name="address"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        public async Task<KeyValuePair<Double, Double>> GetGeocoderAsync(String address, String city = null)
        {
            if (address.IsNullOrEmpty()) throw new ArgumentNullException(nameof(address));

            var rs = new KeyValuePair<Double, Double>();
            var url = GeoCoderUrl.F(address, city);

            var html = await GetStringAsync(GeoCoderUrl.F(address, city));
            if (html.IsNullOrEmpty()) return rs;

            var geo = html.ToJsonEntity<Geocoder>();
            if (geo == null || geo.Status != 0) return rs;
            if (geo.Result?.Location == null) return rs;

            return new KeyValuePair<Double, Double>(geo.Result.Location.lng, geo.Result.Location.lat);
        }
        #endregion
    }
}