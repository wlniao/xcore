using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Wlniao
{
    /// <summary>
    /// 
    /// </summary>
    public class Http
    {
        /// <summary>
        /// 监听的端口
        /// </summary>
        public int ListenPort = 80;

        /// <summary>
        /// 最大并发连接数；超出时快速拒绝，避免无界 Task.Run 拖垮线程池
        /// </summary>
        private const int MaxConnections = 100;

        /// <summary>
        /// 并发连接控制
        /// </summary>
        private static readonly SemaphoreSlim ConnectionSlots = new(MaxConnections, MaxConnections);

        /// <summary>
        /// 请求处理程序
        /// </summary>
        public Action<Context> Handler { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public class Context
        {
            private string _contentType = "text/plain; charset=utf-8";
            /// <summary>  
            /// 请求时间
            /// </summary>  
            public long Time { get; set; }
            /// <summary>  
            /// 响应状态
            /// </summary> 
            public int StatusCode = 200;
            /// <summary>  
            /// 请求路径
            /// </summary>  
            public string Path { get; set; }
            /// <summary>  
            /// 请求参数
            /// </summary>  
            public string Query { get; set; }
            /// <summary>  
            /// 请求方式
            /// </summary>  
            public string Method { get; set; }
            /// <summary>  
            /// 客户端IP
            /// </summary>  
            public string ClientIP { get; set; }
            /// <summary>  
            /// 请求内容
            /// </summary>  
            public string Request { get; set; }
            /// <summary>  
            /// 输出内容（字符串，需由处理器自行序列化；二进制响应请使用 ResponseBytes）。
            /// 改为 string 以契合 NativeAOT：Start() 不再对 object 做运行时反射式 JSON 序列化，
            /// 序列化应在处理器已知具体类型的上下文中完成（如 JsonObject / 源生成 JsonSerializerContext）。
            /// </summary>  
            public string Response { get; set; }
            /// <summary>  
            /// 原始二进制输出（优先级高于 Response；用于图片、文件等非文本响应）。AOT 安全，不涉及反射。
            /// </summary>  
            public byte[] ResponseBytes { get; set; }
            /// <summary>  
            /// 身份令牌
            /// </summary>
            public string AuthToken { get; set; }
            /// <summary>  
            /// 输出格式
            /// </summary>
            [System.Text.Json.Serialization.JsonIgnore]
            public string ContentType
            {
                get
                {
                    return _contentType;
                }
                set
                {
                    _contentType = string.IsNullOrEmpty(value) ? "application/json; charset=utf-8" : value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Http(int port = 80)
        {
            ListenPort = port;
            this.Handler = new Action<Context>((ctx) => { Console.WriteLine("default handler unregistered"); });
        }
        /// <summary>  
        /// 启动服务
        /// </summary>  
        public void Start()
        {
            try
            {
                var listener = new Socket(SocketType.Stream, ProtocolType.Tcp);
                listener.Bind(new IPEndPoint(IPAddress.Any, ListenPort));
                listener.Listen();
                Console.WriteLine("Http listening on: http://127.0.0.1:" + ListenPort, ConsoleColor.DarkGreen);
                while (true)
                {
                    var socket = listener.Accept(); //挂起并继续等待下一个链接
                    Task.Run(() =>
                    {
                        var acquired = false;
                        try
                        {
                            if (!ConnectionSlots.Wait(0))
                            {
                                socket.SendTimeout = 3000;
                                SendSimpleResponse(socket, 503, "Service Unavailable", "HTTP server is busy, please retry later");
                                return;
                            }
                            acquired = true;
                            socket.ReceiveTimeout = 30000;
                            socket.SendTimeout = 3000;

                            //通过clientSocket接收数据
                            var start = DateTime.Now;
                            var encoding = Encoding.UTF8;
                            var buffer = new System.Collections.Generic.List<byte>();
                            var chunk = new byte[8192];
                            var maxRequestSize = 1024 * 1024;
                            var headerEnd = -1;
                            var contentLength = 0;
                            var chunked = false;
                            var headerMatch = 0;
                            int scannedHeaderBytes = 0;
                            string msg = null;
                            while (true)
                            {
                                var receiveNumber = socket.Receive(chunk, chunk.Length, SocketFlags.None);
                                if (receiveNumber <= 0)
                                {
                                    break;
                                }
                                for (var i = 0; i < receiveNumber; i++)
                                {
                                    buffer.Add(chunk[i]);
                                }
                                if (buffer.Count > maxRequestSize)
                                {
                                    SendSimpleResponse(socket, 413, "Payload Too Large", "HTTP request too large");
                                    return;
                                }

                                // 流式扫描 \r\n\r\n，避免每轮 ToArray/GetString 的 O(n²) 开销
                                if (headerEnd < 0)
                                {
                                    while (scannedHeaderBytes < buffer.Count)
                                    {
                                        var b = buffer[scannedHeaderBytes++];
                                        if (b == HeaderEndBytes[headerMatch])
                                        {
                                            headerMatch++;
                                            if (headerMatch == HeaderEndBytes.Length)
                                            {
                                                headerEnd = scannedHeaderBytes - HeaderEndBytes.Length;
                                                break;
                                            }
                                        }
                                        else
                                        {
                                            headerMatch = b == HeaderEndBytes[0] ? 1 : 0;
                                        }
                                    }
                                    if (headerEnd < 0)
                                    {
                                        continue;
                                    }

                                    var headers = encoding.GetString(buffer.GetRange(0, headerEnd).ToArray());
                                    chunked = IsChunked(headers);
                                    if (chunked)
                                    {
                                        // 保持原有能力边界，但显式拒绝而不是等待/解析错误
                                        SendSimpleResponse(socket, 501, "Not Implemented", "Transfer-Encoding: chunked is not supported");
                                        return;
                                    }
                                    contentLength = GetContentLength(headers);
                                }

                                if (buffer.Count >= headerEnd + 4 + contentLength)
                                {
                                    break;
                                }
                            }
                            if (buffer.Count == 0 || headerEnd < 0)
                            {
                                return;
                            }
                            msg = encoding.GetString(buffer.ToArray());
                            var ctx = new Context() { Path = "/", Query = "", Request = "", ClientIP = ((IPEndPoint)socket.RemoteEndPoint).Address.MapToIPv4().ToString(), StatusCode = 200, Time = start.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000000 };
                            var req = msg.Substring(0, msg.IndexOf('\n')).Trim('\r').Split(' ', StringSplitOptions.RemoveEmptyEntries);
                            var idx = msg.IndexOf("\r\n\r\n", StringComparison.Ordinal);
                            if (req.Length == 3)
                            {
                                ctx.Method = req[0].ToUpper();
                                if (req[1].IndexOf('?') < 0)
                                {
                                    ctx.Path = req[1];
                                }
                                else
                                {
                                    var tos = req[1].Split('?', StringSplitOptions.RemoveEmptyEntries);
                                    ctx.Path = tos[0];
                                    ctx.Query = tos[1];
                                }
                            }
                            if (msg.IndexOf("\r\nToken: ", StringComparison.Ordinal) > 0)
                            {
                                var txt = msg.Substring(msg.IndexOf("\r\nToken: ", StringComparison.Ordinal) + 9);
                                ctx.AuthToken = txt.Substring(0, txt.IndexOf("\r\n", StringComparison.Ordinal));
                            }
                            if (ctx.Method == "POST" && idx > 0 && idx + 4 < msg.Length)
                            {
                                ctx.Request = msg.Substring(idx + 4);
                            }
                            try
                            {
                                this.Handler(ctx);
                            }
                            catch (Exception ex)
                            {
                                ctx.Response = ex.Message;
                            }
                            var sb = new System.Text.StringBuilder();
                            sb.Append("HTTP/1.1 " + ctx.StatusCode + "\r\n");
                            sb.Append("Date: " + DateTime.UtcNow.ToString("r") + "\r\n");
                            sb.Append("Server: wlniao/1.0\r\n");
                            sb.Append("X-UseTime: " + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms\r\n");
                            // AOT 安全：Response 为已序列化的字符串，ResponseBytes 为原始字节，均不涉及运行时反射
                            var data = ctx.ResponseBytes;
                            if (data == null && !string.IsNullOrEmpty(ctx.Response))
                            {
                                data = encoding.GetBytes(ctx.Response);
                            }
                            if (data != null)
                            {
                                sb.Append("Content-Type: " + ctx.ContentType + "\r\n");
                                sb.Append("Content-Length: " + data.Length + "\r\n");
                                sb.Append("\r\n");
                            }
                            var headerBytes = encoding.GetBytes(sb.ToString());
                            socket.Send(headerBytes, headerBytes.Length, SocketFlags.None);
                            if (data != null && data.Length > 0)
                            {
                                socket.Send(data, data.Length, SocketFlags.None);
                            }
                        }
                        catch (Exception ex)
                        {
                            Log.Loger.Error("Error:" + ex.Message);
                        }
                        finally
                        {
                            if (acquired)
                            {
                                ConnectionSlots.Release();
                            }
                            try { socket.Shutdown(SocketShutdown.Both); } catch { }
                            try { socket.Dispose(); } catch { }
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }

        /// <summary>
        /// HTTP 头结束符
        /// </summary>
        private static readonly byte[] HeaderEndBytes = { 13, 10, 13, 10 };

        /// <summary>
        /// 读取 HTTP Content-Length
        /// </summary>
        private static int GetContentLength(string headers)
        {
            var pos = headers.IndexOf("Content-Length:", StringComparison.OrdinalIgnoreCase);
            if (pos < 0)
            {
                return 0;
            }
            var value = headers.Substring(pos + "Content-Length:".Length).Trim();
            var lineEnd = value.IndexOf('\r');
            if (lineEnd >= 0)
            {
                value = value.Substring(0, lineEnd);
            }
            return int.TryParse(value.Trim(), out var length) && length >= 0 ? length : 0;
        }

        /// <summary>
        /// 是否使用 chunked 请求体
        /// </summary>
        private static bool IsChunked(string headers)
        {
            var pos = headers.IndexOf("Transfer-Encoding:", StringComparison.OrdinalIgnoreCase);
            if (pos < 0)
            {
                return false;
            }
            var value = headers.Substring(pos + "Transfer-Encoding:".Length).Trim();
            var lineEnd = value.IndexOf('\r');
            if (lineEnd >= 0)
            {
                value = value.Substring(0, lineEnd);
            }
            return value.Trim().EndsWith("chunked", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 发送简单文本响应；用于连接满、请求过大等前置拒绝场景
        /// </summary>
        private static void SendSimpleResponse(Socket socket, int statusCode, string reason, string message)
        {
            try
            {
                var body = Encoding.UTF8.GetBytes(message ?? "");
                var header = $"HTTP/1.1 {statusCode} {reason}\r\n" +
                             $"Date: {DateTime.UtcNow.ToString("r")}\r\n" +
                             "Server: wlniao/1.0\r\n" +
                             "Content-Type: text/plain; charset=utf-8\r\n" +
                             $"Content-Length: {body.Length}\r\n" +
                             "Connection: close\r\n\r\n";
                var headerBytes = Encoding.UTF8.GetBytes(header);
                socket.Send(headerBytes, headerBytes.Length, SocketFlags.None);
                if (body.Length > 0)
                {
                    socket.Send(body, body.Length, SocketFlags.None);
                }
            }
            catch (Exception ex)
            {
                Log.Loger.Error("SendSimpleResponse:" + ex.Message);
            }
        }
    }
}