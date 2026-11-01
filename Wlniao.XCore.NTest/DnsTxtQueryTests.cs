using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using Wlniao.Net.Dns;

namespace NTest
{
    /// <summary>
    /// TXT 类型查询测试。
    ///
    /// 采用进程内 UDP DNS 桩解析器（绑定 127.0.0.1 随机端口）而非真实外网 DNS：
    /// 1) 不依赖网络，CI 与离线环境都能跑；
    /// 2) 可以精确构造「一条记录包含多个 &lt;character-string&gt;」这种真实 DNS 才会出现、
    ///    而公共 DNS 未必稳定复现的报文；
    /// 3) 记录类型全是 internal，用桩服务走公开 API 即可验证，不必给生产库加 InternalsVisibleTo。
    ///
    /// 重点守住两个回归点：
    /// - TXT 超过 255 字节会被切成多段，解析时必须全部读出并拼接（否则 SPF/DKIM 被静默截断）；
    /// - 一个域名通常同时存在多条 TXT，GetTXT 只取第一条，GetTXTs 必须拿到全部。
    /// </summary>
    public class DnsTxtQueryTests
    {
        /// <summary>
        /// 最小化的 DNS 应答构造桩：接受任意查询，按预置内容返回 TXT 应答。
        /// </summary>
        private sealed class StubDnsResolver : System.IDisposable
        {
            private readonly Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            private readonly System.Threading.CancellationTokenSource _cts = new System.Threading.CancellationTokenSource();
            private readonly List<List<string>> _txtRecords = new List<List<string>>();
            private IPEndPoint? _endPoint;
            private string _lastQueryName = "";

            public IPEndPoint EndPoint => _endPoint ?? throw new System.InvalidOperationException("DNS 桩解析器尚未绑定端口。");

            /// <summary>最近一次收到的查询域名</summary>
            public string LastQueryName => _lastQueryName;

            /// <summary>最近一次收到的 QTYPE</summary>
            public int LastQueryType { get; private set; } = -1;

            /// <summary>为 true 时收下查询但不应答，用于验证超时 / 取消路径</summary>
            public bool Silent { get; set; }

            public StubDnsResolver()
            {
                _socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                _endPoint = (IPEndPoint)(_socket.LocalEndPoint
                    ?? throw new System.InvalidOperationException("绑定后未取到本地终结点。"));
                _ = System.Threading.Tasks.Task.Run(ListenLoopAsync);
            }

            /// <summary>
            /// 追加一条 TXT 记录。每个入参构成一个独立的 &lt;character-string&gt;，
            /// 一条记录允许包含多段，这正是超长 TXT 在网上的存在形式。
            /// </summary>
            public StubDnsResolver AddTxt(params string[] chunks)
            {
                _txtRecords.Add(new List<string>(chunks));
                return this;
            }

            public void Dispose()
            {
                _cts.Cancel();
                try { _socket.Dispose(); } catch (System.Exception) { /* Dispose 阶段不再抛出 */ }
                _cts.Dispose();
            }

            private async System.Threading.Tasks.Task ListenLoopAsync()
            {
                var buffer = new byte[1024];
                while (!_cts.IsCancellationRequested)
                {
                    try
                    {
                        var result = await _socket.ReceiveFromAsync(
                            new System.ArraySegment<byte>(buffer), SocketFlags.None,
                            new IPEndPoint(IPAddress.Any, 0)).ConfigureAwait(false);

                        if (result.ReceivedBytes <= 12 || Silent)
                        {
                            continue;
                        }

                        var request = new byte[result.ReceivedBytes];
                        System.Array.Copy(buffer, request, result.ReceivedBytes);

                        // 查询尾部固定是 QTYPE(2) + QCLASS(2)
                        LastQueryType = (request[request.Length - 4] << 8) | request[request.Length - 3];
                        _lastQueryName = ReadQName(request, 12, out var nameEnd);
                        var reply = BuildReply(request, nameEnd);
                        await _socket.SendToAsync(new System.ArraySegment<byte>(reply), SocketFlags.None,
                            result.RemoteEndPoint).ConfigureAwait(false);
                    }
                    catch (System.ObjectDisposedException) { break; }
                    catch (SocketException) { break; }
                    catch (System.OperationCanceledException) { break; }
                }
            }

