using System;

namespace Wlniao.Net.Dns
{
    /// <summary>
    /// PTR record class. PTR 记录的数据部分是一个域名（指针），不是 IP 地址。
    /// </summary>
    internal class DnsRecord_PTR : DnsRecord
    {
        private readonly string m_Alias;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="alias">Pointer domain name.</param>
        /// <param name="ttl">TTL value.</param>
        public DnsRecord_PTR(string name, string alias, int ttl) : base(name, DnsRecordType.PTR, ttl)
        {
            m_Alias = alias;
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
        public static DnsRecord_PTR Parse(string name, byte[] reply, ref int offset, int rdLength, int ttl)
        {
            // PTR = <domain-name>
            var alias = "";
            if (DnsTool.GetQName(reply, ref offset, ref alias))
            {
                return new DnsRecord_PTR(name, alias, ttl);
            }
            else
            {
                throw new ArgumentException("Invalid PTR resource record data !");
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets pointer domain name.
        /// </summary>
        public string Alias
        {
            get { return m_Alias; }
        }

        #endregion
    }
}
