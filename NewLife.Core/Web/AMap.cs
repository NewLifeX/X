using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Serialization;

namespace NewLife.Web
{
    /// <summary>高德地图</summary>
    public class AMap
    {
        #region 属性
        /// <summary>应用密钥</summary>
        public String AppKey { get; set; } = "99ac084eb7dd8015fe0ff4404fa800da";
        #endregion

        #region 方法
        private WebClientX _Client;
        private async Task<String> GetStringAsync(String url)
        {
            if (AppKey.IsNullOrEmpty()) throw new ArgumentNullException(nameof(AppKey));

            if (_Client == null) _Client = new WebClientX();

            if (url.Contains("?"))
                url += "&key=" + AppKey;
            else
                url += "?key=" + AppKey;

            return await _Client.DownloadStringAsync(url);
        }
        #endregion

        #region 地址转坐标
        //class Geocoder
        //{
        //    /// <summary>返回值为 0 或 1，0 表示请求失败；1 表示请求成功。</summary>
        //    public Int32 Status { get; set; }

        //    /// <summary>个数</summary>
        //    public Int32 Count { get; set; }

        //    /// <summary>当 status 为 0 时，info 会返回具体错误原因，否则返回“OK”。</summary>
        //    public String Info { get; set; }

        //    /// <summary>地理编码信息列表</summary>
        //    public result[] geocodes { get; set; }

        //    public class result
        //    {
        //        /// <summary>结构化地址信息</summary>
        //        public String formatted_address { get; set; }

        //        /// <summary>省份</summary>
        //        public String province { get; set; }

        //        /// <summary>结构化地址信息</summary>
        //        public String city { get; set; }

        //        /// <summary>城市编码</summary>
        //        public String citycode { get; set; }

        //        /// <summary>区县</summary>
        //        public String district { get; set; }

        //        /// <summary>乡镇</summary>
        //        public String township { get; set; }

        //        /// <summary>街道</summary>
        //        public String street { get; set; }

        //        /// <summary>门牌</summary>
        //        public String number { get; set; }

        //        /// <summary>区域编码</summary>
        //        public String adcode { get; set; }

        //        /// <summary>坐标点</summary>
        //        public String location { get; set; }

        //        /// <summary>匹配级别</summary>
        //        public String level { get; set; }
        //    }
        //}

        private String GeoCoderUrl = "http://restapi.amap.com/v3/geocode/geo?address={0}&city={1}&output=json";
        /// <summary>查询地址的经纬度坐标</summary>
        /// <param name="address"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        public async Task<IDictionary<String, String>> GetGeocoderAsync(String address, String city = null)
        {
            if (address.IsNullOrEmpty()) throw new ArgumentNullException(nameof(address));

            //var rs = new Dictionary<String, String>();
            var url = GeoCoderUrl.F(address, city);

            var html = await GetStringAsync(GeoCoderUrl.F(address, city));
            if (html.IsNullOrEmpty()) return null;

            var dic = new JsonParser(html).Decode() as IDictionary<String, Object>;
            if (dic == null || dic.Count == 0) return null;

            var status = dic["status"].ToInt();
            if (status != 1) throw new Exception(dic["info"] + "");

            var arr = dic["geocodes"] as IList;
            if (arr == null || arr.Count == 0) return null;

            var geo = arr[0] as IDictionary<String, Object>;
            var rs = new Dictionary<String, String>();
            foreach (var item in geo)
            {
                var v = item.Value;
                if (v is ICollection cs && cs.Count == 0) continue;

                if (v is IList vs) v = vs.Join(",");

                rs[item.Key] = v + "";
            }

            return rs;
        }
        #endregion
    }
}