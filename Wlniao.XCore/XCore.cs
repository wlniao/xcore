/*==============================================================================
    文件名称：XCore.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：XCore内部运行信息及状态
================================================================================
 
    Copyright 2015 XieChaoyi

    Licensed under the Apache License, Version 2.0 (the "License");
    you may not use this file except in compliance with the License.
    You may obtain a copy of the License at

               http://www.apache.org/licenses/LICENSE-2.0

    Unless required by applicable law or agreed to in writing, software
    distributed under the License is distributed on an "AS IS" BASIS,
    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
    See the License for the specific language governing permissions and
    limitations under the License.

===============================================================================*/
using System;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Json;
using System.Text.Unicode;
using System.Text.Encodings.Web;
using Wlniao.Log;

namespace Wlniao
{
    /// <summary>
    /// XCore内部运行信息及状态
    /// </summary>
    public class XCore
    {
        /// <summary>
        /// 单线程锁
        /// </summary>
        public static object Lock = new();
        /// <summary>
        /// 框架启动计时器（单调时钟）。
        /// 不用 DateTime.Now 是因为墙钟受 NTP 校时、手动改时间影响，可能算出负值或跳变。
        /// </summary>
        internal static readonly System.Diagnostics.Stopwatch _startup_watch = System.Diagnostics.Stopwatch.StartNew();
        /// <summary>
        /// 最近一次 Init() 的内部耗时（毫秒），尚未执行过为 -1
        /// </summary>
        private static double _init_use_ms = -1;
        /// <summary>
        /// 重新初始化状态
        /// </summary>
        public static void Init()
        {
            var sw = System.Diagnostics.Stopwatch.StartNew();
            Config.Clear();
            Loger.logLevel = LogLevel.None;
            _init_use_ms = sw.Elapsed.TotalMilliseconds;
        }
        /// <summary>
        /// 输出系统信息
        /// </summary>
        public static void Info()
        {
            try
            {
                // 必须在任何输出之前取样：下面 ASCII 那 20 行同步控制台 I/O 在 Windows 上往往是最大耗时项，
                // 放到打印前再取会把输出耗时也算进启动耗时里。
                var startupElapsed = _startup_watch.Elapsed.TotalMilliseconds;
                Console.ForegroundColor = ConsoleColor.DarkCyan;
                if (!Runtime.SysInfo.IsWindows)
                {
                    Console.WriteLine("      ###    ###           #######            ##          ##    ##           ##              ###########    ");
                    Console.WriteLine("      ####   ####          ### ###            ###         ##    ##          +###            #############   ");
                    Console.WriteLine("       ###    ###         #### ###            ####        ##    ##         .#####          ###############  ");
                    Console.WriteLine("       ####    ###        ###  ###            #####       ##    ##         #######        ####         #### ");
                    Console.WriteLine("        ###    ####      ###   ###            ######      ##    ##        +### ###.      ####           ####");
                    Console.WriteLine("         ###    ###     ####   ###            ## ####     ##    ##        #### ####      ###             ###");
                    Console.WriteLine("         ####   ####    ###    ###            ##  ####    ##    ##       +###   ###.     ###             ###");
                    Console.WriteLine("          ###   ####   ####    ###            ##   ####   ##    ##       ###     ###     ###             ###");
                    Console.WriteLine("          ####  #####  ###     ###            ##    ##### ##    ##      #### ########    ###             ###");
                    Console.WriteLine("           ### ###########     ###            ##     #######    ##     +### ##########   ###            ### ");
                    Console.WriteLine("           ####### ######      ###            ##      ######    ##    +####       ####.   ###           ### ");
                    Console.WriteLine("            #####  #####       ###            ##        ####    ##    ####         ####    #####     #####  ");
                    Console.WriteLine("             ###    ####       #############  ##         ###    ##   .###           ####    #############   ");
                    Console.WriteLine("             ###    ###        #############  ##          ##    ##   ####           ####     ###########    ");
                }
                else
                {
                    Console.WriteLine("      @@@@@     @@@@@               @@@@+ @@@                ;@,             @@@    @@@               #@;                   '@@@@@@@@@@,       ");
                    Console.WriteLine("       @@@@`     @@@@              `@@@@  @@@                @@@+            @@@    @@@              @@@@#               ,@@@@@@@@@@@@@@@@     ");
                    Console.WriteLine("       +@@@@     +@@@@             @@@@:  @@@                @@@@+           @@@    @@@             +@@@@@`             @@@@@@@@@@@@@@@@@@@.   ");
                    Console.WriteLine("        @@@@.     @@@@`           :@@@@   @@@                @@@@@+          @@@    @@@             @@@@@@@            @@@@@@@@@@@@@@@@@@@@@`  ");
                    Console.WriteLine("        :@@@@     :@@@@           @@@@`   @@@                @@@@@@+         @@@    @@@            @@@@@@@@:          +@@@@'            @@@@@  ");
                    Console.WriteLine("         @@@@:     @@@@,         +@@@@    @@@                @@@@@@@+        @@@    @@@           .@@@@+@@@@          @@@@`              @@@@# ");
                    Console.WriteLine("         .@@@@     .@@@@         @@@@     @@@                @@@,@@@@+       @@@    @@@           @@@@+ @@@@#        '@@@,                @@@@ ");
                    Console.WriteLine("          @@@@'     @@@@'       #@@@#     @@@                @@@ ,@@@@+      @@@    @@@          +@@@@  .@@@@        @@@@                 .@@@ ");
                    Console.WriteLine("           @@@@     `@@@@       @@@@      @@@                @@@  ,@@@@+     @@@    @@@          @@@@`   @@@@@       @@@'                  @@@:");
                    Console.WriteLine("           @@@@#     @@@@+     @@@@'      @@@                @@@   ,@@@@+    @@@    @@@         @@@@@     @@@@,      @@@.                  @@@+");
                    Console.WriteLine("            @@@@    `@@@@@     @@@@       @@@                @@@    ,@@@@+   @@@    @@@        .@@@@      +@@@@      @@@                   @@@+");
                    Console.WriteLine("            #@@@@   @@@@@@#   @@@@.       @@@                @@@     ,@@@@+  @@@    @@@        @@@@' @@@@@@@@@@#     @@@,                  @@@:");
                    Console.WriteLine("             @@@@  `@@@@@@@  `@@@@        @@@                @@@      ,@@@@+ @@@    @@@       +@@@@ .@@@@@@@@@@@     @@@@                  @@@ ");
                    Console.WriteLine("             '@@@@ @@@@+@@@@ @@@@`        @@@                @@@       ,@@@@+@@@    @@@       @@@@` @@@@@@@@@@@@@    ;@@@                 @@@@ ");
                    Console.WriteLine("              @@@@,@@@@ @@@@,@@@@         @@@                @@@        ,@@@@@@@    @@@      @@@@@ ,:::::::::@@@@.    @@@@               `@@@# ");
                    Console.WriteLine("              ,@@@@@@@  ,@@@@@@@          @@@                @@@         ,@@@@@@    @@@     .@@@@            +@@@@    +@@@@             ,@@@@  ");
                    Console.WriteLine("               @@@@@@#   @@@@@@#          @@@                @@@          ,@@@@@    @@@     @@@@:             @@@@+    @@@@@@,       `'@@@@@.  ");
                    Console.WriteLine("               `@@@@@    `@@@@@           @@@@@@@@@@@@@@@@   @@@           ,@@@@    @@@    +@@@@              `@@@@     @@@@@@@@@@@@@@@@@@@'   ");
                    Console.WriteLine("                @@@@;     @@@@:           @@@@@@@@@@@@@@@@   @@@            ;@@@    @@@    @@@@                @@@@@     #@@@@@@@@@@@@@@@@`    ");
                    Console.WriteLine("                 @@@       @@@            @@@@@@@@@@@@@@@@   @@@             ,@@    @@@   @@@@#                 @@@@`      #@@@@@@@@@@@@,      ");
                }
                Console.ResetColor();
                Console.WriteLine("");
                if (!string.IsNullOrEmpty(XServerId))
                {
                    Console.WriteLine("XServerId            " + XServerId);
                }
                if (!string.IsNullOrEmpty(WebHost))
                {
                    Console.WriteLine("Web Host             " + WebHost);
                }
                if (!string.IsNullOrEmpty(WebNode))
                {
                    Console.WriteLine("Program Node         " + WebNode);
                }
                if (!string.IsNullOrEmpty(ProgramVersion))
                {
                    Console.WriteLine("Program Version      " + ProgramVersion);
                }
                Console.WriteLine("\r\nXCore Runtime:");
                Console.WriteLine("    LogType          " + Loger.LogProvider.GetType().Name);
                Console.WriteLine("    LogLevel         " + Loger.LogLevel.ToString());
                Console.WriteLine("    Language         " + Runtime.Lang.GetLang());
                Console.WriteLine("    StartupRoot      " + StartupRoot);
                Console.WriteLine("    StartupTime      " + StartupTime);
                Console.WriteLine("    PublishTime      " + DateTools.FormatUnix(PublishTime));
                Console.WriteLine("    Version          " + Version);
                Console.WriteLine("");
                Console.WriteLine("\r\nModules:");
                Console.WriteLine("    OpenApi          Load Finish");
                Console.WriteLine("    XServer          Load Finish");
                Console.WriteLine("    HttpEngine       Load Finish");
                Console.WriteLine("");
                Console.WriteLine("    Startup Elapsed  " + startupElapsed.ToString("F2") + "ms");
                if (_init_use_ms >= 0)
                {
                    Console.WriteLine("    Init Use         " + _init_use_ms.ToString("F2") + "ms");
                }
                Console.WriteLine("");
            }
            catch (Exception ex)
            {
                Loger.Error($"XCore信息输出异常: {ex.Message}, 堆栈: {ex.StackTrace}");
            }
        }


