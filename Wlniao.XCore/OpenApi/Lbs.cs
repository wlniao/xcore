using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Wlniao.OpenApi
{
    /// <summary>
    /// 地理位置工具
    /// </summary>
    public class Lbs
    {
        #region 包含的Model
        /// <summary>
        /// 地理位置建议结果
        /// </summary>
        public class SuggestionModel
        {
            /// <summary>
            /// 
            /// </summary>
            public string name { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string city { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string district { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string business { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string cityid { get; set; }
        }
        /// <summary>
        /// 附近地点结果
        /// </summary>
        public class PlaceModel
        {
            /// <summary>
            /// 
            /// </summary>
            public string name { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string address { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public CoordSimple location { get; set; }
        }



        /// <summary>
        /// 经纬坐标 longitude/latitude
        /// </summary>
        public class Coord
        {
            /// <summary>
            /// 经度
            /// </summary>
            public double longitude { get; set; }
            /// <summary>
            /// 纬度
            /// </summary>
            public double latitude { get; set; }
            /// <summary>
            /// 实例化一个空坐标系
            /// </summary>
            public Coord() { }
            /// <summary>
            /// 实例化一个空坐标系
            /// </summary>
            /// <param name="location">经纬度坐标（纬度在前）</param>
            public Coord(String location)
            {
                var lbs = location.Split(',', ';');
                if (double.Parse(lbs[0]) > 90 || double.Parse(lbs[0]) < -90)
                {
                    this.longitude = double.Parse(lbs[0]);
                    this.latitude = double.Parse(lbs[1]);
                }
                else
                {
                    this.longitude = double.Parse(lbs[1]);
                    this.latitude = double.Parse(lbs[0]);
                }
            }
            /// <summary>
            /// 实例化一个空坐标系
            /// </summary>
            /// <param name="Latitude">纬度</param>
            /// <param name="Longitude">经度</param>
            public Coord(double Latitude, double Longitude)
            {
                this.longitude = Longitude;
                this.latitude = Latitude;
            }
        }
        /// <summary>
        /// 经纬坐标 lng/lat
        /// </summary>
        public class CoordSimple
        {
            /// <summary>
            /// 经度
            /// </summary>
            public double lng { get; set; }
            /// <summary>
            /// 纬度
            /// </summary>
            public double lat { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public CoordSimple()
            {
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="Latitude"></param>
            /// <param name="Longitude"></param>
            public CoordSimple(double Latitude, double Longitude)
            {
                this.lng = Longitude;
                this.lat = Latitude;
            }
        }
        /// <summary>
        /// 坐标计算工具
        /// </summary>
        public class CoordDispose
        {
            private const double EARTH_RADIUS = 6378137.0;//地球半径(米)

            /// <summary>
            /// 角度数转换为弧度公式
            /// </summary>
            /// <param name="d"></param>
            /// <returns></returns>
            private static double radians(double d)
            {
                return d * Math.PI / 180.0;
            }

            /// <summary>
            /// 弧度转换为角度数公式
            /// </summary>
            /// <param name="d"></param>
            /// <returns></returns>
            private static double degrees(double d)
            {
                return d * (180 / Math.PI);
            }

            /// <summary>
            /// 计算两个经纬度之间的直接距离
            /// </summary>
            public static double GetDistance(Coord Degree1, Coord Degree2)
            {
                double radLat1 = radians(Degree1.latitude);
                double radLat2 = radians(Degree2.latitude);
                double a = radLat1 - radLat2;
                double b = radians(Degree1.longitude) - radians(Degree2.longitude);

                double s = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2)
                    + Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2)));
                s = s * EARTH_RADIUS;
                s = Math.Round(s * 10000) / 10000;
                return s;
            }
            /// <summary>
            /// 计算两个经纬度之间的直接距离(google 算法)
            /// </summary>
            public static double GetDistanceGoogle(Coord Degree1, Coord Degree2)
            {
                double radLat1 = radians(Degree1.latitude);
                double radLng1 = radians(Degree1.longitude);
                double radLat2 = radians(Degree2.latitude);
                double radLng2 = radians(Degree2.longitude);

                double s = Math.Acos(Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Cos(radLng1 - radLng2) + Math.Sin(radLat1) * Math.Sin(radLat2));
                s = s * EARTH_RADIUS;
                s = Math.Round(s * 10000) / 10000;
                return s;
            }

            /// <summary>
            /// 以一个经纬度为中心计算出四个顶点
            /// </summary>
            /// <param name="degree">中心坐标</param>
            /// <param name="distance">半径(米)</param>
            /// <returns></returns>
            public static Coord[] GetDegreeCoordinates(Coord degree, double distance)
            {
                double dlng = 2 * Math.Asin(Math.Sin(distance / (2 * EARTH_RADIUS)) / Math.Cos(degree.longitude));
                dlng = degrees(dlng);//转换成角度数

                double dlat = distance / EARTH_RADIUS;
                dlat = degrees(dlat);//转换成角度数

                return new Coord[] { new Coord(Math.Round(degree.latitude - dlat,6),Math.Round(degree.longitude + dlng,6)),//left-top
                                  new Coord(Math.Round(degree.latitude - dlat,6),Math.Round(degree.longitude - dlng,6)),//left-bottom
                                 new Coord( Math.Round(degree.latitude + dlat,6),Math.Round(degree.longitude + dlng,6)),//right-top
                                 new Coord(Math.Round(degree.latitude + dlat,6),Math.Round(degree.longitude - dlng,6) ) //right-bottom
                };
            }

            /// <summary>
            /// 计算多个点的中心点坐标
            /// </summary>
            /// <param name="degrees">半径(米)</param>
            /// <param name="large">大范围/小范围（400km以上/400km以内）</param>
            /// <returns></returns>
            public static Coord GetCenterPoint(List<Coord> degrees,Boolean large=false)
            {
                int total = degrees.Count;
                if (large)
                {
                    // 大范围计算（400km以上）
                    double X = 0, Y = 0, Z = 0;
                    foreach (var degree in degrees)
                    {
                        double lat, lon, x, y, z;
                        lat = degree.latitude * Math.PI / 180;
                        lon = degree.longitude * Math.PI / 180;
                        x = Math.Cos(lat) * Math.Cos(lon);
                        y = Math.Cos(lat) * Math.Sin(lon);
                        z = Math.Sin(lat);
                        X += x;
                        Y += y;
                        Z += z;
                    }
                    X = X / total;
                    Y = Y / total;
                    Z = Z / total;
                    var Lon = Math.Atan2(Y, X);
                    var Hyp = Math.Sqrt(X * X + Y * Y);
                    var Lat = Math.Atan2(Z, Hyp);
                    return new Coord(Lat * 180 / Math.PI, Lon * 180 / Math.PI);
                }
                else
                {
                    // 小范围计算（400km以内）
                    double lat = 0, lon = 0;
                    foreach (var degree in degrees)
                    {
                        lat += degree.latitude * Math.PI / 180;
                        lon += degree.longitude * Math.PI / 180;
                    }
                    lat /= total;
                    lon /= total;
                    return new Coord(lat * 180 / Math.PI, lon * 180 / Math.PI);
                }
            }
        }
        /// <summary>
        /// 
        /// </summary>
        public class IPGis
        {
            /// <summary>
            /// 
            /// </summary>
            public string IPAddress { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string Province { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string CityCode { get; set; }
            /// <summary>
            /// 
            /// </summary>
            public string CityName { get; set; }
            /// <summary>
            /// 经度
            /// </summary>
            public double longitude { get; set; }
            /// <summary>
            /// 纬度
            /// </summary>
            public double latitude { get; set; }
        }


        #endregion

        #region 包含的方法
        /// <summary>
        /// 附近位置查询接口
        /// </summary>
        /// <param name="lat">纬度</param>
        /// <param name="lng">经度</param>
        /// <param name="radius">距离</param>
        /// <param name="query">类型，如：银行,酒店</param>
        /// <returns></returns>
        public static ApiResult<List<PlaceModel>> Search(Double lat, Double lng, Int32 radius, String query = "")
        {
            ApiResult<List<PlaceModel>> rlt = null;
            try
            {
                var json = XServer.Common.Get("openapi", "lbs", "search"
                , new KeyValuePair<string, string>("location", lat.ToString("F8") + "," + lng.ToString("F8"))
                , new KeyValuePair<string, string>("radius", radius.ToString())
                , new KeyValuePair<string, string>("query", query)
                );
                rlt = Json.ToObject<ApiResult<List<PlaceModel>>>(json);
                if (rlt == null)
                {
                    rlt = new ApiResult<List<PlaceModel>>();
                    rlt.success = false;
                    rlt.message = "解析Json结果失败";
                }
            }
            catch (Exception ex)
            {
                rlt = new ApiResult<List<PlaceModel>>();
                rlt.success = false;
                rlt.message = "发生异常：" + ex.Message;
            }
            return rlt;
        }
        /// <summary>
        /// 地理位置建议接口
        /// </summary>
        /// <param name="key">关键字</param>
        /// <param name="city">限定城市范围</param>
        /// <returns></returns>
        public static ApiResult<List<SuggestionModel>> Suggestion(String key, String city = "")
        {
            ApiResult<List<SuggestionModel>> rlt = null;
            try
            {
                var json = XServer.Common.Get("openapi", "lbs", "suggestion"
                , new KeyValuePair<string, string>("key", key)
                , new KeyValuePair<string, string>("city", city)
                );
                rlt = Json.ToObject<ApiResult<List<SuggestionModel>>>(json);
                if (rlt == null)
                {
                    rlt = new ApiResult<List<SuggestionModel>>();
                    rlt.success = false;
                    rlt.message = "解析Json结果失败";
                }
            }
            catch (Exception ex)
            {
                rlt = new ApiResult<List<SuggestionModel>>();
                rlt.success = false;
                rlt.message = "发生异常：" + ex.Message;
            }
            return rlt;
        }

        /// <summary>
        /// 获取路程距离
        /// </summary>
        /// <param name="origins"></param>
        /// <param name="destinations"></param>
        /// <param name="mode">导航模式：walking（步行）、driving（驾车）、line（直线）</param>
        /// <returns></returns>
        public static ApiResult<Double> GetDistance(String origins, String destinations, String mode = "")
        {
            ApiResult<Double> rlt = null;
            try
            {
                var json = XServer.Common.Get("openapi", "lbs", "getdistance"
                , new KeyValuePair<string, string>("origins", origins)
                , new KeyValuePair<string, string>("destinations", destinations)
                , new KeyValuePair<string, string>("mode", mode)
                );
                rlt = Json.ToObject<ApiResult<Double>>(json);
                if (rlt == null)
                {
                    rlt = new ApiResult<Double>();
                    rlt.success = false;
                    rlt.message = "解析Json结果失败";
                }
            }
            catch (Exception ex)
            {
                rlt = new ApiResult<Double>();
                rlt.success = false;
                rlt.message = "发生异常：" + ex.Message;
            }
            return rlt;
        }
        /// <summary>
        /// 通过坐标获取对应地址信息
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <returns></returns>
        public static ApiResult<String> GetAddressByPoint(Double longitude, Double latitude)
        {
            return GetAddressByPoint(longitude.ToString("F8"), latitude.ToString("F8"));
        }
        /// <summary>
        /// 通过坐标获取对应地址信息
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <returns></returns>
        public static ApiResult<String> GetAddressByPoint(String longitude, String latitude)
        {
            ApiResult<String> rlt = null;
            try
            {
                var json = XServer.Common.Get("openapi", "lbs", "getaddressbypoint"
                , new KeyValuePair<string, string>("longitude", longitude)
                , new KeyValuePair<string, string>("latitude", latitude)
                );
                rlt = Json.ToObject<ApiResult<String>>(json);
                if (rlt == null)
                {
                    rlt = new ApiResult<String>();
                    rlt.success = false;
                    rlt.message = "解析Json结果失败";
                }
            }
            catch (Exception ex)
            {
                rlt = new ApiResult<String>();
                rlt.success = false;
                rlt.message = "发生异常：" + ex.Message;
            }
            return rlt;
        }
        /// <summary>
        /// 根据IP地址定位
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static ApiResult<IPGis> GetByIP(String ip)
        {
            ApiResult<IPGis> rlt = null;
            try
            {
                var json = XServer.Common.Get("openapi", "lbs", "getbyip"
                , new KeyValuePair<string, string>("ip", ip)
                );
                rlt = Json.ToObject<ApiResult<IPGis>>(json);
                if (rlt == null)
                {
                    rlt = new ApiResult<IPGis>();
                    rlt.success = false;
                    rlt.message = "解析Json结果失败";
                }
            }
            catch (Exception ex)
            {
                rlt = new ApiResult<IPGis>();
                rlt.success = false;
                rlt.message = "发生异常：" + ex.Message;
            }
            return rlt;
        }


        #endregion
    }
}
