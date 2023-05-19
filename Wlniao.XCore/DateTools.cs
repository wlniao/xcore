/*==============================================================================
    �ļ����ƣ�DateTools.cs
    ���û�����CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    �������������õ�ʱ�����������
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
    /// ���õ�ʱ�����������
    /// </summary>
    public partial class DateTools
    {
        private static bool init = false;
        internal static int _TimeZone = 0;
        /// <summary>
        /// ��ǰ���е�ʱ��
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
                    _TimeZone = cvt.ToInt(Config.GetConfigs("WLN_TIMEZONE", "8"));
                    init = true;
                }
                return _TimeZone;
            }
        }
        /// <summary>
        /// ��ȡUnixʱ���
        /// </summary>
        /// <returns></returns>
        public static long GetUnix()
        {
            return DateTime.UtcNow.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000000;
        }
        /// <summary>
        /// ��ȡUnixʱ���
        /// </summary>
        /// <param name="strtime"></param>
        /// <returns></returns>
        public static long GetUnix(string strtime)
        {
            if (string.IsNullOrEmpty(strtime))
            {
                return 0;
            }
            var time = DateTime.Parse(strtime);
            if (time.Kind == DateTimeKind.Unspecified)
            {
                return time.AddHours(0 - DateTools.TimeZone).Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000000;
            }
            else
            {
                return time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000000;
            }
        }
        /// <summary>
        /// ��ȡUnixʱ���
        /// </summary>
        /// <param name="time"></param>
        /// <returns></returns>
        public static long GetUnix(DateTime time)
        {
            if (time.Kind == DateTimeKind.Unspecified)
            {
                return time.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified)).Ticks / 10000000 - TimeZone * 3600;
            }
            else
            {
                return time.ToUniversalTime().Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000000;
            }
        }
        /// <summary>
        /// ��ȡ��ʱ���ĵ�ǰʱ��
        /// </summary>
        /// <returns></returns>
        public static DateTime GetNow()
        {
            var temp = DateTime.UtcNow.AddHours(TimeZone);
            return new DateTime(temp.Year, temp.Month, temp.Day, temp.Hour, temp.Minute, temp.Second, temp.Millisecond, DateTimeKind.Unspecified);
        }
        /// <summary>
        /// ��Unixʱ���ת��Ϊ��ʱ����ʱ�䣨��ͨ��WLN_TIMEZONE���ã�
        /// </summary>
        /// <param name="unixtime"></param>
        /// <returns></returns>
        public static DateTime Convert(long unixtime)
        {
            return new DateTime(1970, 1, 1, TimeZone, 0, 0, 0, DateTimeKind.Unspecified).Add(new TimeSpan(unixtime * 10000000));
        }
        /// <summary>
        /// ��ʱ���ַ���ת��Ϊ��ʱ����ʱ��
        /// </summary>
        /// <param name="strtime"></param>
        /// <returns></returns>
        public static DateTime Convert(string strtime)
        {
            var time = DateTime.Parse(strtime);
            if (time.Kind == DateTimeKind.Unspecified)
            {
                return time;
            }
            else
            {
                return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Millisecond, DateTimeKind.Unspecified).AddHours(TimeZone);
            }
        }

        /// <summary>
        /// ��Unixʱ���ת��ΪUTC����Э��ʱ��
        /// </summary>
        /// <param name="unixtime"></param>
        /// <returns></returns>
        public static DateTime ConvertToUtc(long unixtime)
        {
            return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Add(new TimeSpan(unixtime * 10000000));
        }
        /// <summary>
        /// ��ʱ���ַ���ת��ΪUTC����Э��ʱ��
        /// </summary>
        /// <param name="strtime"></param>
        /// <returns></returns>
        public static DateTime ConvertToUtc(string strtime)
        {
            var time = DateTime.Parse(strtime);
            if (time.Kind == DateTimeKind.Unspecified)
            {
                return new DateTime(time.Year, time.Month, time.Day, time.Hour, time.Minute, time.Second, time.Millisecond, DateTimeKind.Utc).AddHours(0 - TimeZone); ;
            }
            else if (time.Kind == DateTimeKind.Local)
            {
                return time.ToUniversalTime();
            }
            else
            {
                return time;
            }
        }

        /// <summary>
        /// ��Unixʱ���ת����GMT��ʽ
        /// </summary>
        /// <param name="unixtime"></param>
        /// <returns></returns>
        public static string ConvertToGMT(long unixtime = 0)
        {
            if (unixtime > 0)
            {
                return ConvertToUtc(unixtime).ToString("r");
            }
            else
            {
                return DateTime.UtcNow.ToString("r");
            }
        }
        /// <summary>
        /// ��GMT��ʽת��ΪUTC����Э��ʱ��
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
        /// ��ʱ�䰴ָ����ʽ�������ͨ��WLN_TIMEZONE���ã�
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string Format(string format = "yyyy-MM-dd HH:mm:ss")
        {
            return DateTime.UtcNow.AddHours(TimeZone).ToString(format);// ��UTCʱ�����ʱ������
        }
        /// <summary>
        /// ��ʱ�䰴ָ����ʽ�������ͨ��WLN_TIMEZONE���ã�
        /// </summary>
        /// <param name="time"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string Format(DateTime time, string format = "yyyy-MM-dd HH:mm:ss")
        {
            if (time.Kind == DateTimeKind.Utc)
            {
                return time.AddHours(TimeZone).ToString(format);// ֱ�Ӹ�ʽ�������ʱ��
            }
            else if (time.Kind == DateTimeKind.Local)
            {
                return time.ToUniversalTime().AddHours(TimeZone).ToString(format);// ������ʱ���ʽ���
            }
            else
            {
                return time.ToString(format);// ֱ�Ӹ�ʽ�������ʱ��
            }
        }

        /// <summary>
        /// ����ǰUTCʱ�䰴ָ����ʽ���
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatUtc(string format = "yyyy-MM-dd HH:mm:ss")
        {
            return DateTime.UtcNow.ToString(format);
        }
        /// <summary>
        /// ��ʽ�������ʱ��
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatLocal(string format = "yyyy-MM-dd HH:mm:ss")
        {
            return DateTime.Now.ToString(format); //��UTCʱ�����ʱ������
        }
        /// <summary>
        /// ��ʽ���ָ��ʱ����ʱ��
        /// </summary>
        /// <param name="timezone"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatTimeZone(int timezone, string format = "yyyy-MM-dd HH:mm:ss")
        {
            return DateTime.UtcNow.AddHours(timezone).ToString(format); //��UTCʱ�����ʱ������
        }
        /// <summary>
        /// ��Unixʱ�����ָ����ʽ�������ͨ��WLN_TIMEZONE���ã�
        /// </summary>
        /// <param name="unixtime"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatUnix(long unixtime, string format = "yyyy-MM-dd HH:mm:ss")
        {
            return unixtime <= 0 ? "" : new DateTime(1970, 1, 1, TimeZone, 0, 0, 0, DateTimeKind.Utc).Add(new TimeSpan(unixtime * 10000000)).ToString(format);
        }
        /// <summary>
        /// ��Unixʱ�����UTCЭ��ʱ��ָ����ʽ���
        /// </summary>
        /// <param name="unixtime"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatUnixToUtc(long unixtime, string format = "yyyy-MM-dd HH:mm:ss")
        {
            return unixtime <= 0 ? "" : new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Add(new TimeSpan(unixtime * 10000000)).ToString(format);
        }
        /// <summary>
        /// ��Unixʱ���������ʱ�估ָ����ʽ���
        /// </summary>
        /// <param name="unixtime"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatUnixToLocal(long unixtime, string format = "yyyy-MM-dd HH:mm:ss")
        {
            return unixtime <= 0 ? "" : new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc).Add(new TimeSpan(unixtime * 10000000)).ToLocalTime().ToString(format);
        }
        /// <summary>
        /// ��Unixʱ�����ָ��ʱ������ʽ���
        /// </summary>
        /// <param name="timezone"></param>
        /// <param name="unixtime"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatUnixToTimeZone(long unixtime, int timezone, string format = "yyyy-MM-dd HH:mm:ss")
        {
            if (unixtime <= 0)
            {
                return "";
            }
            else
            {
                return new DateTime(1970, 1, 1, timezone, 0, 0, 0, DateTimeKind.Utc).Add(new TimeSpan(unixtime * 10000000)).ToString(format); //��UTCʱ�����ʱ������
            }
        }
        /// <summary>
        /// ����ǰ���ڰ�ָ����ʽ�������ͨ��WLN_TIMEZONE���ã�
        /// </summary>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string FormatDate(string format = "yyyy-MM-dd")
        {
            return DateTime.UtcNow.AddHours(TimeZone).ToString(format);// ��UTCʱ�����ʱ������
        }
        /// <summary>
        /// ��ȡ������ʼʱ�䣨��ͨ��WLN_TIMEZONE���ã�
        /// </summary>
        /// <param name="unixtime"></param>
        /// <returns></returns>
        public static long GetDayStart(long unixtime)
        {
            return unixtime - ((unixtime + 3600 * TimeZone) % 86400);
        }
        /// <summary>
        /// ��ȡһ���е�ĳ��
        /// </summary>
        /// <returns></returns>
        public static DayOfWeek GetDayOfWeek()
        {
            return DateTime.UtcNow.AddSeconds(3600 * TimeZone).DayOfWeek;
        }
        /// <summary>
        /// ��ȡһ���е�ĳ��
        /// </summary>
        /// <param name="unixtime"></param>
        /// <returns></returns>
        public static DayOfWeek GetDayOfWeek(long unixtime)
        {
            return new DateTime(1970, 1, 1, TimeZone, 0, 0, 0, DateTimeKind.Utc).Add(new TimeSpan(unixtime * 10000000)).DayOfWeek;
        }
        /// <summary>
        /// ��ȡ������ʼʱ��
        /// </summary>
        /// <param name="unixtime"></param>
        /// <returns></returns>
        public static long GetUtcDayStart(long unixtime)
        {
            return unixtime - unixtime % 86400;
        }
    }
}