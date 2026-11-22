/*==============================================================================
    文件名称：ApiSignGuard.cs
    功能描述：XServer/TokenApi 出站请求签名与入站校验的公共辅助
              1. 时间窗双向校验（同时拒绝过期与超前时间戳）
              2. 常量时间签名比较
              3. 进程内重放拦截（同一 sign 在时间窗内只允许出现一次）
===============================================================================*/
using System;
using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;

namespace Wlniao.XServer
{
    /// <summary>
    /// API 请求签名校验辅助：双向时间窗、常量时间比较、重放拦截
    /// </summary>
    public static class ApiSignGuard
    {
        /// <summary>时间窗外的允许偏差（秒）：过去与未来对称，覆盖时钟偏移</summary>
        public const long MaxSkewSeconds = 3600;

        private static readonly ConcurrentDictionary<string, long> SeenSigns = new();
        private static long _lastSweepUnix;

        /// <summary>
        /// 双向时间窗校验：|now - timestamp| 必须不超过 <see cref="MaxSkewSeconds"/>
        /// </summary>
        /// <param name="timestamp">请求携带的 Unix 秒级时间戳</param>
        /// <param name="nowUnix">当前 Unix 时间戳</param>
        /// <returns>true 表示在允许窗口内</returns>
        public static bool IsTimestampValid(long timestamp, long nowUnix)
        {
            var delta = timestamp - nowUnix;
            if (delta < 0)
            {
                delta = -delta;
            }
            return delta <= MaxSkewSeconds;
        }

        /// <summary>
        /// 常量时间字符串比较（长度不同也走固定路径，避免早退出侧信道）
        /// </summary>
        public static bool FixedTimeEquals(string expected, string actual)
        {
            if (expected == null || actual == null)
            {
                return false;
            }
            var a = Encoding.UTF8.GetBytes(expected);
            var b = Encoding.UTF8.GetBytes(actual);
            return CryptographicOperations.FixedTimeEquals(a, b);
        }

        /// <summary>
        /// 校验请求签名是否首次出现：首次返回 true 并记录；时间窗内重复出现返回 false（重放）。
        /// 以 sign 本身作为去重键——同一请求重放时 sign 必然相同，无需改协议加 nonce。
        /// </summary>
        /// <param name="sign">请求签名（SM3 十六进制）</param>
        /// <param name="nowUnix">当前 Unix 时间戳</param>
        /// <returns>true=首次通过；false=窗口内已出现过（重放）</returns>
        public static bool TryAcceptOnce(string sign, long nowUnix)
        {
            if (string.IsNullOrEmpty(sign))
            {
                return false;
            }
            Sweep(nowUnix);
            // 过期条目允许覆盖写回（等价于首次出现）
            if (SeenSigns.TryGetValue(sign, out var expireAt))
            {
                if (expireAt > nowUnix)
                {
                    return false;
                }
                SeenSigns[sign] = nowUnix + MaxSkewSeconds;
                return true;
            }
            return SeenSigns.TryAdd(sign, nowUnix + MaxSkewSeconds);
        }

        /// <summary>
        /// 生成响应完整性签名：SM3(data + token)，data 为响应密文（或失败路径的明文状态串）
        /// </summary>
        public static string SignResponse(string data, string token)
        {
            return Encryptor.SM3Encrypt((data ?? "") + (token ?? ""));
        }

        private static void Sweep(long nowUnix)
        {
            // 每小时最多全表扫一次，避免高频请求下反复遍历
            if (nowUnix - _lastSweepUnix < 3600)
            {
                return;
            }
            _lastSweepUnix = nowUnix;
            foreach (var kv in SeenSigns)
            {
                if (kv.Value <= nowUnix)
                {
                    SeenSigns.TryRemove(kv.Key, out _);
                }
            }
        }
    }
}
