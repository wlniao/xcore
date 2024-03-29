﻿/*==============================================================================
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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Server.Kestrel.Core;

namespace Wlniao
{
    /// <summary>
    /// XCore内部运行信息及状态
    /// </summary>
    public partial class XCore
    {
        /// <summary>
        /// 单线程锁
        /// </summary>
        internal static Object Lock = new object();
        /// <summary>
        /// 内部输出开关
        /// </summary>
        internal static Boolean _console = false;
        /// <summary>
        /// 当前日志输出等级
        /// </summary>
        private static Log.LogLevel _log_level = Log.LogLevel.None;
        /// <summary>
        /// 当前日志输出路径
        /// </summary>
        private static string _log_path = null;
        /// <summary>
        /// 当前日志输出工具
        /// </summary>
        private static string _log_provider = null;
        /// <summary>
        /// 重新初始化状态
        /// </summary>
        public static void Init()
        {
            Config.Clear();
            XServer.Common.Init();
            _log_level = Log.LogLevel.None;
            _console = true;
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
                Console.WriteLine("System Name          Wlniao XCore");
                Console.WriteLine("Version              " + Version);
                Console.WriteLine("\r\nXCore Runtime:");
                //Console.WriteLine("    Language         " + lang.GetLang());
                Console.WriteLine("    XServerId        " + XServerId);
                Console.WriteLine("    StartupRoot      " + StartupRoot);
                Console.WriteLine("    ProgramVersion   " + ProgramVersion);
                Console.WriteLine("    LogLevel         " + LogLevel.ToString());
                Console.WriteLine("    Cache            " + (Cache.cType == Caching.CacheType.Redis ? "Redis" : "InMemory"));
                Console.WriteLine("");
                Console.WriteLine("\r\nModules:");
                Console.WriteLine("    OpenApi          Load Finish");
                Console.WriteLine("    XServer          Load Finish");
                Console.WriteLine("    HttpEngine       Load Finish");
                Console.WriteLine("    XCore.Aliyun     Not Found");
                Console.WriteLine("    PublishTime      " + DateTools.FormatUnix(PublishTime));
                Console.WriteLine("\r\nInit Use:        " + DateTime.Now.Subtract(StartupTime).TotalMilliseconds.ToString("F2") + "ms");
                Console.WriteLine("");
            }
            catch { }
        }

        /// <summary>
        /// 服务停用及停用消息
        /// </summary>
        /// <param name="node"></param>
        /// <param name="code"></param>
        /// <param name="message"></param>
        public static void ServiceStop(String node, String code = "302", String message = "服务器正在维护中 Server maintenance.")
        {
            var json = Newtonsoft.Json.JsonConvert.SerializeObject(new { node, code, success = false, message });
            new WebHostBuilder().UseKestrel(o => { o.Listen(System.Net.IPAddress.IPv6Any, ListenPort); }).Configure(app =>
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("Node " + node + " on maintenance mode: " + code);
                Console.ForegroundColor = ConsoleColor.White;
                app.Run((HttpContext context) =>
                {
                    context.Response.Headers.TryAdd("Content-Type", "application/json");
                    return context.Response.WriteAsync(json);
                });
            }).Build().Run();
        }

        /// <summary>
        /// HTTPS证书验证
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        public static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
        {
            return true;
        }
        /// <summary>
        /// 关闭服务端SSL证书检查
        /// </summary>
        public static void CloseServerCertificateValidation()
        {
            System.Net.ServicePointManager.ServerCertificateValidationCallback = new System.Net.Security.RemoteCertificateValidationCallback(ValidateServerCertificate);
        }

        #region 系统信息
        private static Int64 nowUnix = 0;
        private static String startupRoot = null;
        private static String _XServerId = null;
        private static String _XServerIP = null;
        private static String _WebNode = null;
        private static String _WebHost = null;
        /// <summary>
        /// 当前系统Unix时间戳
        /// </summary>
        public static Int64 NowUnix
        {
            get
            {
                if (nowUnix == 0)
                {
                    nowUnix = 1;
                    nowUnix = DateTools.GetUnix();
                    System.Threading.Tasks.Task.Run(() =>
                    {
                        while (true)
                        {
                            System.Threading.Tasks.Task.Delay(1000).Wait();
                            nowUnix = DateTools.GetUnix();
                        }
                    });
                }
                return nowUnix;
            }
        }
        /// <summary>
        /// 程序启动时间
        /// </summary>
        public static DateTime StartupTime = DateTime.Now;
        /// <summary>
        /// 程序启动路径，默认为 /
        /// </summary>
        public static String StartupRoot
        {
            get
            {
                if (string.IsNullOrEmpty(startupRoot))
                {
                    startupRoot = Config.GetEnvironment("WLN_STARTUP_ROOT");
                    if (string.IsNullOrEmpty(startupRoot))
                    {
                        startupRoot = System.IO.Directory.GetCurrentDirectory();
                    }
                    if (startupRoot.IndexOf('/') >= 0)
                    {
                        Runtime.SysInfo.IsLinux = true;
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
                        Runtime.SysInfo.IsWindows = true;
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
        public const String FrameworkRoot = "xcore";
        private static int port = 0;
        /// <summary>
        /// 默认监听端口
        /// </summary>
        public static Int32 ListenPort
        {
            get
            {
                if (port == 0)
                {
                    port = cvt.ToInt(Config.GetEnvironment("WLN_LISTEN_PORT"));
                    if (port <= 0)
                    {
                        port = cvt.ToInt(Config.GetConfigs("WLN_LISTEN_PORT"));
                        if (port <= 0)
                        {
                            port = 5000;
                        }
                    }
                }
                return port;
            }
        }
        /// <summary>
        /// 默认监听地址
        /// </summary>
        public static String ListenUrls
        {
            get
            {
                var listenUrls = Config.GetEnvironment("WLN_LISTEN_URLS");
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
        public static Boolean IsDevTest
        {
            get
            {
                return Config.GetSetting("WLN_DEVTEST").ToLower() == "true";
            }
        }
        /// <summary>
        /// 服务器Id
        /// </summary>
        public static string XServerId
        {
            get
            {
                if (_XServerId == null)
                {
                    _XServerId = Config.GetSetting("XServerId");
                }
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
                if (attributes != null && attributes.Length > 0)
                {
                    return ((System.Reflection.AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
                }
                return "";
            }
        }
        /// <summary>
        /// 当前程序版本号
        /// </summary>
        public static string ProgramVersion
        {
            get
            {
                var attributes = System.Reflection.Assembly.GetEntryAssembly().GetCustomAttributes(typeof(System.Reflection.AssemblyInformationalVersionAttribute), true);
                if (attributes != null && attributes.Length > 0)
                {
                    return ((System.Reflection.AssemblyInformationalVersionAttribute)attributes[0]).InformationalVersion;
                }
                return "";
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
                if (files != null)
                {
                    return DateTools.GetUnix(new System.IO.FileInfo(files.Location).LastWriteTime);
                }
                return 0;
            }
        }

        /// <summary>
        /// 当前程序Web服务节点(通过WLN_NODE进行设置)
        /// </summary>
        public static string WebNode
        {
            get
            {
                if (_WebNode == null)
                {
                    _WebNode = Config.GetSetting("WLN_NODE");
                }
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
                if (_WebHost == null)
                {
                    _WebHost = Config.GetSetting("WLN_HOST");
                }
                return _WebHost;
            }
        }
        /// <summary>
        /// 当前日志输出路径
        /// </summary>
        internal static string LogPath
        {
            get
            {
                if (_log_path == null)
                {
                    _log_path = Config.GetSetting("WLN_LOG_PATH");
                    if (string.IsNullOrEmpty(_log_path))
                    {
                        _log_path = XCore.FrameworkRoot + "/logs";
                    }
                }
                return _log_path;
            }
        }
        /// <summary>
        /// 当前日志输出等级
        /// </summary>
        internal static Log.LogLevel LogLevel
        {
            get
            {
                if (_log_level == Log.LogLevel.None)
                {
                    var level = Config.GetSetting("WLN_LOG_LEVEL");
                    if (!string.IsNullOrEmpty(level))
                    {
                        try
                        {
                            _log_level = (Log.LogLevel)Enum.Parse(typeof(Log.LogLevel), level, true);
                        }
                        catch { }
                    }
                    if (_log_level == Log.LogLevel.None)
                    {
                        _log_level = Log.LogLevel.Error;
                    }
                }
                return _log_level;
            }
        }
        /// <summary>
        /// 当前日志输出工具
        /// </summary>
        /// <returns></returns>
        internal static string LogProvider
        {
            get
            {
                if (_log_provider == null)
                {
                    _log_provider = Config.GetSetting("WLN_LOG_PROVIDER");
                    if (string.IsNullOrEmpty(_log_provider))
                    {
                        _log_provider = "file";
                    }
                    else
                    {
                        _log_provider = _log_provider.ToLower();
                    }
                }
                return _log_provider;
            }
        }
        #endregion


        #region 其它方法
        /// <summary>
        /// 设置日志提供工具
        /// </summary>
        /// <param name="LogLevel"></param>
        /// <param name="Provider"></param>
        public static void SetLogger(String LogLevel = "debug", String Provider = "file")
        {
            _log_level = (Log.LogLevel)Enum.Parse(typeof(Log.LogLevel), LogLevel, true);
            _log_provider = Provider;
            Config.SetConfigs("WLN_LOG_LEVEL", LogLevel);
            Config.SetConfigs("WLN_LOG_PROVIDER", Provider);
        }
        #endregion
    }
}