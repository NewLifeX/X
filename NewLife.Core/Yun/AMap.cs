using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using NewLife.Data;
using NewLife.Serialization;

namespace NewLife.Yun
{
    /// <summary>高德地图</summary>
    /// <remarks>
    /// 参考地址 http://lbs.amap.com/api/webservice/guide/api/georegeo/#geo
    /// </remarks>
    [DisplayName("高德地图")]
    public class AMap : Map, IMap
    {
        #region 构造
        /// <summary>高德地图</summary>
        public AMap()
        {
            AppKey = "" +
                // 大石头
                "99ac084eb7dd8015fe0ff4404fa800da," +
                "37262598ce2e94f31349ce892b4dbde1," +
                "c313359ec97eb28b57861c2ba177daef," +
                "bcd72261d7c2e9a00cea3ba5d234eda6," +
                "33cad9379e0592a40185c2e4faf10348," +
                "18a6fa58d5ed2ff21711671c2f07cd3b," +
                "cba659ee8bff537bd78e1af625e29c7e," +
                "8a02a1813747a77cc202896ace9d50cc," +
                "953d44e2b8f1a7f126b5970dd2883b2f," +
                "3607f421048ff109ba56f36c0e77d3a1," +
                "" +
                // 六条
                "2aada76e462af71e1b67ba1df22d0fa4," +
                "038a84bf20e8306fdd2203110739110c," +
                "29360e6eeb7b921d644cde3068ddf24f," +
                "a8e5e3e7b4068be9c525bd2b7854eb20," +
                "9935cf01abd570532ab7a19f83f905d3," +
                "ecacc934a6529b39513ea2bfa8a03def," +
                "08c70a500587c1006e10e4a096cb6b58," +
                "3508dadf3777531cef63bdc061ac020f," +
                "331566353c89521faffd84af22cd4f5f," +
                "6f19a71c6fd71baf54680eb63c4d5fce," +
                "" +
                // 照月
                "e21e2089c19945c83d3ed8cecbcbb685," +
                "3ca359e489f0251de0255ec1f53b0c70," +
                "1c3fa2a36a844b90ec16cb160a46478b," +
                "2dbec473a85f148b6392289385fee01e," +
                "4e09815630bcc72af47e9b3123c3a1f4," +
                "43c6c1d607b62118951e14830d171cbd," +
                "10858d92ddcabd5ddf5e41ab142e5ca0," +
                "d14b543d43be7d4a00ffc19a93487a44," +
                "9cf4e3e6e6bf207943f47d517818cb32," +
                "a5ebae758cf05562f2db8b7ff296876b," +
                "" +
                // 老邱
                "88518231bdda6c6eec394488b9c456fd," +
                "d8c5125ae7947e1e7eaac6c92a898801," +
                "001d285749a8cef63b5d1aaed8333e66," +
                "cd671f341ef0f25b169d4ea780514e63," +
                "ab086ac2309ae555a64baa2b32beefd0," +
                "2bd19587c3f9eefefe77ecfccec05b7a," +
                "c751fd89fbde2572956e09e3a09dee41," +
                "0d751f074063ff308b93cae343cad9f2," +
                "8e3802ad274b4c079619e772671f851f";
            KeyName = "key";
            //CoordType = "wgs84ll";
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
            if (status != 1)
            {
                var msg = dic["info"] + "";

                // 删除无效密钥
                if (IsValidKey(msg)) RemoveKey(LastKey);

                return !ThrowException ? default(T) : throw new Exception(msg);
            }

            if (result.IsNullOrEmpty()) return (T)dic;

            return (T)dic[result];
        }
        #endregion

        #region 地址编码
        private readonly String _geoUrl = "http://restapi.amap.com/v3/geocode/geo?address={0}&city={1}&output=json";
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

            var url = _geoUrl.F(address, city);

            var list = await InvokeAsync<IList<Object>>(url, "geocodes");
            return list?.FirstOrDefault() as IDictionary<String, Object>;
        }

        /// <summary>查询地址获取坐标</summary>
        /// <param name="address">地址</param>
        /// <param name="city">城市</param>
        /// <param name="formatAddress">是否格式化地址。高德地图默认已经格式化地址</param>
        /// <returns></returns>
        public async Task<GeoAddress> GetGeoAsync(String address, String city = null, Boolean formatAddress = false)
        {
            var rs = await GetGeocoderAsync(address, city);
            if (rs == null || rs.Count == 0) return null;

            var gp = new GeoPoint();

            var ds = (rs["location"] + "").Split(",");
            if (ds != null && ds.Length >= 2)
            {
                gp.Longitude = ds[0].ToDouble();
                gp.Latitude = ds[1].ToDouble();
            }

            var geo = new GeoAddress();
            var reader = new JsonReader();
            reader.ToObject(rs, null, geo);

            geo.Code = rs["adcode"].ToInt();

            if (rs["township"] is IList<Object> ts && ts.Count > 0) geo.Township = ts[0] + "";
            if (rs["number"] is IList<Object> ns && ns.Count > 0) geo.StreetNumber = ns[0] + "";

            geo.Location = gp;

            if (formatAddress)
            {
                var geo2 = await GetGeoAsync(gp);
                if (geo2 != null)
                {
                    geo = geo2;
                    if (geo.Level.IsNullOrEmpty()) geo.Level = rs["level"] + "";
                }
            }

            {
                var addr = rs["formatted_address"] + "";
                if (!addr.IsNullOrEmpty()) geo.Address = addr;
            }
            // 替换竖线
            if (!geo.Address.IsNullOrEmpty()) geo.Address = geo.Address.Replace("|", null);

            return geo;
        }
        #endregion

