using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;

namespace Wlniao;

/// <summary>
/// 开发代理服务
/// </summary>
public class DevProxy
{
    /// <summary>
    /// 复用 HttpClient，避免每次请求新建实例导致套接字耗尽
    /// </summary>
    private static readonly HttpClient _client = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        Timeout = TimeSpan.FromSeconds(60)
    };

    /// <summary>
    /// 不允许透传给目标服务的请求头（由 HttpClient 自行计算或与升级协议冲突）
    /// </summary>
    private static readonly string[] _skipHeaders =
        ["host", "connection", "content-length", "transfer-encoding", "keep-alive", "upgrade", "proxy-connection"];

    /// <summary>
    /// Http代理
    /// </summary>
    /// <remarks>
    /// 普通请求返回上游响应，调用方负责回写状态码、响应头与响应体。
    /// 当请求为 WebSocket 升级请求时，本方法会接管连接直到其关闭，返回值的 StatusCode 固定为
    /// <see cref="HttpStatusCode.SwitchingProtocols"/>，**仅用于表示代理已结束**：
    /// 此时响应已升级完成，调用方不得再把该对象写入客户端响应。
    /// </remarks>
    /// <param name="request"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static async Task<HttpResponseMessage> Http(HttpRequest request, string target)
    {
        var context = request.HttpContext;
        if (context.WebSockets.IsWebSocketRequest)
        {
            var wst = "ws://" + target[(target.IndexOf("://", StringComparison.Ordinal) + 3)..];
            // WebSocket 代理会持续到连接关闭，必须等待其结束，否则 HttpContext 被提前释放
            await ProxyWebSocketAsync(context, wst);
            return new HttpResponseMessage(HttpStatusCode.SwitchingProtocols);
        }

        var uri = new Uri(target + request.Path.Value + context.Request.QueryString);
        using var msg = new HttpRequestMessage
        {
            Method = new HttpMethod(request.Method),
            RequestUri = uri
        };
        msg.Headers.Host = uri.Authority;

        if (request is { ContentLength: > 0, ContentType: not null })
        {
            // 包一层不释放内层流的包装器：HttpRequestMessage.Dispose → StreamContent.Dispose 会连带释放 Request.Body
            msg.Content = new StreamContent(new NonDisposingStream(context.Request.Body));
            msg.Content.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType);
            // content-length 未参与透传，这里显式回填，避免出站请求退化为 chunked 而不被部分下游接受
            msg.Content.Headers.ContentLength = request.ContentLength;
        }

        // 复制请求头
        foreach (var header in context.Request.Headers)
        {
            if (Array.IndexOf(_skipHeaders, header.Key.ToLowerInvariant()) >= 0)
            {
                continue;
            }
            if (!msg.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]) && msg.Content != null)
            {
                msg.Content.Headers.TryAddWithoutValidation(header.Key, [.. header.Value]);
            }
        }

        return await _client.SendAsync(msg, HttpCompletionOption.ResponseHeadersRead, context.RequestAborted);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="targetWebSocketUrl"></param>
    /// <returns></returns>
    private static async Task ProxyWebSocketAsync(HttpContext context, string targetWebSocketUrl)
    {
        if (!context.WebSockets.IsWebSocketRequest)
        {
            return;
        }

        var cancellationToken = context.RequestAborted;
        using var clientWebSocket = await context.WebSockets.AcceptWebSocketAsync();
        using var targetWebSocket = new ClientWebSocket();

        try
        {
            await targetWebSocket.ConnectAsync(new Uri(targetWebSocketUrl), cancellationToken);
            Log.Loger.Debug($"WebSocket proxy established: {context.Connection.RemoteIpAddress} -> {targetWebSocketUrl}");

            // 双向转发，任意一个方向结束即视为连接关闭
            await Task.WhenAny(
                ForwardWebSocketDataAsync(clientWebSocket, targetWebSocket, "Client->Target", cancellationToken),
                ForwardWebSocketDataAsync(targetWebSocket, clientWebSocket, "Target->Client", cancellationToken));
        }
        catch (OperationCanceledException)
        {
            // 客户端主动断开，忽略
        }
        catch (Exception ex)
        {
            Log.Loger.Error($"WebSocket proxy error => {ex.Message}{Environment.NewLine}{ex.StackTrace}");
        }
        finally
        {
            await CloseWebSocketAsync(clientWebSocket, WebSocketCloseStatus.NormalClosure, "Proxy closed", CancellationToken.None);
            await CloseWebSocketAsync(targetWebSocket, WebSocketCloseStatus.NormalClosure, "Proxy closed", CancellationToken.None);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="direction"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static async Task ForwardWebSocketDataAsync(WebSocket source, WebSocket destination, string direction, CancellationToken cancellationToken)
    {
        var buffer = new byte[8192];
        try
        {
            while (!cancellationToken.IsCancellationRequested && source.State == WebSocketState.Open)
            {
                // 必须等待接收完成后再发送，且发送完成前不能复用 buffer，否则会出现乱序或数据错乱
                var result = await source.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await CloseWebSocketAsync(destination, WebSocketCloseStatus.NormalClosure, "Proxy close", CancellationToken.None);
                    break;
                }

                await destination.SendAsync(new ArraySegment<byte>(buffer, 0, result.Count), result.MessageType, result.EndOfMessage, cancellationToken);
            }
        }
        catch (OperationCanceledException)
        {
            // 连接已关闭，忽略
        }
        catch (Exception ex)
        {
            Log.Loger.Debug($"{direction}: {ex.Message}");
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="webSocket"></param>
    /// <param name="closeStatus"></param>
    /// <param name="statusDescription"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static async Task CloseWebSocketAsync(WebSocket webSocket, WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
    {
        try
        {
            if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
            {
                await webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
            }
        }
        catch (Exception ex)
        {
            Log.Loger.Debug($"WebSocket close: {ex.Message}");
        }
    }

    /// <summary>
    /// 只读转发包装器：释放自身时不释放内层流，
    /// 避免 HttpRequestMessage 释放时把 ASP.NET Core 的 Request.Body 一起关掉
    /// </summary>
    private sealed class NonDisposingStream(Stream inner) : Stream
    {
        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;
        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }
        public override void Flush() => inner.Flush();
        public override int Read(byte[] buffer, int offset, int count) => inner.Read(buffer, offset, count);
        public override int Read(Span<byte> buffer) => inner.Read(buffer);
        public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken cancellationToken = default)
            => inner.ReadAsync(buffer, cancellationToken);
        public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
            => inner.ReadAsync(buffer, offset, count, cancellationToken);
        public override long Seek(long offset, SeekOrigin origin) => inner.Seek(offset, origin);
        public override void SetLength(long value) => inner.SetLength(value);
        public override void Write(byte[] buffer, int offset, int count) => inner.Write(buffer, offset, count);
        protected override void Dispose(bool disposing)
        {
            // 故意不释放 inner
        }
        public override ValueTask DisposeAsync() => default;
    }
}
