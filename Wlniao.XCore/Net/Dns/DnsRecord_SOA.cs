using System;

namespace Wlniao.Net.Dns
{
    /// <summary>
    /// SOA record class. SOA 记录描述区域的起始授权机构，数据部分是 2 个域名 + 5 个 32 位无符号整数。
    /// </summary>
    internal class DnsRecord_SOA : DnsRecord
    {
        private readonly string m_MName;
        private readonly string m_RName;
        private readonly uint m_Serial;
        private readonly uint m_Refresh;
        private readonly uint m_Retry;
        private readonly uint m_Expire;
        private readonly uint m_Minimum;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="mname">主名称服务器域名</param>
        /// <param name="rname">负责人邮箱域名</param>
        /// <param name="serial">区域版本号</param>
        /// <param name="refresh">刷新间隔（秒）</param>
        /// <param name="retry">重试间隔（秒）</param>
        /// <param name="expire">过期时间（秒）</param>
        /// <param name="minimum">负缓存 TTL（秒）</param>
        /// <param name="ttl">TTL value.</param>
        public DnsRecord_SOA(string name, string mname, string rname, uint serial, uint refresh, uint retry, uint expire, uint minimum, int ttl)
            : base(name, DnsRecordType.SOA, ttl)
        {
            m_MName = mname;
            m_RName = rname;
            m_Serial = serial;
            m_Refresh = refresh;
            m_Retry = retry;
            m_Expire = expire;
            m_Minimum = minimum;
        }


        #region static method Parse

        /// <summary>
        /// Parses resource record from reply data.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="reply">DNS server reply data.</param>
        /// <param name="offset">Current offset in reply data.</param>
        /// <param name="rdLength">Resource record data length.</param>
        /// <param name="ttl">Time to live in seconds.</param>
        public static DnsRecord_SOA Parse(string name, byte[] reply, ref int offset, int rdLength, int ttl)
        {
            // SOA = <domain-name> <domain-name> 5 x 32bit
            var end = offset + rdLength;
            if (end > reply.Length)
            {
                end = reply.Length;
            }

            var mname = "";
            if (!DnsTool.GetQName(reply, ref offset, ref mname))
            {
                throw new ArgumentException("Invalid SOA resource record data !");
            }
            var rname = "";
            if (!DnsTool.GetQName(reply, ref offset, ref rname))
            {
                throw new ArgumentException("Invalid SOA resource record data !");
            }

            var serial = ReadUInt32(reply, ref offset, end);
            var refresh = ReadUInt32(reply, ref offset, end);
            var retry = ReadUInt32(reply, ref offset, end);
            var expire = ReadUInt32(reply, ref offset, end);
            var minimum = ReadUInt32(reply, ref offset, end);

            // 定长字段读完后对齐到 rdLength，避免报文长度不一致时错位影响后续记录
            if (offset < end)
            {
                offset = end;
            }

            return new DnsRecord_SOA(name, mname, rname, serial, refresh, retry, expire, minimum, ttl);
        }

        /// <summary>
        /// 读取网络字节序的 32 位无符号整数
        /// </summary>
        /// <param name="reply">DNS 应答报文</param>
        /// <param name="offset">当前读取位置</param>
        /// <param name="endOffset">本条 RDATA 的结束位置（不含）</param>
        /// <remarks>
        /// 必须受 RDATA 边界约束：RDLENGTH 不足 20 字节的畸形 SOA 一旦按报文长度读，
        /// 就会把后面记录的字节当成定时器字段吞掉，甚至越界抛出异常让整份报文被丢弃。
        /// </remarks>
        private static uint ReadUInt32(byte[] reply, ref int offset, int endOffset)
        {
            if (offset < 0 || offset + 4 > endOffset || offset + 4 > reply.Length)
            {
                throw new ArgumentException("Invalid SOA resource record data !");
            }
            var value = ((uint)reply[offset++] << 24) | ((uint)reply[offset++] << 16) | ((uint)reply[offset++] << 8) | reply[offset++];
            return value;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets 主名称服务器域名
        /// </summary>
        public string MName
        {
            get { return m_MName; }
        }

        /// <summary>
        /// Gets 负责人邮箱域名
        /// </summary>
        public string RName
        {
            get { return m_RName; }
        }

        /// <summary>
        /// Gets 区域版本号
        /// </summary>
        public uint Serial
        {
            get { return m_Serial; }
        }

        /// <summary>
        /// Gets 刷新间隔（秒）
        /// </summary>
        public uint Refresh
        {
            get { return m_Refresh; }
        }

        /// <summary>
        /// Gets 重试间隔（秒）
        /// </summary>
        public uint Retry
        {
            get { return m_Retry; }
        }

        /// <summary>
        /// Gets 过期时间（秒）
        /// </summary>
        public uint Expire
        {
            get { return m_Expire; }
        }

        /// <summary>
        /// Gets 负缓存 TTL（秒）
        /// </summary>
        public uint Minimum
        {
            get { return m_Minimum; }
        }

        #endregion
    }
}
