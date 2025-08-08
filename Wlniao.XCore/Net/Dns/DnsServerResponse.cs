using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

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


        #region method GetARecords

        /// <summary>
        /// Gets IPv4 host addess records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_A[] GetARecords()
        {
            var retVal = new List<DnsRecord_A>();
            foreach (var record in m_pAnswers)
            {
                if (record.RecordType == DnsRecordType.A)
                {
                    retVal.Add((DnsRecord_A)record);
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetAAAARecords

        /// <summary>
        /// Gets IPv6 host addess records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_AAAA[] GetAAAARecords()
        {
            var retVal = new List<DnsRecord_AAAA>();
            foreach (var record in m_pAnswers)
            {
                if (record.RecordType == DnsRecordType.AAAA)
                {
                    retVal.Add((DnsRecord_AAAA)record);
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetNSRecords

        /// <summary>
        /// Gets name server records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_NS[] GetNSRecords()
        {
            var retVal = new List<DnsRecord_NS>();
            foreach (var record in m_pAnswers)
            {
                if (record.RecordType == DnsRecordType.NS)
                {
                    retVal.Add((DnsRecord_NS)record);
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetCNAMERecords

        /// <summary>
        /// Gets CNAME records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_CNAME[] GetCNAMERecords()
        {
            var retVal = new List<DnsRecord_CNAME>();
            foreach (var record in m_pAnswers)
            {
                if (record.RecordType == DnsRecordType.CNAME)
                {
                    retVal.Add((DnsRecord_CNAME)record);
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetMXRecords

        /// <summary>
        /// Gets MX records.(MX records are sorted by preference, lower array element is prefered)
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_MX[] GetMXRecords()
        {
            var mx = new List<DnsRecord_MX>();
            foreach (var record in m_pAnswers)
            {
                if (record.RecordType == DnsRecordType.MX)
                {
                    mx.Add((DnsRecord_MX)record);
                }
            }

            // Sort MX records by preference.
            var retVal = mx.ToArray();
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
            var retVal = new List<DnsRecord_TXT>();
            foreach (var record in m_pAnswers)
            {
                if (record.RecordType == DnsRecordType.TXT)
                {
                    retVal.Add((DnsRecord_TXT)record);
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetSOARecords

        /// <summary>
        /// Gets SOA records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_SOA[] GetSOARecords()
        {
            var retVal = new List<DnsRecord_SOA>();
            foreach (var record in m_pAnswers)
            {
                if (record.RecordType == DnsRecordType.SOA)
                {
                    retVal.Add((DnsRecord_SOA)record);
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetPTRRecords

        /// <summary>
        /// Gets PTR records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_PTR[] GetPTRRecords()
        {
            var retVal = new List<DnsRecord_PTR>();
            foreach (var record in m_pAnswers)
            {
                if (record.RecordType == DnsRecordType.PTR)
                {
                    retVal.Add((DnsRecord_PTR)record);
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetHINFORecords

        /// <summary>
        /// Gets HINFO records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_HINFO[] GetHINFORecords()
        {
            var retVal = new List<DnsRecord_HINFO>();
            foreach (var record in m_pAnswers)
            {
                if (record.RecordType == DnsRecordType.HINFO)
                {
                    retVal.Add((DnsRecord_HINFO)record);
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetSRVRecords

        /// <summary>
        /// Gets SRV resource records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_SRV[] GetSRVRecords()
        {
            var retVal = new List<DnsRecord_SRV>();
            foreach (var record in m_pAnswers)
            {
                if (record.RecordType == DnsRecordType.SRV)
                {
                    retVal.Add((DnsRecord_SRV)record);
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetNAPTRRecords

        /// <summary>
        /// Gets NAPTR resource records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_NAPTR[] GetNAPTRRecords()
        {
            var retVal = new List<DnsRecord_NAPTR>();
            foreach (var record in m_pAnswers)
            {
                if (record.RecordType == DnsRecordType.NAPTR)
                {
                    retVal.Add((DnsRecord_NAPTR)record);
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetSPFRecords

        /// <summary>
        /// Gets SPF resource records.
        /// </summary>
        /// <returns></returns>
        internal DnsRecord_SPF[] GetSPFRecords()
        {
            var retVal = new List<DnsRecord_SPF>();
            foreach (var record in m_pAnswers)
            {
                if (record.RecordType == DnsRecordType.SPF)
                {
                    retVal.Add((DnsRecord_SPF)record);
                }
            }

            return retVal.ToArray();
        }

        #endregion


        #region method FilterRecords

        /// <summary>
        /// Filters out specified type of records from answer.
        /// </summary>
        /// <param name="answers"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        private List<DnsRecord> FilterRecordsX(List<DnsRecord> answers, DnsRecordType type)
        {
            var retVal = new List<DnsRecord>();
            foreach (var record in answers)
            {
                if (record.RecordType == type)
                {
                    retVal.Add(record);
                }
            }

            return retVal;
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
        /// <code>
        /// // NOTE: DNS server may return diffrent record types even if you query MX.
        /// //       For example you query lumisoft.ee MX and server may response:	
        ///	//		 1) MX - mail.lumisoft.ee
        ///	//		 2) A  - lumisoft.ee
        ///	// 
        ///	//       Before casting to right record type, see what type record is !
        ///				
        /// 
        /// foreach(DnsRecordBase record in Answers){
        ///		// MX record, cast it to MX_Record
        ///		if(record.RecordType == QTYPE.MX){
        ///			MX_Record mx = (MX_Record)record;
        ///		}
        /// }
        /// </code>
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