        #region 逆地址编码
        private readonly String _regeoUrl = "http://restapi.amap.com/v3/geocode/regeo?location={0},{1}&extensions=base&output=json";
        /// <summary>根据坐标获取地址</summary>
        /// <remarks>
        /// http://lbs.amap.com/api/webservice/guide/api/georegeo/#regeo
        /// </remarks>
        /// <param name="point"></param>
        /// <returns></returns>
        public async Task<IDictionary<String, Object>> GetGeocoderAsync(GeoPoint point)
        {
            if (point.Longitude < 0.1 || point.Latitude < 0.1) throw new ArgumentNullException(nameof(point));

            var url = _regeoUrl.F(point.Longitude, point.Latitude);

            return await InvokeAsync<IDictionary<String, Object>>(url, "regeocode");
        }

        /// <summary>根据坐标获取地址</summary>
        /// <param name="point"></param>
        /// <returns></returns>
        public async Task<GeoAddress> GetGeoAsync(GeoPoint point)
        {
            var rs = await GetGeocoderAsync(point);
            if (rs == null || rs.Count == 0) return null;

            var geo = new GeoAddress
            {
                Address = rs["formatted_address"] + ""
            };
            geo.Location = new GeoPoint
            {
                Longitude = point.Longitude,
                Latitude = point.Latitude
            };
            if (rs["addressComponent"] is IDictionary<String, Object> component)
            {
                var reader = new JsonReader();
                reader.ToObject(component, null, geo);

                geo.Code = component["adcode"].ToInt();

                geo.Township = null;
                geo.Towncode = null;
                if (component["township"] is String ts) geo.Township = ts;
                if (component["towncode"] is String tc) geo.Towncode = tc;

                // 去掉乡镇代码后面多余的0
                var tcode = geo.Towncode;
                if (!tcode.IsNullOrEmpty() && tcode.Length > 6 + 3) geo.Towncode = tcode.TrimEnd("000");

                if (component["streetNumber"] is IDictionary<String, Object> sn && sn.Count > 0)
                {
                    geo.Street = sn["street"] + "";
                    geo.StreetNumber = sn["number"] + "";
                }
            }

            geo.Location = point;
            // 替换竖线
            if (!geo.Address.IsNullOrEmpty()) geo.Address = geo.Address.Replace("|", null);

            return geo;
        }
        #endregion

        #region 路径规划
        private readonly String _distanceUrl = "http://restapi.amap.com/v3/distance?origins={0},{1}&destination={2},{3}&type={4}&output=json";
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
        public async Task<Driving> GetDistanceAsync(GeoPoint origin, GeoPoint destination, Int32 type = 1)
        {
            if (origin == null || origin.Longitude < 1 && origin.Latitude < 1) throw new ArgumentNullException(nameof(origin));
            if (destination == null || destination.Longitude < 1 && destination.Latitude < 1) throw new ArgumentNullException(nameof(destination));

            if (type <= 0) type = 1;
            var url = _distanceUrl.F(origin.Longitude, origin.Latitude, destination.Longitude, destination.Latitude, type);

            var list = await InvokeAsync<IList<Object>>(url, "results");
            if (list == null || list.Count == 0) return null;

            var geo = list.FirstOrDefault() as IDictionary<String, Object>;
            if (geo == null) return null;

            var rs = new Driving
            {
                Distance = geo["distance"].ToInt(),
                Duration = geo["duration"].ToInt()
            };

            return rs;
        }
        #endregion

        #region 行政区划
        //private String url3 = "http://restapi.amap.com/v3/config/district?keywords={0}&subdistrict={1}&filter={2}&extensions=all&output=json";
        private readonly String _areaUrl = "http://restapi.amap.com/v3/config/district?keywords={0}&subdistrict={1}&filter={2}&extensions=base&output=json";
        /// <summary>行政区划</summary>
        /// <remarks>
        /// http://lbs.amap.com/api/webservice/guide/api/district
        /// </remarks>
        /// <param name="keywords">查询关键字</param>
        /// <param name="subdistrict">设置显示下级行政区级数</param>
        /// <param name="code">按照指定行政区划进行过滤，填入后则只返回该省/直辖市信息</param>
        /// <returns></returns>
        public async Task<IList<GeoArea>> GetAreaAsync(String keywords, Int32 subdistrict = 1, Int32 code = 0)
        {
            if (keywords.IsNullOrEmpty()) throw new ArgumentNullException(nameof(keywords));

            // 编码
            keywords = HttpUtility.UrlEncode(keywords);

            var url = _areaUrl.F(keywords, subdistrict, code);

            var list = await InvokeAsync<IList<Object>>(url, "districts");
            if (list == null || list.Count == 0) return null;

            var geo = list.FirstOrDefault() as IDictionary<String, Object>;
            if (geo == null) return null;

            var addrs = GetArea(geo, 0);

            return addrs;
        }

        private IList<GeoArea> GetArea(IDictionary<String, Object> geo, Int32 parentCode)
        {
            if (geo == null || geo.Count == 0) return null;

            var addrs = new List<GeoArea>();

            var root = new GeoArea();
            new JsonReader().ToObject(geo, null, root);
            root.Code = geo["adcode"].ToInt();
            if (parentCode > 0) root.ParentCode = parentCode;

            addrs.Add(root);

            if (geo["districts"] is IList<Object> childs && childs.Count > 0)
            {
                foreach (var item in childs)
                {
                    if (item is IDictionary<String, Object> geo2)
                    {
                        var rs = GetArea(geo2, root.Code);
                        if (rs != null && rs.Count > 0) addrs.AddRange(rs);
                    }
                }
            }

            return addrs;
        }
        #endregion

        #region 密钥管理
        private readonly String[] _KeyWords = new[] { "TOO_FREQUENT", "LIMIT", "NOMATCH", "RECYCLED" };
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