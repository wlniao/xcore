using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace Wlniao.Net;

/// <summary>
/// 自定义 HttpClient 处理器工厂，用于拦截底层 TCP 连接获取实际 IP
/// </summary>
/// <remarks>
/// 因 <see cref="SocketsHttpHandler"/> 是密封类型、无法继承，本类以「处理器工厂」形态工作：
/// 由 <see cref="CreateHandler"/> / <see cref="CreateHttpClient"/> 产出挂好 ConnectCallback 的实例，
/// 用法：<c>new CustomHttpClientHandler{...}.CreateHttpClient()</c>。
/// 注意：HTTPS 场景下 TLS 握手由上层在本回调返回的裸 TCP 流之上自行完成，
/// 因此回调不能再包装 <c>SslStream</c>，否则会造成双重加密。
/// </remarks>
public class CustomHttpClientHandler
{
    /// <summary>
    /// 定义回调委托，用于传递实际连接的 IP
    /// </summary>
    public Action<string> OnConnectionEstablished { get; set; }

    /// <summary>
    /// 连接池中连接的最大生存时间，用于强制 DNS 重新解析
    /// </summary>
    public TimeSpan PooledConnectionLifetime { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// 连接超时时间
    /// </summary>
    public TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// 创建已挂接连接拦截回调的处理器
    /// </summary>
    /// <returns>可直接交给 HttpClient 使用的处理器</returns>
    public SocketsHttpHandler CreateHandler()
    {
        var handler = new SocketsHttpHandler
        {
            PooledConnectionLifetime = PooledConnectionLifetime,
            ConnectTimeout = ConnectTimeout
        };
        handler.ConnectCallback = OnConnectAsync;
        // 不设置则走操作系统默认校验，会与 HttpClientManager 的策略不一致；
        // 这里统一挂框架的证书策略，同样受 XCore.AcceptAnyServerCertificate 全局开关控制
        handler.SslOptions.RemoteCertificateValidationCallback = Wlniao.XCore.ValidateServerCertificate;
        return handler;
    }

    /// <summary>
    /// 创建已挂接连接拦截回调的 HttpClient
    /// </summary>
    /// <returns>HttpClient 实例，由调用方负责释放</returns>
    public HttpClient CreateHttpClient()
    {
        return new HttpClient(CreateHandler());
    }

    /// <summary>
    /// 拦截连接建立过程：逐个尝试 DNS 返回的 IP，取第一个可连通的作为实际出口 IP
    /// </summary>
    /// <param name="context">连接上下文</param>
    /// <param name="cancellationToken">取消标记</param>
    /// <returns>已建立的裸 TCP 流（TLS 由上层按需包装）</returns>
    private async ValueTask<Stream> OnConnectAsync(SocketsHttpConnectionContext context, CancellationToken cancellationToken)
    {
        var uri = context.InitialRequestMessage.RequestUri;
        var port = uri.Port == -1 ? (uri.Scheme == "https" ? 443 : 80) : uri.Port;
        var host = uri.Host;

        // 步骤 1：解析域名获取所有 IP
        var ipHostEntry = await System.Net.Dns.GetHostEntryAsync(host, cancellationToken);
        IPAddress targetIp = null;
        TcpClient tcpClient = null;

        // 步骤 2：遍历 IP，尝试建立 TCP 连接（模拟系统默认的 IP 选择逻辑）
        foreach (var ip in ipHostEntry.AddressList)
        {
            try
            {
                tcpClient = new TcpClient(ip.AddressFamily);
                await tcpClient.ConnectAsync(ip, port, cancellationToken);
                targetIp = ip;
                break;
            }
            catch (OperationCanceledException)
            {
                // 取消是调用方的意图，不能被当成「这个 IP 连不上」而继续换下一个
                tcpClient?.Dispose();
                tcpClient = null;
                throw;
            }
            catch (Exception)
            {
                tcpClient?.Dispose();
                tcpClient = null;
                continue;
            }
        }

        if (targetIp == null || tcpClient == null)
        {
            throw new SocketException((int)SocketError.HostUnreachable);
        }

        // 步骤 3：触发回调，传递实际连接的 IP
        OnConnectionEstablished?.Invoke(targetIp.ToString());

        // 步骤 4：返回裸流，供上层（必要时包装 TLS）使用
        return tcpClient.GetStream();
    }
}