            private byte[] BuildReply(byte[] request, int nameEnd)
            {
                using var ms = new System.IO.MemoryStream();
                void U16(int v)
                {
                    ms.WriteByte((byte)(v >> 8));
                    ms.WriteByte((byte)(v & 0xFF));
                }

                U16((request[0] << 8) | request[1]);  // ID 回显
                U16(0x8180);                          // Flags: QR=1, RD=1, RA=1, RCODE=0
                U16(1);                               // QDCOUNT
                U16(_txtRecords.Count);               // ANCOUNT
                U16(0);                               // NSCOUNT
                U16(0);                               // ARCOUNT
                ms.Write(request, 12, nameEnd + 4 - 12 + 1);   // 原样回显 Question 段

                foreach (var chunks in _txtRecords)
                {
                    U16(0xC00C);                      // NAME：指针，回指 Question 里的域名
                    U16(16);                          // TYPE = TXT
                    U16(1);                           // CLASS = IN
                    U16(0);                           // TTL 高 16 位
                    U16(300);                         // TTL = 300s

                    var rdata = new List<byte>();
                    foreach (var chunk in chunks)
                    {
                        var bytes = Encoding.UTF8.GetBytes(chunk);
                        Assert.That(bytes.Length <= 255, Is.True,
                            "单个 <character-string> 不得超过 255 字节，测试报文构造错误。");
                        rdata.Add((byte)bytes.Length);
                        rdata.AddRange(bytes);
                    }
                    U16(rdata.Count);
                    ms.Write(rdata.ToArray(), 0, rdata.Count);
                }

                return ms.ToArray();
            }

            private static string ReadQName(byte[] data, int offset, out int nameEnd)
            {
                var labels = new List<string>();
                while (true)
                {
                    var length = data[offset];
                    if (length == 0)
                    {
                        nameEnd = offset;
                        break;
                    }
                    labels.Add(Encoding.ASCII.GetString(data, offset + 1, length));
                    offset += length + 1;
                }
                return string.Join(".", labels);
            }
        }

        [Test]
        public void GetTXT_ConcatenatesAllCharacterStrings()
        {
            // 构造一个必然被拆段的 SPF：单段 255 字节 + 收尾段，合计 > 255
            var head = "v=spf1 include:_spf.a.com include:_spf.b.com include:_spf.c.com " + new string('x', 180);
            var tail = " ~all";
            using var stub = new StubDnsResolver().AddTxt(head, tail);
            using var dns = new DnsTool(new[] { stub.EndPoint });

            var text = dns.GetTXT("wlniao.com");

            Assert.That(text, Is.EqualTo(head + tail), "TXT 记录的多个 <character-string> 未按序拼接");
            Assert.That(stub.LastQueryType, Is.EqualTo(16), "QTYPE 应为 16(TXT)");
            Assert.That(stub.LastQueryName, Is.EqualTo("wlniao.com"));
        }

        [Test]
        public void GetTXT_KeepsFullValue_WhenSingleRecordExceeds255Bytes()
        {
            // 回归用例：修复前 Parse 只读第一个 <character-string>，此断言必然失败（值被截断到 255 字节）
            var full = new string('a', 250) + new string('b', 250) + new string('c', 10);
            using var stub = new StubDnsResolver().AddTxt(full.Substring(0, 255), full.Substring(255));
            using var dns = new DnsTool(new[] { stub.EndPoint });

            Assert.That(dns.GetTXT("long.example.com"), Is.EqualTo(full));
        }

