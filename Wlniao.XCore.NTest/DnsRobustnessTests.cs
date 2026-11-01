using System.Collections.Generic;
using System.Diagnostics;
using System.Net;
using System.Net.Sockets;
using Wlniao.Net.Dns;

namespace NTest
{
    /// <summary>
    /// DNS 应答健壮性测试：用进程内 UDP 桩直接回放畸形报文，验证解析器不会崩溃、不会误判。
    ///
    /// 判别性设计：每条畸形报文里都放一条**内容合法的 TXT 记录**（文本各不相同）。
    /// 这样「报文被正确丢弃」时结果为 null，而一旦解析器接受了它，返回值里就会出现那段文本——
    /// 断言才有区分力。只断言 Is.Null / Is.Empty 的写法在有缺陷的实现上同样成立，等于没测。
    /// </summary>
    public class DnsRobustnessTests
    {
        /// <summary>
        /// 回放任意字节的 DNS 桩：builder 收到请求报文，返回要回放的应答字节。
        /// </summary>
        private sealed class RawDnsResponder : System.IDisposable
        {
            private readonly Socket _socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, ProtocolType.Udp);
            private readonly System.Threading.CancellationTokenSource _cts = new System.Threading.CancellationTokenSource();
            private readonly System.Func<byte[], byte[]> _builder;
            private readonly IPEndPoint _endPoint;

            public IPEndPoint EndPoint => _endPoint;

