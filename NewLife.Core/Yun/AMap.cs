using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.Yun
{
    /// <summary>高德地图</summary>
    /// <remarks>
    /// 参考地址 http://lbs.amap.com/api/webservice/guide/api/georegeo/#geo
    /// </remarks>
    public class AMap : Map, IMap
    {
        #region 构造
        /// <summary>高德地图</summary>
        public AMap()
        {
            AppKey = "99ac084eb7dd8015fe0ff4404fa800da";
            KeyName = "key";
        }
        #endregion

        #region 地址编码
        private String GeoCoderUrl = "http://restapi.amap.com/v3/geocode/geo?address={0}&city={1}&output=json";
        /// <summary>查询地址的经纬度坐标</summary>
        /// <param name="address"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        public async Task<IDictionary<String, Object>> GetGeocoderAsync(String address, String city = null)
        {
            if (address.IsNullOrEmpty()) throw new ArgumentNullException(nameof(address));

            var html = await GetStringAsync(GeoCoderUrl.F(address, city));
            if (html.IsNullOrEmpty()) return null;

            var dic = new JsonParser(html).Decode() as IDictionary<String, Object>;
            if (dic == null || dic.Count == 0) return null;

            var status = dic["status"].ToInt();
            if (status != 1) throw new Exception(dic["info"] + "");

            var arr = dic["geocodes"] as IList;
            if (arr == null || arr.Count == 0) return null;

            return arr[0] as IDictionary<String, Object>;
        }

        /// <summary>查询地址获取坐标</summary>
        /// <param name="address"></param>
        /// <param name="city"></param>
        /// <returns></returns>
        public async Task<GeoAddress> GetGeoAsync(String address, String city = null)
        {
            var rs = await GetGeocoderAsync(address, city);
            if (rs == null || rs.Count == 0) return null;

            var point = new GeoPoint();

            var ds = (rs["location"] + "").Split(",");
            if (ds != null && ds.Length >= 2)
            {
                point.Longitude = ds[0].ToDouble();
                point.Latitude = ds[1].ToDouble();
            }

            var addr = new GeoAddress();

            var reader = new JsonReader();
            reader.ToObject(rs, null, addr);

            addr.Code = rs["adcode"].ToInt();
            addr.Town = rs["township"] + "";
            addr.StreetNumber = rs["number"] + "";

            addr.Location = point;

            return addr;
        }
        #endregion

        #region 计算距离
        private String DistanceUrl = "http://restapi.amap.com/v3/distance?origins={0},{1}&destination={2},{3}&type={4}&output=json";
        /// <summary>计算距离和驾车时间</summary>
        /// <remarks>
        /// http://lbs.amap.com/api/webservice/guide/api/direction
        /// 
        /// type:
        /// 0：直线距离
        /// 1：驾车导航距离（仅支持国内坐标）。
        /// 必须指出，当为1时会考虑路况，故在不同时间请求返回结果可能不同。
        /// 此策略和driving接口的 strategy = 4策略一致
        /// 2：公交规划距离（仅支持同城坐标）
        /// 3：步行规划距离（仅支持5km之间的距离）
        /// 
        /// distance    路径距离，单位：米
        /// duration    预计行驶时间，单位：秒
        /// </remarks>
        /// <param name="origin"></param>
        /// <param name="destination"></param>
        /// <param name="type">路径计算的方式和方法</param>
        /// <returns></returns>
        public async Task<IDictionary<String, Int32>> GetDistanceAsync(GeoPoint origin, GeoPoint destination, Int32 type = 1)
        {
            if (origin == null || origin.Longitude < 1 && origin.Latitude < 1) throw new ArgumentNullException(nameof(origin));
            if (destination == null || destination.Longitude < 1 && destination.Latitude < 1) throw new ArgumentNullException(nameof(destination));

            var html = await GetStringAsync(DistanceUrl.F(origin.Longitude, origin.Latitude, destination.Longitude, destination.Latitude, type));
            if (html.IsNullOrEmpty()) return null;

            var dic = new JsonParser(html).Decode() as IDictionary<String, Object>;
            if (dic == null || dic.Count == 0) return null;

            var status = dic["status"].ToInt();
            if (status != 1) throw new Exception(dic["info"] + "");

            var geo = (dic["results"] as IList<Object>).FirstOrDefault() as IDictionary<String, Object>;
            if (geo == null) return null;

            var rs = new Dictionary<String, Int32>();
            foreach (var item in geo)
            {
                var v = item.Value.ToInt();
                if (v > 0) rs[item.Key] = v;
            }

            return rs;
        }
        #endregion
    }
}