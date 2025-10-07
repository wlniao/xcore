namespace Wlniao.Net.Dns
{
    /// <summary>
    /// TXT record class.
    /// </summary>
    internal class DnsRecord_TXT : DnsRecord
    {
        private string m_Text = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="text">Text.</param>
        /// <param name="ttl">TTL value.</param>
        public DnsRecord_TXT(string name, string text, int ttl) : base(name,DnsRecordType.TXT,ttl)
		{
            m_Text = text;
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
        public static DnsRecord_TXT Parse(string name, byte[] reply, ref int offset, int rdLength, int ttl)
        {
            // TXT RR

            var text = DnsTool.ReadCharacterString(reply, ref offset);

            return new DnsRecord_TXT(name, text, ttl);
        }

        #endregion


        #region Properties Implementation

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