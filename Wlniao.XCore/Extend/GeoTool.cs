/*==============================================================================
    文件名称：GeoTool.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：地址坐标处理方法
================================================================================
 
    Copyright 2018 XieChaoyi

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.

===============================================================================*/
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Wlniao;
namespace Wlniao
{
    /// <summary>
    /// 
    /// </summary>
    public class GeoTool
    {
        /// <summary>
        /// 
        /// </summary>
        public enum Direction
        {
            /// <summary>
            /// 上
            /// </summary>
            Top = 0,
            /// <summary>
            /// 右
            /// </summary>
            Right = 1,
            /// <summary>
            /// 下
            /// </summary>
            Bottom = 2,
            /// <summary>
            /// 左
            /// </summary>
            Left = 3
        }
        private const double EARTH_RADIUS = 6378137; //地球半径常量
        private const string Base32 = "0123456789bcdefghjkmnpqrstuvwxyz";
        private static readonly int[] Bits = new[] { 16, 8, 4, 2, 1 };
        private static readonly string[][] Neighbors = {
            new[] {"p0r21436x8zb9dcf5h7kjnmqesgutwvy", "bc01fg45238967deuvhjyznpkmstqrwx", "14365h7k9dcfesgujnmqp0r2twvyx8zb", "238967debc01fg45kmstqrwxuvhjyznp"},//Top,Right,Bottom,Left 
            new[] {"bc01fg45238967deuvhjyznpkmstqrwx", "p0r21436x8zb9dcf5h7kjnmqesgutwvy", "238967debc01fg45kmstqrwxuvhjyznp", "14365h7k9dcfesgujnmqp0r2twvyx8zb"} //Top,Right,Bottom,Left
        };
        private static readonly string[][] Borders = {
            new[] {"prxz", "bcfguvyz", "028b", "0145hjnp"},
            new[] {"bcfguvyz", "prxz", "0145hjnp", "028b"}
        };
        /// <summary>
        /// 计算相邻
        /// </summary>
        /// <param name="hash"></param>
        /// <param name="direction"></param>
        /// <returns></returns>
        public static String CalculateAdjacent(String hash, Direction direction)
        {
            hash = hash.ToLower();

            char lastChr = hash[hash.Length - 1];
            int type = hash.Length % 2;
            var dir = (int)direction;
            string nHash = hash.Substring(0, hash.Length - 1);

            if (Borders[type][dir].IndexOf(lastChr) != -1)
            {
                nHash = CalculateAdjacent(nHash, (Direction)dir);
            }
            return nHash + Base32[Neighbors[type][dir].IndexOf(lastChr)];
        }
        /// <summary>
        /// 细化间隔
        /// </summary>
        /// <param name="interval"></param>
        /// <param name="cd"></param>
        /// <param name="mask"></param>
        public static void RefineInterval(ref double[] interval, int cd, int mask)
        {
            if ((cd & mask) != 0)
            {
                interval[0] = (interval[0] + interval[1]) / 2;
            }
            else
            {
                interval[1] = (interval[0] + interval[1]) / 2;
            }
        }

