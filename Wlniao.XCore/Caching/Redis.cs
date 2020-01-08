/*==============================================================================
    文件名称：Redis.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：Redis驱动类
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
using System.Net.Sockets;
using System.Text;

namespace Wlniao.Caching
{
    /// <summary>
    /// Redis缓存
    /// </summary>
    public class Redis
    {
        internal static string _host = Config.GetSetting("WLN_REDIS_HOST");
        internal static string _pass = Config.GetSetting("WLN_REDIS_PASS");
        internal static int _port = cvt.ToInt(Config.GetSetting("WLN_REDIS_PORT"));
        internal static bool CanUse()
        {
            if (string.IsNullOrEmpty(_host))
            {
                return false;
            }
            return true;
        }
        private static List<RedisClient> clients = new List<RedisClient>();


        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expire"></param>
        public static Boolean Set(String key, String value, Int32 expire)
        {
            if (expire > 0)
            {
                return rltToBool(SetByPipeline(key, value, expire));
            }
            return rltToBool(SendCmd(RedisCommand.Set, key, value));
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <param name="obj"></param>
        /// <param name="expire"></param>
        /// <returns></returns>
        public static Boolean Set<T>(String key, T obj, Int32 expire)
        {
            var value = Encryptor.Base64Encrypt(Json.ToString(obj));
            return Set(key, value, expire);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public static Boolean Del(String key)
        {
            var rlt = SendCmd(RedisCommand.Del, key);
            if (!string.IsNullOrEmpty(rlt))
            {
                var lines = rlt.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    if (lines[0].StartsWith(":") && cvt.ToInt(lines[0].Substring(1)) > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public static Boolean Exists(String key)
        {
            var rlt = SendCmd(RedisCommand.Exists, key);
            if (!string.IsNullOrEmpty(rlt))
            {
                var lines = rlt.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 0)
                {
                    if (lines[0].StartsWith(":") && cvt.ToInt(lines[0].Substring(1)) > 0)
                    {
                        return true;
                    }
                }
            }
            return false;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public static String Get(String key)
        {
            try
            {
                var rlt = SendCmd(RedisCommand.Get, key);
                var lines = rlt.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 1)
                {
                    if (lines[1].StartsWith("+OK"))
                    {
                        lines[1] = lines[1].Substring(3);
                    }
                    return lines[1];
                }
            }
            catch { }
            return "";
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public static String GetAllowNull(String key)
        {
            try
            {
                var rlt = SendCmd(RedisCommand.Get, key);
                var lines = rlt.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 1)
                {
                    if (lines[1].StartsWith("+OK"))
                    {
                        lines[1] = lines[1].Substring(3);
                    }
                    return lines[1];
                }
            }
            catch { }
            return null;
        }
        /// <summary>
        /// 
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="key"></param>
        /// <returns></returns>
        public static T Get<T>(String key)
        {
            var str = Get(key);
            if (!string.IsNullOrEmpty(str))
            {
                return Json.ToObject<T>(Encryptor.Base64Decrypt(str));
            }
            return default(T);
        }


        private static RedisClient GetClient()
        {
            lock (clients)
            {
                RedisClient client = null;
                var i = 0;
                for (; i < clients.Count; i++)
                {
                    if (clients[i].CanUse)
                    {
                        client = clients[i];
                        client.Use();
                        break;
                    }
                    else if (clients[i].Socket == null || !clients[i].Socket.Connected)
                    {
                        clients.Remove(client);
                    }
                }
                if (client == null && !string.IsNullOrEmpty(_host))
                {
                    client = new RedisClient(_host, _port == 0 ? 6379 : _port);
                    client.Connect();
                    if (!string.IsNullOrEmpty(_pass))
                    {
                        var _rlt = client.SendCommand(RedisCommand.Auth, _pass);
                        if (!rltToBool(_rlt))
                        {
                            log.Fatal("Redis Password Is Error[" + i + "]!");
                            return null;
                        }
                    }
                    client.Use();
                    clients.Add(client);
                }
                return client;
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="cmd"></param>
        /// <param name="args"></param>
        public static String SendCmd(RedisCommand cmd, params string[] args)
        {
            var client = GetClient();
            if (client == null)
            {
                return "";
            }
            return client.SendCommand(cmd, args);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expire"></param>
        public static String SetByPipeline(String key,String value, Int32 expire)
        {
            var client = GetClient();
            if (client == null)
            {
                return "";
            }
            return client.SetByPipeline(key, value, expire);
        }

        #region 基础方法
        private static Boolean rltToBool(String rlt)
        {
            if (!string.IsNullOrEmpty(rlt) && rlt.StartsWith("+OK"))
            {
                return true;
            }
            return false;
        }
        private static String rltToStr(String rlt)
        {
            if (!string.IsNullOrEmpty(rlt))
            {
                var lines = rlt.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length > 1)
                {
                    if (lines[1].StartsWith("+OK"))
                    {
                        lines[1] = lines[1].Substring(3);
                    }
                    return lines[1];
                }
            }
            return "";
        }
        #endregion


        /// <summary>
        /// Redis客户端
        /// </summary>
        public class RedisClient
        {
            /// <summary>
            /// Redis服务地址
            /// </summary>
            private System.Net.IPAddress HostIP = null;
            /// <summary>
            /// Redis服务端口号
            /// </summary>
            private int Port = 6379;
            /// <summary>
            /// Socket 是否正在使用 Nagle 算法。
            /// </summary>
            private bool NoDelaySocket = false;
            private bool Using = false;


            //通信socket
            private Socket socket;
            /// <summary>
            /// 客户端是否可用
            /// </summary>
            public bool CanUse
            {
                get
                {
                    if (!Using && socket != null && socket.Connected)
                    {
                        return true;
                    }
                    return false;
                }
            }
            /// <summary>
            /// 客户端Socket
            /// </summary>
            public Socket Socket
            {
                get
                {
                    return socket;
                }
            }
            /// <summary>
            /// 
            /// </summary>
            public void Use()
            {
                Using = true;
            }
            //接收字节数组
            private byte[] ReceiveBuffer;
            /// <summary>
            /// 
            /// </summary>
            /// <param name="host"></param>
            /// <param name="port"></param>
            /// <param name="nagle"></param>
            public RedisClient(String host = "127.0.0.1", Int32 port = 6379, Boolean nagle = false)
            {
                if (strUtil.IsIP(host))
                {
                    HostIP = System.Net.IPAddress.Parse(host);
                }
                else
                {
                    HostIP = System.Net.Dns.GetHostAddresses(host)[0].MapToIPv4();
                }
                Port = port;
                NoDelaySocket = nagle;
            }
            /// <summary>
            /// 链接服务器
            /// </summary>
            public void Connect()
            {
                if (socket != null && socket.Connected)
                {
                    return;
                }
                else if (socket == null)
                {
                    socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp)
                    {
                        NoDelay = NoDelaySocket
                    };
                }
                socket.Connect(HostIP, Port);
                if (socket.Connected)
                {
                    return;
                }
            }
            /// <summary>
            /// 创建事务
            /// </summary>
            public void Multi()
            {
                SendCommand(RedisCommand.Multi, new string[] { }, true);
            }
            /// <summary>
            /// 执行事务
            /// </summary>
            /// <returns></returns>
            public string Exec()
            {
                return SendCommand(RedisCommand.Exec, new string[] { }, false);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="key"></param>
            /// <param name="value"></param>
            /// <param name="second"></param>
            /// <returns></returns>
            public string SetByPipeline(string key, string value, int second)
            {
                this.Multi();
                this.AddCommand(RedisCommand.Set, key, value);
                this.AddCommand(RedisCommand.Expire, key, second.ToString());
                return this.Exec();
            }
            /// <summary>
            /// 添加命令
            /// </summary>
            /// <param name="command"></param>
            /// <param name="args"></param>
            /// <returns></returns>
            public string AddCommand(RedisCommand command, params string[] args)
            {
                return SendCommand(command, args, true);
            }
            /// <summary>
            /// 发送命令
            /// </summary>
            /// <param name="command"></param>
            /// <param name="args"></param>
            /// <returns></returns>
            public string SendCommand(RedisCommand command, params string[] args)
            {
                return SendCommand(command, args, false);
            }
            /// <summary>
            /// 
            /// </summary>
            /// <param name="command"></param>
            /// <param name="args"></param>
            /// <param name="keepConnect"></param>
            /// <returns></returns>
            public string SendCommand(RedisCommand command, string[] args, bool keepConnect = false)
            {
                try
                {
                    //请求头部格式， *<number of arguments>\r\n
                    const string headstr = "*{0}\r\n";
                    //参数信息       $<number of bytes of argument N>\r\n<argument data>\r\n
                    const string bulkstr = "${0}\r\n{1}\r\n";

                    var sb = new StringBuilder();
                    sb.AppendFormat(headstr, args.Length + 1);
                    var cmd = command.ToString();
                    sb.AppendFormat(bulkstr, cmd.Length, cmd);
                    foreach (var arg in args)
                    {
                        sb.AppendFormat(bulkstr, arg.Length, arg);
                    }
                    socket.Send(Encoding.UTF8.GetBytes(sb.ToString()));
                    ReceiveBuffer = new byte[65536];
                    if (command == RedisCommand.Get)
                    {
                        var total = 0;
                        var length = 0;
                        while (true)
                        {
                            try
                            {
                                var rev = new byte[65536];
                                var temp = socket.Receive(rev, rev.Length, SocketFlags.None);
                                Buffer.BlockCopy(rev, 0, ReceiveBuffer, total, temp);
                                total += temp;
                                if (length == 0)
                                {
                                    var line = ReadData().Split(new[] { '$', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)[0];
                                    length = cvt.ToInt(line);
                                    if (length > 60000)
                                    {
                                        ReceiveBuffer = new byte[length + 1000];
                                        Buffer.BlockCopy(rev, 0, ReceiveBuffer, 0, temp);
                                    }
                                }
                                if (total >= length)
                                {
                                    break;
                                }
                            }
                            catch { break; }
                        }
                    }
                    else
                    {
                        socket.Receive(ReceiveBuffer);
                    }
                    var data = ReadData();
                    if (!keepConnect)
                    {
                        Using = false;
                    }
                    return data;
                }
                catch { }
                Using = false;
                return null;
            }
            /// <summary>
            /// 发送数据
            /// </summary>
            /// <returns></returns>
            private string ReadData()
            {
                var data = Encoding.UTF8.GetString(ReceiveBuffer).Replace("\0", "");
                if (data[0] == '-')
                {
                    //错误消息检查。
                    //异常处理。
                    throw new Exception(data);
                }
                return data;
            }
        }

        /// <summary>
        /// Redis命令
        /// </summary>
        public enum RedisCommand
        {
            /// <summary>
            /// 简单密码认证
            /// </summary>
            Auth,
            /// <summary>
            /// Redis信息
            /// </summary>
            Info,
            /// <summary>
            /// 添加或更新一个值
            /// </summary>
            Set,
            /// <summary>
            /// 删除键
            /// </summary>
            Del,
            /// <summary>
            /// 获取一个key的值
            /// </summary>
            Get,
            /// <summary>
            /// 查询键
            /// </summary>
            Keys,
            /// <summary>
            /// 确认一个key是否存在
            /// </summary>
            Exists,
            /// <summary>
            /// 设置过期时间
            /// </summary>
            Expire,
            /// <summary>
            /// 标记一个事务块开始
            /// </summary>
            Multi,
            /// <summary>
            /// 执行所有 MULTI 之后发的命令
            /// </summary>
            Exec,
            /// <summary>
            /// 将数据同步保存到磁盘
            /// </summary>
            Save,
            /// <summary>
            /// 将数据异步保存到磁盘
            /// </summary>
            BgSave,
        }
    }
}
