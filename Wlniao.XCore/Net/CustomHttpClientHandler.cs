using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Security;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Wlniao.Net;

/// <summary>
/// 自定义 HttpClientHandler，用于拦截底层 TCP 连接获取实际 IP
/// </summary>
public class CustomHttpClientHandler : HttpClientHandler
{
    /// <summary>
    /// 定义回调委托，用于传递实际连接的 IP
    /// </summary>
    public Action<string> OnConnectionEstablished { get; set; }

    /// <summary>
    /// 重写 CreateConnection 方法，拦截连接创建过程
    /// </summary>
    protected async Task<Stream> CreateConnectionAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        // 先提取目标域名和端口
        var uri = request.RequestUri;
        var port = uri!.Port == -1 ? (uri.Scheme == "https" ? 443 : 80) : uri!.Port;
        var host = uri?.Host;

        // 步骤 1：解析域名获取所有 IP（和方案一一致）
        var ipHostEntry = await System.Net.Dns.GetHostEntryAsync(host!, cancellationToken);
        IPAddress targetIp = null;
        TcpClient tcpClient = null;

        // 步骤 2：遍历 IP，尝试建立 TCP 连接（模拟系统默认的 IP 选择逻辑）
        foreach (var ip in ipHostEntry.AddressList)
        {
            try
            {
                tcpClient = new TcpClient();
                // 尝试连接该 IP 和对应端口
                await tcpClient.ConnectAsync(ip, port, cancellationToken);
                targetIp = ip;
                break; // 连接成功，退出遍历
            }
            catch
            {
                tcpClient?.Dispose();
                continue; // 连接失败，尝试下一个 IP
            }
        }

        if (targetIp == null || tcpClient == null)
        {
            throw new SocketException((int)SocketError.HostUnreachable);
        }

        // 步骤 3：触发回调，传递实际连接的 IP
        OnConnectionEstablished?.Invoke(targetIp.ToString());

        // 步骤 4：创建并返回流（供 HttpClient 后续使用）
        Stream stream = tcpClient.GetStream();
        // 如果是 HTTPS，需要包装 SSL 流
        if (uri?.Scheme == "https")
        {
            var sslStream = new SslStream(stream, true);
            await sslStream.AuthenticateAsClientAsync(host, null, System.Security.Authentication.SslProtocols.Tls12, false);
            return sslStream;
        }

        return stream;
    }
}