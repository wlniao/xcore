using System.Net;

namespace Wlniao.Net.Dns
{
    /// <summary>
    /// A record class.
    /// </summary>
    internal class DnsRecord_A : DnsRecord_Address
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="ip">IP address.</param>
        /// <param name="ttl">TTL value.</param>
        public DnsRecord_A(string name, IPAddress ip, int ttl) : base(name, DnsRecordType.A, ip, ttl)
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
        public static DnsRecord_A Parse(string name, byte[] reply, ref int offset, int rdLength, int ttl)
        {
            // IPv4 = byte byte byte byte
            return new DnsRecord_A(name, ReadIP(reply, ref offset, rdLength), ttl);
        }

        #endregion
    }
}
