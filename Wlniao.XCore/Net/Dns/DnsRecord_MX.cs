using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
namespace Wlniao.Net.Dns
{
    /// <summary>
    /// MX record class.
    /// </summary>
    internal class DnsRecord_MX : DnsRecord
    {
        private int m_Preference = 0;
        private string m_Host = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="preference">MX record preference.</param>
        /// <param name="host">Mail host dns name.</param>
        /// <param name="ttl">TTL value.</param>
        public DnsRecord_MX(string name, int preference, string host, int ttl) : base(name,DnsRecordType.MX,ttl)
		{
            m_Preference = preference;
            m_Host = host;
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
        public static DnsRecord_MX Parse(string name, byte[] reply, ref int offset, int rdLength, int ttl)
        {
            /* RFC 1035	3.3.9. MX RDATA format

			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                  PREFERENCE                   |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			/                   EXCHANGE                    /
			/                                               /
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

			where:

			PREFERENCE      
				A 16 bit integer which specifies the preference given to
				this RR among others at the same owner.  Lower values
                are preferred.

			EXCHANGE 
			    A <domain-name> which specifies a host willing to act as
                a mail exchange for the owner name. 
			*/

            var pref = reply[offset++] << 8 | reply[offset++];

            var server = "";
            if (DnsTool.GetQName(reply, ref offset, ref server))
            {
                return new DnsRecord_MX(name, pref, server, ttl);
            }
            else
            {
                throw new ArgumentException("Invalid MX resource record data !");
            }
        }

        #endregion


        #region IComparable Implementation

        /// <summary>
        /// Compares the current instance with another object of the same type. 
        /// </summary>
        /// <param name="obj">An object to compare with this instance. </param>
        /// <returns>Returns 0 if two objects are equal, returns negative value if this object is less,
        /// returns positive value if this object is grater.</returns>
        public int CompareTo(object obj)
        {
            if (obj == null)
            {
                throw new ArgumentNullException("obj");
            }
            if (!(obj is DnsRecord_MX))
            {
                throw new ArgumentException("Argument obj is not MX_Record !");
            }

            var mx = (DnsRecord_MX)obj;
            if (this.Preference > mx.Preference)
            {
                return 1;
            }
            else if (this.Preference < mx.Preference)
            {
                return -1;
            }
            else
            {
                return 0;
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
		/// Gets MX record preference. The lower number is the higher priority server.
		/// </summary>
		public int Preference
        {
            get { return m_Preference; }
        }

        /// <summary>
        /// Gets mail host dns name.
        /// </summary>
        public string Host
        {
            get { return m_Host; }
        }

        #endregion
    }
}