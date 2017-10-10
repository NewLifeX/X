using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.Yun
{
    /// <summary>百度地图</summary>
    /// <remarks>
    /// 参考手册 http://lbsyun.baidu.com/index.php?title=webapi/guide/webservice-geocoding
    /// </remarks>
    public class BaiduMap : Map, IMap
    {
        #region 构造
        /// <summary>高德地图</summary>
        public BaiduMap()
        {
            AppKey = "C73357a276668f8b0563d3f936475007";
            KeyName = "ak";
        }
        #endregion

        #region 地址编码
        private String GeoCoderUrl = "http://api.map.baidu.com/geocoder/v2/?address={0}&city={1}&output=json";
        /// <summary>查询地址的经纬度坐标</summary>
        /// <param name="address"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        public async Task<IDictionary<String, Object>> GetGeocoderAsync(String address, String city = null)
        {
            if (address.IsNullOrEmpty()) throw new ArgumentNullException(nameof(address));

            var url = GeoCoderUrl.F(address, city);

            var html = await GetStringAsync(url);
            if (html.IsNullOrEmpty()) return null;

            var dic = new JsonParser(html).Decode() as IDictionary<String, Object>;
            if (dic == null || dic.Count == 0) return null;

            var status = dic["status"].ToInt();
            if (status != 0) throw new Exception(dic["msg"] + "");

            return dic["result"] as IDictionary<String, Object>;
        }

        /// <summary>查询地址获取坐标</summary>
        /// <param name="address"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        public async Task<GeoPoint> GetGeoAsync(String address, String city = null)
        {
            var rs = await GetGeocoderAsync(address, city);
            if (rs == null || rs.Count == 0) return null;

            var ds = rs["location"] as IDictionary<String, Object>;
            if (ds == null || ds.Count < 2) return null;

            var gp = new GeoPoint
            {
                Longitude = ds["lng"].ToDouble(),
                Latitude = ds["lat"].ToDouble()
            };

            return gp;
        }
        #endregion

        #region 逆地址编码
        private String url2 = "http://api.map.baidu.com/geocoder/v2/?location={0},{1}&output=json";
        /// <summary>根据坐标获取地址</summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public async Task<IDictionary<String, Object>> GetGeocoderAsync(GeoPoint point)
        {
            if (point.Longitude < 0.1 || point.Latitude < 0.1) throw new ArgumentNullException(nameof(point));

            var url = url2.F(point.Latitude, point.Longitude);

            var html = await GetStringAsync(url);
            if (html.IsNullOrEmpty()) return null;

            var dic = new JsonParser(html).Decode() as IDictionary<String, Object>;
            if (dic == null || dic.Count == 0) return null;

            var status = dic["status"].ToInt();
            if (status != 0) throw new Exception(dic["msg"] + "");

            return dic["result"] as IDictionary<String, Object>;
        }

        /// <summary>根据坐标获取地址</summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public async Task<GeoAddress> GetGeoAsync(GeoPoint point)
        {
            var rs = await GetGeocoderAsync(point);
            if (rs == null || rs.Count == 0) return null;

            if (rs["location"] is IDictionary<String, Object> ds && ds.Count >= 2)
            {
                point.Longitude = ds["lng"].ToDouble();
                point.Latitude = ds["lat"].ToDouble();
            }

            var addr = new GeoAddress();
            addr.Address = rs["formatted_address"] + "";
            if (rs["addressComponent"] is IDictionary<String, Object> component)
            {
                var reader = new JsonReader();
                reader.ToObject(component, null, addr);

                addr.Code = component["adcode"].ToInt();
                addr.StreetNumber = component["street_number"] + "";
            }

            addr.Location = point;

            return addr;
        }
        #endregion
    }
}