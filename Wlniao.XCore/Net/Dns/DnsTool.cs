using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
namespace Wlniao.Net.Dns
{
    /// <summary>
    /// 
    /// </summary>
    public class DnsTool
    {
        private Int32 requestID = 0;
        private IPAddress[] serverIPs;
        /// <summary>
        /// 
        /// </summary>
        public DnsTool()
        {
            serverIPs = new IPAddress[] {
                IPAddress.Parse("223.5.5.5")        //阿里 AliDNS
                ,
                IPAddress.Parse("119.29.29.29")     //腾讯公共DNS
                ,
                IPAddress.Parse("8.8.8.8")          //Google DNS
                ,
                IPAddress.Parse("114.114.114.114")  //114 DNS
            };
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="ServerIPorHost"></param>
        public DnsTool(String ServerIPorHost)
        {
            if (Text.StringUtil.IsIP(ServerIPorHost))
            {
                serverIPs = new IPAddress[] { IPAddress.Parse(ServerIPorHost) };
            }
            else
            {
                serverIPs = new IPAddress[] { new DnsTool().GetIPAddress(ServerIPorHost) };
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qname"></param>
        /// <returns></returns>
        public IPAddress GetIPAddressDefault(String qname)
        {
            IPAddress address = null;
            try
            {
                System.Net.Dns.GetHostAddressesAsync(qname).ContinueWith((task) =>
                {
                    if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && task.Result != null && task.Result.Length > 0)
                    {
                        address = task.Result[0];
                    }
                }).Wait();
            }
            catch { }
            return address;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qname"></param>
        /// <returns></returns>
        public IPAddress GetIPAddress(String qname)
        {
            if (qname.IndexOf('.') > 0)
            {
                var res = GetResponse(qname, DnsRecordType.A);
                if (res != null)
                {
                    var record = res.GetARecords();
                    if (record != null && record.Length > 0)
                    {
                        return record[0].IP;
                    }
                }
            }
            else
            {
                IPAddress address = null;
                try
                {
                    System.Net.Dns.GetHostAddressesAsync(qname).ContinueWith((task) =>
                    {
                        if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion && task.Result != null && task.Result.Length > 0)
                        {
                            address = task.Result[0];
                        }
                    }).Wait();
                }
                catch { }
                return address;
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qname"></param>
        /// <returns></returns>
        public string GetCNAME(String qname)
        {
            var res = GetResponse(qname, DnsRecordType.CNAME);
            if (res != null)
            {
                var record = res.GetCNAMERecords();
                if (record != null && record.Length > 0)
                {
                    return record[0].Alias;
                }
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qname"></param>
        /// <returns></returns>
        public String GetTXT(String qname)
        {
            var res = GetResponse(qname, DnsRecordType.TXT);
            if (res != null)
            {
                var record = res.GetTXTRecords();
                if (record != null && record.Length > 0)
                {
                    return record[0].Text;
                }
            }
            return null;
        }

        /// <summary>
        /// 获取服务器输出
        /// </summary>
        /// <param name="qname">记录名称</param>
        /// <param name="type">Query type</param>
        /// <returns></returns>
        private DnsServerResponse GetResponse(String qname, DnsRecordType type)
        {
            DnsServerResponse rlt = null;
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp))
            {
                var buffer = new byte[512];
                var sendLength = CreateQuery(buffer, requestID++, qname, type, 1);
                socket.ReceiveTimeout = 500;
                foreach (var serverIP in serverIPs)
                {
                    try
                    {
                        socket.Connect(serverIP, 53);
                        if (socket.Send(buffer, sendLength, SocketFlags.None) > 0)
                        {
                            var response = new byte[1024];
                            if (socket.Receive(response) > 0)
                            {
                                rlt = ParseQuery(response);
                                socket.Shutdown(SocketShutdown.Both);
                                socket.Dispose();
                                break;
                            }
                        }
                    }
                    catch { }
                }
            }
            return rlt;
        }

        /// <summary>
        /// Creates binary query.
        /// </summary>
        /// <param name="buffer">Buffer where to store query.</param>
        /// <param name="ID">Query ID.</param>
        /// <param name="qname">Query text.</param>
        /// <param name="qtype">Query type.</param>
        /// <param name="qclass">Query class.</param>
        /// <returns>Returns number of bytes stored to <b>buffer</b>.</returns>
        private static int CreateQuery(byte[] buffer, int ID, string qname, DnsRecordType qtype, int qclass)
        {
            // DNS协议参考http://www.cnblogs.com/topdog/archive/2011/11/15/2250185.html

            #region DNS协议说明
            /*  

            DNS结构：分为5个部分，分别为Header、Question、Answer、Authority、Additional。
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            |                    Header                     |
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            |                   Question                    |
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            |                    Answer                     |
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            |                   Authority                   |
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            |                   Additional                  |
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            其中头部的大小是固定的为12字节。这5个部分不是全部都是必须的，在向服务器发送查询请求的时候，只需要前2个。回复的时候也不一定包含5个（按查询的内容和返回的信息而定）。



            Header 部分：
                                          1  1  1  1  1  1
            0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            |                      ID                       |
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            |QR|   Opcode  |AA|TC|RD|RA|   Z    |   RCODE   |
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            |                    QDCOUNT                    |
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            |                    ANCOUNT                    |
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            |                    NSCOUNT                    |
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            |                    ARCOUNT                    |
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

            ID:长度为16位，是一个用户发送查询的时候定义的随机数，当服务器返回结果的时候，返回包的ID与用户发送的一致。

            QR:长度1位，值0是请求，1是应答。

            Opcode:长度4位，值0是标准查询，1是反向查询，2死服务器状态查询。

            AA:长度1位，授权应答(Authoritative Answer) - 这个比特位在应答的时候才有意义，指出给出应答的服务器是查询域名的授权解析服务器。

            TC:长度1位，截断(TrunCation) - 用来指出报文比允许的长度还要长，导致被截断。

            RD:长度1位，期望递归(Recursion Desired) - 这个比特位被请求设置，应答的时候使用的相同的值返回。如果设置了RD，就建议域名服务器进行递归解析，递归查询的支持是可选的。

            RA:长度1位，支持递归(Recursion Available) - 这个比特位在应答中设置或取消，用来代表服务器是否支持递归查询。

            Z:长度3位，保留值，值为0.

            RCode:长度4位，应答码，类似http的stateCode一样，值0没有错误、1格式错误、2服务器错误、3名字错误、4服务器不支持、5拒绝。

            QDCount:长度16位，报文请求段中的问题记录数。

            ANCount:长度16位，报文回答段中的回答记录数。

            NSCOUNT :长度16位，报文授权段中的授权记录数。

            ARCOUNT :长度16位，报文附加段中的附加记录数。


            Question 部分：这部分的内容是你要查询的内容。

                                             1  1  1  1  1  1
            0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            |                                               |
            /                     QNAME                     /
            /                                               /
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            |                     QTYPE                     |
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            |                     QCLASS                    |
            +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

            QName：是你要查询的域名，属于不定长字段。他的格式是“长度（1字节）+N字节内容
            （N由前面的长度定义）+～～～+长度0。以一个长度单位N为开始，然后连续的N字节为
            其内容，然后又是一个N2长度的一字节，然后后面又是N2个字节内容，直到遇到长度
            为0的长度标记。记录类型：

            A=0x01, //指定计算机 IP 地址。 
            NS=0x02, //指定用于命名区域的 DNS 名称服务器。 
            MD=0x03, //指定邮件接收站（此类型已经过时了，使用MX代替） 
            MF=0x04, //指定邮件中转站（此类型已经过时了，使用MX代替） 
            CNAME=0x05, //指定用于别名的规范名称。 
            SOA=0x06, //指定用于 DNS 区域的“起始授权机构”。 
            MB=0x07, //指定邮箱域名。 
            MG=0x08, //指定邮件组成员。 
            MR=0x09, //指定邮件重命名域名。 
            NULL=0x0A, //指定空的资源记录 
            WKS=0x0B, //描述已知服务。 
            PTR=0x0C, //如果查询是 IP 地址，则指定计算机名；否则指定指向其它信息的指针。 
            HINFO=0x0D, //指定计算机 CPU 以及操作系统类型。 
            MINFO=0x0E, //指定邮箱或邮件列表信息。 
            MX=0x0F, //指定邮件交换器。 
            TXT=0x10, //指定文本信息。 
            UINFO=0x64, //指定用户信息。 
            UID=0x65, //指定用户标识符。 
            GID=0x66, //指定组名的组标识符。 
            ANY=0xFF //指定所有数据类型。 

            */
            #endregion





            //--------- Header 部分 -----------------------------------//
            buffer[0] = (byte)(ID >> 8);
            buffer[1] = (byte)(ID & 0xFF);
            buffer[2] = (byte)1;
            buffer[3] = (byte)0;
            buffer[4] = (byte)0;
            buffer[5] = (byte)1;
            buffer[6] = (byte)0;
            buffer[7] = (byte)0;
            buffer[8] = (byte)0;
            buffer[9] = (byte)0;
            buffer[10] = (byte)0;
            buffer[11] = (byte)0;
            //---------------------------------------------------------//


            //----Create query ------------------------------------//

            // Convert unicode domain name. For more info see RFC 5890.
            System.Globalization.IdnMapping ldn = new System.Globalization.IdnMapping();
            qname = ldn.GetAscii(qname);

            string[] labels = qname.Split(new char[] { '.' });
            int position = 12;

            // Copy all domain parts(labels) to query
            // eg. lumisoft.ee = 2 labels, lumisoft and ee.
            // format = label.length + label(bytes)
            foreach (string label in labels)
            {
                // convert label string to byte array
                byte[] b = Encoding.ASCII.GetBytes(label);

                // add label lenght to query
                buffer[position++] = (byte)(b.Length);
                b.CopyTo(buffer, position);

                // Move position by label length
                position += b.Length;
            }

            // Terminate domain (see note above)
            buffer[position++] = (byte)0;

            // Set QTYPE 
            buffer[position++] = (byte)0;
            buffer[position++] = (byte)qtype;

            // Set QCLASS
            buffer[position++] = (byte)0;
            buffer[position++] = (byte)qclass;
            //-------------------------------------------------------//

            return position;
        }


        #region method ParseQuery

        /// <summary>
        /// Parses query.
        /// </summary>
        /// <param name="reply">Dns server reply.</param>
        /// <returns></returns>
        private DnsServerResponse ParseQuery(byte[] reply)
        {
            //--- Parse headers ------------------------------------//

            /* RFC 1035 4.1.1. Header section format

                                            1  1  1  1  1  1
              0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
             +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
             |                      ID                       |
             +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
             |QR|   Opcode  |AA|TC|RD|RA|   Z    |   RCODE   |
             +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
             |                    QDCOUNT                    |
             +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
             |                    ANCOUNT                    |
             +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
             |                    NSCOUNT                    |
             +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
             |                    ARCOUNT                    |
             +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+

            QDCOUNT
                an unsigned 16 bit integer specifying the number of
                entries in the question section.

            ANCOUNT
                an unsigned 16 bit integer specifying the number of
                resource records in the answer section.

            NSCOUNT
                an unsigned 16 bit integer specifying the number of name
                server resource records in the authority records section.

            ARCOUNT
                an unsigned 16 bit integer specifying the number of
                resource records in the additional records section.

            */

            // Get reply code
            int id = (reply[0] << 8 | reply[1]);
            OpCode opcode = (OpCode)((reply[2] >> 3) & 15);
            ResponceCode replyCode = (ResponceCode)(reply[3] & 15);
            int queryCount = (reply[4] << 8 | reply[5]);
            int answerCount = (reply[6] << 8 | reply[7]);
            int authoritiveAnswerCount = (reply[8] << 8 | reply[9]);
            int additionalAnswerCount = (reply[10] << 8 | reply[11]);
            //---- End of headers ---------------------------------//

            int pos = 12;

            //----- Parse question part ------------//
            for (int q = 0; q < queryCount; q++)
            {
                string dummy = "";
                GetQName(reply, ref pos, ref dummy);
                //qtype + qclass
                pos += 4;
            }
            //--------------------------------------//

            // 1) parse answers
            // 2) parse authoritive answers
            // 3) parse additional answers
            List<DnsRecord> answers = ParseAnswers(reply, answerCount, ref pos);
            List<DnsRecord> authoritiveAnswers = ParseAnswers(reply, authoritiveAnswerCount, ref pos);
            List<DnsRecord> additionalAnswers = ParseAnswers(reply, additionalAnswerCount, ref pos);

            return new DnsServerResponse(true, id, replyCode, answers, authoritiveAnswers, additionalAnswers);
        }
        #endregion

        #region method ParseAnswers

        /// <summary>
        /// Parses specified count of answers from query.
        /// </summary>
        /// <param name="reply">Server returned query.</param>
        /// <param name="answerCount">Number of answers to parse.</param>
        /// <param name="offset">Position from where to start parsing answers.</param>
        /// <returns></returns>
        private List<DnsRecord> ParseAnswers(byte[] reply, int answerCount, ref int offset)
        {
            /* RFC 1035 4.1.3. Resource record format
			 
										   1  1  1  1  1  1
			 0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                                               |
			/                                               /
			/                      NAME                     /
			|                                               |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                      TYPE                     |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                     CLASS                     |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                      TTL                      |
			|                                               |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                   RDLENGTH                    |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--|
			/                     RDATA                     /
			/                                               /
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			*/

            var answers = new List<DnsRecord>();
            //---- Start parsing answers ------------------------------------------------------------------//
            for (int i = 0; i < answerCount; i++)
            {
                string name = "";
                if (!GetQName(reply, ref offset, ref name))
                {
                    break;
                }

                int type = reply[offset++] << 8 | reply[offset++];
                int rdClass = reply[offset++] << 8 | reply[offset++];
                int ttl = reply[offset++] << 24 | reply[offset++] << 16 | reply[offset++] << 8 | reply[offset++];
                int rdLength = reply[offset++] << 8 | reply[offset++];

                var _type = (DnsRecordType)type;
                if (_type == DnsRecordType.A)
                {
                    answers.Add(DnsRecord_A.Parse(name, reply, ref offset, rdLength, ttl));
                }
                else if (_type == DnsRecordType.AAAA)
                {
                    answers.Add(DnsRecord_AAAA.Parse(name, reply, ref offset, rdLength, ttl));
                }
                else if (_type == DnsRecordType.NS)
                {
                    answers.Add(DnsRecord_NS.Parse(name, reply, ref offset, rdLength, ttl));
                }
                else if (_type == DnsRecordType.CNAME)
                {
                    answers.Add(DnsRecord_CNAME.Parse(name, reply, ref offset, rdLength, ttl));
                }
                else if (_type == DnsRecordType.MX)
                {
                    answers.Add(DnsRecord_MX.Parse(name, reply, ref offset, rdLength, ttl));
                }
                else if (_type == DnsRecordType.TXT)
                {
                    answers.Add(DnsRecord_TXT.Parse(name, reply, ref offset, rdLength, ttl));
                }
                else if (_type == DnsRecordType.SOA)
                {
                    answers.Add(DnsRecord_SOA.Parse(name, reply, ref offset, rdLength, ttl));
                }
                else if (_type == DnsRecordType.PTR)
                {
                    answers.Add(DnsRecord_PTR.Parse(name, reply, ref offset, rdLength, ttl));
                }
                else if (_type == DnsRecordType.HINFO)
                {
                    answers.Add(DnsRecord_HINFO.Parse(name, reply, ref offset, rdLength, ttl));
                }
                else if (_type == DnsRecordType.SRV)
                {
                    answers.Add(DnsRecord_SRV.Parse(name, reply, ref offset, rdLength, ttl));
                }
                else if (_type == DnsRecordType.NAPTR)
                {
                    answers.Add(DnsRecord_NAPTR.Parse(name, reply, ref offset, rdLength, ttl));
                }
                else if (_type == DnsRecordType.SPF)
                {
                    answers.Add(DnsRecord_SPF.Parse(name, reply, ref offset, rdLength, ttl));
                }
                else
                {
                    // Unknown record, skip it.
                    offset += rdLength;
                }
            }

            return answers;
        }

        #endregion

        #region method ReadCharacterString

        /// <summary>
        /// Reads character-string from spefcified data and offset.
        /// </summary>
        /// <param name="data">Data from where to read.</param>
        /// <param name="offset">Offset from where to start reading.</param>
        /// <returns>Returns readed string.</returns>
        internal static string ReadCharacterString(byte[] data, ref int offset)
        {
            /* RFC 1035 3.3.
                <character-string> is a single length octet followed by that number of characters. 
                <character-string> is treated as binary information, and can be up to 256 characters 
                in length (including the length octet).
            */

            int dataLength = (int)data[offset++];
            string retVal = Encoding.ASCII.GetString(data, offset, dataLength);
            offset += dataLength;

            return retVal;
        }

        #endregion

        #region method GetQName
        internal static bool GetQName(byte[] reply, ref int offset, ref string name)
        {
            bool retVal = GetQNameI(reply, ref offset, ref name);

            // Convert domain name to unicode. For more info see RFC 5890.
            System.Globalization.IdnMapping ldn = new System.Globalization.IdnMapping();
            name = ldn.GetUnicode(name);

            return retVal;
        }

        private static bool GetQNameI(byte[] reply, ref int offset, ref string name)
        {
            try
            {
                while (true)
                {
                    // Invalid DNS packet, offset goes beyound reply size, probably terminator missing.
                    if (offset >= reply.Length)
                    {
                        return false;
                    }
                    // We have label terminator "0".
                    if (reply[offset] == 0)
                    {
                        break;
                    }

                    // Check if it's pointer(In pointer first two bits always 1)
                    bool isPointer = ((reply[offset] & 0xC0) == 0xC0);

                    // If pointer
                    if (isPointer)
                    {
                        /* Pointer location number is 2 bytes long
						    0 | 1 | 2 | 3 | 4 | 5 | 6 | 7  # byte 2 # 0 | 1 | 2 | | 3 | 4 | 5 | 6 | 7
						    empty | < ---- pointer location number --------------------------------->
                        */
                        int pStart = ((reply[offset] & 0x3F) << 8) | (reply[++offset]);
                        offset++;

                        return GetQNameI(reply, ref pStart, ref name);
                    }
                    else
                    {
                        /* Label length (length = 8Bit and first 2 bits always 0)
						    0 | 1 | 2 | 3 | 4 | 5 | 6 | 7
						    empty | lablel length in bytes 
                        */
                        int labelLength = (reply[offset] & 0x3F);
                        offset++;

                        // Copy label into name 
                        name += Encoding.UTF8.GetString(reply, offset, labelLength);
                        offset += labelLength;
                    }

                    // If the next char isn't terminator, label continues - add dot between two labels.
                    if (reply[offset] != 0)
                    {
                        name += ".";
                    }
                }

                // Move offset by terminator length.
                offset++;

                return true;
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}