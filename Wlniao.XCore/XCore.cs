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
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;

namespace Wlniao
{
    /// <summary>
    /// XCore内部运行信息及状态
    /// </summary>
    public partial class XCore
    {
        /// <summary>
        /// 重新初始化状态
        /// </summary>
        public static void Init()
        {
            Config.Clear();
            XServer.Common.Init();
            _level = Log.LogLevel.None;
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
                    Console.SetWindowSize(150, 50);
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
        /// HTTPS证书验证
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="certificate"></param>
        /// <param name="chain"></param>
        /// <param name="sslPolicyErrors"></param>
        /// <returns></returns>
        internal static bool ValidateServerCertificate(object sender, X509Certificate certificate, X509Chain chain, SslPolicyErrors sslPolicyErrors)
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
        private static String startupRoot = null;
        private static String _XServerId = null;
        private static String _XServerIP = null;
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
                        Runtime.SysInfo.IsWindows = false;
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
                        Runtime.SysInfo.IsLinux = false;
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
        private static Log.LogLevel _level = Log.LogLevel.None;
        internal static Log.LogLevel LogLevel
        {
            get
            {
                if (_level == Log.LogLevel.None)
                {
                    var level = Config.GetSetting("WLN_LOG_LEVEL");
                    if (!string.IsNullOrEmpty(level))
                    {
                        try
                        {
                            _level = (Log.LogLevel)Enum.Parse(typeof(Log.LogLevel), level, true);
                        }
                        catch { }
                    }
                    if (_level == Log.LogLevel.None)
                    {
                        _level = Log.LogLevel.Error;
                    }
                }
                return _level;
            }
        }
        private static string _provider = null;
        /// <summary>
        /// 默认日志输出工具
        /// </summary>
        /// <returns></returns>
        internal static string LogProvider
        {
            get
            {
                if (_provider == null)
                {
                    _provider = Config.GetConfigs("WLN_LOG_PROVIDER");
                    if (string.IsNullOrEmpty(_provider))
                    {
                        _provider = "file";
                    }
                    else
                    {
                        _provider = _provider.ToLower();
                    }
                }
                return _provider;
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
            _level = (Log.LogLevel)Enum.Parse(typeof(Log.LogLevel), LogLevel, true);
            _provider = Provider;
            Config.SetConfigs("WLN_LOG_LEVEL", LogLevel);
            Config.SetConfigs("WLN_LOG_PROVIDER", Provider);
        }
        #endregion
    }
}