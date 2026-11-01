using System;
using System.Collections.Generic;

namespace Wlniao.Net.Dns
{
    /// <summary>
    /// This class represents dns server response.
    /// </summary>
    public class DnsServerResponse
    {
        private bool m_Success = true;
        private int m_ID = 0;
        private ResponceCode m_RCODE = ResponceCode.NO_ERROR;
        private List<DnsRecord> m_pAnswers = null;
        private List<DnsRecord> m_pAuthoritiveAnswers = null;
        private List<DnsRecord> m_pAdditionalAnswers = null;

        internal DnsServerResponse()
        {

        }
        internal DnsServerResponse(bool connectionOk, int id, ResponceCode rcode
            , List<DnsRecord> answers
            , List<DnsRecord> authoritiveAnswers
            , List<DnsRecord> additionalAnswers)
        {
            m_Success = connectionOk;
            m_ID = id;
            m_RCODE = rcode;
            m_pAnswers = answers;
            m_pAuthoritiveAnswers = authoritiveAnswers;
            m_pAdditionalAnswers = additionalAnswers;
        }


        #region method GetRecords

        /// <summary>
        /// 从应答中筛选指定类型的记录并转换为具体记录类型。
        /// </summary>
        /// <typeparam name="TRecord">具体的 DNS 记录类型</typeparam>
        /// <param name="type">要筛选的记录类型</param>
        /// <returns>指定类型的记录数组</returns>
        internal TRecord[] GetRecords<TRecord>(DnsRecordType type) where TRecord : DnsRecord
        {
            var retVal = new List<TRecord>();
            if (m_pAnswers == null)
            {
                return retVal.ToArray();
            }
            foreach (var record in m_pAnswers)
            {
                if (record.RecordType == type && record is TRecord typed)
                {
                    retVal.Add(typed);
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetARecords

        /// <summary>
        /// Gets IPv4 host addess records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_A[] GetARecords()
        {
            return GetRecords<DnsRecord_A>(DnsRecordType.A);
        }

        #endregion

        #region method GetAAAARecords

        /// <summary>
        /// Gets IPv6 host addess records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_AAAA[] GetAAAARecords()
        {
            return GetRecords<DnsRecord_AAAA>(DnsRecordType.AAAA);
        }

        #endregion

        #region method GetNSRecords

        /// <summary>
        /// Gets name server records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_NS[] GetNSRecords()
        {
            return GetRecords<DnsRecord_NS>(DnsRecordType.NS);
        }

        #endregion

        #region method GetCNAMERecords

        /// <summary>
        /// Gets CNAME records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_CNAME[] GetCNAMERecords()
        {
            return GetRecords<DnsRecord_CNAME>(DnsRecordType.CNAME);
        }

        #endregion

        #region method GetMXRecords

        /// <summary>
        /// Gets MX records.(MX records are sorted by preference, lower array element is prefered)
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_MX[] GetMXRecords()
        {
            // Sort MX records by preference.
            var retVal = GetRecords<DnsRecord_MX>(DnsRecordType.MX);
            Array.Sort(retVal);

            return retVal;
        }

        #endregion

        #region method GetTXTRecords

        /// <summary>
        /// Gets text records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_TXT[] GetTXTRecords()
        {
            return GetRecords<DnsRecord_TXT>(DnsRecordType.TXT);
        }

        #endregion

        #region method GetSOARecords

        /// <summary>
        /// Gets SOA records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_SOA[] GetSOARecords()
        {
            return GetRecords<DnsRecord_SOA>(DnsRecordType.SOA);
        }

        #endregion

        #region method GetPTRRecords

        /// <summary>
        /// Gets PTR records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_PTR[] GetPTRRecords()
        {
            return GetRecords<DnsRecord_PTR>(DnsRecordType.PTR);
        }

        #endregion

        #region method GetHINFORecords

        /// <summary>
        /// Gets HINFO records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_HINFO[] GetHINFORecords()
        {
            return GetRecords<DnsRecord_HINFO>(DnsRecordType.HINFO);
        }

        #endregion

        #region method GetSRVRecords

        /// <summary>
        /// Gets SRV resource records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_SRV[] GetSRVRecords()
        {
            return GetRecords<DnsRecord_SRV>(DnsRecordType.SRV);
        }

        #endregion

        #region method GetNAPTRRecords

        /// <summary>
        /// Gets NAPTR resource records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_NAPTR[] GetNAPTRRecords()
        {
            return GetRecords<DnsRecord_NAPTR>(DnsRecordType.NAPTR);
        }

        #endregion

        #region method GetSPFRecords

        /// <summary>
        /// Gets SPF resource records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_SPF[] GetSPFRecords()
        {
            return GetRecords<DnsRecord_SPF>(DnsRecordType.SPF);
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets if connection to dns server was successful.
        /// </summary>
        public bool ConnectionOk
        {
            get { return m_Success; }
        }

        /// <summary>
        /// Gets DNS transaction ID.
        /// </summary>
        public int ID
        {
            get { return m_ID; }
        }

        /// <summary>
        /// Gets dns server response code.
        /// </summary>
        internal ResponceCode ResponseCode
        {
            get { return m_RCODE; }
        }


        /// <summary>
        /// Gets all resource records returned by server (answer records section + authority records section + additional records section). 
        /// NOTE: Before using this property ensure that ConnectionOk=true and ResponseCode=RCODE.NO_ERROR.
        /// </summary>
        internal DnsRecord[] AllAnswers
        {
            get
            {
                var retVal = new List<DnsRecord>();
                retVal.AddRange(m_pAnswers.ToArray());
                retVal.AddRange(m_pAuthoritiveAnswers.ToArray());
                retVal.AddRange(m_pAdditionalAnswers.ToArray());

                return retVal.ToArray();
            }
        }

        /// <summary>
        /// Gets dns server returned answers. NOTE: Before using this property ensure that ConnectionOk=true and ResponseCode=RCODE.NO_ERROR.
        /// </summary>
        internal DnsRecord[] Answers
        {
            get { return m_pAnswers.ToArray(); }
        }

        /// <summary>
        /// Gets name server resource records in the authority records section. NOTE: Before using this property ensure that ConnectionOk=true and ResponseCode=RCODE.NO_ERROR.
        /// </summary>
        internal DnsRecord[] AuthoritiveAnswers
        {
            get { return m_pAuthoritiveAnswers.ToArray(); }
        }

        /// <summary>
        /// Gets resource records in the additional records section. NOTE: Before using this property ensure that ConnectionOk=true and ResponseCode=RCODE.NO_ERROR.
        /// </summary>
        internal DnsRecord[] AdditionalAnswers
        {
            get { return m_pAdditionalAnswers.ToArray(); }
        }

        #endregion
    }
}
