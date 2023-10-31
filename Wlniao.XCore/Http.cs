using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
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
        public Int32 ListenPort = 80;

        /// <summary>
        /// 请求处理程序
        /// </summary>
        public Action<Context> Handler { get; set; }

        /// <summary>
        /// 
        /// </summary>
        public class Context
        {
            private string contentType = "text/plain; charset=utf-8";
            /// <summary>  
            /// 请求时间
            /// </summary>  
            public Int64 Time { get; set; }
            /// <summary>  
            /// 响应状态
            /// </summary> 
            public Int32 StatusCode = 200;
            /// <summary>  
            /// 请求路径
            /// </summary>  
            public String Path { get; set; }
            /// <summary>  
            /// 请求参数
            /// </summary>  
            public String Query { get; set; }
            /// <summary>  
            /// 请求方式
            /// </summary>  
            public String Method { get; set; }
            /// <summary>  
            /// 请求内容
            /// </summary>  
            public String Request { get; set; }
            /// <summary>  
            /// 输出内容
            /// </summary>  
            public Object Response { get; set; }
            /// <summary>  
            /// 身份令牌
            /// </summary>  
            public String AuthToken { get; set; }
            /// <summary>  
            /// 输出格式
            /// </summary>  
            [Serialization.NotSerialize]
            public String ContentType
            {
                get
                {
                    return contentType;
                }
                set
                {
                    contentType = string.IsNullOrEmpty(value) ? "application/json; charset=utf-8" : value;
                }
            }
        }

        /// <summary>
        /// 
        /// </summary>
        public Http(Int32 port = 80)
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
                        try
                        {
                            //通过clientSocket接收数据
                            var start = DateTime.Now;
                            var request = new byte[1024 * 64];
                            int receiveNumber = socket.Receive(request);
                            if (receiveNumber > 0)
                            {
                                var encoding = Encoding.UTF8;
                                var tempRequest = new System.Collections.Generic.List<byte>();
                                var msg = encoding.GetString(request, 0, receiveNumber);
                                var ctx = new Context() { Path = "/", Query = "", Request = "", StatusCode = 200, Time = start.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc)).Ticks / 10000000 };
                                var req = msg.Substring(0, msg.IndexOf('\n')).Trim('\r').Split(' ', StringSplitOptions.RemoveEmptyEntries);
                                var idx = msg.IndexOf("\r\n\r\n");
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
                                if (msg.IndexOf("\r\nToken: ") > 0)
                                {
                                    var txt = msg.Substring(msg.IndexOf("\r\nToken: ") + 9);
                                    ctx.AuthToken = txt.Substring(0, txt.IndexOf("\r\n"));
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
                                if (ctx.Request != null)
                                {
                                    var res = "";
                                    if (ctx.Response is string)
                                    {
                                        res = ctx.Response.ToString();
                                    }
                                    else if (ctx.Response != null)
                                    {
                                        ctx.ContentType = "application/json; charset=utf-8";
                                        res = Serialization.JsonString.Convert(ctx.Response);
                                    }
                                    if (res != null)
                                    {
                                        sb.Append("Content-Type: " + ctx.ContentType + "\r\n");
                                        sb.Append("Content-Length: " + encoding.GetByteCount(res) + "\r\n");
                                        sb.Append("\r\n" + res);
                                    }
                                }
                                msg = sb.ToString();
                                var response = encoding.GetBytes(msg);
                                socket.Send(response, response.Length, SocketFlags.None);
                                socket.SendTimeout = 100;
                            }
                        }
                        catch (Exception ex)
                        {
                            log.Error("Error:" + ex.Message);
                        }
                        finally
                        {
                            socket.Shutdown(SocketShutdown.Both);
                        }
                    });
                }
            }
            catch (Exception ex)
            {
                log.Error(ex.Message);
            }
        }
    }
}