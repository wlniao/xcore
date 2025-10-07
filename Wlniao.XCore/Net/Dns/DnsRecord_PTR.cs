﻿using System;
using System.Net;

namespace Wlniao.Net.Dns
{
    /// <summary>
    /// PTR record class.
    /// </summary>
    internal class DnsRecord_PTR : DnsRecord
    {
        private IPAddress m_IP = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="ip">IP address.</param>
        /// <param name="ttl">TTL value.</param>
        public DnsRecord_PTR(string name, IPAddress ip, int ttl) : base(name, DnsRecordType.A, ttl)
        {
            m_IP = ip;
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

            var ip = new byte[rdLength];
            Array.Copy(reply, offset, ip, 0, rdLength);
            offset += rdLength;

            return new DnsRecord_A(name, new IPAddress(ip), ttl);
        }

        #endregion


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