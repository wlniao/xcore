using System;
using System.Collections.Generic;
using System.Text;

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
            /* RFC 1035 3.3.14. TXT RDATA format

                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                /                   TXT-DATA                    /
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

                TXT-DATA = 一个或多个 <character-string>
                <character-string> = 1 字节长度 + 该长度的字节内容，单串最长 255 字节。
                读完 RDATA 内的全部串并按出现顺序拼接。
            */
            var endOffset = Math.Min(offset + rdLength, reply.Length);
            var texts = new List<string>();
            while (offset < endOffset)
            {
                // TXT 值在现代用法里按 UTF-8 解释（非 ASCII 会被 ASCII 解码成 '?'），
                // 这里显式用 UTF-8；其余记录类型仍沿用 ReadCharacterString 默认的 ASCII 语义。
                texts.Add(DnsTool.ReadCharacterString(reply, ref offset, Encoding.UTF8, endOffset));
            }

            return new DnsRecord_TXT(name, string.Concat(texts), ttl);
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