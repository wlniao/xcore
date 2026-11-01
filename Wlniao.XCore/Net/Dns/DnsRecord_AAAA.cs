using System.Net;

namespace Wlniao.Net.Dns
{
    /// <summary>
    /// AAAA record class.
    /// </summary>
    internal class DnsRecord_AAAA : DnsRecord_Address
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="ip">IP address.</param>
        /// <param name="ttl">Time to live in seconds.</param>
        public DnsRecord_AAAA(string name, IPAddress ip, int ttl) : base(name, DnsRecordType.AAAA, ip, ttl)
        {
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
        public static DnsRecord_AAAA Parse(string name, byte[] reply, ref int offset, int rdLength, int ttl)
        {
            // IPv6 = 16xbyte
            return new DnsRecord_AAAA(name, ReadIP(reply, ref offset, rdLength), ttl);
        }

        #endregion
    }
}
