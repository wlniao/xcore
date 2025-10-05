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
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
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
        public static object Lock = new object();
        /// <summary>
        /// 框架初始化时间
        /// </summary>
        internal static DateTime _startup_time = DateTime.Now;
        /// <summary>
        /// 重新初始化状态
        /// </summary>
        public static void Init()
        {
            Config.Clear();
            Loger.logLevel = LogLevel.None;
        }
        /// <summary>
        /// 输出系统信息
        /// </summary>
        public static void Info()
        {
            try
            {
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
                Console.WriteLine("\r\nInit Use:        " + DateTime.Now.Subtract(_startup_time).TotalMilliseconds.ToString("F2") + "ms");
                Console.WriteLine("");
            }
            catch { }
        }


        /// <summary>
        /// 托管的SSL证书检查服务
        /// </summary>
        public static Func<HttpRequestMessage, X509Certificate2?, X509Chain?, SslPolicyErrors, bool> ServerCertificateCustomValidationCallback = ServerCertificateCustomValidation;
        
        /// <summary>
        /// 基础的JSON序列化选项
        /// </summary>
        public static JsonSerializerOptions JsonSerializerOptions = new JsonSerializerOptions
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
        private static bool ServerCertificateCustomValidation(HttpRequestMessage requestMessage, X509Certificate2? certificate, X509Chain? chain, SslPolicyErrors sslErrors)
        {
            if (sslErrors == SslPolicyErrors.None)
            {
                return true;
            }
            if (chain != null && chain.ChainPolicy != null && chain.ChainPolicy.ExtraStore != null && chain.ChainPolicy.ExtraStore.Count > 0)
            {
                var print = chain?.ChainPolicy?.ExtraStore?.LastOrDefault()?.Thumbprint.ToLower();
                if (print == "ff32d07177d55d328a1307595ca21331e1b8149f" || print == "c7a791caaf68b5b46bde11175463f11071fa8675")
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 关闭服务端SSL证书检查
        /// </summary>
        public static void CloseServerCertificateValidation()
        {
            XCore.ServerCertificateCustomValidationCallback = System.Net.Http.HttpClientHandler.DangerousAcceptAnyServerCertificateValidator;
            System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(delegate { return true; });
        }

        #region 系统信息
        private static short microNode = 0;
        private static string startupTime = null;
        private static string startupRoot = null;
        private static string sessionEncryptKey = null;
        private static string _XServerId = null;
        private static string _XServerIP = null;
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
                        if (startupRoot.IndexOf("/bin") > 0)
                        {
                            startupRoot = startupRoot.Substring(0, startupRoot.IndexOf("/bin") + 1);
                        }
                        else if (!startupRoot.EndsWith("/"))
                        {
                            startupRoot += "/";
                        }
                    }
                    else
                    {
                        if (startupRoot.IndexOf("\\bin") > 0)
                        {
                            startupRoot = startupRoot.Substring(0, startupRoot.IndexOf("\\bin") + 1);
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
        private static int port = 0;
        /// <summary>
        /// 默认监听端口
        /// </summary>
        public static int ListenPort
        {
            get
            {
                if (port == 0)
                {
                    port = Convert.ToInt(Config.GetConfigs("WLN_LISTEN_PORT"));
                    if (port <= 0)
                    {
                        port = 5000;
                    }
                }
                return port;
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
        /// 服务器IP
        /// </summary>
        public static string XServerIP
        {
            get
            {
                if (string.IsNullOrEmpty(_XServerIP))
                {
                    _XServerIP = OpenApi.Tool.GetIP();
                }
                return _XServerIP;
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
                var files = System.Reflection.Assembly.GetEntryAssembly();
                return files != null ? DateTools.GetUnix(new System.IO.FileInfo(files.Location).LastWriteTime) : 0;
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