        [Test]
        public void GetTXTs_ReturnsEveryTxtRecord()
        {
            using var stub = new StubDnsResolver()
                .AddTxt("v=spf1 include:_spf.google.com ~all")
                .AddTxt("google-site-verification=" + new string('k', 40))
                .AddTxt("MS=" + new string('9', 16));
            using var dns = new DnsTool(new[] { stub.EndPoint });

            var texts = dns.GetTXTs("example.com");

            Assert.That(texts.Length, Is.EqualTo(3), "三条 TXT 记录都应当返回");
            Assert.That(texts[0], Is.EqualTo("v=spf1 include:_spf.google.com ~all"));
            Assert.That(texts[1], Does.StartWith("google-site-verification=k"));
            Assert.That(texts[2], Does.StartWith("MS="));
        }

        [Test]
        public void GetTXT_ReturnsFirstRecordOnly()
        {
            using var stub = new StubDnsResolver().AddTxt("first").AddTxt("second");
            using var dns = new DnsTool(new[] { stub.EndPoint });

            Assert.That(dns.GetTXT("multi.example.com"), Is.EqualTo("first"));
        }

        [Test]
        public void GetTXT_HandlesUnderscorePrefixedName()
        {
            // _dmarc / _domainkey 这类带下划线的名称是 TXT 查询的主流场景，
            // CreateQuery 里过了一道 IdnMapping.GetAscii，这里守住不会因此异常。
            using var stub = new StubDnsResolver().AddTxt("v=DMARC1; p=none; rua=mailto:dmarc@example.com");
            using var dns = new DnsTool(new[] { stub.EndPoint });

            var text = dns.GetTXT("_dmarc.example.com");

            Assert.That(text, Is.EqualTo("v=DMARC1; p=none; rua=mailto:dmarc@example.com"));
            Assert.That(stub.LastQueryName, Is.EqualTo("_dmarc.example.com"));
        }

        [Test]
        public void GetTXTs_ReturnsEmpty_WhenNoTxtRecord()
        {
            using var stub = new StubDnsResolver();     // 不预置任何记录：ANCOUNT=0
            using var dns = new DnsTool(new[] { stub.EndPoint });

            Assert.That(dns.GetTXT("nx.example.com"), Is.Null, "无应答记录时 GetTXT 应返回 null");
            Assert.That(dns.GetTXTs("nx.example.com"), Is.Empty, "无应答记录时 GetTXTs 应返回空数组");
        }

        [Test]
        public void GetTXTs_DecodesNonAsciiValueAsUtf8()
        {
            using var stub = new StubDnsResolver().AddTxt("验证代码=中文ABC");
            using var dns = new DnsTool(new[] { stub.EndPoint });

            Assert.That(dns.GetTXT("utf8.example.com"), Is.EqualTo("验证代码=中文ABC"),
                "TXT 承载任意文本，非 ASCII 不应被 ASCII 解码替换成 '?'");
        }

        [Test]
        public async System.Threading.Tasks.Task GetTXTAsync_UsesAsyncChannel()
        {
            using var stub = new StubDnsResolver().AddTxt("head", "tail");
            using var dns = new DnsTool(new[] { stub.EndPoint });

            var text = await dns.GetTXTAsync("async.example.com");
            var texts = await dns.GetTXTsAsync("async.example.com");

            Assert.That(text, Is.EqualTo("headtail"));
            Assert.That(texts.Length, Is.EqualTo(1));
            Assert.That(stub.LastQueryType, Is.EqualTo(16));
        }

        [Test]
        public void GetTXTAsync_ThrowsWhenCancelled()
        {
            using var stub = new StubDnsResolver { Silent = true };   // 收下查询但永不应答
            using var dns = new DnsTool(new[] { stub.EndPoint });
            using var cts = new System.Threading.CancellationTokenSource(500);

            Assert.ThrowsAsync<System.OperationCanceledException>(async () =>
                await dns.GetTXTAsync("timeout.example.com", cts.Token));
        }
    }
}
