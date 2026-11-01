using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Wlniao.Net
{
    /// <summary>
    /// HttpClient管理器，用于解决DNS缓存问题
    /// </summary>
    public class HttpClientManager : IDisposable
    {
        private readonly Timer _timer;
        private HttpClient _httpClient;
        private readonly object _lock = new object();
        private bool _disposed = false;
        /// <summary>
        /// 已退役但尚未释放的 HttpClient 及其到期时间。
        /// </summary>
        /// <remarks>
        /// 刷新时不能立刻释放旧实例：刷新发生的那一刻可能还有请求正跑在旧实例上，
        /// 直接 Dispose 会让这些在途请求以「已释放」异常失败。这里给旧实例留一段宽限期
        /// （默认 1 分钟），到期后在下一次刷新时统一释放。
        /// </remarks>
        private readonly List<(HttpClient Client, DateTime DueUtc)> _retired = new List<(HttpClient, DateTime)>();
        /// <summary>
        /// 旧实例从退役到真正释放之间的宽限期。
        /// </summary>
        private readonly TimeSpan _retireGrace;
        /// <summary>
        /// HttpClient共享管理器，用于解决DNS缓存问题
        /// </summary>
        /// <remarks>
        /// 用 Lazy 而不是 <c>??=</c>：<c>??=</c> 不是原子操作，并发首次访问会各自创建一个管理器，
        /// 未被引用的那个连同它的 Timer 与 HttpClient 一起泄漏（看起来只是「多了一个实例」，
        /// 实际是每并发一次就多一个永不释放的连接池和定时器）。
        /// </remarks>
        private static readonly Lazy<HttpClientManager> _shared =
            new Lazy<HttpClientManager>(() => new HttpClientManager(), LazyThreadSafetyMode.ExecutionAndPublication);

        /// <summary>
        /// 框架共享HttpClient实例
        /// </summary>
        public static HttpClient SharedInstance
        {
            get
            {
                return _shared.Value.HttpClient;
            }
        }

        /// <summary>
        /// 获取当前HttpClient实例
        /// </summary>
        /// <remarks>
        /// 读取实例与释放必须在同一把锁里完成：先判 <c>_disposed</c> 再取字段是两段独立读取，
        /// 中间被 <see cref="Dispose"/> 插进来时会把字段的 null 交出去，调用方随后用它的
        /// 方法就会得到 NullReferenceException，而不是明确的「已释放」异常。
        /// </remarks>
        public HttpClient HttpClient 
        { 
            get 
            {
                lock (_lock)
                {
                    if (_disposed || _httpClient == null)
                    {
                        throw new ObjectDisposedException(nameof(HttpClientManager));
                    }
                    return _httpClient;
                }
            } 
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dnsRefreshInterval">DNS刷新间隔，默认12分钟</param>
        /// <param name="retireGrace">旧实例退役后保留多久再释放，默认1分钟；传 <see cref="TimeSpan.Zero"/> 表示刷新后立即释放</param>
        public HttpClientManager(TimeSpan? dnsRefreshInterval = null, TimeSpan? retireGrace = null)
        {
            var interval = dnsRefreshInterval ?? TimeSpan.FromMinutes(12); // 默认12分钟刷新一次
            _retireGrace = retireGrace ?? TimeSpan.FromMinutes(1);
            if (_retireGrace < TimeSpan.Zero)
            {
                _retireGrace = TimeSpan.Zero;
            }
            _httpClient = CreateNewHttpClient();
            _timer = new Timer(RefreshHttpClient, null, interval, interval);
        }

        /// <summary>
        /// 创建新的HttpClient实例
        /// </summary>
        /// <returns></returns>
        private HttpClient CreateNewHttpClient()
        {
            var handler = new SocketsHttpHandler
            {
                // 设置连接池中连接的最大生存时间，这会强制DNS重新解析
                PooledConnectionLifetime = TimeSpan.FromMinutes(5), // 连接存活时间略小于刷新周期
                MaxConnectionsPerServer = 200, // 最大并发数
                UseProxy = false, // 不使用代理
                AllowAutoRedirect = true, // 允许自动重定向
                ConnectTimeout = TimeSpan.FromSeconds(10), // 连接超时时间
            };

            // 证书策略统一走框架的 Wlniao.XCore.ServerCertificateCustomValidation：
            // 其内部的 XCore.AcceptAnyServerCertificate 放行开关（默认 false，按标准链校验；
            // 需放行全部服务端证书时显式设为 true 或配置 WLN_ACCEPT_ANY_CERT=true）既覆盖本处，
            // 也覆盖 XApi / LokiLoger / XCenter 等所有复用 ServerCertificateCustomValidationCallback 的调用方，
            // 避免出现「某个 HttpClient 放行、另一个不放行」的割裂。
            // SslOptions 的回调是旧式委托（object + X509Certificate），XCore 的策略方法签名是
            // (HttpRequestMessage, X509Certificate2, X509Chain, SslPolicyErrors)，两者不兼容，这里做适配。
            handler.SslOptions.RemoteCertificateValidationCallback = Wlniao.XCore.ValidateServerCertificate;

            return new HttpClient(handler);
        }

        /// <summary>
        /// 刷新HttpClient实例
        /// </summary>
        /// <param name="state"></param>
        private void RefreshHttpClient(object state)
        {
            var toDispose = new List<HttpClient>();
            lock (_lock)
            {
                if (_disposed) return;

                try
                {
                    var oldClient = _httpClient;
                    _httpClient = CreateNewHttpClient();

                    if (oldClient != null)
                    {
                        _retired.Add((oldClient, DateTime.UtcNow + _retireGrace));
                    }

                    // 顺带清理已过宽限期的旧实例
                    var now = DateTime.UtcNow;
                    for (var i = _retired.Count - 1; i >= 0; i--)
                    {
                        if (_retired[i].DueUtc <= now)
                        {
                            toDispose.Add(_retired[i].Client);
                            _retired.RemoveAt(i);
                        }
                    }
                }
                catch
                {
                    // 如果刷新失败，继续使用当前客户端
                }
            }

            // 释放动作放到锁外，避免 Dispose 的耗时拖住其它线程的读取
            DisposeClients(toDispose);
        }

        /// <summary>
        /// 逐个释放 HttpClient，忽略清理阶段的异常——日志/网络清理不应反向影响调用方。
        /// </summary>
        /// <param name="clients">待释放实例</param>
        private static void DisposeClients(List<HttpClient> clients)
        {
            foreach (var client in clients)
            {
                try
                {
                    client?.Dispose();
                }
                catch
                {
                    // 忽略清理异常
                }
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            var toDispose = new List<HttpClient>();
            lock (_lock)
            {
                if (_disposed) return;

                _disposed = true;
                _timer?.Dispose();
                if (_httpClient != null)
                {
                    toDispose.Add(_httpClient);
                    _httpClient = null;
                }
                foreach (var item in _retired)
                {
                    toDispose.Add(item.Client);
                }
                _retired.Clear();
            }

            DisposeClients(toDispose);
        }
    }
}