using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
namespace Wlniao.Net.Dns
{
    /// <summary>
    /// SPF record class.
    /// </summary>
    internal class DnsRecord_SPF : DnsRecord
    {
        private string m_Text = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="text">SPF text.</param>
		/// <param name="ttl">TTL (time to live) value in seconds.</param>
        public DnsRecord_SPF(string name, string text, int ttl) : base(name,DnsRecordType.SPF,ttl)
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
        public static DnsRecord_SPF Parse(string name, byte[] reply, ref int offset, int rdLength, int ttl)
        {
            // SPF RR

            var text = DnsTool.ReadCharacterString(reply, ref offset);

            return new DnsRecord_SPF(name, text, ttl);
        }

        #endregion


        #region Properties implementation

        /// <summary>
		/// Gets text.
		/// </summary>
		public string Text
        {
            get { return m_Text; }
        }

        #endregion

    }
}