        /// <summary>
        /// 托管的SSL证书检查服务
        /// </summary>
        public static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback = ServerCertificateCustomValidation;

        /// <summary>
        /// 全局 TLS 服务端证书放行开关的配置项名称（环境变量同名者优先于 xcore.config）
        /// </summary>
        internal const string AcceptAnyServerCertificateKey = "WLN_ACCEPT_ANY_CERT";

        /// <summary>
        /// -1 表示尚未解析；0/1 表示最终取值。
        /// 用 int 而非 bool? 是为了保证读写原子性（Nullable&lt;bool&gt; 的读写不保证原子）
        /// </summary>
        private static int _acceptAnyServerCertificate = -1;

        /// <summary>
        /// “全部放行”的告警是否已输出过（进程内仅一次）
        /// </summary>
        private static int _acceptAnyServerCertificateWarned = 0;

        /// <summary>
        /// 全局统一的 TLS 服务端证书放行开关，默认 false（收紧，按标准链校验）
        /// </summary>
        /// <remarks>
        /// <para><b>false</b>（默认）：<see cref="ServerCertificateCustomValidation"/> 先按标准链校验，通过才放行；
        /// 未通过时仅放行内置受信任的根 CA / 二级 CA 指纹。过期、域名不匹配、自签、链不完整都会被拒绝。
        /// 这是安全默认值 —— 历史版本是无条件放行，现改为必须显式开启才放行。</para>
        /// <para><b>true</b>：<see cref="ServerCertificateCustomValidation"/> 对任何服务端证书无条件放行，
        /// 过期、域名不匹配、自签、链不完整都会被接受。仅在对接内部自签证书又不想逐台导 CA 时才启用，
        /// 风险是中间人可以用任意证书冒充目标站点。</para>
        /// <para>取值来源按优先级：先取代码显式赋值；未显式赋值时，首次读取按配置项
        /// <c>WLN_ACCEPT_ANY_CERT</c>（环境变量优先，其次 xcore.config）解析一次并缓存 ——
        /// 因此运行期修改配置文件不会自动生效，需要重启或代码赋值。
        /// 解析规则是「只有显式肯定值才放行」：<c>1/true/yes/on</c>（不分大小写）放行，
        /// 空值与无法识别的取值一律收紧，避免配置笔误变成全局放行。</para>
        /// <para>开关实现在 <see cref="ServerCertificateCustomValidation"/> 内部，所以所有复用该策略的
        /// HttpClient / SocketsHttpHandler（XApi、LokiLoger、HttpClientManager 等）统一受控。
        /// 若调用方自行替换了 <see cref="ServerCertificateCustomValidationCallback"/>，本开关不再生效。</para>
        /// </remarks>
        public static bool AcceptAnyServerCertificate
        {
            get
            {
                var value = System.Threading.Volatile.Read(ref _acceptAnyServerCertificate);
                if (value < 0)
                {
                    value = ResolveAcceptAnyServerCertificate() ? 1 : 0;
                    System.Threading.Volatile.Write(ref _acceptAnyServerCertificate, value);
                }
                return value == 1;
            }
            set => System.Threading.Volatile.Write(ref _acceptAnyServerCertificate, value ? 1 : 0);
        }

