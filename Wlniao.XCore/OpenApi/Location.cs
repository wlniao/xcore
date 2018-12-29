using System;
using System.Collections.Generic;
using Wlniao;

namespace Wlniao.OpenApi
{
    /// <summary>
    /// 地理位置相关
    /// </summary>
    public class Location
    {
        /// <summary>
        /// LocationId
        /// </summary>
        public string LocationId { get; set; }
        /// <summary>
        /// 上级LocationId
        /// </summary>
        public string ParentId { get; set; }
        /// <summary>
        /// 简称
        /// </summary>
        public string Name { get; set; }
        /// <summary>
        /// 标签
        /// </summary>
        public string Tags { get; set; }
        /// <summary>
        /// 地址/全称
        /// </summary>
        public string Address { get; set; }
        /// <summary>
        /// 经度
        /// </summary>
        public double Longitude { get; set; }
        /// <summary>
        /// 纬度
        /// </summary>
        public double Latitude { get; set; }
        /// <summary>
        /// 与指定坐标的距离
        /// </summary>
        public double Distance { get; set; }
        private static Dictionary<String, String> namelist = new Dictionary<String, String>();
        /// <summary>
        /// 根据LocationId获取地点名称
        /// </summary>
        /// <param name="LocationId"></param>
        /// <returns></returns>
        public static String GetName(String LocationId)
        {
            if (string.IsNullOrEmpty(LocationId))
            {
                return "";
            }
            else if (namelist.ContainsKey(LocationId))
            {
                return namelist[LocationId];
            }
            else
            {
                var item = Get(LocationId);
                if (item != null)
                {
                    namelist.Add(LocationId, item.Name);
                    return item.Name;
                }
            }
            return null;
        }
        /// <summary>
        /// 通过LocationId获取地点信息
        /// </summary>
        /// <param name="LocationId"></param>
        /// <returns></returns>
        public static Location Get(string LocationId)
        {
            var rlt = GetWithApiResult(LocationId);
            if (rlt.success)
            {
                return rlt.data;
            }
            else
            {
                return null;
            }
        }
        /// <summary>
        /// 带接口输出信息
        /// </summary>
        /// <param name="LocationId"></param>
        /// <returns></returns>
        public static ApiResult<Location> GetWithApiResult(string LocationId)
        {
            return Common.Get<Location>("location", "get"
                , new KeyValuePair<String, String>("LocationId", LocationId));
        }

        /// <summary>
        /// 通过GPS获取最近的地址
        /// </summary>
        /// <param name="Longitude">经度</param>
        /// <param name="Latitude">纬度</param>
        /// <param name="Tag">标签</param>
        /// <param name="GPSType">坐标类型 1,GPS坐标  2,百度坐标</param>
        /// <returns></returns>
        public static Location GetByGPS(String Longitude, String Latitude, String Tag = "", Int32 GPSType = 1)
        {
            String json = Common.Get("location", "getbygps"
                , new KeyValuePair<String, String>("longitude", Longitude)
                , new KeyValuePair<String, String>("latitude", Latitude)
                , new KeyValuePair<String, String>("tag", Tag)
                , new KeyValuePair<String, String>("gpstype", GPSType.ToString()));
            if (string.IsNullOrEmpty(json))
            {
                return null;
            }
            else
            {
                return Json.ToObject<Location>(json);
            }
        }

        /// <summary>
        /// 通过GPS获取最近的地点列表
        /// </summary>
        /// <param name="Longitude"></param>
        /// <param name="Latitude"></param>
        /// <param name="Tag"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public static List<Location> GetNearest(Double Longitude, Double Latitude, String Tag = "", Int32 Count = 15)
        {
            String json = Common.GetOnlyData("Location", "GetNearest"
                , new KeyValuePair<String, String>("Longitude", Longitude.ToString("F8"))
                , new KeyValuePair<String, String>("Latitude", Latitude.ToString("F8"))
                , new KeyValuePair<String, String>("Tag", Tag)
                , new KeyValuePair<String, String>("Count", Count.ToString()));
            if (!string.IsNullOrEmpty(json))
            {
                return Json.ToList<Location>(json);
            }
            return new List<Location>();
        }

        /// <summary>
        /// 获取地点列表
        /// </summary>
        /// <returns></returns>
        public static List<Location> GetList(string Parent, int Count, params KeyValuePair<String, String>[] kvs)
        {
            var kvList = new List<KeyValuePair<String, String>>(kvs);
            kvList.Add(new KeyValuePair<String, String>("parent", Parent));
            kvList.Add(new KeyValuePair<String, String>("count", Count.ToString()));
            var json = Common.GetOnlyData("location", "getlist", kvList.ToArray());
            if (!string.IsNullOrEmpty(json))
            {
                return Json.ToList<Location>(json);
            }
            return new List<Location>();
        }
        /// <summary>
        /// 获取地点列表
        /// </summary>
        /// <returns></returns>
        public static List<Location> GetList(string Parent, int Count, Double Longitude = 0, Double Latitude = 0, int GPSType = 0, params KeyValuePair<String, String>[] kvs)
        {
            var kvList = new List<KeyValuePair<String, String>>(kvs);
            kvList.Add(new KeyValuePair<String, String>("count", Count.ToString()));
            kvList.Add(new KeyValuePair<String, String>("parent", Parent));
            kvList.Add(new KeyValuePair<String, String>("longitude", Longitude.ToString("F8")));
            kvList.Add(new KeyValuePair<String, String>("latitude", Latitude.ToString("F8")));
            kvList.Add(new KeyValuePair<String, String>("gpstype", GPSType.ToString()));
            var json = Common.GetOnlyData("location", "getlist", kvList.ToArray());
            if (!string.IsNullOrEmpty(json))
            {
                return Json.ToList<Location>(json);
            }
            return new List<Location>();
        }

        /// <summary>
        /// 获取地点列表
        /// </summary>
        /// <returns></returns>
        public static String GetTree(string Parent)
        {
            return Common.GetOnlyData("location", "gettree", new KeyValuePair<String, String>("Parent", Parent));
        }
    }
}
