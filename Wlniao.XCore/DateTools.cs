/*==============================================================================
    文件名称：DateTools.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：常用的时间操作处理方法
================================================================================
 
    Copyright 2014 XieChaoyi

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
namespace Wlniao
{
    /// <summary>
    /// 常用的时间操作处理方法
    /// </summary>
    public partial class DateTools
    {
        private static bool init = false;
        internal static int _TimeZone = 0;
        /// <summary>
        /// 当前运行的时区
        /// </summary>
        public static int TimeZone
        {
            get
            {
                if (init)
                {
                    return _TimeZone;
                }
                else
                {
                    _TimeZone = cvt.ToInt(Config.GetSetting("WLN_TIMEZONE"));
                    init = true;
                }
                return _TimeZone;
            }
        }
        /// <summary>
        /// 获取Unix时间戳
        /// </summary>
        /// <returns></returns>
        public static long GetUnix()
        {
            return DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000000;
        }
        /// <summary>
        /// 获取Unix时间戳
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long GetUnix(string time)
        {
            try
            {
                return GetUnix(Convert(time));
            }
            catch
            {
                return 0;
            }
        }
        /// <summary>
        /// 获取Unix时间戳
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long GetUnix(DateTime time)
        {
            if (time.Kind == DateTimeKind.Unspecified)
            {
                return time.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)).Ticks / 10000000 - TimeZone * 3600;
            }
            else if (time.Kind == DateTimeKind.Utc)
            {
                return time.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000000;
            }
            else
            {
                return time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000000;
            }
        }
        /// <summary>
        /// 获取无时区的当前时间
        /// </summary>
        /// <returns></returns>
        public static DateTime GetNow()
        {
            var temp = DateTime.UtcNow.AddHours(TimeZone);
            return new DateTime(temp.Year, temp.Month, temp.Day, temp.Hour, temp.Minute, temp.Second, temp.Millisecond, DateTimeKind.Utc);
        }
        /// <summary>
        /// 将Unix时间戳转换为无时区的当前时间（可通过WLN_TIMEZONE设置）
        /// </summary>
        /// <param name="unixtime"></param>
        /// <returns></returns>
        public static DateTime Convert(long unixtime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified).Add(new TimeSpan(unixtime * 10000000)).AddHours(TimeZone);
        }
        /// <summary>
        /// 将时间字符串转换为无时区的当前时间
        /// </summary>
        /// <param name="strtime"></param>
        /// <returns></returns>
        public static DateTime Convert(string strtime)
        {
            var temp = System.Convert.ToDateTime(strtime);
            return new DateTime(temp.Year, temp.Month, temp.Day, temp.Hour, temp.Minute, temp.Second, temp.Millisecond, DateTimeKind.Utc).AddHours(0 - TimeZone);
        }
        /// <summary>
        /// 将Unix时间戳转换为UTC世界协调时间
        /// </summary>
        /// <param name="unixtime"></param>
        /// <returns></returns>
        public static DateTime ConvertToUtc(long unixtime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Add(new TimeSpan(unixtime * 10000000));
        }
        /// <summary>
        /// 将时间字符串转换为UTC世界协调时间
        /// </summary>
        /// <param name="strtime"></param>
        /// <returns></returns>
        public static DateTime ConvertToUtc(string strtime)
        {
            var temp = TimeZone == 0 ? System.Convert.ToDateTime(strtime) : System.Convert.ToDateTime(strtime).AddHours(0 - TimeZone);
            return new DateTime(temp.Year, temp.Month, temp.Day, temp.Hour, temp.Minute, temp.Second, temp.Millisecond, DateTimeKind.Utc);
        }
        /// <summary>
        /// 将Unix时间戳按当地时间及指定格式输出
        /// </summary>
        /// <param name="unixtime"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ConvertToFormat(long unixtime, string format = "yyyy-MM-dd HH:mm:ss")
        {
            return Convert(unixtime).ToString(format);
        }
        /// <summary>
        /// 将Unix时间戳转换成GMT格式
        /// </summary>
        /// <param name="unixtime"></param>
        /// <returns></returns>
        public static string ConvertToGMT(long unixtime = 0)
        {
            var dt = DateTime.UtcNow;
            if (unixtime > 0)
            {
                dt = ConvertToUtc(unixtime);
            }
            return dt.ToString("r");
        }
        /// <summary>
        /// 将GMT格式转换为UTC世界协调时间
        /// </summary>
        /// <param name="gmt"></param>
        /// <returns></returns>
        public static DateTime ConvertGmtToUtc(string gmt)
        {
            var dt = DateTime.MinValue;
            try
            {
                string pattern = "";
                if (gmt.IndexOf("+0") != -1)
                {
                    gmt = gmt.Replace("GMT", "");
                    pattern = "ddd, dd MMM yyyy HH':'mm':'ss zzz";
                }
                if (gmt.ToUpper().IndexOf("GMT") != -1)
                {
                    pattern = "ddd, dd MMM yyyy HH':'mm':'ss 'GMT'";
                }
                if (string.IsNullOrEmpty(pattern))
                {
                    dt = System.Convert.ToDateTime(gmt);
                }
                else
                {
                    dt = DateTime.ParseExact(gmt, pattern, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AdjustToUniversal);
                }
            }
            catch { }
            return dt;
        }
        /// <summary>
        /// 将当前时间按指定格式输出
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string Format(string format = "yyyy-MM-dd HH:mm:ss")
        {
            return DateTime.UtcNow.AddHours(TimeZone).ToString(format);// 以UTC时间进行时区计算
        }
        /// <summary>
        /// 将时间按指定格式输出
        /// </summary>
        /// <param name="time"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string Format(DateTime time, string format = "yyyy-MM-dd HH:mm:ss")
        {
            if (time.Kind == DateTimeKind.Utc)
            {
                return time.AddHours(TimeZone).ToString(format);// 直接格式输出本地时间
            }
            else if (time.Kind == DateTimeKind.Local)
            {
                var temp = time.ToUniversalTime().AddHours(TimeZone);
                return temp.ToString(format);// 按本地时间格式输出
            }
            else
            {
                return time.ToString(format);// 直接格式输出本地时间
            }
        }
        /// <summary>
        /// 将当前UTC时间按指定格式输出
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatUtc(string format = "yyyy-MM-dd HH:mm:ss")
        {
            return DateTime.UtcNow.ToString(format);
        }
        /// <summary>
        /// 格式输出本地时间
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatTimeZone(string format = "yyyy-MM-dd HH:mm:ss")
        {
            if (TimeZone == 0)
            {
                return DateTime.Now.ToString(format); //直接格式输出本地时间
            }
            else
            {
                return DateTime.UtcNow.AddHours(TimeZone).ToString(format); //以UTC时间进行时区计算
            }
        }
        /// <summary>
        /// 格式输出指定时区的时间
        /// </summary>
        /// <param name="timezone"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatTimeZone(int timezone, string format = "yyyy-MM-dd HH:mm:ss")
        {
            return DateTime.UtcNow.AddHours(timezone).ToString(format); //以UTC时间进行时区计算
        }
        /// <summary>
        /// 将Unix时间戳按本地时区及指定格式输出
        /// </summary>
        /// <param name="unixtime"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatUnix(long unixtime, string format = "yyyy-MM-dd HH:mm:ss")
        {
            return Convert(unixtime).ToString(format);
        }
        /// <summary>
        /// 将Unix时间戳按UTC协调时及指定格式输出
        /// </summary>
        /// <param name="unixtime"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatUnixToUtc(long unixtime, string format = "yyyy-MM-dd HH:mm:ss")
        {
            var utc = ConvertToUtc(unixtime);
            return utc.ToString(format);
        }
        /// <summary>
        /// 将Unix时间戳按本地时区及格式输出
        /// </summary>
        /// <param name="unixtime"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatUnixToTimeZone(long unixtime, string format = "yyyy-MM-dd HH:mm:ss")
        {
            var utc = ConvertToUtc(unixtime);
            if (TimeZone == 0)
            {
                return utc.ToLocalTime().ToString(format); //直接以本地时间格式输出
            }
            else
            {
                return utc.AddHours(TimeZone).ToString(format); //以UTC时间进行时区计算
            }
        }
        /// <summary>
        /// 将Unix时间戳按指定时区及格式输出
        /// </summary>
        /// <param name="unixtime"></param>
        /// <param name="timezone"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatUnixToTimeZone(long unixtime, int timezone, string format = "yyyy-MM-dd HH:mm:ss")
        {
            var utc = ConvertToUtc(unixtime);
            if (timezone == 0)
            {
                return utc.ToLocalTime().ToString(format); //直接以本地时间格式输出
            }
            else
            {
                return utc.AddHours(timezone).ToString(format); //以UTC时间进行时区计算
            }
        }
        /// <summary>
        /// 将当前时间按指定格式输出
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatDate(string format = "yyyy-MM-dd")
        {
            return DateTime.UtcNow.AddHours(TimeZone).ToString(format);// 以UTC时间进行时区计算
        }
        /// <summary>
        /// 获取当日起始时间
        /// </summary>
        /// <param name="unixtime"></param>
        /// <returns></returns>
        public static long GetDayStart(long unixtime)
        {
            var temp = unixtime + TimeZone * 3600;
            temp = unixtime - unixtime % 86400;
            return temp;
        }
        /// <summary>
        /// 获取当日起始时间
        /// </summary>
        /// <param name="unixtime"></param>
        /// <returns></returns>
        public static long GetUnixDayStart(long unixtime)
        {
            return unixtime - unixtime % 86400;
        }
    }
}