        /// <summary>
        /// 按配置解析放行开关。默认收紧：只有配置显式给出肯定值才放行，
        /// 空值与无法识别的取值一律收紧 —— 宁可与目标站点握手失败，也不把开关误开成全局放行。
        /// </summary>
        private static bool ResolveAcceptAnyServerCertificate()
        {
            return ParseAcceptAnyServerCertificate(Config.GetConfigs(AcceptAnyServerCertificateKey, bool.FalseString));
        }

        /// <summary>
        /// 把配置项的文本取值解析为放行开关，仅显式肯定值（1/true/yes/on，不分大小写）放行，其余一律收紧。
        /// 抽出独立方法是为了让解析规则可以被单元测试直接覆盖（<see cref="ResolveAcceptAnyServerCertificate"/> 依赖进程级配置，无法逐用例注入）。
        /// </summary>
        /// <param name="configured">配置项取值，可为 null / 空白</param>
        internal static bool ParseAcceptAnyServerCertificate(string? configured)
        {
            if (string.IsNullOrWhiteSpace(configured))
            {
                return false;
            }
            switch (configured.Trim().ToLowerInvariant())
            {
                case "1":
                case "true":
                case "yes":
                case "on":
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>
        /// 进程内首次命中“全部放行”时输出一次告警，便于发现无人知晓的全局 TLS 放行
        /// </summary>
        private static void WarnAcceptAnyServerCertificateOnce()
        {
            // Exchange 先置位再写日志：即便日志链路本身要走 HTTPS（如 LokiLoger）也不会递归重复告警
            if (System.Threading.Interlocked.Exchange(ref _acceptAnyServerCertificateWarned, 1) == 0)
            {
                try
                {
                    Loger.Warn("TLS 证书校验当前处于“全部放行”状态（XCore.AcceptAnyServerCertificate=true）："
                        + "任意服务端证书都会被接受，存在中间人攻击风险。"
                        + "如需收紧请设置 XCore.AcceptAnyServerCertificate=false，"
                        + "或配置 " + AcceptAnyServerCertificateKey + "=false。");
                }
                catch
                {
                    // 证书回调里不允许向外抛异常，日志失败不影响握手判定
                }
            }
        }
        
        /// <summary>
        /// 基础的JSON序列化选项
        /// </summary>
        public static JsonSerializerOptions JsonSerializerOptions = new ()
        {
            Encoder = JavaScriptEncoder.Create(UnicodeRanges.All), //Json序列化的时候对中文进行处理
            UnknownTypeHandling = System.Text.Json.Serialization.JsonUnknownTypeHandling.JsonNode
        };
        
        /// <summary>
        /// 内置SSL证书检查服务（放通内部自签CA）
        /// </summary>
        /// <param name="requestMessage"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslErrors"></param>
        /// <returns></returns>
        internal static bool ServerCertificateCustomValidation(HttpRequestMessage requestMessage, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors sslErrors)
        {
            // 全局放行开关（默认收紧）：命中则跳过后续一切校验
            if (AcceptAnyServerCertificate)
            {
                WarnAcceptAnyServerCertificateOnce();
                return true;
            }
            if (sslErrors == SslPolicyErrors.None)
            {
                return true;
            }
            // 证书链空白时，直接返回异常
            if (chain == null || chain.ChainPolicy.ExtraStore.Count == 0)
            {
                return false;
            }
            // 证书链大于0（包含根证书、中间 CA 等）遍历证书链查找受信任证书
            foreach (var cert in chain.ChainElements.Select(chainElement => chainElement.Certificate))
            {
                // 根证书是自签名的（颁发者与主题相同），判断CA证书的指纹是否受信任
                if (cert.Issuer == cert.Subject && cert.Thumbprint?.ToLower() is "ff32d07177d55d328a1307595ca21331e1b8149f")
                {
                    return true;
                }
                // 中间证书，判断是否长效期的二级CA
                if(cert.Thumbprint?.ToLower() is "202a1b4447e29152d34a08e5821961ea3a4b16ed" or "c7a791caaf68b5b46bde11175463f11071fa8675")
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 旧式 TLS 回调（object + X509Certificate）到框架统一证书策略的适配器
        /// </summary>
        /// <param name="sender">回调发起方，框架策略不使用</param>
        /// <param name="certificate">服务端证书</param>
        /// <param name="chain">证书链</param>
        /// <param name="sslErrors">校验错误</param>
        /// <returns>是否接受该证书</returns>
        /// <remarks>
        /// SocketsHttpHandler.SslOptions.RemoteCertificateValidationCallback 用的是旧式委托签名，
        /// 与 <see cref="ServerCertificateCustomValidation"/> 不同，这里做适配以免各处各写一份 lambda。
        /// </remarks>
        internal static bool ValidateServerCertificate(object sender, X509Certificate? certificate, X509Chain? chain, SslPolicyErrors sslErrors)
        {
            return ServerCertificateCustomValidation(null!, certificate as X509Certificate2, chain, sslErrors);
        }

        #region 系统信息
        private static short microNode = 0;
        private static string startupTime = null;
        private static string startupRoot = null;
        private static string sessionEncryptKey = null;
        private static string _XServerId = null;
        private static string _WebNode = null;
        private static string _WebHost = null;
        private static string _Webroxy = null;
        /// <summary>
        /// 程序启动时间
        /// </summary>
        public static string StartupTime
        {
            get
            {
                if (string.IsNullOrEmpty(startupTime))
                {
                    startupTime = DateTools.Format();
                }
                return startupTime;
            }
        }
        /// <summary>
        /// 程序启动路径，默认为 /
        /// </summary>
        public static string StartupRoot
        {
            get
            {
                if (string.IsNullOrEmpty(startupRoot))
                {
                    //startupRoot = Config.GetConfigs("WLN_STARTUP_ROOT");
                    //if (string.IsNullOrEmpty(startupRoot))
                    //{
                    //    startupRoot = System.IO.Directory.GetCurrentDirectory();
                    //}
                    startupRoot = Config.GetEnvironment("WLN_STARTUP_ROOT", System.IO.Directory.GetCurrentDirectory());
                    if (startupRoot.IndexOf('/') >= 0)
                    {
                        if (startupRoot.IndexOf("/bin", StringComparison.Ordinal) > 0)
                        {
                            startupRoot = startupRoot.Substring(0, startupRoot.IndexOf("/bin", StringComparison.Ordinal) + 1);
                        }
                        else if (!startupRoot.EndsWith("/"))
                        {
                            startupRoot += "/";
                        }
                    }
                    else
                    {
                        if (startupRoot.IndexOf("\\bin", StringComparison.Ordinal) > 0)
                        {
                            startupRoot = startupRoot.Substring(0, startupRoot.IndexOf("\\bin", StringComparison.Ordinal) + 1);
                        }
                        else if (!startupRoot.EndsWith("\\"))
                        {
                            startupRoot += "\\";
                        }
                    }
                    Init();
                }
                return startupRoot;
            }
        }
        /// <summary>
        /// 框架的根目录
        /// </summary>
        public const string FrameworkRoot = "xcore";
        private static int _port = 0;
        /// <summary>
        /// 默认监听端口
        /// </summary>
        public static int ListenPort
        {
            get
            {
                if (_port != 0)
                {
                    return _port;
                }
                _port = Convert.ToInt(Config.GetConfigs("WLN_LISTEN_PORT"));
                if (_port <= 0)
                {
                    _port = 5000;
                }
                return _port;
            }
        }
        /// <summary>
        /// 默认监听地址
        /// </summary>
        public static string ListenUrls
        {
            get
            {
                var listenUrls = Config.GetConfigs("WLN_LISTEN_URLS");
                if (string.IsNullOrEmpty(listenUrls))
                {
                    return "http://*:" + ListenPort;
                }
                else
                {
                    return listenUrls;
                }
            }
        }
        /// <summary>
        /// 是否为开发测试环境
        /// </summary>
        public static bool IsDevTest => Config.GetConfigs("WLN_DEVTEST").ToLower() == "true";

        /// <summary>
        /// 程序访问是否通过HTTPS
        /// </summary>
        public static bool IsUseHttps { get; set; } = true;
        
        /// <summary>
        /// 是否微服务运行节点
        /// </summary>
        public static bool IsMicroservicesNode
        {
            get
            {
                if (microNode != 0)
                {
                    return microNode > 0;
                }
                if (Environment.GetEnvironmentVariable("MicroservicesNode") == "true")
                {
                    microNode = 1;
                }
                else
                {
                    microNode = -1;
                }
                return microNode > 0;
            }
        }

        /// <summary>
        /// 服务器Id
        /// </summary>
        public static string XServerId
        {
            get
            {
                _XServerId ??= Config.GetConfigs("XServerId");
                return _XServerId;
            }
        }
        /// <summary>
        /// XCore版本号
        /// </summary>
        public static string Version
        {
            get
            {
                var attributes = System.Reflection.Assembly.GetExecutingAssembly().GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), true);
                return attributes is { Length: > 0 } ? ((System.Reflection.AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion : "";
            }
        }
        /// <summary>
        /// 当前程序版本号
        /// </summary>
        public static string ProgramVersion
        {
            get
            {
                var attributes = System.Reflection.Assembly.GetEntryAssembly()?.GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), true);
                return attributes is { Length: > 0 } ? ((System.Reflection.AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion : "";
            }
        }
        /// <summary>
        /// 当前程序发布时间(UnixTime)
        /// </summary>
        public static long PublishTime
        {
            get
            {
                var exePath = Environment.ProcessPath;
                return exePath != null ? DateTools.GetUnix(new System.IO.FileInfo(exePath).LastWriteTime) : 0;
            }
        }

        /// <summary>
        /// 当前程序Web服务节点(通过WLN_NODE进行设置)
        /// </summary>
        public static string WebNode
        {
            get
            {
                _WebNode ??= Config.GetConfigs("WLN_NODE", "xcore");
                return _WebNode;
            }
        }
        
        /// <summary>
        /// 当前程序Web服务地址(通过WLN_HOST进行设置)
        /// </summary>
        public static string WebHost
        {
            get
            {
                _WebHost ??= Config.GetConfigs("WLN_HOST", "http://127.0.0.1:" + ListenPort);
                return _WebHost;
            }
        }
        
        /// <summary>
        /// Webroxy代理服务器地址
        /// </summary>
        public static string Webroxy
        {
            get
            {
                _Webroxy ??= Config.GetConfigs("Webroxy");
                return _Webroxy;
            }
        }
        
        /// <summary>
        /// 根据配置信息计算得出相对固定的加密密钥
        /// </summary>
        public static string SessionEncryptKey
        {
            get
            {
                if (sessionEncryptKey != null)
                {
                    return sessionEncryptKey;
                }
                sessionEncryptKey = Config.GetConfigs("SessionEncryptKey");
                if (string.IsNullOrEmpty(sessionEncryptKey))
                {
                    sessionEncryptKey = Encryptor.Md5Encryptor16(WebNode + WebHost + DbConnectInfo.WLN_CONNSTR_TYPE + DbConnectInfo.WLN_CONNSTR_NAME).ToLower();
                }
                return sessionEncryptKey;
            }
        }
        #endregion

    }
}