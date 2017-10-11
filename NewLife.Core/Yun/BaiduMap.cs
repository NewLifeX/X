using System;
using System.Collections.Generic;
using System.Linq;
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

        #region 方法
        /// <summary>远程调用</summary>
        /// <param name="url">目标Url</param>
        /// <param name="result">结果字段</param>
        /// <returns></returns>
        public override async Task<T> InvokeAsync<T>(String url, String result)
        {
            var dic = await base.InvokeAsync<IDictionary<String, Object>>(url, result);
            if (dic == null || dic.Count == 0) return default(T);

            var status = dic["status"].ToInt();
            if (status != 0) throw new Exception((dic["msg"] ?? dic["message"]) + "");

            if (result.IsNullOrEmpty()) return (T)dic;

            return (T)dic[result];
        }
        #endregion

        #region 地址编码
        private String GeoCoderUrl = "http://api.map.baidu.com/geocoder/v2/?address={0}&city={1}&coord_type=wgs84&output=json";
        /// <summary>查询地址的经纬度坐标</summary>
        /// <param name="address"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        public async Task<IDictionary<String, Object>> GetGeocoderAsync(String address, String city = null)
        {
            if (address.IsNullOrEmpty()) throw new ArgumentNullException(nameof(address));

            var url = GeoCoderUrl.F(address, city);

            return await InvokeAsync<IDictionary<String, Object>>(url, "result");
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
        private String url2 = "http://api.map.baidu.com/geocoder/v2/?location={0},{1}&coord_type=wgs84&output=json";
        /// <summary>根据坐标获取地址</summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public async Task<IDictionary<String, Object>> GetGeocoderAsync(GeoPoint point)
        {
            if (point.Longitude < 0.1 || point.Latitude < 0.1) throw new ArgumentNullException(nameof(point));

            var url = url2.F(point.Latitude, point.Longitude);

            return await InvokeAsync<IDictionary<String, Object>>(url, "result");
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

            var addr = new GeoAddress
            {
                Address = rs["formatted_address"] + ""
            };
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

        #region 路径规划
        private String DistanceUrl = "http://api.map.baidu.com/routematrix/v2/driving?origins={0},{1}&destinations={2},{3}&tactics={4}&coord_type=wgs84&output=json";
        /// <summary>计算距离和驾车时间</summary>
        /// <remarks>
        /// http://lbsyun.baidu.com/index.php?title=webapi/route-matrix-api-v2
        /// </remarks>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <param name="type">路径计算的方式和方法</param>
        /// <returns></returns>
        public async Task<Driving> GetDistanceAsync(GeoPoint origin, GeoPoint destination, Int32 type = 13)
        {
            if (origin == null || origin.Longitude < 1 && origin.Latitude < 1) throw new ArgumentNullException(nameof(origin));
            if (destination == null || destination.Longitude < 1 && destination.Latitude < 1) throw new ArgumentNullException(nameof(destination));

            var url = DistanceUrl.F(origin.Latitude, origin.Longitude, destination.Latitude, destination.Longitude, type);

            var list = await InvokeAsync<IList<Object>>(url, "result");
            if (list == null || list.Count == 0) return null;

            var geo = list.FirstOrDefault() as IDictionary<String, Object>;
            if (geo == null) return null;

            var d1 = geo["distance"] as IDictionary<String, Object>;
            var d2 = geo["duration"] as IDictionary<String, Object>;

            var rs = new Driving
            {
                Distance = d1["value"].ToInt(),
                Duration = d2["value"].ToInt()
            };

            return rs;
        }
        #endregion
    }
}