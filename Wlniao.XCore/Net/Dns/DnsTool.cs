using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wlniao.Net.Dns
{
    /// <summary>
    /// DNS 查询工具。内置公共 DNS 兜底，可指定服务器与端口；查询失败时自动顺延到下一台服务器。
    /// 实现 <c>IDisposable</c>：释放后继续使用会抛 <c>ObjectDisposedException</c>。
    /// </summary>
    public class DnsTool : System.IDisposable
    {
        private int _requestId = 0;
        private bool _disposed = false; // 用于检测重复释放
        private readonly IPEndPoint[] _dnsServers;
        
        /// <summary>
        /// 
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            System.GC.SuppressFinalize(this);
        }
        
        /// <summary>
        /// 
        /// </summary>
        /// <param name="disposing"></param>
        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                // 清理资源
                _disposed = true;
            }
        }

        /// <summary>
        /// 单个 DNS 查询等待应答的最长时间（毫秒），超时后自动尝试下一台服务器。
        /// </summary>
        public const int TimeoutMilliseconds = 15000;

        /// <summary>
        /// 使用内置的公共 DNS 服务器（阿里 / 腾讯 / Google / 114），端口 53。
        /// </summary>
        public DnsTool()
        {
            _dnsServers = new[]
            {
                new IPEndPoint(IPAddress.Parse("223.5.5.5"), 53),       //阿里 AliDNS
                new IPEndPoint(IPAddress.Parse("119.29.29.29"), 53),    //腾讯公共DNS
                new IPEndPoint(IPAddress.Parse("8.8.8.8"), 53),         //Google DNS
                new IPEndPoint(IPAddress.Parse("114.114.114.114"), 53)  //114 DNS
            };
        }
        /// <summary>
        /// 使用指定的 DNS 服务器地址，端口 53。
        /// </summary>
        /// <param name="ServerIPs">DNS 服务器 IP 列表，按顺序尝试</param>
        public DnsTool(IPAddress[] ServerIPs)
        {
            if (ServerIPs == null || ServerIPs.Length == 0)
            {
                throw new System.ArgumentException("至少需要指定一台 DNS 服务器", nameof(ServerIPs));
            }
            _dnsServers = new IPEndPoint[ServerIPs.Length];
            for (var i = 0; i < ServerIPs.Length; i++)
            {
                _dnsServers[i] = new IPEndPoint(ServerIPs[i], 53);
            }
        }
        /// <summary>
        /// 使用指定的 DNS 服务器终结点，可自定义端口（用于内网 DNS、dnsmasq、测试桩等监听非 53 端口的场景）。
        /// </summary>
        /// <param name="dnsServers">DNS 服务器终结点列表，按顺序尝试</param>
        public DnsTool(IPEndPoint[] dnsServers)
        {
            if (dnsServers == null || dnsServers.Length == 0)
            {
                throw new System.ArgumentException("至少需要指定一台 DNS 服务器", nameof(dnsServers));
            }
            _dnsServers = (IPEndPoint[])dnsServers.Clone();
        }
        /// <summary>
        /// 使用单个 DNS 服务器的 IP 或主机名，端口 53。
        /// </summary>
        /// <param name="ServerIPorHost">DNS 服务器 IP 或主机名</param>
        public DnsTool(string ServerIPorHost)
        {
            if (Text.StringUtil.IsIP(ServerIPorHost))
            {
                var serverIP = IPAddress.Parse(ServerIPorHost);
                if (serverIP.IsIPv4MappedToIPv6)
                {
                    serverIP = serverIP.MapToIPv4();
                }
                _dnsServers = new[] { new IPEndPoint(serverIP, 53) };
            }
            else
            {
                _dnsServers = new[] { new IPEndPoint(new DnsTool().GetIPAddress(ServerIPorHost), 53) };
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qname"></param>
        /// <returns></returns>
        public IPAddress GetIPAddressDefault(string qname)
        {
            // 使用异步版本的同步包装
            return GetIPAddressDefaultAsync(qname).GetAwaiter().GetResult();
        }
        
        /// <summary>
        /// 异步获取IP地址
        /// </summary>
        /// <param name="qname"></param>
        /// <returns></returns>
        public async Task<IPAddress> GetIPAddressDefaultAsync(string qname)
        {
            try
            {
                var addresses = await System.Net.Dns.GetHostAddressesAsync(qname);
                if (addresses.Length > 0)
                {
                    return addresses[0];
                }
            }
            catch (System.Exception e)
            {
                Wlniao.Log.Loger.Error($"DNS解析异常: {e.Message}, 堆栈: {e.StackTrace}");
            }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="qname"></param>
        /// <param name="type"></param>
        /// <returns></returns>
        public IPAddress GetIPAddress(string qname, DnsRecordType type = DnsRecordType.A)
        {
            if (qname.IndexOf('.') > 0)
            {
                var res = GetResponse(qname, type);
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
                catch (System.Exception e)
                {
                    Wlniao.Log.Loger.Error($"DNS解析异常: {e.Message}, 堆栈: {e.StackTrace}");
                }
                return address;
            }
            return null;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="qname"></param>
        /// <returns></returns>
        public string GetCNAME(string qname)
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
        /// 查询指定域名的第一条 TXT 记录。
        /// </summary>
        /// <param name="qname">记录名称，例如 example.com 或 _dmarc.example.com</param>
        /// <returns>
        /// 第一条 TXT 记录的文本内容；单条记录超过 255 字节会被权威服务器拆成多个
        /// &lt;character-string&gt;，这里已按 RFC 1035 3.3.14 依次读出并拼接为完整值。
        /// 不存在记录或所有 DNS 服务器不可达时返回 null。
        /// </returns>
        public string GetTXT(string qname)
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
        /// 查询指定域名的全部 TXT 记录。
        /// </summary>
        /// <param name="qname">记录名称，例如 example.com 或 _dmarc.example.com</param>
        /// <returns>
        /// 按应答顺序返回每条 TXT 记录的文本；每条内部的多段
        /// &lt;character-string&gt; 已各自拼接为完整值。
        /// 一个域名常同时存在多条 TXT（SPF、域名所有权验证、DKIM 等），
        /// <see cref="GetTXT"/> 只取第一条，本方法返回全部。
        /// 不存在记录或所有 DNS 服务器不可达时返回空数组。
        /// </returns>
        public string[] GetTXTs(string qname)
        {
            var res = GetResponse(qname, DnsRecordType.TXT);
            if (res == null)
            {
                return Array.Empty<string>();
            }
            var records = res.GetTXTRecords();
            if (records == null || records.Length == 0)
            {
                return Array.Empty<string>();
            }
            var retVal = new string[records.Length];
            for (var i = 0; i < records.Length; i++)
            {
                retVal[i] = records[i].Text;
            }
            return retVal;
        }

        /// <summary>
        /// 异步查询指定域名的第一条 TXT 记录。
        /// </summary>
        /// <param name="qname">记录名称，例如 example.com 或 _dmarc.example.com</param>
        /// <param name="cancellationToken">取消标记</param>
        /// <returns>第一条 TXT 记录的文本内容；不存在或查询失败时返回 null</returns>
        public async Task<string> GetTXTAsync(string qname, CancellationToken cancellationToken = default)
        {
            var res = await GetResponseAsync(qname, DnsRecordType.TXT, cancellationToken).ConfigureAwait(false);
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
        /// 异步查询指定域名的全部 TXT 记录。
        /// </summary>
        /// <param name="qname">记录名称，例如 example.com 或 _dmarc.example.com</param>
        /// <param name="cancellationToken">取消标记</param>
        /// <returns>按应答顺序返回每条 TXT 记录的文本；不存在或查询失败时返回空数组</returns>
        public async Task<string[]> GetTXTsAsync(string qname, CancellationToken cancellationToken = default)
        {
            var res = await GetResponseAsync(qname, DnsRecordType.TXT, cancellationToken).ConfigureAwait(false);
            if (res == null)
            {
                return Array.Empty<string>();
            }
            var records = res.GetTXTRecords();
            if (records == null || records.Length == 0)
            {
                return Array.Empty<string>();
            }
            var retVal = new string[records.Length];
            for (var i = 0; i < records.Length; i++)
            {
                retVal[i] = records[i].Text;
            }
            return retVal;
        }

        /// <summary>
        /// 获取服务器输出
        /// </summary>
        /// <param name="qname">记录名称</param>
        /// <param name="type">Query type</param>
        /// <returns></returns>
        private DnsServerResponse GetResponse(string qname, DnsRecordType type)
        {
            if (_disposed) throw new System.ObjectDisposedException(nameof(DnsTool));

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            socket.ReceiveTimeout = TimeoutMilliseconds;

            var buffer = new byte[512];
            var requestId = NextRequestId();
            var sendLength = CreateQuery(buffer, requestId, qname, type, 1);

            foreach (var endPoint in _dnsServers)
            {
                try
                {
                    socket.Connect(endPoint);

                    if (socket.Send(buffer, sendLength, SocketFlags.None) > 0)
                    {
                        var response = new byte[1024];
                        var received = socket.Receive(response);

                        if (IsValidReply(response, received, requestId))
                        {
                            var parsed = ParseReply(response, received);
                            if (parsed != null)
                            {
                                return parsed;
                            }
                        }
                    }
                }
                catch (SocketException)
                {
                    // 继续尝试下一个DNS服务器
                    continue;
                }
            }

            return null;
        }

        /// <summary>
        /// 异步获取服务器输出，逐个尝试已配置的 DNS 服务器直到拿到应答。
        /// </summary>
        /// <param name="qname">记录名称</param>
        /// <param name="type">Query type</param>
        /// <param name="cancellationToken">取消标记，取消后不再尝试后续服务器</param>
        /// <returns>解析后的应答；全部服务器都不可达或超时时返回 null</returns>
        private async Task<DnsServerResponse> GetResponseAsync(string qname, DnsRecordType type, CancellationToken cancellationToken = default)
        {
            if (_disposed) throw new System.ObjectDisposedException(nameof(DnsTool));

            using var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);

            var buffer = new byte[512];
            var requestId = NextRequestId();
            var sendLength = CreateQuery(buffer, requestId, qname, type, 1);

            foreach (var endPoint in _dnsServers)
            {
                try
                {
                    socket.Connect(endPoint);

                    if (await socket.SendAsync(new ArraySegment<byte>(buffer, 0, sendLength), SocketFlags.None).ConfigureAwait(false) > 0)
                    {
                        var response = new byte[1024];
                        int received;
                        // 连接态 UDP 的 ReceiveTimeout 对 async 不生效，改用 CancellationToken 做超时控制
                        using (var timeoutSource = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken))
                        {
                            timeoutSource.CancelAfter(TimeoutMilliseconds);
                            received = await socket.ReceiveAsync(new Memory<byte>(response), SocketFlags.None, timeoutSource.Token).ConfigureAwait(false);
                        }

                        if (IsValidReply(response, received, requestId))
                        {
                            var parsed = ParseReply(response, received);
                            if (parsed != null)
                            {
                                return parsed;
                            }
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // 等待应答超时（或调用方主动取消），继续尝试下一个DNS服务器
                    if (cancellationToken.IsCancellationRequested)
                    {
                        throw;
                    }
                    continue;
                }
                catch (SocketException)
                {
                    // 继续尝试下一个DNS服务器
                    continue;
                }
            }

            return null;
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
            var ldn = new System.Globalization.IdnMapping();
            qname = ldn.GetAscii(qname);

            var labels = qname.Split(new char[] { '.' });
            var position = 12;

            // Copy all domain parts(labels) to query
            // eg. lumisoft.ee = 2 labels, lumisoft and ee.
            // format = label.length + label(bytes)
            foreach (var label in labels)
            {
                // convert label string to byte array
                var b = Encoding.ASCII.GetBytes(label);

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
        /// DNS 报文头长度（RFC 1035 4.1.1），少于该长度的报文不存在任何可解析内容。
        /// </summary>
        private const int HeaderLength = 12;

        /// <summary>
        /// 取下一个查询用的事务 ID。
        /// </summary>
        /// <remarks>
        /// 报文头只留 16 位给事务 ID（RFC 1035 4.1.1），这里直接取低 16 位返回，
        /// 让「写进报文的值」与「校验应答时比较的值」在构造上保持一致；
        /// 若把自增计数器的完整值一路传下去，跨过 65536 次查询后两边就再也不会相等。
        /// </remarks>
        private int NextRequestId()
        {
            return System.Threading.Interlocked.Increment(ref _requestId) & 0xFFFF;
        }

        /// <summary>
        /// 校验收到的 UDP 报文是否为本查询的有效应答。
        /// </summary>
        /// <remarks>
        /// 三个条件缺一不可：
        /// <list type="number">
        /// <item>长度至少覆盖 12 字节固定头部 —— 否则后续按偏移读字段会读到缓冲区里补零的垃圾，</item>
        /// <item>QR 位为 1 —— 只接受应答，避免把回环的查询报文当成应答，</item>
        /// <item>事务 ID 与本次请求一致（按 16 位比较，容纳计数器回绕）—— 连接态 UDP 端口可被猜中，ID 是最后一道校验。</item>
        /// </list>
        /// </remarks>
        /// <param name="reply">收到的报文</param>
        /// <param name="length">报文实际长度（UDP 接收可能远小于缓冲区）</param>
        /// <param name="expectedId">本次查询发出的事务 ID</param>
        private static bool IsValidReply(byte[] reply, int length, int expectedId)
        {
            if (reply == null || length < HeaderLength || length > reply.Length)
            {
                return false;
            }
            if ((reply[2] & 0x80) == 0)
            {
                return false;
            }
            // 报文里的事务 ID 只有 16 位，而调用方传入的是自增计数器：
            // 超过 65535 次查询后线上值已回绕、比较值若不取低 16 位则永不相等，
            // 常驻实例会在此后把每一条合法应答都判成伪造且无法自愈。
            return ((reply[0] << 8) | reply[1]) == (expectedId & 0xFFFF);
        }

        /// <summary>
        /// 把报文裁剪到实际长度后解析，并吞掉畸形报文引发的解析异常。
        /// </summary>
        /// <remarks>
        /// 截断的 / 伪造的报文只应被视为「这台服务器没给出可用应答」，交由调用方顺延到下一台服务器，
        /// 不应把解析异常抛给业务调用方 —— 否则一次 DNS 污染就能让上层的名称解析直接崩掉。
        /// </remarks>
        /// <param name="reply">收到的报文</param>
        /// <param name="length">报文实际长度</param>
        /// <returns>解析结果；报文畸形时返回 null</returns>
        private DnsServerResponse ParseReply(byte[] reply, int length)
        {
            byte[] data;
            if (length == reply.Length)
            {
                data = reply;
            }
            else
            {
                // 只把实际收到的字节交给解析器：缓冲区剩余的补零部分不是报文内容
                data = new byte[length];
                System.Array.Copy(reply, data, length);
            }

            try
            {
                return ParseQuery(data);
            }
            catch (Exception ex) when (IsMalformedDataError(ex))
            {
                return null;
            }
        }

        /// <summary>
        /// 判断异常是否属于「报文畸形」这一类，而非程序缺陷。
        /// </summary>
        /// <remarks>
        /// 只有这一类异常可以被就地消化（丢弃报文 / 跳过单条记录）；
        /// 其余异常照常向上抛，避免把真正的代码缺陷悄悄吞进解析结果里。
        /// </remarks>
        /// <param name="ex">解析过程中抛出的异常</param>
        private static bool IsMalformedDataError(Exception ex)
        {
            return ex is ArgumentException || ex is IndexOutOfRangeException
                || ex is InvalidCastException || ex is FormatException || ex is OverflowException
                || ex is NotSupportedException;
        }

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
            // 头部至少 12 字节；不足说明报文被截断，按偏移读只会读到越界或垃圾数据
            if (reply == null || reply.Length < HeaderLength)
            {
                return null;
            }
            var id = (reply[0] << 8 | reply[1]);
            var opcode = (OpCode)((reply[2] >> 3) & 15);
            var replyCode = (ResponceCode)(reply[3] & 15);
            var queryCount = (reply[4] << 8 | reply[5]);
            var answerCount = (reply[6] << 8 | reply[7]);
            var authoritiveAnswerCount = (reply[8] << 8 | reply[9]);
            var additionalAnswerCount = (reply[10] << 8 | reply[11]);
            //---- End of headers ---------------------------------//

            var pos = 12;

            //----- Parse question part ------------//
            for (var q = 0; q < queryCount; q++)
            {
                var dummy = "";
                if (!GetQName(reply, ref pos, ref dummy) || pos + 4 > reply.Length)
                {
                    return null;
                }
                //qtype + qclass
                pos += 4;
            }
            //--------------------------------------//

            // 1) parse answers
            // 2) parse authoritive answers
            // 3) parse additional answers
            var answers = ParseAnswers(reply, answerCount, ref pos);
            var authoritiveAnswers = ParseAnswers(reply, authoritiveAnswerCount, ref pos);
            var additionalAnswers = ParseAnswers(reply, additionalAnswerCount, ref pos);

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
            for (var i = 0; i < answerCount; i++)
            {
                // NAME 至少 1 字节终止符，TYPE/CLASS/TTL/RDLENGTH 共 10 字节
                if (offset < 0 || offset + 11 > reply.Length)
                {
                    break;
                }

                var name = "";
                if (!GetQName(reply, ref offset, ref name))
                {
                    break;
                }
                if (offset + 10 > reply.Length)
                {
                    break;
                }

                var type = reply[offset++] << 8 | reply[offset++];
                var rdClass = reply[offset++] << 8 | reply[offset++];
                var ttl = reply[offset++] << 24 | reply[offset++] << 16 | reply[offset++] << 8 | reply[offset++];
                var rdLength = reply[offset++] << 8 | reply[offset++];

                // RDATA 声明长度超出报文剩余部分，说明报文在记录中间被截断：
                // 这条记录及其后所有记录都不完整，继续解析只会读到缓冲区的补零部分。
                if (offset + rdLength > reply.Length)
                {
                    break;
                }

                // RDATA 的真实边界由 RDLENGTH 决定：各类型的 Parse 只负责解释内容，
                // 收尾统一在这里把游标对到 RDATA 末尾，避免某个类型少读几个字节后
                // 后续记录全部错位（压缩域名只占 2 字节、而 RDLENGTH 可能更大就是典型场景）。
                var rdataEnd = offset + rdLength;

                var _type = (DnsRecordType)type;
                try
                {
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
                    // 未知类型不解释 RDATA，交给 finally 把游标对到边界即可
                }
                catch (Exception ex) when (IsMalformedDataError(ex))
                {
                    // RDATA 畸形只作废这一条记录：RDLENGTH 已经定出它的边界，
                    // 后面记录的位置依然算得出来；若让异常穿出去，整份报文会被丢弃，
                    // 一条坏记录就能连带抹掉同一包里所有合法应答。
                }
                finally
                {
                    // 统一把游标对到 RDATA 末尾：Parse 少读时补齐，多读时回退到边界，
                    // 两种情况都不能让它吃掉（或漏掉）后面的记录；解析中途抛异常时
                    // 也由这里对齐，否则后续记录会全部错位。
                    offset = rdataEnd;
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
            return ReadCharacterString(data, ref offset, Encoding.ASCII);
        }

        /// <summary>
        /// Reads character-string from spefcified data and offset, bounded by <paramref name="endOffset"/>.
        /// </summary>
        /// <param name="data">Data from where to read.</param>
        /// <param name="offset">Offset from where to start reading.</param>
        /// <param name="endOffset">本条记录 RDATA 的结束位置（不含），超出即非法。</param>
        /// <returns>Returns readed string.</returns>
        internal static string ReadCharacterString(byte[] data, ref int offset, int endOffset)
        {
            return ReadCharacterString(data, ref offset, Encoding.ASCII, endOffset);
        }

        /// <summary>
        /// Reads character-string from spefcified data and offset.
        /// </summary>
        /// <param name="data">Data from where to read.</param>
        /// <param name="offset">Offset from where to start reading.</param>
        /// <param name="encoding">字符解码方式。RFC 1035 时代默认 ASCII；TXT 这类承载任意文本的
        /// 记录类型在现代用法中按 UTF-8 解释，由调用方显式指定。</param>
        /// <returns>Returns readed string.</returns>
        internal static string ReadCharacterString(byte[] data, ref int offset, Encoding encoding)
        {
            return ReadCharacterString(data, ref offset, encoding, data.Length);
        }

        /// <summary>
        /// Reads character-string from spefcified data and offset, bounded by <paramref name="endOffset"/>.
        /// </summary>
        /// <remarks>
        /// 边界必须是所在 RDATA 的末尾，而不是整个报文长度：畸形记录可以把长度字节写得比 RDATA 剩余部分更大，
        /// 若按报文长度截断，读出的内容会越过本条记录、吞掉后面记录的字节，导致后续记录全部错位。
        /// </remarks>
        /// <param name="data">Data from where to read.</param>
        /// <param name="offset">Offset from where to start reading.</param>
        /// <param name="encoding">字符解码方式。</param>
        /// <param name="endOffset">本条记录 RDATA 的结束位置（不含），超出即非法。</param>
        /// <returns>Returns readed string.</returns>
        internal static string ReadCharacterString(byte[] data, ref int offset, Encoding encoding, int endOffset)
        {
            /* RFC 1035 3.3.
                <character-string> is a single length octet followed by that number of characters. 
                <character-string> is treated as binary information, and can be up to 256 characters 
                in length (including the length octet).
            */

            // 游标已经到达（或越过）RDATA 边界：不再有字符，也不能读边界外的长度字节
            if (offset < 0 || offset >= endOffset || offset >= data.Length)
            {
                return "";
            }

            var dataLength = (int)data[offset++];
            var available = Math.Min(endOffset, data.Length) - offset;
            if (available <= 0)
            {
                return "";
            }
            // 报文或 RDATA 被截断时不要越界读到最后一段
            if (dataLength > available)
            {
                dataLength = available;
            }
            var retVal = encoding.GetString(data, offset, dataLength);
            offset += dataLength;

            return retVal;
        }

        #endregion

        #region method GetQName

        /// <summary>
        /// 域名压缩指针允许的最大跳转深度。
        /// </summary>
        /// <remarks>
        /// RFC 1035 允许指针指向报文任意位置，因此畸形报文可以构造出指针成环（A→B→A）。
        /// 递归解析时无上限，环会一直展开直到线程栈耗尽 —— StackOverflowException 无法被
        /// catch 捕获，进程直接终止。32 层已远超正常报文所需（真实响应通常只跳 1~2 次），
        /// 超出即判定报文非法。
        /// </remarks>
        private const int MaxCompressionDepth = 32;

        internal static bool GetQName(byte[] reply, ref int offset, ref string name)
        {
            if (!GetQNameI(reply, ref offset, ref name, 0))
            {
                return false;
            }

            // 根域名（QNAME 直接以 0 终止）为空串，IdnMapping 不接受空入参
            if (name.Length == 0)
            {
                return true;
            }

            // Convert domain name to unicode. For more info see RFC 5890.
            try
            {
                var ldn = new System.Globalization.IdnMapping();
                name = ldn.GetUnicode(name);
            }
            catch (ArgumentException)
            {
                return false;
            }

            return true;
        }

        private static bool GetQNameI(byte[] reply, ref int offset, ref string name, int depth)
        {
            // 指针链深度超限：报文畸形，直接判定失败而不是继续展开
            if (depth > MaxCompressionDepth)
            {
                return false;
            }

            try
            {
                while (true)
                {
                    // Invalid DNS packet, offset goes beyound reply size, probably terminator missing.
                    if (offset < 0 || offset >= reply.Length)
                    {
                        return false;
                    }
                    // We have label terminator "0".
                    if (reply[offset] == 0)
                    {
                        break;
                    }

                    // Check if it's pointer(In pointer first two bits always 1)
                    var isPointer = ((reply[offset] & 0xC0) == 0xC0);

                    // If pointer
                    if (isPointer)
                    {
                        /* Pointer location number is 2 bytes long
						    0 | 1 | 2 | 3 | 4 | 5 | 6 | 7  # byte 2 # 0 | 1 | 2 | | 3 | 4 | 5 | 6 | 7
						    empty | < ---- pointer location number --------------------------------->
                        */
                        if (offset + 1 >= reply.Length)
                        {
                            return false;
                        }
                        var pStart = ((reply[offset] & 0x3F) << 8) | (reply[++offset]);
                        offset++;

                        if (pStart >= reply.Length)
                        {
                            return false;
                        }

                        return GetQNameI(reply, ref pStart, ref name, depth + 1);
                    }
                    else
                    {
                        /* Label length (length = 8Bit and first 2 bits always 0)
						    0 | 1 | 2 | 3 | 4 | 5 | 6 | 7
						    empty | lablel length in bytes 
                        */
                        var labelLength = (reply[offset] & 0x3F);
                        offset++;

                        // 标签内容必须完整落在报文内，且其后至少还有一个字节（终止符或下一标签长度）
                        if (labelLength == 0 || offset + labelLength >= reply.Length)
                        {
                            return false;
                        }

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