            public RawDnsResponder(System.Func<byte[], byte[]> builder)
            {
                _builder = builder;
                _socket.Bind(new IPEndPoint(IPAddress.Loopback, 0));
                _endPoint = (IPEndPoint)(_socket.LocalEndPoint
                    ?? throw new System.InvalidOperationException("绑定后未取到本地终结点。"));
                _ = System.Threading.Tasks.Task.Run(ListenLoopAsync);
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

                        if (result.ReceivedBytes <= 0)
                        {
                            continue;
                        }

                        var request = new byte[result.ReceivedBytes];
                        System.Array.Copy(buffer, request, result.ReceivedBytes);

                        var reply = _builder(request);
                        if (reply != null && reply.Length > 0)
                        {
                            await _socket.SendToAsync(new System.ArraySegment<byte>(reply), SocketFlags.None,
                                result.RemoteEndPoint).ConfigureAwait(false);
                        }
                    }
                    catch (System.ObjectDisposedException) { break; }
                    catch (SocketException) { break; }
                    catch (System.OperationCanceledException) { break; }
                }
            }
        }

        /// <summary>
        /// 构造一个 12 字节 DNS 头部（不含任何记录段）。
        /// </summary>
        private static byte[] Header(byte[] request, int flags, int qdCount, int anCount)
        {
            var reply = new byte[12];
            reply[0] = request[0];
            reply[1] = request[1];
            reply[2] = (byte)(flags >> 8);
            reply[3] = (byte)(flags & 0xFF);
            reply[4] = (byte)(qdCount >> 8);
            reply[5] = (byte)(qdCount & 0xFF);
            reply[6] = (byte)(anCount >> 8);
            reply[7] = (byte)(anCount & 0xFF);
            return reply;
        }

        /// <summary>
        /// 追加一段 Question：根域名 + QTYPE=TXT + QCLASS=IN，共 5 字节，
        /// 于是后续记录的 NAME 可以用 <c>0xC0 0x0C</c> 指回偏移 12。
        /// </summary>
        private static void AddQuestion(List<byte> reply)
        {
            reply.AddRange(new byte[] { 0x00 });                    // QNAME = 根
            reply.AddRange(new byte[] { 0x00, 0x10 });              // QTYPE = TXT
            reply.AddRange(new byte[] { 0x00, 0x01 });              // QCLASS = IN
        }

        /// <summary>
        /// 追加一条 TXT 记录。RDLENGTH 与实际写入的 RDATA 字节数故意解耦，
        /// 用于构造「声明长度大于实际内容」的截断报文。
        /// </summary>
        private static void AddTxtRecord(List<byte> reply, int declaredRdLength, byte[] rdata)
        {
            reply.AddRange(new byte[] { 0xC0, 0x0C });              // NAME：指针指向 Question(12)
            reply.AddRange(new byte[] { 0x00, 0x10 });              // TYPE = TXT
            reply.AddRange(new byte[] { 0x00, 0x01 });              // CLASS = IN
            reply.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });  // TTL
            reply.AddRange(new byte[] { (byte)(declaredRdLength >> 8), (byte)(declaredRdLength & 0xFF) });
            if (rdata != null && rdata.Length > 0)
            {
                reply.AddRange(rdata);
            }
        }

        /// <summary>
        /// 构造一段 TXT 的 RDATA：一个 &lt;character-string&gt;（长度字节 + 内容）。
        /// </summary>
        private static byte[] TextRdata(string text)
        {
            var payload = System.Text.Encoding.ASCII.GetBytes(text);
            var rdata = new byte[payload.Length + 1];
            rdata[0] = (byte)payload.Length;
            System.Array.Copy(payload, 0, rdata, 1, payload.Length);
            return rdata;
        }

        /// <summary>
        /// 构造一条「完全合法」的单记录 TXT 应答。
        /// </summary>
        private static byte[] ValidTxtReply(byte[] request, string text, int flags = 0x8180)
        {
            var reply = new List<byte>(Header(request, flags, 1, 1));
            AddQuestion(reply);
            var rdata = TextRdata(text);
            AddTxtRecord(reply, rdata.Length, rdata);
            return reply.ToArray();
        }

        [Test]
        public void GetTXT_FallsThroughToNextServer_WhenReplyShorterThanHeader()
        {
            // 第一台只回 3 字节。旧实现把 1024 字节缓冲区整体交给解析器，
            // 补零部分被读成「QR=0、RCODE=0、无记录」的成功应答并立即返回 null，
            // 于是第二台服务器根本没被尝试 —— 断言拿到第二台的内容即可暴露该缺陷。
            using var shortStub = new RawDnsResponder(_ => new byte[] { 0x12, 0x34, 0x81 });
            using var goodStub = new RawDnsResponder(request => ValidTxtReply(request, "ok-failover"));
            using var dns = new DnsTool(new[] { shortStub.EndPoint, goodStub.EndPoint });

            Assert.That(dns.GetTXT("short.example.com"), Is.EqualTo("ok-failover"),
                "短于头部的报文必须被丢弃，并顺延到下一台服务器");
        }

        [Test]
        public void GetTXT_DropsReply_WhenTransactionIdMismatches()
        {
            using var stub = new RawDnsResponder(request =>
            {
                var reply = ValidTxtReply(request, "evil-txid");
                reply[0] ^= 0xFF;   // 事务 ID 与请求不一致
                reply[1] ^= 0xFF;
                return reply;
            });
            using var dns = new DnsTool(new[] { stub.EndPoint });

            Assert.That(dns.GetTXT("id.example.com"), Is.Null, "事务 ID 不符的应答必须丢弃");
            Assert.That(dns.GetTXTs("id.example.com"), Is.Empty, "丢弃后不得留下任何记录");
        }

        [Test]
        public void GetTXT_DropsReply_WhenQrBitIsZero()
        {
            // QR=0 说明这是一份查询而非应答（UDP 自环 / 反射伪造的典型形态）。
            // 报文其余部分完全合法且带 TXT 内容，解析器一旦接受就会返回 "evil-qr"。
            using var stub = new RawDnsResponder(request => ValidTxtReply(request, "evil-qr", 0x0100));
            using var dns = new DnsTool(new[] { stub.EndPoint });

            Assert.That(dns.GetTXT("qr.example.com"), Is.Null, "QR=0 的报文必须丢弃");
            Assert.That(dns.GetTXTs("qr.example.com"), Is.Empty);
        }

        [Test]
        public void GetTXT_DropsAnswer_WhenRdataIsTruncated()
        {
            // 头部声明 1 条应答，RDLENGTH 写 64，实际只跟了 2 字节 RDATA。
            // 若按缓冲区长度（而非报文实际长度 / RDLENGTH）读取，补零会被解析成一条空文本记录。
            using var stub = new RawDnsResponder(request =>
            {
                var reply = new List<byte>(Header(request, 0x8180, 1, 1));
                AddQuestion(reply);
                AddTxtRecord(reply, 64, new byte[] { 0x02, (byte)'x' });
                return reply.ToArray();
            });
            using var dns = new DnsTool(new[] { stub.EndPoint });

            Assert.That(dns.GetTXT("truncated.example.com"), Is.Null, "RDATA 被截断的记录必须整条丢弃");
            Assert.That(dns.GetTXTs("truncated.example.com"), Is.Empty);
        }

        [Test]
        public void GetTXTs_DoesNotCrossRdataBoundary_WhenCharacterStringOverruns()
        {
            // 第一条记录 RDLENGTH=2，长度字节却写 200（远超本条 RDATA 剩余 1 字节）。
            // 按报文长度截断会读进第二条记录，把它的字节吞进第一条文本里。
            using var stub = new RawDnsResponder(request =>
            {
                var reply = new List<byte>(Header(request, 0x8180, 1, 2));
                AddQuestion(reply);
                // 记录 1：rdata = [200, 'a']，长度字节声明的 200 被 RDATA 边界裁到 1
                AddTxtRecord(reply, 2, new byte[] { 200, (byte)'a' });
                // 记录 2：正常内容，必须原样读出
                var second = TextRdata("second");
                AddTxtRecord(reply, second.Length, second);
                return reply.ToArray();
            });
            using var dns = new DnsTool(new[] { stub.EndPoint });

            Assert.That(dns.GetTXTs("overrun.example.com"), Is.EqualTo(new[] { "a", "second" }),
                "字符段必须被裁到本条 RDATA 边界内，不能读穿到下一条记录");
        }

        [Test]
        public void GetTXT_DoesNotOverflowStack_OnCompressionPointerCycle()
        {
            // 一条 NAME 指向自身的记录：Question 段 12~16，应答记录的 NAME 从 17 开始，
            // 指针 0xC011 正好指回它自己所在位置，形成 A→A 的自环。
            // 无深度上限时递归永不终止，栈耗尽后进程直接崩溃（StackOverflowException 不可捕获）。
            using var stub = new RawDnsResponder(request =>
            {
                var reply = new List<byte>(Header(request, 0x8180, 1, 1));
                reply.AddRange(new byte[] { 0x00 });                          // QNAME = 根
                reply.AddRange(new byte[] { 0x00, 0x10 });                    // QTYPE = TXT
                reply.AddRange(new byte[] { 0x00, 0x01 });                    // QCLASS = IN
                reply.AddRange(new byte[] { 0xC0, 0x11 });                     // NAME：指针，指回自身(17)
                reply.AddRange(new byte[] { 0x00, 0x10 });                     // TYPE = TXT
                reply.AddRange(new byte[] { 0x00, 0x01 });                     // CLASS = IN
                reply.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });          // TTL
                reply.AddRange(new byte[] { 0x00, 0x00 });                     // RDLENGTH = 0
                return reply.ToArray();
            });
            using var dns = new DnsTool(new[] { stub.EndPoint });

            var watch = Stopwatch.StartNew();
            var text = dns.GetTXT("cycle.example.com");
            watch.Stop();

            Assert.That(text, Is.Null, "指针成环的记录必须被判定为非法，不能当成有效记录");
            Assert.That(watch.ElapsedMilliseconds, Is.LessThan(2000), "指针环必须在有限深度内终止");
        }

        [Test]
        public void GetTXT_ReadsRecord_WhenCompressionPointerIsValid()
        {
            // 正向对照：指针指向 Question 里的域名（偏移 12）是标准写法，必须仍然能解析出记录
            using var stub = new RawDnsResponder(request => ValidTxtReply(request, "hello"));
            using var dns = new DnsTool(new[] { stub.EndPoint });

            Assert.That(dns.GetTXT("ok.example.com"), Is.EqualTo("hello"));
        }

        [Test]
        public void GetTXT_AcceptsReply_WhenTransactionIdWrapsPastSixteenBits()
        {
            // 报文头里的事务 ID 只有 16 位：计数器走到 65536 时线上写的是 0。
            // 旧实现拿完整 int 的 65536 与线上的 0 比较，永远不相等，
            // 常驻实例跨过回绕点之后会把每一条合法应答都判成伪造（各查询静默返回 null 且无法自愈）。
            using var stub = new RawDnsResponder(request => ValidTxtReply(request, "after-wrap"));
            using var dns = new DnsTool(new[] { stub.EndPoint });

            var counter = typeof(DnsTool).GetField("_requestId",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            Assert.That(counter, Is.Not.Null, "内部计数器字段名变更后需同步本测试");
            counter!.SetValue(dns, 0xFFFF);   // 下一次查询取到的 ID 是 65536，线上只能写 0

            Assert.That(dns.GetTXT("wrap.example.com"), Is.EqualTo("after-wrap"),
                "事务 ID 回绕后应答必须仍被接受");
        }

        [Test]
        public void GetTXTs_KeepsOtherRecords_WhenSoaRdataIsMalformed()
        {
            // SOA 的 RDATA 只声明 4 字节（两个根域名，5 个 32 位定时器全部缺失），且紧贴报文末尾。
            // 旧实现读定时器时不看 RDATA 边界，越界抛出异常后被当成「整份报文畸形」丢掉，
            // 同一包里排在它前面的合法 TXT 也跟着一起消失。
            using var stub = new RawDnsResponder(request =>
            {
                var reply = new List<byte>(Header(request, 0x8180, 1, 2));
                AddQuestion(reply);
                var text = TextRdata("keep-me");
                AddTxtRecord(reply, text.Length, text);
                reply.AddRange(new byte[] { 0xC0, 0x0C });              // NAME
                reply.AddRange(new byte[] { 0x00, 0x06 });              // TYPE = SOA
                reply.AddRange(new byte[] { 0x00, 0x01 });              // CLASS = IN
                reply.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });  // TTL
                reply.AddRange(new byte[] { 0x00, 0x04 });              // RDLENGTH = 4
                reply.AddRange(new byte[] { 0x00, 0x00, 0x00, 0x00 });  // MNAME=根、RNAME=根，定时器缺失
                return reply.ToArray();
            });
            using var dns = new DnsTool(new[] { stub.EndPoint });

            Assert.That(dns.GetTXTs("soa.example.com"), Is.EqualTo(new[] { "keep-me" }),
                "一条 SOA 的 RDATA 畸形只应作废它自己，不能牵连同一报文里的合法记录");
        }

        [Test]
        public void NaptrParse_StopsAtRdataEnd_WhenFlagsOverrun()
        {
            // NAPTR 的 RDATA = ORDER(2) + PREFERENCE(2) + FLAGS + SERVICES + REGEXP + REPLACEMENT。
            // 这里 FLAGS 的长度字节写 200，而本条 RDATA 只剩 1 字节内容，其后紧跟另一条记录的字节。
            // 旧实现在读完 ORDER/PREFERENCE（已推进 4 字节）之后才算 offset + rdLength，
            // 边界比真实末尾宽 4 字节，FLAGS 就会把这 4 个字节一起吞进去。
            var rdata = new byte[]
            {
                0x00, 0x0A,             // ORDER = 10
                0x00, 0x14,             // PREFERENCE = 20
                200,  (byte)'u',        // FLAGS：长度字节声明 200，实际只给 1 字节
            };
            var next = new byte[] { 0xC0, 0x0C, 0x00, 0x10, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00 };
            var reply = new byte[rdata.Length + next.Length];
            System.Array.Copy(rdata, 0, reply, 0, rdata.Length);
            System.Array.Copy(next, 0, reply, rdata.Length, next.Length);   // 越界后会出现在 FLAGS 里的字节

            const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.Public
                | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static;
            var type = typeof(DnsTool).Assembly.GetType("Wlniao.Net.Dns.DnsRecord_NAPTR", true);
            Assert.That(type, Is.Not.Null, "内部记录类型改名后需同步本测试");
            var parse = type!.GetMethod("Parse", flags);
            Assert.That(parse, Is.Not.Null);

            var record = parse!.Invoke(null, new object[] { "naptr.example.com", reply, 0, rdata.Length, 60 });
            var parsed = type.GetProperty("Flags")!.GetValue(record);

            Assert.That(parsed, Is.EqualTo("u"), "FLAGS 必须被裁到本条 RDATA 边界内，不能读进后续记录");
        }
    }
}
