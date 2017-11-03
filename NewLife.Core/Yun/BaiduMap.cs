using System;
using System.Collections.Generic;
using System.ComponentModel;
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
    [DisplayName("百度地图")]
    public class BaiduMap : Map, IMap
    {
        #region 构造
        /// <summary>高德地图</summary>
        public BaiduMap()
        {
            AppKey = "C73357a276668f8b0563d3f936475007";
            KeyName = "ak";
            CoordType = "wgs84ll";
        }
        #endregion

        #region 方法
        /// <summary>远程调用</summary>
        /// <param name="url">目标Url</param>
        /// <param name="result">结果字段</param>
        /// <returns></returns>
        protected override async Task<T> InvokeAsync<T>(String url, String result)
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
        private String _geoUrl = "http://api.map.baidu.com/geocoder/v2/?address={0}&city={1}&ret_coordtype={2}&output=json";
        /// <summary>查询地址的经纬度坐标</summary>
        /// <param name="address"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        public async Task<IDictionary<String, Object>> GetGeocoderAsync(String address, String city = null)
        {
            if (address.IsNullOrEmpty()) throw new ArgumentNullException(nameof(address));

            var url = _geoUrl.F(address, city, CoordType);

            return await InvokeAsync<IDictionary<String, Object>>(url, "result");
        }

        /// <summary>查询地址获取坐标</summary>
        /// <param name="address">地址</param>
        /// <param name="city">城市</param>
        /// <param name="formatAddress">是否格式化地址</param>
        /// <returns></returns>
        public async Task<GeoAddress> GetGeoAsync(String address, String city = null, Boolean formatAddress = false)
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

            var geo = new GeoAddress
            {
                Location = gp,
            };

            if (formatAddress) geo = await GetGeoAsync(gp);

            geo.Precise = rs["precise"].ToBoolean();
            geo.Confidence = rs["confidence"].ToInt();
            geo.Level = rs["level"] + "";

            return geo;
        }
        #endregion

        #region 逆地址编码
        private String _regeoUrl = "http://api.map.baidu.com/geocoder/v2/?location={0},{1}&extensions_town=true&latest_admin=1&coord_type={2}&output=json";
        /// <summary>根据坐标获取地址</summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public async Task<IDictionary<String, Object>> GetGeocoderAsync(GeoPoint point)
        {
            if (point.Longitude < 0.1 || point.Latitude < 0.1) throw new ArgumentNullException(nameof(point));

            var url = _regeoUrl.F(point.Latitude, point.Longitude, CoordType);

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
                Address = rs["formatted_address"] + "",
                Confidence = rs["confidence"].ToInt(),
            };
            if (rs["addressComponent"] is IDictionary<String, Object> component)
            {
                var reader = new JsonReader();
                reader.ToObject(component, null, addr);

                addr.Code = component["adcode"].ToInt();
                addr.Township = component["town"] + "";
                addr.StreetNumber = component["street_number"] + "";
            }

            addr.Location = point;

            return addr;
        }
        #endregion

        #region 路径规划
        private String _distanceUrl = "http://api.map.baidu.com/routematrix/v2/driving?origins={0},{1}&destinations={2},{3}&tactics={4}&coord_type={5}&output=json";
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

            var url = _distanceUrl.F(origin.Latitude, origin.Longitude, destination.Latitude, destination.Longitude, type, CoordType);

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

        #region 地址检索
        private String _placeUrl = "http://api.map.baidu.com/place/v2/search?output=json";
        /// <summary>行政区划区域检索</summary>
        /// <remarks>
        /// http://lbsyun.baidu.com/index.php?title=webapi/guide/webservice-placeapi
        /// </remarks>
        /// <param name="query"></param>
        /// <param name="tag"></param>
        /// <param name="region"></param>
        /// <param name="formatAddress"></param>
        /// <returns></returns>
        public async Task<GeoAddress> PlaceSearchAsync(String query, String tag, String region, Boolean formatAddress = true)
        {
            var url = _placeUrl + $"&query={query}&tag={tag}&region={region}&city_limit=true&ret_coordtype={CoordType}";

            var list = await InvokeAsync<IList<Object>>(url, "results");
            if (list == null || list.Count == 0) return null;

            var rs = list.FirstOrDefault() as IDictionary<String, Object>;
            if (rs == null) return null;

            var geo = new GeoAddress();

            if (rs["location"] is IDictionary<String, Object> ds && ds.Count >= 2)
            {
                var point = new GeoPoint();
                point.Longitude = ds["lng"].ToDouble();
                point.Latitude = ds["lat"].ToDouble();

                geo.Location = point;
            }

            if (formatAddress) geo = await GetGeoAsync(geo.Location);

            geo.Name = rs["name"] + "";
            geo.Address = rs["address"] + "";

            return geo;
        }
        #endregion
    }
}