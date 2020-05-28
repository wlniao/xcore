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
        public string id { get; set; }
        /// <summary>
        /// 上级LocationId
        /// </summary>
        public string parent { get; set; }
        /// <summary>
        /// 简称
        /// </summary>
        public string name { get; set; }
        /// <summary>
        /// 标签
        /// </summary>
        public string tags { get; set; }
        /// <summary>
        /// 地址/全称
        /// </summary>
        public string address { get; set; }
        /// <summary>
        /// 经度
        /// </summary>
        public double lng { get; set; }
        /// <summary>
        /// 纬度
        /// </summary>
        public double lat { get; set; }
        /// <summary>
        /// 与指定坐标的距离
        /// </summary>
        public double distance { get; set; }
        /// <summary>
        /// 
        /// </summary>
        private static Dictionary<String, String> namelist = new Dictionary<String, String>();
        /// <summary>
        /// 根据LocationId获取地点名称
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static String GetName(String Id)
        {
            if (string.IsNullOrEmpty(Id))
            {
                return "";
            }
            else if (namelist.ContainsKey(Id))
            {
                return namelist[Id];
            }
            else
            {
                var item = Get(Id);
                if (item != null)
                {
                    namelist.Add(Id, item.name);
                    return item.name;
                }
            }
            return null;
        }
        /// <summary>
        /// 通过LocationId获取地点信息
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static Location Get(string Id)
        {
            var rlt = Common.Get<Location>("location", "get"
                , new KeyValuePair<String, String>("id", Id));
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
        /// 通过GPS获取最近的地址
        /// </summary>
        /// <param name="Longitude">经度</param>
        /// <param name="Latitude">纬度</param>
        /// <param name="GPSType">坐标类型 1,GPS坐标  2,百度坐标</param>
        /// <returns></returns>
        public static Location Get(String Longitude, String Latitude, Int32 GPSType = 1)
        {
            String json = Common.Get("location", "get"
                , new KeyValuePair<String, String>("longitude", Longitude)
                , new KeyValuePair<String, String>("latitude", Latitude)
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
        /// 获取地点列表
        /// </summary>
        /// <returns></returns>
        public static List<Location> GetList(String Parent, Int32 Count, params KeyValuePair<String, String>[] kvs)
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
        public static List<Location> GetList(String Parent, Int32 Count, Double Longitude = 0, Double Latitude = 0, Int32 GPSType = 0, params KeyValuePair<String, String>[] kvs)
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
        /// 通过GPS获取最近的地点列表
        /// </summary>
        /// <param name="Longitude"></param>
        /// <param name="Latitude"></param>
        /// <param name="GPSType"></param>
        /// <param name="Tag"></param>
        /// <param name="Count"></param>
        /// <returns></returns>
        public static List<Location> GetListNearBy(Double Longitude, Double Latitude, Int32 GPSType = 0, String Tag = "", Int32 Count = 15)
        {
            String json = Common.GetOnlyData("location", "getlist"
                , new KeyValuePair<String, String>("tag", Tag)
                , new KeyValuePair<String, String>("count", Count.ToString())
                , new KeyValuePair<String, String>("latitude", Latitude.ToString("F8"))
                , new KeyValuePair<String, String>("longitude", Longitude.ToString("F8"))
                , new KeyValuePair<String, String>("gpstype", GPSType.ToString())
                , new KeyValuePair<String, String>("nearby", "true"));
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
        public static String GetTree(String Parent, String Tag = "", String Key = "")
        {
            return Common.Get("location", "gettree"
                , new KeyValuePair<String, String>("key", Key)
                , new KeyValuePair<String, String>("tag", Tag)
                , new KeyValuePair<String, String>("parent", Parent));
        }

        /// <summary>
        /// 获取地点路径
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static List<String> GetPath(String id)
        {
            var zxs = "11,12,31,50";   //直辖市ID
            var ids = new List<String>();

            if (id.Length <= 21 && (id.Length == 2 || id.Length == 4 || id.Length == 6 || id.Length == 9 || id.Length == 11
                 || id.Length == 12 || id.Length == 14 || id.Length == 15 || id.Length == 16 || id.Length == 17
                 || id.Length == 18 || id.Length == 20 || id.Length == 21))
            {
                if (id.Length >= 2)
                {
                    //查询地级市、直辖市区县
                    ids.Add(id.Substring(0, 2));
                }
                if (id.Length >= 4)
                {
                    //非直辖市四查询区县
                    if (!zxs.Contains(id.Substring(0, 2)))
                    {
                        ids.Add(id.Substring(0, 4));
                    }
                }
                if (id.Length >= 6)
                {
                    //根据区县查询乡镇节点
                    ids.Add(id.Substring(0, 6));

                    //根据乡镇查询村、社区
                    if (id.Length == 9)
                    {
                        ids.Add(id);
                    }
                    else if (id.Length == 11)
                    {
                        ids.Add(id);
                    }
                    else if (id.Length == 12 || id.Length == 14)
                    {
                        ids.Add(id.Substring(0, 9));
                        ids.Add(id);
                    }
                    else if (id.Length == 15 || id.Length == 17)
                    {
                        ids.Add(id.Substring(0, 9));
                        ids.Add(id.Substring(0, 12));
                        ids.Add(id);
                    }
                    else if (id.Length == 16)
                    {
                        ids.Add(id.Substring(0, 11));
                        ids.Add(id);
                    }
                    else if (id.Length == 18 || id.Length == 20)
                    {
                        ids.Add(id.Substring(0, 9));
                        ids.Add(id.Substring(0, 12));
                        ids.Add(id.Substring(0, 15));
                        ids.Add(id);
                    }
                    else if (id.Length == 21)
                    {
                        ids.Add(id.Substring(0, 11));
                        ids.Add(id.Substring(0, 16));
                        ids.Add(id);
                    }
                }
            }
            return ids;
        }
    }
}
