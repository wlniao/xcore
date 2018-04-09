/*==============================================================================
    文件名称：Tool.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：调用OpenApi服务端提供的方法
================================================================================
 
    Copyright 2015 XieChaoyi

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

namespace Wlniao.OpenApi
{
    /// <summary>
    /// 调用OpenApi服务端提供的方法
    /// </summary>
    public class Tool
    {
        /// <summary>
        /// 获取本机IP
        /// </summary>
        /// <returns></returns>
        public static String GetIP()
        {
            var ip = XServer.Common.Get("openapi", "tool", "getip");
            if (strUtil.IsIP(ip))
            {
                return ip;
            }
            return "";
        }
        /// <summary>
        /// 将汉字转换成拼音
        /// </summary>
        /// <returns></returns>
        public static String GetPinyin(String str)
        {
            return XServer.Common.Get("openapi", "tool", "getpinyin", new KeyValuePair<string, string>("str", strUtil.UrlEncode(str)));
        }
        /// <summary>
        /// 将汉字转换成拼音并获取首字母
        /// </summary>
        /// <returns></returns>
        public static String GetChineseSpell(String str)
        {
            var py = GetPinyin(str);
            var words = py.Split(' ');
            var pystr = "";
            foreach (var word in words)
            {
                pystr += word.Substring(0, 1);
            }
            return pystr;
        }


        /// <summary>
        /// 百度地图坐标转换成GPS坐标
        /// </summary>
        /// <param name="location">经纬度坐标（纬度在前）</param>
        /// <returns></returns>
        public static ApiResult<Lbs.Coord> ConvertBaiduToGPS(String location)
        {
            var lbs = location.Split(',', ';');
            if (double.Parse(lbs[0]) > 90 || double.Parse(lbs[0]) < -90)
            {
                return ConvertBaiduToGPS(lbs[1], lbs[0]);
            }
            else
            {
                return ConvertBaiduToGPS(lbs[0], lbs[1]);
            }
        }
        /// <summary>
        /// 百度地图坐标转换成GPS坐标
        /// </summary>
        /// <param name="latitude">维度</param>
        /// <param name="longitude">经度</param>
        /// <returns></returns>
        public static ApiResult<Lbs.Coord> ConvertBaiduToGPS(String latitude, String longitude)
        {
            var rlt = new ApiResult<Lbs.Coord>();
            try
            {
                var json = XServer.Common.Get("openapi", "tool", "geoconvert"
                , new KeyValuePair<string, string>("lat", latitude)
                , new KeyValuePair<string, string>("lng", longitude));
                var _rlt = Json.ToObject<ApiResult<String>>(json);
                if (_rlt.success)
                {
                    rlt.success = true;
                    rlt.message = "success";
                    rlt.data = new Lbs.Coord(_rlt.data);
                }
                else
                {
                    rlt.message = _rlt.message;
                }
            }
            catch (Exception ex)
            {
                rlt.success = false;
                rlt.message = "发生异常：" + ex.Message;
            }
            return rlt;
        }
        /// <summary>
        /// GPS转换成百度地图坐标
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static ApiResult<Lbs.Coord> ConvertGPSToBaiDu(String location)
        {
            var lbs = location.Split(',', ';');
            if (double.Parse(lbs[0]) > 90 || double.Parse(lbs[0]) < -90)
            {
                return ConvertGPSToBaiDu(lbs[1], lbs[0]);
            }
            else
            {
                return ConvertGPSToBaiDu(lbs[0], lbs[1]);
            }
        }
        /// <summary>
        /// GPS转换成百度地图坐标
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <returns></returns>
        public static ApiResult<Lbs.Coord> ConvertGPSToBaiDu(String longitude, String latitude)
        {
            var rlt = new ApiResult<Lbs.Coord>();
            try
            {
                var json = XServer.Common.Get("openapi", "tool", "geoconvert2baidu"
                , new KeyValuePair<string, string>("lat", latitude)
                , new KeyValuePair<string, string>("lng", longitude));
                var _rlt = Json.ToObject<ApiResult<String>>(json);
                if (_rlt.success)
                {
                    rlt.success = true;
                    rlt.message = "success";
                    rlt.data = new Lbs.Coord(_rlt.data);
                }
                else
                {
                    rlt.message = _rlt.message;
                }
            }
            catch (Exception ex)
            {
                rlt.success = false;
                rlt.message = "发生异常：" + ex.Message;
            }
            return rlt;
        }
        /// <summary>
        /// GPS转换成搜搜地图坐标
        /// </summary>
        /// <param name="location"></param>
        /// <returns></returns>
        public static ApiResult<Lbs.Coord> ConvertGPSToSoSo(String location)
        {
            var lbs = location.Split(',', ';');
            if (double.Parse(lbs[0]) > 90 || double.Parse(lbs[0]) < -90)
            {
                return ConvertGPSToSoSo(lbs[1], lbs[0]);
            }
            else
            {
                return ConvertGPSToSoSo(lbs[0], lbs[1]);
            }
        }
        /// <summary>
        /// GPS转换成搜搜地图坐标
        /// </summary>
        /// <param name="longitude"></param>
        /// <param name="latitude"></param>
        /// <returns></returns>
        public static ApiResult<Lbs.Coord> ConvertGPSToSoSo(String longitude, String latitude)
        {
            var rlt = new ApiResult<Lbs.Coord>();
            try
            {
                var json = XServer.Common.Get("openapi", "tool", "geoconvert2soso"
                , new KeyValuePair<string, string>("lat", latitude)
                , new KeyValuePair<string, string>("lng", longitude));
                var _rlt = Json.ToObject<ApiResult<String>>(json);
                if (_rlt.success)
                {
                    rlt.success = true;
                    rlt.message = "success";
                    rlt.data = new Lbs.Coord(_rlt.data);
                }
                else
                {
                    rlt.message = _rlt.message;
                }
            }
            catch (Exception ex)
            {
                rlt.success = false;
                rlt.message = "发生异常：" + ex.Message;
            }
            return rlt;
        }
    }
}
