using System;
using System.Net;

namespace Wlniao.Net.Dns
{
    /// <summary>
    /// 承载 IP 地址的 DNS 记录基类（A / AAAA）。
    /// </summary>
    internal abstract class DnsRecord_Address : DnsRecord
    {
        private readonly IPAddress m_IP;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="recordType">Record type (A, AAAA).</param>
        /// <param name="ip">IP address.</param>
        /// <param name="ttl">TTL value.</param>
        protected DnsRecord_Address(string name, DnsRecordType recordType, IPAddress ip, int ttl)
            : base(name, recordType, ttl)
        {
            m_IP = ip;
        }

        /// <summary>
        /// 从应答报文中读取定长的 IP 地址数据
        /// </summary>
        /// <param name="reply">DNS server reply data.</param>
        /// <param name="offset">Current offset in reply data.</param>
        /// <param name="rdLength">Resource record data length.</param>
        protected static IPAddress ReadIP(byte[] reply, ref int offset, int rdLength)
        {
            var ip = new byte[rdLength];
            Array.Copy(reply, offset, ip, 0, rdLength);
            offset += rdLength;

            return new IPAddress(ip);
        }

        #region Properties Implementation

        /// <summary>
        /// Gets host IP address.
        /// </summary>
        public IPAddress IP
        {
            get { return m_IP; }
        }

        #endregion
    }
}
