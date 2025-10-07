using System;

namespace Wlniao.Net.Dns
{
    /// <summary>
    /// NS record class.
    /// </summary>
    internal class DnsRecord_NS : DnsRecord
    {
        private string m_NameServer = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="nameServer">Name server name.</param>
        /// <param name="ttl">TTL value.</param>
        public DnsRecord_NS(string name, string nameServer, int ttl) : base(name,DnsRecordType.NS,ttl)
		{
            m_NameServer = nameServer;
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
        public static DnsRecord_NS Parse(string name, byte[] reply, ref int offset, int rdLength, int ttl)
        {
            // Name server name

            var server = "";
            if (DnsTool.GetQName(reply, ref offset, ref server))
            {
                return new DnsRecord_NS(name, server, ttl);
            }
            else
            {
                throw new ArgumentException("Invalid NS resource record data !");
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
		/// Gets name server name.
		/// </summary>
		public string NameServer
        {
            get { return m_NameServer; }
        }

        #endregion
    }
}