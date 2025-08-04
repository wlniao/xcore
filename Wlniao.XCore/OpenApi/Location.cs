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
        private static Dictionary<string, string> namelist = new Dictionary<string, string>();
        /// <summary>
        /// 根据LocationId获取地点名称
        /// </summary>
        /// <param name="Id"></param>
        /// <returns></returns>
        public static string GetName(string Id)
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
                , new KeyValuePair<string, string>("id", Id));
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
        public static Location Get(string Longitude, string Latitude, int GPSType = 1)
        {
            string json = Common.Get("location", "get"
                , new KeyValuePair<string, string>("longitude", Longitude)
                , new KeyValuePair<string, string>("latitude", Latitude)
                , new KeyValuePair<string, string>("gpstype", GPSType.ToString()));
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
        public static List<Location> GetList(string Parent, int Count, params KeyValuePair<string, string>[] kvs)
        {
            var kvList = new List<KeyValuePair<string, string>>(kvs);
            kvList.Add(new KeyValuePair<string, string>("parent", Parent));
            kvList.Add(new KeyValuePair<string, string>("count", Count.ToString()));
            var rlt = Common.Get<List<Location>>("location", "getlist", kvList.ToArray());
            if (rlt.success)
            {
                return rlt.data;
            }
            return new List<Location>();
        }
        /// <summary>
        /// 获取地点列表
        /// </summary>
        /// <returns></returns>
        public static List<Location> GetList(string Parent, int Count, double Longitude = 0, double Latitude = 0, int GPSType = 0, params KeyValuePair<string, string>[] kvs)
        {
            var kvList = new List<KeyValuePair<string, string>>(kvs);
            kvList.Add(new KeyValuePair<string, string>("count", Count.ToString()));
            kvList.Add(new KeyValuePair<string, string>("parent", Parent));
            kvList.Add(new KeyValuePair<string, string>("longitude", Longitude.ToString("F8")));
            kvList.Add(new KeyValuePair<string, string>("latitude", Latitude.ToString("F8")));
            kvList.Add(new KeyValuePair<string, string>("gpstype", GPSType.ToString()));
            var rlt = Common.Get<List<Location>>("location", "getlist", kvList.ToArray());
            if (rlt.success)
            {
                return rlt.data;
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
        public static List<Location> GetListNearBy(double Longitude, double Latitude, int GPSType = 0, string Tag = "", int Count = 15)
        {
            var rlt = Common.Get<List<Location>>("location", "getlist"
                , new KeyValuePair<string, string>("tag", Tag)
                , new KeyValuePair<string, string>("count", Count.ToString())
                , new KeyValuePair<string, string>("latitude", Latitude.ToString("F8"))
                , new KeyValuePair<string, string>("longitude", Longitude.ToString("F8"))
                , new KeyValuePair<string, string>("gpstype", GPSType.ToString())
                , new KeyValuePair<string, string>("nearby", "true"));
            if (rlt.success)
            {
                return rlt.data;
            }
            return new List<Location>();
        }

        /// <summary>
        /// 获取地点列表
        /// </summary>
        /// <returns></returns>
        public static string GetTree(string Parent, string Tag = "", string Key = "")
        {
            return Common.Get("location", "gettree"
                , new KeyValuePair<string, string>("key", Key)
                , new KeyValuePair<string, string>("tag", Tag)
                , new KeyValuePair<string, string>("parent", Parent));
        }

        /// <summary>
        /// 获取地点路径
        /// </summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public static List<string> GetPath(string id)
        {
            var zxs = "11,12,31,50";   //直辖市ID
            var ids = new List<string>();

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
