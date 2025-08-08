using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Net;
using System.Net.Sockets;
namespace Wlniao.Net.Dns
{
    /// <summary>
    /// SRV record class.
    /// </summary>
    internal class DnsRecord_SRV : DnsRecord
    {
        private int m_Priority = 1;
        private int m_Weight = 1;
        private int m_Port = 0;
        private string m_Target = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="priority">Service priority.</param>
        /// <param name="weight">Weight value.</param>
        /// <param name="port">Service port.</param>
        /// <param name="target">Service provider host name or IP address.</param>
        /// <param name="ttl">Time to live value in seconds.</param>
        public DnsRecord_SRV(string name, int priority, int weight, int port, string target, int ttl) 
            : base(name,DnsRecordType.SRV,ttl)
        {
            m_Priority = priority;
            m_Weight = weight;
            m_Port = port;
            m_Target = target;
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
        public static DnsRecord_SRV Parse(string name, byte[] reply, ref int offset, int rdLength, int ttl)
        {
            // Priority Weight Port Target

            // Priority
            var priority = reply[offset++] << 8 | reply[offset++];

            // Weight
            var weight = reply[offset++] << 8 | reply[offset++];

            // Port
            var port = reply[offset++] << 8 | reply[offset++];

            // Target
            var target = "";
            DnsTool.GetQName(reply, ref offset, ref target);

            return new DnsRecord_SRV(name, priority, weight, port, target, ttl);
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets service priority. Lowest value means greater priority.
        /// </summary>
        public int Priority
        {
            get { return m_Priority; }
        }

        /// <summary>
        /// Gets weight. The weight field specifies a relative weight for entries with the same priority. 
        /// Larger weights SHOULD be given a proportionately higher probability of being selected.
        /// </summary>
        public int Weight
        {
            get { return m_Weight; }
        }

        /// <summary>
        /// Port where service runs.
        /// </summary>
        public int Port
        {
            get { return m_Port; }
        }

        /// <summary>
        /// Service provider host name or IP address.
        /// </summary>
        public string Target
        {
            get { return m_Target; }
        }

        #endregion

    }
}