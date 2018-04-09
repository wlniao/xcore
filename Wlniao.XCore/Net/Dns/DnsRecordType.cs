namespace Wlniao.Net.Dns
{
    /// <summary>
    /// DNS记录类型
    /// </summary>
    public enum DnsRecordType
    {
        /// <summary>
        /// IPv4 host address
        /// </summary>
        A = 1,
        /// <summary>
        /// An authoritative name server.
        /// </summary>
        NS = 2,
        /// <summary>
        /// The canonical name for an alias.
        /// </summary>
        CNAME = 5,
        /// <summary>
        /// Marks the start of a zone of authority.
        /// </summary>
        SOA = 6,
        /// <summary>
        /// A domain name pointer.
        /// </summary>
        PTR = 12,
        /// <summary>
        /// Host information.
        /// </summary>
        HINFO = 13,
        /// <summary>
        /// Mailbox or mail list information.
        /// </summary>
        MINFO = 14,
        /// <summary>
        /// Mail exchange.
        /// </summary>
        MX = 15,
        /// <summary>
        /// Text strings.
        /// </summary>
        TXT = 16,
        /// <summary>
        /// IPv6 host address.
        /// </summary>
        AAAA = 28,
        /// <summary>
        /// SRV record specifies the location of services.
        /// </summary>
        SRV = 33,
        /// <summary>
        /// NAPTR(Naming Authority Pointer) record.
        /// </summary>
        NAPTR = 35,
        /// <summary>
        /// SPF(Sender Policy Framework) record.
        /// </summary>
        SPF = 99,
        /// <summary>
        /// All records what server returns.
        /// </summary>
        ANY = 255,
    };
}
