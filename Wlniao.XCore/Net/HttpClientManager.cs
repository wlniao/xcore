using System;
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
        /// HttpClient共享管理器，用于解决DNS缓存问题
        /// </summary>
        private static HttpClientManager _sharedManage;
        
        /// <summary>
        /// 框架共享HttpClient实例
        /// </summary>
        public static HttpClient SharedInstance
        {
            get
            {
                _sharedManage ??= new HttpClientManager();
                return _sharedManage.HttpClient;
            }
        }

        /// <summary>
        /// 获取当前HttpClient实例
        /// </summary>
        public HttpClient HttpClient 
        { 
            get 
            {
                EnsureNotDisposed();
                return _httpClient; 
            } 
        }

        /// <summary>
        /// 构造函数
        /// </summary>
        /// <param name="dnsRefreshInterval">DNS刷新间隔，默认12分钟</param>
        public HttpClientManager(TimeSpan? dnsRefreshInterval = null)
        {
            var interval = dnsRefreshInterval ?? TimeSpan.FromMinutes(12); // 默认12分钟刷新一次
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
                PooledConnectionLifetime = TimeSpan.FromMinutes(10), // 连接存活时间略小于刷新周期
                MaxConnectionsPerServer = 200, // 最大并发数
                UseProxy = false, // 不使用代理
                AllowAutoRedirect = true, // 允许自动重定向
                ConnectTimeout = TimeSpan.FromSeconds(10), // 连接超时时间
            };

            // 设置证书验证回调
            handler.SslOptions.RemoteCertificateValidationCallback = (message, cert, chain, errors) => true;

            return new HttpClient(handler);
        }

        /// <summary>
        /// 刷新HttpClient实例
        /// </summary>
        /// <param name="state"></param>
        private void RefreshHttpClient(object state)
        {
            lock (_lock)
            {
                if (_disposed) return;

                try
                {
                    var oldClient = _httpClient;
                    _httpClient = CreateNewHttpClient();
                    
                    // 异步释放旧的HttpClient
                    _ = Task.Run(async () =>
                    {
                        try
                        {
                            oldClient?.Dispose();
                        }
                        catch
                        {
                            // 忽略清理异常
                        }
                    });
                }
                catch
                {
                    // 如果刷新失败，继续使用当前客户端
                }
            }
        }

        /// <summary>
        /// 确保未被释放
        /// </summary>
        private void EnsureNotDisposed()
        {
            if (_disposed)
            {
                throw new ObjectDisposedException(nameof(HttpClientManager));
            }
        }

        /// <summary>
        /// 释放资源
        /// </summary>
        public void Dispose()
        {
            lock (_lock)
            {
                if (_disposed) return;
                
                _disposed = true;
                _timer?.Dispose();
                _httpClient?.Dispose();
            }
        }
    }
}