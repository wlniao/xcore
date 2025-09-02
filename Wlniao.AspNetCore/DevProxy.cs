using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Net.Http.Headers;
using System.Threading;
using Microsoft.AspNetCore.Http;

namespace Wlniao;

/// <summary>
/// 开发代理服务
/// </summary>
public class DevProxy
{

    /// <summary>
    /// Http代理
    /// </summary>
    /// <param name="request"></param>
    /// <param name="target"></param>
    /// <returns></returns>
    public static Task<HttpResponseMessage> Http(HttpRequest request, string target)
    {
        var context = request.HttpContext;
        if(context.WebSockets.IsWebSocketRequest)
        {
            var wst = "ws://" + target.Substring(target.IndexOf("://", StringComparison.Ordinal) + 3);
            ProxyWebSocketAsync(context, wst);
            return new Task<HttpResponseMessage>(null);
        }
        var client = new HttpClient();
        var uri = new Uri(target + request.Path.Value + context.Request.QueryString);
        var msg = new HttpRequestMessage();
        client.Timeout = TimeSpan.FromSeconds(60);
        msg.Method = new HttpMethod(request.Method);
        msg.RequestUri = uri;
        msg.Headers.Host = uri.Host;
        if (request.ContentLength > 0)
        {
            msg.Content = new StreamContent(context.Request.Body);
            if (request.ContentType != null)
            {
                msg.Content.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType);
            }
        }
        // 复制请求头
        foreach (var header in context.Request.Headers)
        {
            if (header.Key.ToLower() == "host" || header.Key.ToLower() == "connection")
            {
                continue;
            }
            if (!msg.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray()) && msg.Content != null)
            {
                msg.Content?.Headers.TryAddWithoutValidation(header.Key, header.Value.ToArray());
            }
        }

        // 复制请求体（如果是 POST、PUT 等）
        if (request is not { ContentLength: > 0, ContentType: not null })
        {
            return client.SendAsync(msg);
        }
        msg.Content = new StreamContent(request.Body);
        msg.Content.Headers.ContentType = new MediaTypeHeaderValue(request.ContentType);

        // using (var responseMessageTask = client.SendAsync(msg, System.Net.Http.HttpCompletionOption.ResponseHeadersRead, context.RequestAborted))
        // {
        //     var responseMessage = responseMessageTask.Result;
        //     foreach (var header in responseMessage.Content.Headers)
        //     {
        //         context.Response.Headers.TryAdd(header.Key, header.Value.ToArray());
        //     }
        //     context.Response.StatusCode = (int)responseMessage.StatusCode;
        //     responseMessage.Content.CopyToAsync(context.Response.Body, context.RequestAborted);
        // }


        // 发送请求
        return client.SendAsync(msg);
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="context"></param>
    /// <param name="targetWebSocketUrl"></param>
    /// <returns></returns>
    private static Task ProxyWebSocketAsync(HttpContext context, string targetWebSocketUrl)
    {
        // 检查当前请求是否是 WebSocket 请求
        if (!context.WebSockets.IsWebSocketRequest)
        {
            // context.Response.StatusCode = StatusCodes.Status400BadRequest;
            // context.Response.WriteAsync("This endpoint requires WebSocket protocol");
            return Task.CompletedTask;
        }

        // 接受客户端 WebSocket 连接
        using var clientWebSocket = context.WebSockets.AcceptWebSocketAsync().Result;
        
        // 创建到目标服务器的 WebSocket 连接
        using var targetWebSocket = new ClientWebSocket();
        
        try
        {
            // 连接到目标 WebSocket 服务器
            targetWebSocket.ConnectAsync(new Uri(targetWebSocketUrl), context.RequestAborted);

            Console.WriteLine("WebSocket proxy established: {Client} -> {Target}", context.Connection.RemoteIpAddress, targetWebSocketUrl);

            // 创建双向数据传输任务
            var clientToTarget = Task.Run(() => 
                ForwardWebSocketDataAsync(clientWebSocket, targetWebSocket, "Client->Target", context.RequestAborted));
            
            var targetToClient = Task.Run(() => 
                ForwardWebSocketDataAsync(targetWebSocket, clientWebSocket, "Target->Client", context.RequestAborted));

            // 等待任意一个任务完成（连接关闭）
            return Task.WhenAny(clientToTarget, targetToClient);
        }
        catch (Exception)
        {
            Console.WriteLine("WebSocket proxy error");
        }
        finally
        {
            // 关闭连接
            CloseWebSocketAsync(clientWebSocket, WebSocketCloseStatus.NormalClosure, "Proxy closed", context.RequestAborted).Wait();
            CloseWebSocketAsync(targetWebSocket, WebSocketCloseStatus.NormalClosure, "Proxy closed", CancellationToken.None).Wait();
        }
        return Task.CompletedTask;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="source"></param>
    /// <param name="destination"></param>
    /// <param name="direction"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static Task ForwardWebSocketDataAsync(WebSocket source, WebSocket destination, string direction, CancellationToken cancellationToken)
    {
        var buffer = new byte[4096];
        WebSocketReceiveResult result;
        do
        {
            result = source.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken).Result;            
            if (result.MessageType == WebSocketMessageType.Close)
            {
                Console.WriteLine("{Direction}: Received close message", direction);
                destination.CloseAsync(WebSocketCloseStatus.NormalClosure, "Proxy close", cancellationToken);
                break;
            }

            // 转发消息到目标
            destination.SendAsync(
                new ArraySegment<byte>(buffer, 0, result.Count),
                result.MessageType,
                result.EndOfMessage,
                cancellationToken);

            Console.WriteLine("{Direction}: Forwarded {Count} bytes, MessageType: {MessageType}, EndOfMessage: {EndOfMessage}",
                direction, result.Count, result.MessageType, result.EndOfMessage);

        } while (!result.CloseStatus.HasValue && !cancellationToken.IsCancellationRequested);
        return Task.CompletedTask;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="webSocket"></param>
    /// <param name="closeStatus"></param>
    /// <param name="statusDescription"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private static Task CloseWebSocketAsync(WebSocket webSocket, WebSocketCloseStatus closeStatus, string statusDescription, CancellationToken cancellationToken)
    {
        if (webSocket.State == WebSocketState.Open || webSocket.State == WebSocketState.CloseReceived)
        {
            return webSocket.CloseAsync(closeStatus, statusDescription, cancellationToken);
        }
        return Task.CompletedTask;
    }
}