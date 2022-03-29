﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
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
            //CoordType = "wgs84ll";
            CoordType = "bd09ll";
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
            if (dic == null || dic.Count == 0) return default;

            var status = dic["status"].ToInt();
            if (status != 0)
            {
                var msg = (dic["msg"] ?? dic["message"]) + "";

                // 删除无效密钥
                if (IsValidKey(msg)) RemoveKey(LastKey);

                return !ThrowException ? default(T) : throw new Exception(msg);
            }

            if (result.IsNullOrEmpty()) return (T)dic;

            return (T)dic[result];
        }
        #endregion

        #region 地址编码
        /// <summary>查询地址的经纬度坐标</summary>
        /// <param name="address"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        public async Task<IDictionary<String, Object>> GetGeocoderAsync(String address, String city = null)
        {
            if (address.IsNullOrEmpty()) throw new ArgumentNullException(nameof(address));

            // 编码
            address = HttpUtility.UrlEncode(address);
            city = HttpUtility.UrlEncode(city);

            var url = $"http://api.map.baidu.com/geocoder/v2/?address={address}&city={city}&ret_coordtype={CoordType}&output=json";

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

            if (rs["location"] is not IDictionary<String, Object> ds || ds.Count < 2) return null;

            var gp = new GeoPoint
            {
                Longitude = ds["lng"].ToDouble(),
                Latitude = ds["lat"].ToDouble()
            };

            var geo = new GeoAddress
            {
                Location = gp,
            };

            if (formatAddress && gp != null) geo = await GetGeoAsync(gp);

            geo.Precise = rs["precise"].ToBoolean();
            geo.Confidence = rs["confidence"].ToInt();
            geo.Level = rs["level"] + "";

            return geo;
        }
        #endregion

        #region 逆地址编码
        /// <summary>根据坐标获取地址</summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public async Task<IDictionary<String, Object>> GetGeocoderAsync(GeoPoint point)
        {
            if (point == null || point.Longitude < 0.1 || point.Latitude < 0.1) throw new ArgumentNullException(nameof(point));

            var url = $"http://api.map.baidu.com/geocoder/v2/?location={point.Latitude},{point.Longitude}&extensions_town=true&latest_admin=1&coord_type={CoordType}&output=json";

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

            if (type <= 0) type = 13;
            var coord = CoordType;
            if (!coord.IsNullOrEmpty() && coord.Length > 6) coord = coord.TrimEnd("ll");
            var url = $"http://api.map.baidu.com/routematrix/v2/driving?origins={origin.Latitude},{origin.Longitude}&destinations={destination.Latitude},{destination.Longitude}&tactics={type}&coord_type={coord}&output=json";

            var list = await InvokeAsync<IList<Object>>(url, "result");
            if (list == null || list.Count == 0) return null;

            if (list.FirstOrDefault() is not IDictionary<String, Object> geo) return null;

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
        private readonly String _placeUrl = "http://api.map.baidu.com/place/v2/search?output=json";
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
            // 编码
            query = HttpUtility.UrlEncode(query);
            tag = HttpUtility.UrlEncode(tag);
            region = HttpUtility.UrlEncode(region);

            var url = _placeUrl + $"&query={query}&tag={tag}&region={region}&city_limit=true&ret_coordtype={CoordType}";

            var list = await InvokeAsync<IList<Object>>(url, "results");
            if (list == null || list.Count == 0) return null;

            if (list.FirstOrDefault() is not IDictionary<String, Object> rs) return null;

            var geo = new GeoAddress();

            if (rs["location"] is IDictionary<String, Object> ds && ds.Count >= 2)
            {
                var point = new GeoPoint
                {
                    Longitude = ds["lng"].ToDouble(),
                    Latitude = ds["lat"].ToDouble()
                };

                geo.Location = point;
            }
            //else if (rs["num"] is Int32 num && num > 0 && rs["name"] != null)
            //{
            //    // 多个目标城市匹配，重新搜索
            //    return await PlaceSearchAsync(query, tag, rs["name"] + "", formatAddress);
            //}
            else
                return null;

            if (formatAddress && geo?.Location != null) geo = await GetGeoAsync(geo.Location);

            geo.Name = rs["name"] + "";
            var addr = rs["address"] + "";
            if (!addr.IsNullOrEmpty()) geo.Address = addr;

            return geo;
        }
        #endregion

        #region IP定位
        private readonly String _ipUrl = "https://api.map.baidu.com/location/ip";
        /// <summary>IP定位</summary>
        /// <remarks>
        /// https://lbsyun.baidu.com/index.php?title=webapi/ip-api
        /// </remarks>
        /// <param name="ip"></param>
        /// <returns></returns>
        public async Task<IDictionary<String, Object>> IpLocationAsync(String ip)
        {
            var url = _ipUrl + $"?ip={ip}&coor={CoordType}";

            var dic = await InvokeAsync<IDictionary<String, Object>>(url, null);
            if (dic == null || dic.Count == 0) return null;

            if (dic["content"] is not IDictionary<String, Object> rs) return null;

            if (dic.TryGetValue("address", out var fulladdress)) rs["full_address"] = fulladdress;
            if (rs.TryGetValue("address_detail", out var v1))
            {
                rs.Merge(v1);
                rs.Remove("address_detail");
            }
            if (rs.TryGetValue("point", out var v2))
            {
                rs.Merge(v2);
                rs.Remove("point");
            }

            return rs;
        }
        #endregion

        #region 密钥管理
        private readonly String[] _KeyWords = new[] { "AK" };
        /// <summary>是否无效Key。可能禁用或超出限制</summary>
        /// <param name="result"></param>
        /// <returns></returns>
        protected override Boolean IsValidKey(String result)
        {
            if (result.IsNullOrEmpty()) return false;

            if (_KeyWords.Any(e => result.Contains(e))) return true;

            return base.IsValidKey(result);
        }
        #endregion
    }
}