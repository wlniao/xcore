using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
namespace Wlniao.Net.Dns
{
    /// <summary>
    /// CNAME record class.
    /// </summary>
    internal class DnsRecord_CNAME : DnsRecord
    {
        private string m_Alias = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="alias">Alias.</param>
        /// <param name="ttl">TTL value.</param>
        public DnsRecord_CNAME(string name, string alias, int ttl) : base(name,DnsRecordType.CNAME,ttl)
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
        public static DnsRecord_CNAME Parse(string name, byte[] reply, ref int offset, int rdLength, int ttl)
        {
            string alias = "";
            if (DnsTool.GetQName(reply, ref offset, ref alias))
            {
                return new DnsRecord_CNAME(name, alias, ttl);
            }
            else
            {
                throw new ArgumentException("Invalid CNAME resource record data !");
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
		/// Gets alias.
		/// </summary>
		public string Alias
        {
            get { return m_Alias; }
        }

        #endregion
    }
}