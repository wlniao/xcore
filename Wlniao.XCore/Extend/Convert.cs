/*==============================================================================
    �ļ����ƣ�Convert.cs
    ���û�����CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    ������������������ת������
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
using System.Linq;
using System.Collections.Generic;
using System.Text;
using Wlniao.Text;

namespace Wlniao
{
    /// <summary>
    /// ��������ת������
    /// </summary>
    public class Convert
    {
        /// <summary>
        /// �ж��ַ����Ƿ���С��������
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsDecimal(string str)
        {
            if (StringUtil.IsNullOrEmpty(str))
                return false;
            if (str.StartsWith("-"))
                return isDecimal_private(str.TrimStart('-'));
            else
                return isDecimal_private(str);
        }
        private static bool isDecimal_private(string str)
        {
            foreach (var ch in str.ToCharArray())
            {
                if (!(char.IsDigit(ch) || (ch == '.')))
                    return false;
            }
            return true;
        }
        /// <summary>
        /// �ж��ַ����Ƿ��Ƕ���������б�������֮�����ͨ��Ӣ�Ķ��ŷָ�
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static bool IsIdListValid(string ids)
        {
            if (StringUtil.IsNullOrEmpty(ids))
            {
                return false;
            }
            var strArray = ids.Split(new char[] { ',' });
            foreach (var str in strArray)
            {
                if (!IsInt(str))
                {
                    return false;
                }
            }
            return true;
        }
        /// <summary>
        /// �ж��ַ����Ƿ�������������
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static bool IsInt(string str)
        {
            if (StringUtil.IsNullOrEmpty(str))
                return false;
            if (str.StartsWith("-"))
                str = str.Substring(1, str.Length - 1);
            if (str.Length > 10)
                return false;
            var chArray = str.ToCharArray();
            foreach (var ch in chArray)
            {
                if (!char.IsDigit(ch))
                    return false;
            }
            if (chArray.Length == 10)
            {
                int charInt;
                int.TryParse(chArray[0].ToString(), out charInt);
                if (charInt > 2)
                    return false;
                int charInt2;
                int.TryParse(chArray[1].ToString(), out charInt2);
                if ((charInt == 2) && (charInt2 > 0))
                    return false;
            }
            return true;
        }
        /// <summary>
        /// �ж��ַ����Ƿ���"true"��"false"(�����ִ�Сд)
        /// </summary>
        /// <param name="str"></param>
        /// <returns>ֻ���ַ�����"true"��"false"(�����ִ�Сд)ʱ���ŷ���true</returns>
        public static bool IsBool(string str)
        {
            if (str == null) return false;
            if (StringUtil.EqualsIgnoreCase(str, "true") || StringUtil.EqualsIgnoreCase(str, "false")) return true;
            return false;
        }
        /// <summary>
        /// ������ת����Ŀ������
        /// </summary>
        /// <param name="val"></param>
        /// <param name="destinationType"></param>
        /// <returns></returns>
        public static object To(object val, Type destinationType)
        {
            return System.Convert.ChangeType(val, destinationType);
        }
        /// <summary>
        /// ������ת���� Boolean ���͡�ֻ�в�������1ʱ���ŷ���true
        /// </summary>
        /// <param name="integer"></param>
        /// <returns>ֻ�в�������1ʱ���ŷ���true</returns>
        public static bool ToBool(int integer)
        {
            return (integer == 1);
        }
        /// <summary>
        /// ������ת���� Boolean ���͡�ֻ�ж�����ַ�����ʽ����1����true(�����ִ�Сд)ʱ���ŷ���true
        /// </summary>
        /// <param name="objBool"></param>
        /// <returns>ֻ�ж�����ַ�����ʽ����1����true(�����ִ�Сд)ʱ���ŷ���true</returns>
        public static bool ToBool(object objBool)
        {
            if (objBool == null)
            {
                return false;
            }
            var str = objBool.ToString();
            return (str.Equals("1") || str.ToUpper().Equals("TRUE"));
        }
        /// <summary>
        /// ���ַ���(�����ִ�Сд)ת���� Boolean ���͡�ֻ���ַ�������1����trueʱ���ŷ���true
        /// </summary>
        /// <param name="str"></param>
        /// <returns>ֻ���ַ�������1����trueʱ���ŷ���true</returns>
        public static bool ToBool(string str)
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
        /// ���ַ���ת���� System.Decimal ���͡����str����������С��������0
        /// </summary>
        /// <param name="str"></param>
        /// <returns>���str����������С��������0</returns>
        public static decimal ToDecimal(string str)
        {
            if (!IsDecimal(str))
            {
                return 0;
            }
            return System.Convert.ToDecimal(str);
        }
        /// <summary>
        /// ���ַ���ת���� System.Double ���͡����str����������С��������0
        /// </summary>
        /// <param name="str"></param>
        /// <returns>���str����������С��������0</returns>
        public static double ToDouble(string str)
        {
            if (!IsDecimal(str))
            {
                return 0;
            }
            return System.Convert.ToDouble(str);
        }
        /// <summary>
        /// ���ַ���ת���� System.Decimal ���͡����str����������С�������ز��� defaultValue ָ����ֵ
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static decimal ToDecimal(string str, decimal defaultValue)
        {
            if (!IsDecimal(str))
            {
                return defaultValue;
            }
            return System.Convert.ToDecimal(str);
        }
        /// <summary>
        /// ���ַ���ת���� float ���͡����str����������С�������ز��� defaultValue ָ����ֵ
        /// </summary>
        /// <param name="str"></param>
        /// <param name="defaultValue"></param>
        /// <returns></returns>
        public static float ToFloat(string str, float defaultValue = 0)
        {
            if (!IsDecimal(str))
            {
                return defaultValue;
            }
            return float.Parse(str);
        }
        /// <summary>
        /// ������ת���ɳ�������������ǳ��������򷵻�0
        /// </summary>
        /// <param name="objLong"></param>
        /// <returns>������ǳ��������򷵻�0</returns>
        public static long ToLong(object objLong)
        {
            if ((objLong != null) && IsInt(objLong.ToString()))
            {
                long result;
                long.TryParse(objLong.ToString(), out result);
                return result;
            }
            return 0;
        }
        /// <summary>
        /// ������ת���ɳ�������������ǳ��������򷵻�0
        /// </summary>
        /// <param name="strLong"></param>
        /// <returns>������ǳ��������򷵻�0</returns>
        public static long ToLong(string strLong)
        {
            long result = 0;
            long.TryParse(strLong, out result);
            return result;
        }
        /// <summary>
        /// ������ת������������������������򷵻�0
        /// </summary>
        /// <param name="objInt"></param>
        /// <returns>��������������򷵻�0</returns>
        public static int ToInt(object objInt)
        {
            if (objInt != null)
            {
                if (IsInt(objInt.ToString()))
                {
                    int result;
                    int.TryParse(objInt.ToString(), out result);
                    return result;
                }
            }
            return 0;
        }
        /// <summary>
        /// �� float ת��������
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
        /// �� double ת��������
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
        /// �� decimal ת��������
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

        #region ����ת��

        /// <summary>
        /// ��10��������ת��Ϊn����
        /// </summary>
        /// <param name="inputNum">10��������</param>
        /// <param name="chars"></param>
        /// <returns></returns>
        public static string ToHex(long inputNum, string chars)
        {
            var cbase = chars.Length;
            int imod;
            var result = "";
            while (inputNum >= cbase)
            {
                imod = (int)(inputNum % cbase);
                result = chars[imod] + result;
                inputNum = inputNum / cbase;
            }
            return chars[(int)inputNum] + result;
        }
        /// <summary>
        /// ��n����ת��Ϊ10��������
        /// </summary>
        /// <param name="str">��Ҫת����n������</param>
        /// <param name="chars"></param>
        /// <returns>10��������</returns>
        public static long DeHex(string str, string chars)
        {
            var hex = chars.Length;
            var len = str.Length;
            long result = 0;
            for (var i = 0; i < len; i++)
            {
                var index = chars.IndexOf(str[i]);
                if (index < 0)
                {
                    return 0;
                }
                result += chars.IndexOf(str[i]) * (long)Math.Pow(hex, (len - i - 1));
            }
            return result;
        }

        /// <summary>
        /// ʮ������ת���ɶ����ˡ�ʮ��������
        /// </summary>
        /// <param name="int_value">ʮ������</param>
        /// <param name="mod">����</param>
        /// <returns></returns>
        public static string IntToHex(int int_value, int mod)
        {
            return Int64ToHex(int_value, mod);
        }
        /// <summary>
        /// ʮ������ת���ɶ����ˡ�ʮ��������
        /// </summary>
        /// <param name="int_value">ʮ������</param>
        /// <param name="mod">����</param>
        /// <returns></returns>
        public static string Int64ToHex(long int_value, int mod)
        {
            var hex_value = string.Empty;
            long add_value, mod_value, temp;
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

        #region ��ʮ������ת��
        /// <summary>
        /// 10����ת����26���ƣ�26��д��ĸ��
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string IntToHex26(long val)
        {
            return ToHex(val, "abcdefghijklmnokprstuvwxyz");
        }
        /// <summary>
        /// 26����ת����10����
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int Hex26ToInt(string str)
        {
            if (str.ToUpper() == str)
            {
                str = str.ToLower();
            }
            return (int)DeHex(str, "abcdefghijklmnokprstuvwxyz");
        }
        /// <summary>
        /// 26����ת����10����
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static long Hex26ToInt64(string str)
        {
            return DeHex(str, "abcdefghijklmnokprstuvwxyz");
        }
        #endregion

        #region ��ʮ������ת��
        /// <summary>
        /// 10����ת����52���ƣ�52��Ӣ����ĸ��
        /// </summary>
        /// <param name="val"></param>
        /// <returns></returns>
        public static string IntToHex52(long val)
        {
            return ToHex(val, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }
        /// <summary>
        /// 52����ת����10����
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static int Hex52ToInt(string str)
        {
            return (int)DeHex(str, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }
        /// <summary>
        /// 52����ת����10����
        /// </summary>
        /// <param name="str"></param>
        /// <returns></returns>
        public static long Hex52ToInt64(string str)
        {
            return DeHex(str, "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ");
        }
        #endregion

        #endregion

        /// <summary>
        /// IPv4��ַת��ΪIPv6��ʽ
        /// </summary>
        /// <param name="ip"></param>
        /// <returns></returns>
        public static string IPv4ToIPv6(string ip)
        {
            if (StringUtil.IsIPv4(ip))
            {
                var ips = ip.SplitBy(".");
                var low = int.Parse(ips[2]) * 256 + int.Parse(ips[3]);
                var high = int.Parse(ips[0]) * 256 + int.Parse(ips[1]);
                ip = "::ffff:" + high.ToString("x") + ":" + low.ToString("x");
            }
            return ip;
        }

        /// <summary>
        /// ������ת���ɷ�Null��ʽ���������Ĳ����� null���򷵻ؿ��ַ���(��""��Ҳ��string.Empty)
        /// </summary>
        /// <param name="str"></param>
        /// <returns>���Ϊnull���򷵻ؿ��ַ���(��""��Ҳ��string.Empty)</returns>
        public static string ToNotNull(object str)
        {
            if (str == null)
            {
                return "";
            }
            return str.ToString();
        }
        /// <summary>
        /// ������ת���� DateTime ��ʽ����������ϸ�ʽ���򷵻ص�ǰʱ��
        /// </summary>
        /// <param name="objTime"></param>
        /// <returns>��������ϸ�ʽ���򷵻ص�ǰʱ��</returns>
        public static DateTime ToTime(object objTime)
        {
            return ToTime(objTime, DateTime.Now);
        }
        /// <summary>
        /// ������ת���� DateTime ��ʽ����������ϸ�ʽ���򷵻صڶ�������ָ����ʱ��
        /// </summary>
        /// <param name="objTime"></param>
        /// <param name="targetTime"></param>
        /// <returns></returns>
        public static DateTime ToTime(object objTime, DateTime targetTime)
        {
            if (objTime == null)
            {
                return targetTime;
            }
            try
            {
                if (objTime.GetType() == typeof(string))
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
        /// �ж�����ʱ��������Ƿ���ͬ(Ҫ��ͬ��ͬ��ͬ��)
        /// </summary>
        /// <param name="day1"></param>
        /// <param name="day2"></param>
        /// <returns></returns>
        public static bool IsDayEqual(DateTime day1, DateTime day2)
        {
            return (day1.Year == day2.Year && day1.Month == day2.Month && day1.Day == day2.Day);
        }
        /// <summary>
        /// ��ȡ���ڵ��ճ�������ʽ��Ҫ��������������� {���죬���죬ǰ��} ��ʾ
        /// </summary>
        /// <param name="day"></param>
        /// <returns>Ҫ��������������� {���졢���졢ǰ��} ��ʾ</returns>
        public static string ToDayString(DateTime day)
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
        /// ��ȡʱ����ճ�������ʽ����ʽΪ {**Сʱǰ��**����ǰ��**��ǰ}���Լ� {���죬ǰ��}
        /// </summary>
        /// <param name="t"></param>
        /// <returns>��ʽΪ {**Сʱǰ��**����ǰ��**��ǰ}���Լ� {���죬ǰ��}</returns>
        public static string ToTimeString(DateTime t)
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
        /// ��ȡʱ���Ӣ�ı�����ʽ����ʽ�� {Monday, November 12, 2012}
        /// </summary>
        /// <param name="t"></param>
        /// <returns>��ʽ�� {Monday, November 12, 2012}</returns>
        public static string ToDateEnString(DateTime t)
        {
            return t.ToString("D", new System.Globalization.CultureInfo("en").DateTimeFormat);
        }
        /// <summary>
        /// ��ȡʱ���Ӣ�ı�����ʽ����ʽ�� {Apr 07,2012}
        /// </summary>
        /// <param name="t"></param>
        /// <returns>��ʽ�� {Apr 07,2012}</returns>
        public static string ToDateEnShortString(DateTime t)
        {
            var d = t.ToString("r", new System.Globalization.CultureInfo("en").DateTimeFormat);
            d = d.Remove(d.IndexOf(':') - 2);
            return d;
        }
        /// <summary>
        /// ��ȡʱ���Ӣ�ı�����ʽ����ʽ�� {Mon, 12 Nov 2012 00:00:00 GMT}
        /// </summary>
        /// <param name="t"></param>
        /// <returns>��ʽ�� {Mon, 12 Nov 2012 00:00:00 GMT}</returns>
        public static string ToDateEnLongString(DateTime t)
        {
            return t.ToString("r", new System.Globalization.CultureInfo("en").DateTimeFormat);
        }
        /// <summary>
        /// ������ת�����ַ�����ʽ���������֮����Ӣ�Ķ��ŷָ�
        /// </summary>
        /// <param name="ids"></param>
        /// <returns></returns>
        public static string ToString(int[] ids)
        {
            if (ids == null || ids.Length == 0)
            {
                return "";
            }
            var builder = new StringBuilder();
            for (var i = 0; i < ids.Length; i++)
            {
                builder.Append(ids[i]);
                if (i < ids.Length - 1) builder.Append(',');
            }
            return builder.ToString();
        }
        /// <summary>
        /// ���ı�ת��Ϊ�����ֵ�
        /// </summary>
        /// <param name="doc"></param>
        /// <returns></returns>
        public static Dictionary<string, string> ToDictionary(string doc)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            if (!string.IsNullOrEmpty(doc))
            {
                var arrLine = doc.Split(new char[] { '\n', '\r' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var oneLine in arrLine)
                {
                    //��ֵ��������
                    var tempLine = oneLine.TrimStart().TrimStart('-').TrimStart();
                    //ע��������
                    if (tempLine.StartsWith("//") || tempLine.StartsWith("#"))
                    {
                        continue;
                    }
                    var arrPair = tempLine.Split(new char[] { '=' }, 2);
                    if (arrPair.Length == 2)
                    {
                        var arrTrim = new char[] { '"', '\'' };
                        var itemKey = arrPair[0].Trim().TrimStart(arrTrim).TrimEnd(arrTrim).Trim();
                        var itemValue = arrPair[1].Trim().TrimStart(arrTrim).TrimEnd(arrTrim).Trim();
                        var tmpKey = result.Keys.Where(o => o.ToUpper() == itemKey.ToUpper()).FirstOrDefault();
                        if (!string.IsNullOrEmpty(tmpKey) && result.ContainsKey(tmpKey))
                        {
                            result[tmpKey] = itemValue;
                        }
                        else
                        {
                            result.Add(itemKey, itemValue);
                        }
                    }
                    else
                    {
                        if (tempLine.ToUpper() == "YAML")
                        {
                            result.Add("yaml", "true");
                        }
                    }
                }
            }
            return result;
        }
        /// <summary>
        /// ���ַ�����ʽ�� id �б�ת������������
        /// </summary>
        /// <param name="myids"></param>
        /// <returns></returns>
        public static int[] ToIntArray(string myids)
        {
            if (StringUtil.IsNullOrEmpty(myids)) return new int[] { };
            var arrIds = myids.Split(',');
            var Ids = new int[arrIds.Length];
            for (var i = 0; i < arrIds.Length; i++)
            {
                var oneID = ToInt(arrIds[i].Trim());
                Ids[i] = oneID;
            }
            return Ids;
        }
        /// <summary>
        /// ���ַ���ת�����Ծ��ſ�ͷ�ı�����ʽ�����������Ч����ɫֵ���򷵻�null
        /// </summary>
        /// <param name="val"></param>
        /// <returns>���ַ���ת�����Ծ��ſ�ͷ�ı�����ʽ�����������Ч����ɫֵ���򷵻�null</returns>
        public static string ToColorValue(string val)
        {
            if (StringUtil.IsColorValue(val) == false)
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
        /// �� Stream ת�� byte[]
        /// </summary>
        /// <param name="stream"></param>
        /// <returns></returns>
        public static byte[] ToBytes(System.IO.Stream stream)
        {
            var bytes = new byte[stream.Length];
            stream.Read(bytes, 0, bytes.Length);
            // ���õ�ǰ����λ��Ϊ���Ŀ�ʼ 
            stream.Seek(0, System.IO.SeekOrigin.Begin);
            return bytes;
        }

        /// <summary>
        /// �� byte[] ת�� Stream
        /// </summary>
        /// <param name="bytes"></param>
        /// <returns></returns>
        public static System.IO.Stream ToStream(byte[] bytes)
        {
            var stream = new System.IO.MemoryStream(bytes);
            return stream;
        }


        /// <summary>
        /// ����ʵ���е�ָ�� <see cref="sbyte"/> �ַ�����ת���� <see cref="byte"/> �ַ����顣
        /// </summary>
        /// <param name="sbyteArray">Ҫת���� <see cref="sbyte"/> �ַ�����</param>
        /// <returns>����ת����� <see cref="byte"/> �ַ�����</returns>
        public static byte[] ToByteArray(sbyte[] sbyteArray)
        {
            var byteArray = new byte[sbyteArray.Length];
            for (var index = 0; index < sbyteArray.Length; index++)
                byteArray[index] = (byte)sbyteArray[index];
            return byteArray;
        }


        /// <summary>
        /// ����ʵ���е�ָ���ַ���ת���� <see cref="byte"/> �ַ����顣
        /// </summary>
        /// <param name="sourceString">Ҫת�����ַ���</param>
        /// <returns>����ת����� <see cref="byte"/> �ַ�����</returns>
        public static byte[] ToByteArray(string sourceString)
        {
            var byteArray = new byte[sourceString.Length];
            for (var index = 0; index < sourceString.Length; index++)
                byteArray[index] = (byte)sourceString[index];
            return byteArray;
        }


        /// <summary>
        /// ����ʵ���е�ָ�� <see cref="object"/> ����ת���� <see cref="byte"/> �ַ����顣
        /// </summary>
        /// <param name="tempObjectArray">Ҫת���� <see cref="object"/> �ַ�����</param>
        /// <returns>����ת����� <see cref="byte"/> �ַ�����</returns>
        public static byte[] ToByteArray(object[] tempObjectArray)
        {
            var byteArray = new byte[tempObjectArray.Length];
            for (var index = 0; index < tempObjectArray.Length; index++)
                byteArray[index] = (byte)tempObjectArray[index];
            return byteArray;
        }

        /// <summary>
        /// ����ʵ���е�ָ�� <see cref="byte"/> �ַ�����ת���� <see cref="sbyte"/> �ַ����顣
        /// </summary>
        /// <param name="byteArray">Ҫת���� <see cref="byte"/> �ַ�����</param>
        /// <returns>����ת����� <see cref="sbyte"/> �ַ�����</returns>
        public static sbyte[] ToSByteArray(byte[] byteArray)
        {
            var sbyteArray = new sbyte[byteArray.Length];
            for (var index = 0; index < byteArray.Length; index++)
                sbyteArray[index] = (sbyte)byteArray[index];
            return sbyteArray;
        }


        /// <summary>
        /// �ַ���ת16�����ֽ�����
        /// </summary>
        /// <param name="hexString"></param>
        /// <returns></returns>
        public static byte[] HexStringToBytes(string hexString)
        {
            //byte[] msg1 = System.Text.UTF8Encoding.UTF8.GetBytes(data);
            //string hexString = BytesToHexString(msg1);
            //byte[] returnBytes = new byte[hexString.Length / 2];
            //for (int i = 0; i < returnBytes.Length; i++)
            //{
            //    returnBytes[i] = System.Convert.ToByte(hexString.Substring(i * 2, 2), 10);
            //}
            //return returnBytes;
            if ((hexString.Length % 2) != 0)
            {
                hexString += " ";
            }
            var returnBytes = new byte[hexString.Length / 2];
            for (var i = 0; i < returnBytes.Length; i++)
            {
                returnBytes[i] = System.Convert.ToByte(hexString.Substring(i * 2, 2), 16);
            }
            return returnBytes;
        }

        /// <summary>
        /// byte[]����ת16�����ַ���
        /// </summary>
        /// <param name="input">byte[]����</param>
        /// <returns>16�����ַ���</returns>
        public static string BytesToHexString(byte[] input)
        {
            var hexString = new StringBuilder();
            for (var i = 0; i < input.Length; i++)
            {
                hexString.Append(string.Format("{0:x2}", input[i]));
            }
            return hexString.ToString();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="money"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ToMoney(float money, string format = "")
        {
            if (string.IsNullOrEmpty(format) || !format.StartsWith("F"))
            {
                var _str = money.ToString("F2");
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
        /// 
        /// </summary>
        /// <param name="money"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ToMoney(double money, string format = "")
        {
            return ToMoney((float)money, format);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="money"></param>
        /// <param name="format"></param>
        /// <returns></returns>
        public static string ToMoney(decimal money, string format = "")
        {
            return ToMoney((float)money, format);
        }
        /// <summary>
        /// ��Զ��SvgͼƬת����Svg��ʽ���ַ�������ת��Url������
        /// </summary>
        /// <param name="url"></param>
        /// <returns></returns>
        public static string ToSvgStr(string url)
        {
            var svg = XServer.Common.GetResponseString(url);
            if (!string.IsNullOrEmpty(svg))
            {
                svg = svg.Substring(svg.IndexOf("<svg"));
                svg = System.Text.RegularExpressions.Regex.Replace(svg, "<!--.*?-->", string.Empty);
                if (url.IndexOf('?') > 0)
                {
                    url = url.Substring(url.IndexOf('?') + 1);
                    var args = StringUtil.GetQueryString(url);
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
        /// ���ݲ�ͬ���Ƶ�д�����ض�Ӧ��ֵ
        /// </summary>
        /// <param name="literal">������д��</param>
        /// <returns>��Ӧ��ֵ</returns>
        public static long Identity(long literal)
        {
            return literal;
        }

        /// <summary>
        /// ���ݲ�ͬ���Ƶ�д�����ض�Ӧ��ֵ
        /// </summary>
        /// <param name="literal">������д��</param>
        /// <returns>��Ӧ��ֵ</returns>
        public static ulong Identity(ulong literal)
        {
            return literal;
        }

        /// <summary>
        /// ���ݲ�ͬ���Ƶ�д�����ض�Ӧ��ֵ
        /// </summary>
        /// <param name="literal">������д��</param>
        /// <returns>��Ӧ��ֵ</returns>
        public static float Identity(float literal)
        {
            return literal;
        }

        /// <summary>
        /// ���ݲ�ͬ���Ƶ�д�����ض�Ӧ��ֵ
        /// </summary>
        /// <param name="literal">������д��</param>
        /// <returns>��Ӧ��ֵ</returns>
        public static double Identity(double literal)
        {
            return literal;
        }
    }
}