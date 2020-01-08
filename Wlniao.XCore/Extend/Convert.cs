/*==============================================================================
    文件名称：Convert.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：常用类型转换方法
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
using System.Collections.Generic;
using System.Text;
namespace Wlniao
{
    /// <summary>
    /// 常用类型转换方法
    /// </summary>
    public class Convert
    {
        /// <summary>
        /// 判断字符串是否是小数或整数
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Boolean IsDecimal(String str)
        {
            if (strUtil.IsNullOrEmpty(str))
                return false;
            if (str.StartsWith("-"))
                return isDecimal_private(str.TrimStart('-'));
            else
                return isDecimal_private(str);
        }
        private static Boolean isDecimal_private(String str)
        {
            foreach (char ch in str.ToCharArray())
            {
                if (!(char.IsDigit(ch) || (ch == '.')))
                    return false;
            }
            return true;
        }
        /// <summary>
        /// 判断字符串是否是多个整数的列表，整数之间必须通过英文逗号分隔
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static Boolean IsIdListValid(String ids)
        {
            if (strUtil.IsNullOrEmpty(ids))
            {
                return false;
            }
            String[] strArray = ids.Split(new char[] { ',' });
            foreach (String str in strArray)
            {
                if (!IsInt(str))
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// 判断字符串是否是整数或负整数
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Boolean IsInt(String str)
        {
            if (strUtil.IsNullOrEmpty(str))
                return false;
            if (str.StartsWith("-"))
                str = str.Substring(1, str.Length - 1);
            if (str.Length > 10)
                return false;
            char[] chArray = str.ToCharArray();
            foreach (char ch in chArray)
            {
                if (!char.IsDigit(ch))
                    return false;
            }
            if (chArray.Length == 10)
            {
                int charInt;
                Int32.TryParse(chArray[0].ToString(), out charInt);
                if (charInt > 2)
                    return false;
                int charInt2;
                Int32.TryParse(chArray[1].ToString(), out charInt2);
                if ((charInt == 2) && (charInt2 > 0))
                    return false;
            }
            return true;
        }
        /// <summary>
        /// 判断字符串是否是"true"或"false"(不区分大小写)
        /// </summary>
        /// <param name="str"></param>
        /// <returns>只有字符串是"true"或"false"(不区分大小写)时，才返回true</returns>
        public static Boolean IsBool(String str)
        {
            if (str == null) return false;
            if (strUtil.EqualsIgnoreCase(str, "true") || strUtil.EqualsIgnoreCase(str, "false")) return true;
            return false;
        }
        /// <summary>
        /// 将对象转换成目标类型
        /// </summary>
        /// <param name="val"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public static Object To(Object val, Type destinationType)
        {
            return System.Convert.ChangeType(val, destinationType);
        }
        /// <summary>
        /// 将整数转换成 Boolean 类型。只有参数等于1时，才返回true
        /// </summary>
        /// <param name="integer"></param>
        /// <returns>只有参数等于1时，才返回true</returns>
        public static Boolean ToBool(int integer)
        {
            return (integer == 1);
        }
        /// <summary>
        /// 将对象转换成 Boolean 类型。只有对象的字符串形式等于1或者true(不区分大小写)时，才返回true
        /// </summary>
        /// <param name="objBool"></param>
        /// <returns>只有对象的字符串形式等于1或者true(不区分大小写)时，才返回true</returns>
        public static Boolean ToBool(Object objBool)
        {
            if (objBool == null)
            {
                return false;
            }
            String str = objBool.ToString();
            return (str.Equals("1") || str.ToUpper().Equals("TRUE"));
        }
        /// <summary>
        /// 将字符串(不区分大小写)转换成 Boolean 类型。只有字符串等于1或者true时，才返回true
        /// </summary>
        /// <param name="str"></param>
        /// <returns>只有字符串等于1或者true时，才返回true</returns>
        public static Boolean ToBool(String str)
        {
            if (str == null)
                return false;
            if (str.ToUpper().Equals("TRUE"))
                return true;
            if (str.ToUpper().Equals("FALSE"))
                return false;
            return (str.Equals("1") || str.ToUpper().Equals("TRUE"));
        }
        /// <summary>
        /// 将字符串转换成 System.Decimal 类型。如果str不是整数或小数，返回0
        /// </summary>
        /// <param name="str"></param>
        /// <returns>如果str不是整数或小数，返回0</returns>
        public static decimal ToDecimal(String str)
        {
            if (!IsDecimal(str))
            {
                return 0;
            }
            return System.Convert.ToDecimal(str);
        }
        /// <summary>
        /// 将字符串转换成 System.Double 类型。如果str不是整数或小数，返回0
        /// </summary>
        /// <param name="str"></param>
        /// <returns>如果str不是整数或小数，返回0</returns>
        public static Double ToDouble(String str)
        {
            if (!IsDecimal(str))
            {
                return 0;
            }
            return System.Convert.ToDouble(str);
        }
        /// <summary>
        /// 将字符串转换成 System.Decimal 类型。如果str不是整数或小数，返回参数 defaultValue 指定的值
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static decimal ToDecimal(String str, decimal defaultValue)
        {
            if (!IsDecimal(str))
            {
                return defaultValue;
            }
            return System.Convert.ToDecimal(str);
        }
        /// <summary>
        /// 将字符串转换成 float 类型。如果str不是整数或小数，返回参数 defaultValue 指定的值
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static float ToFloat(String str, float defaultValue = 0)
        {
            if (!IsDecimal(str))
            {
                return defaultValue;
            }
            return float.Parse(str);
        }
        /// <summary>
        /// 将对象转换成长整数；如果不是长整数，则返回0
        /// </summary>
        /// <param name="objLong"></param>
        /// <returns>如果不是长整数，则返回0</returns>
        public static long ToLong(Object objLong)
        {
            if ((objLong != null) && IsInt(objLong.ToString()))
            {
                long result;
                Int64.TryParse(objLong.ToString(), out result);
                return result;
            }
            return 0;
        }
        /// <summary>
        /// 将对象转换成长整数；如果不是长整数，则返回0
        /// </summary>
        /// <param name="strLong"></param>
        /// <returns>如果不是长整数，则返回0</returns>
        public static long ToLong(String strLong)
        {
            long result = 0;
            Int64.TryParse(strLong, out result);
            return result;
        }
        /// <summary>
        /// 将对象转换成整数；如果不是整数，则返回0
        /// </summary>
        /// <param name="objInt"></param>
        /// <returns>如果不是整数，则返回0</returns>
        public static int ToInt(Object objInt)
        {
            if (objInt != null)
            {
                if (IsInt(objInt.ToString()))
                {
                    int result;
                    Int32.TryParse(objInt.ToString(), out result);
                    return result;
                }
            }
            return 0;
        }
        /// <summary>
        /// 将 float 转换成整数
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static int ToInt(float number)
        {
            try
            {
                return (int)number;
            }
            catch { return 0; }
        }
        /// <summary>
        /// 将 double 转换成整数
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static int ToInt(double number)
        {
            try
            {
                return (int)number;
            }
            catch { return 0; }
        }
        /// <summary>
        /// 将 decimal 转换成整数
        /// </summary>
        /// <param name="number"></param>
        /// <returns></returns>
        public static int ToInt(decimal number)
        {
            try
            {
                return (int)number;
            }
            catch { return 0; }
        }

        #region 进制转换

        /// <summary>
        /// 将10进制整数转换为n进制
        /// </summary>
        /// <param name="inputNum">10进制整数</param>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static String ToHex(Int64 inputNum, String chars)
        {
            int cbase = chars.Length;
            int imod;
            String result = "";
            while (inputNum >= cbase)
            {
                imod = (int)(inputNum % cbase);
                result = chars[imod] + result;
                inputNum = inputNum / cbase;
            }
            return chars[(int)inputNum] + result;
        }
        /// <summary>
        /// 将n进制转换为10进制整数
        /// </summary>
        /// <param name="str">需要转换的n进制数</param>
        /// <param name="chars"></param>
        /// <returns>10进制整数</returns>
        public static Int64 DeHex(String str, String chars)
        {
            Int32 hex = chars.Length;
            Int32 len = str.Length;
            Int64 result = 0;
            for (int i = 0; i < len; i++)
            {
                var index = chars.IndexOf(str[i]);
                if (index < 0)
                {
                    return 0;
                }
                result += chars.IndexOf(str[i]) * (Int64)Math.Pow(hex, (len - i - 1));
            }
            return result;
        }

        /// <summary>
        /// 十进制数转换成二、八、十六进制数
        /// </summary>
        /// <param name="int_value">十进制数</param>
        /// <param name="mod">进制</param>
        /// <returns></returns>
        public static string IntToHex(Int32 int_value, Int32 mod)
        {
            return Int64ToHex(int_value, mod);
        }
        /// <summary>
        /// 十进制数转换成二、八、十六进制数
        /// </summary>
        /// <param name="int_value">十进制数</param>
        /// <param name="mod">进制</param>
        /// <returns></returns>
        public static string Int64ToHex(Int64 int_value, Int32 mod)
        {
            string hex_value = string.Empty;
            Int64 add_value, mod_value, temp;
            char char_mod_value;
            temp = int_value;
            while (temp > 0)
            {
                add_value = temp / mod;
                mod_value = temp % mod;
                if (mod_value >= 10)
                {
                    char_mod_value = (char)(mod_value + 55);
                }
                else
                {
                    char_mod_value = (char)(mod_value + 48);
                }
                hex_value = char_mod_value + hex_value;
                temp = add_value;
            }
            return hex_value;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="h"></param>
        /// <returns></returns>
        public static int HexToInt(char h)
        {
            if ((h >= '0') && (h <= '9'))
            {
                return (h - '0');
            }
            if ((h >= 'a') && (h <= 'f'))
            {
                return ((h - 'a') + 10);
            }
            if ((h >= 'A') && (h <= 'F'))
            {
                return ((h - 'A') + 10);
            }
            return -1;
        }

        #region 二十六进制转换
        /// <summary>
        /// 10进制转换成26进制（26大写字母）
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string IntToHex26(Int64 val)
        {
            return ToHex(val, "abcdefghijklmnokprstuvwxyz");
        }
        /// <summary>
        /// 26进制转换成10进制
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Int32 Hex26ToInt(string str)
        {
            if (str.ToUpper() == str)
            {
                str = str.ToLower();
            }
            return (Int32)DeHex(str, "abcdefghijklmnokprstuvwxyz");
        }
        /// <summary>
        /// 26进制转换成10进制
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Int64 Hex26ToInt64(string str)
        {
            return DeHex(str, "abcdefghijklmnokprstuvwxyz");
        }
        #endregion

        #region 五十二进制转换
        /// <summary>
        /// 10进制转换成52进制（52个英文字母）
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string IntToHex52(Int64 val)
        {
            return ToHex(val, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }
        /// <summary>
        /// 52进制转换成10进制
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Int32 Hex52ToInt(string str)
        {
            return (Int32)DeHex(str, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }
        /// <summary>
        /// 52进制转换成10进制
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static Int64 Hex52ToInt64(string str)
        {
            return DeHex(str, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }
        #endregion

        #endregion
        /// <summary>
        /// 将对象转换成非Null形式，如果传入的参数是 null，则返回空字符串(即""，也即string.Empty)
        /// </summary>
        /// <param name="str"></param>
        /// <returns>如果为null，则返回空字符串(即""，也即string.Empty)</returns>
        public static String ToNotNull(Object str)
        {
            if (str == null)
            {
                return "";
            }
            return str.ToString();
        }
        /// <summary>
        /// 将对象转换成 DateTime 形式，如果不符合格式，则返回当前时间
        /// </summary>
        /// <param name="objTime"></param>
        /// <returns>如果不符合格式，则返回当前时间</returns>
        public static DateTime ToTime(Object objTime)
        {
            return ToTime(objTime, DateTime.Now);
        }
        /// <summary>
        /// 将对象转换成 DateTime 形式，如果不符合格式，则返回第二个参数指定的时间
        /// </summary>
        /// <param name="objTime"></param>
        /// <param name="targetTime"></param>
        /// <returns></returns>
        public static DateTime ToTime(Object objTime, DateTime targetTime)
        {
            if (objTime == null)
            {
                return targetTime;
            }
            try
            {
                if (objTime.GetType() == typeof(String))
                {
                    return DateTools.Convert(objTime.ToString());
                }
                else
                {
                    return System.Convert.ToDateTime(objTime);
                }
            }
            catch
            {
                return targetTime;
            }
        }
        /// <summary>
        /// 判断两个时间的日期是否相同(要求同年同月同日)
        /// </summary>
        /// <param name="day1"></param>
        /// <param name="day2"></param>
        /// <returns></returns>
        public static Boolean IsDayEqual(DateTime day1, DateTime day2)
        {
            return (day1.Year == day2.Year && day1.Month == day2.Month && day1.Day == day2.Day);
        }
        /// <summary>
        /// 获取日期的日常表达形式，要求最近三天依次用 {今天，昨天，前天} 表示
        /// </summary>
        /// <param name="day"></param>
        /// <returns>要求最近三天依次用 {今天、昨天、前天} 表示</returns>
        public static String ToDayString(DateTime day)
        {
            var today = DateTools.GetNow();
            if (day.Kind == DateTimeKind.Utc)
            {
                day = new DateTime(day.Year, day.Month, day.Day, day.Hour, day.Minute, day.Second, day.Millisecond, DateTimeKind.Unspecified).AddHours(DateTools.TimeZone);
            }
            else if (day.Kind == DateTimeKind.Local)
            {
                day = day.ToUniversalTime();
                day = new DateTime(day.Year, day.Month, day.Day, day.Hour, day.Minute, day.Second, day.Millisecond, DateTimeKind.Unspecified).AddHours(DateTools.TimeZone);
            }
            if (IsDayEqual(day, today))
            {
                return Runtime.Lang.Get("today");
            }
            if (IsDayEqual(day, today.AddDays(-1)))
            {
                return Runtime.Lang.Get("yesterday");
            }
            if (IsDayEqual(day, today.AddDays(-2)))
            {
                return Runtime.Lang.Get("thedaybeforeyesterday");
            }
            return day.ToString("yyyy-MM-dd");
        }
        /// <summary>
        /// 获取时间的日常表达形式，格式为 {**小时前，**分钟前，**秒前}，以及 {昨天，前天}
        /// </summary>
        /// <param name="t"></param>
        /// <returns>格式为 {**小时前，**分钟前，**秒前}，以及 {昨天，前天}</returns>
        public static String ToTimeString(DateTime t)
        {
            var now = DateTools.GetNow();
            if (t.Kind == DateTimeKind.Utc)
            {
                t = new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second, t.Millisecond, DateTimeKind.Unspecified).AddHours(DateTools.TimeZone);
            }
            else if (t.Kind == DateTimeKind.Local)
            {
                t = t.ToUniversalTime();
                t = new DateTime(t.Year, t.Month, t.Day, t.Hour, t.Minute, t.Second, t.Millisecond, DateTimeKind.Unspecified).AddHours(DateTools.TimeZone);
            }
            var span = now.Subtract(t);
            if (cvt.IsDayEqual(t, now))
            {
                if (span.Hours > 0)
                {
                    return span.Hours + Runtime.Lang.Get("houresAgo");
                }
                else
                {
                    if (span.Minutes == 0)
                    {
                        if (span.Seconds <= 2)
                        {
                            return Runtime.Lang.Get("justNow");
                        }
                        else
                        {
                            return span.Seconds + Runtime.Lang.Get("secondAgo");
                        }
                    }
                    else
                    {
                        return span.Minutes + Runtime.Lang.Get("minuteAgo");
                    }
                }
            }
            if (cvt.IsDayEqual(t, now.AddDays(-1)))
            {
                return Runtime.Lang.Get("yesterday");
            }
            else if (cvt.IsDayEqual(t, now.AddDays(-2)))
            {
                return Runtime.Lang.Get("thedaybeforeyesterday");
            }
            else
            {
                return t.ToString("yyyy-MM-dd");
            }
        }
        /// <summary>
        /// 获取时间的英文表达形式，格式如 {Monday, November 12, 2012}
        /// </summary>
        /// <param name="t"></param>
        /// <returns>格式如 {Monday, November 12, 2012}</returns>
        public static String ToDateEnString(DateTime t)
        {
            return t.ToString("D", new System.Globalization.CultureInfo("en").DateTimeFormat);
        }
        /// <summary>
        /// 获取时间的英文表达形式，格式如 {Apr 07,2012}
        /// </summary>
        /// <param name="t"></param>
        /// <returns>格式如 {Apr 07,2012}</returns>
        public static String ToDateEnShortString(DateTime t)
        {
            string d = t.ToString("r", new System.Globalization.CultureInfo("en").DateTimeFormat);
            d = d.Remove(d.IndexOf(':') - 2);
            return d;
        }
        /// <summary>
        /// 获取时间的英文表达形式，格式如 {Mon, 12 Nov 2012 00:00:00 GMT}
        /// </summary>
        /// <param name="t"></param>
        /// <returns>格式如 {Mon, 12 Nov 2012 00:00:00 GMT}</returns>
        public static String ToDateEnLongString(DateTime t)
        {
            return t.ToString("r", new System.Globalization.CultureInfo("en").DateTimeFormat);
        }
        /// <summary>
        /// 将整数转换成字符串形式，多个整数之间用英文逗号分隔
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static String ToString(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return "";
            }
            var builder = new StringBuilder();
            for (int i = 0; i < ids.Length; i++)
            {
                builder.Append(ids[i]);
                if (i < ids.Length - 1) builder.Append(',');
            }
            return builder.ToString();
        }
        /// <summary>
        /// 将文本转换为数据字典
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static Dictionary<String, String> ToDictionary(string doc)
        {
            var result = new Dictionary<String, String>();
            if (!string.IsNullOrEmpty(doc))
            {
                String[] arrLine = doc.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (String oneLine in arrLine)
                {
                    //无值的行跳过
                    var tempLine = oneLine.TrimStart().TrimStart('-').TrimStart();
                    //注释行跳过
                    if (tempLine.StartsWith("//") || tempLine.StartsWith("#"))
                    {
                        continue;
                    }
                    String[] arrPair = tempLine.Split(new char[] { '=' }, 2);
                    if (arrPair.Length == 2)
                    {
                        char[] arrTrim = new char[] { '"', '\'' };
                        String itemKey = arrPair[0].Trim().TrimStart(arrTrim).TrimEnd(arrTrim).Trim().ToLower();
                        String itemValue = arrPair[1].Trim().TrimStart(arrTrim).TrimEnd(arrTrim).Trim();
                        if (result.ContainsKey(itemKey))
                        {
                            result[itemKey] = itemValue;
                        }
                        else
                        {
                            result.Add(itemKey, itemValue);
                        }
                    }
                    else
                    {
                        if (tempLine.ToLower() == "yaml")
                        {
                            result.Add("yaml", "true");
                        }
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// 将字符串形式的 id 列表转换成整型数组
        /// </summary>
        /// <param name="myids"></param>
        /// <returns></returns>
        public static int[] ToIntArray(String myids)
        {
            if (strUtil.IsNullOrEmpty(myids)) return new int[] { };
            String[] arrIds = myids.Split(',');
            int[] Ids = new int[arrIds.Length];
            for (int i = 0; i < arrIds.Length; i++)
            {
                int oneID = ToInt(arrIds[i].Trim());
                Ids[i] = oneID;
            }
            return Ids;
        }
        /// <summary>
        /// 将字符串转换成以井号开头的表达形式；如果不是有效的颜色值，则返回null
        /// </summary>
        /// <param name="val"></param>
        /// <returns>将字符串转换成以井号开头的表达形式；如果不是有效的颜色值，则返回null</returns>
        public static String ToColorValue(String val)
        {
            if (strUtil.IsColorValue(val) == false)
            {
                return null;
            }
            if (val.StartsWith("#"))
            {
                return val;
            }
            return "#" + val;
        }

        /// <summary>
        /// 将 Stream 转成 byte[]
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static byte[] ToBytes(System.IO.Stream stream)
        {
            byte[] bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            // 设置当前流的位置为流的开始 
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            return bytes;
        }

        /// <summary>
        /// 将 byte[] 转成 Stream
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static System.IO.Stream ToStream(byte[] bytes)
        {
            var stream = new System.IO.MemoryStream(bytes);
            return stream;
        }


        /// <summary>
        /// 将此实例中的指定 <see cref="sbyte"/> 字符数组转换到 <see cref="byte"/> 字符数组。
        /// </summary>
        /// <param name="sbyteArray">要转换的 <see cref="sbyte"/> 字符数组</param>
        /// <returns>返回转换后的 <see cref="byte"/> 字符数组</returns>
        public static byte[] ToByteArray(sbyte[] sbyteArray)
        {
            byte[] byteArray = new byte[sbyteArray.Length];
            for (int index = 0; index < sbyteArray.Length; index++)
                byteArray[index] = (byte)sbyteArray[index];
            return byteArray;
        }


        /// <summary>
        /// 将此实例中的指定字符串转换到 <see cref="byte"/> 字符数组。
        /// </summary>
        /// <param name="sourceString">要转换的字符串</param>
        /// <returns>返回转换后的 <see cref="byte"/> 字符数组</returns>
        public static byte[] ToByteArray(string sourceString)
        {
            byte[] byteArray = new byte[sourceString.Length];
            for (int index = 0; index < sourceString.Length; index++)
                byteArray[index] = (byte)sourceString[index];
            return byteArray;
        }


        /// <summary>
        /// 将此实例中的指定 <see cref="object"/> 数组转换到 <see cref="byte"/> 字符数组。
        /// </summary>
        /// <param name="tempObjectArray">要转换的 <see cref="object"/> 字符数组</param>
        /// <returns>返回转换后的 <see cref="byte"/> 字符数组</returns>
        public static byte[] ToByteArray(object[] tempObjectArray)
        {
            byte[] byteArray = new byte[tempObjectArray.Length];
            for (int index = 0; index < tempObjectArray.Length; index++)
                byteArray[index] = (byte)tempObjectArray[index];
            return byteArray;
        }

        /// <summary>
        /// 将此实例中的指定 <see cref="byte"/> 字符数组转换到 <see cref="sbyte"/> 字符数组。
        /// </summary>
        /// <param name="byteArray">要转换的 <see cref="byte"/> 字符数组</param>
        /// <returns>返回转换后的 <see cref="sbyte"/> 字符数组</returns>
        public static sbyte[] ToSByteArray(byte[] byteArray)
        {
            sbyte[] sbyteArray = new sbyte[byteArray.Length];
            for (int index = 0; index < byteArray.Length; index++)
                sbyteArray[index] = (sbyte)byteArray[index];
            return sbyteArray;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="money"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static String ToMoney(float money, string format = "")
        {
            if (string.IsNullOrEmpty(format) || !format.StartsWith("F"))
            {
                string _str = money.ToString("F2");
                if (_str.EndsWith("0"))
                {
                    _str = money.ToString("F1");
                    if (_str.EndsWith(".0"))
                    {
                        _str = money.ToString("F0");
                    }
                }
                return _str;
            }
            else
            {
                return money.ToString(format);
            }
        }
        /// <summary>
        /// 将远程Svg图片转换成Svg格式的字符串（可转换Url参数）
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static String ToSvgStr(String url)
        {
            var svg = XServer.Common.GetResponseString(url);
            if (!string.IsNullOrEmpty(svg))
            {
                svg = svg.Substring(svg.IndexOf("<svg"));
                svg = System.Text.RegularExpressions.Regex.Replace(svg, "<!--.*?-->", string.Empty);
                if (url.IndexOf('?') > 0)
                {
                    url = url.Substring(url.IndexOf('?') + 1);
                    var args = strUtil.GetQueryString(url);
                    if (args != null && args.Count > 0)
                    {
                        var attr = "";
                        foreach (var arg in args)
                        {
                            attr += " " + arg.Key + "=\"" + arg.Value + "\"";
                        }
                        return svg.Substring(0, svg.IndexOf("<g") + 2) + attr + svg.Substring(svg.IndexOf("<g") + 2);
                    }
                }
            }
            return svg;
        }

        
        /// <summary>
        /// 根据不同进制的写法返回对应数值
        /// </summary>
        /// <param name="literal">各进制写法</param>
        /// <returns>对应数值</returns>
        public static long Identity(long literal)
        {
            return literal;
        }

        /// <summary>
        /// 根据不同进制的写法返回对应数值
        /// </summary>
        /// <param name="literal">各进制写法</param>
        /// <returns>对应数值</returns>
        public static ulong Identity(ulong literal)
        {
            return literal;
        }

        /// <summary>
        /// 根据不同进制的写法返回对应数值
        /// </summary>
        /// <param name="literal">各进制写法</param>
        /// <returns>对应数值</returns>
        public static float Identity(float literal)
        {
            return literal;
        }

        /// <summary>
        /// 根据不同进制的写法返回对应数值
        /// </summary>
        /// <param name="literal">各进制写法</param>
        /// <returns>对应数值</returns>
        public static double Identity(double literal)
        {
            return literal;
        }
    }
}