        /// <summary>
        /// 解码
        /// </summary>
        /// <param name="geohash"></param>
        /// <returns></returns>
        public static double[] Decode(String geohash)
        {
            bool even = true;
            double[] lat = { -90.0, 90.0 };
            double[] lon = { -180.0, 180.0 };

            foreach (char c in geohash)
            {
                int cd = Base32.IndexOf(c);
                for (int j = 0; j < 5; j++)
                {
                    int mask = Bits[j];
                    if (even)
                    {
                        RefineInterval(ref lon, cd, mask);
                    }
                    else
                    {
                        RefineInterval(ref lat, cd, mask);
                    }
                    even = !even;
                }
            }

            return new[] { (lat[0] + lat[1]) / 2, (lon[0] + lon[1]) / 2 };
        }
        /// <summary>
        /// 编码
        /// </summary>
        /// <param name="latitude">纬度</param>
        /// <param name="longitude">经度</param>
        /// <param name="precision">精度误差：1,2500km 2,630km; 3,78km; 4,20kmm; 5,2.4km; 6,610m; 7,76m; 8,19m; 9,2m; </param>
        /// <returns></returns>
        public static String Encode(double latitude, double longitude, int precision = 12)
        {
            bool even = true;
            int bit = 0;
            int ch = 0;
            string geohash = "";

            double[] lat = { -90.0, 90.0 };
            double[] lon = { -180.0, 180.0 };

            if (precision < 1 || precision > 20) precision = 12;

            while (geohash.Length < precision)
            {
                double mid;

                if (even)
                {
                    mid = (lon[0] + lon[1]) / 2;
                    if (longitude > mid)
                    {
                        ch |= Bits[bit];
                        lon[0] = mid;
                    }
                    else
                        lon[1] = mid;
                }
                else
                {
                    mid = (lat[0] + lat[1]) / 2;
                    if (latitude > mid)
                    {
                        ch |= Bits[bit];
                        lat[0] = mid;
                    }
                    else
                        lat[1] = mid;
                }

                even = !even;
                if (bit < 4)
                    bit++;
                else
                {
                    geohash += Base32[ch];
                    bit = 0;
                    ch = 0;
                }
            }
            return geohash;
        }

        /// <summary>
        /// 获取九个格子 顺序 本身 上、下、左、右、 左上、 右上、 左下、右下
        /// </summary>
        /// <param name="geohash"></param>
        /// <returns></returns>
        public static String[] GetGeoHashExpand(String geohash)
        {

            try
            {
                String geohashTop = CalculateAdjacent(geohash, Direction.Top);//上

                String geohashBottom = CalculateAdjacent(geohash, Direction.Bottom);//下

                String geohashLeft = CalculateAdjacent(geohash, Direction.Left);//左

                String geohashRight = CalculateAdjacent(geohash, Direction.Right);//右


                String geohashTopLeft = CalculateAdjacent(geohashLeft, Direction.Top);//左上

                String geohashTopRight = CalculateAdjacent(geohashRight, Direction.Top);//右上

                String geohashBottomLeft = CalculateAdjacent(geohashLeft, Direction.Bottom);//左下

                String geohashBottomRight = CalculateAdjacent(geohashRight, Direction.Bottom);//右下

                String[] expand = { geohash, geohashTop, geohashBottom, geohashLeft, geohashRight, geohashTopLeft, geohashTopRight, geohashBottomLeft, geohashBottomRight };
                return expand;
            }
            catch
            {
                return null;
            }
        }



        /// <summary>
        /// 经纬度转化成弧度
        /// </summary>
        /// <param name="d">经纬度</param>
        /// <returns></returns>
        private static double Rad(double d)
        {
            return (double)d * Math.PI / 180d;
        }
        /// <summary>
        /// 计算两点位置的距离，返回两点的距离，单位米
        /// </summary>
        /// <param name="lat1">第一点纬度</param>
        /// <param name="lng1">第一点经度</param>
        /// <param name="lat2">第二点纬度</param>
        /// <param name="lng2">第二点经度</param>
        /// <returns></returns>
        public static double GetDistance(double lat1, double lng1, double lat2, double lng2)
        {
            double radLat1 = Rad(lat1);
            double radLng1 = Rad(lng1);
            double radLat2 = Rad(lat2);
            double radLng2 = Rad(lng2);
            double a = radLat1 - radLat2;
            double b = radLng1 - radLng2;
            double result = 2 * Math.Asin(Math.Sqrt(Math.Pow(Math.Sin(a / 2), 2) + Math.Cos(radLat1) * Math.Cos(radLat2) * Math.Pow(Math.Sin(b / 2), 2))) * EARTH_RADIUS;
            return result;
        }
    }
}