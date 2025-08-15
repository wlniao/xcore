/*==============================================================================
    文件名称：WlnSocket.cs
    适用环境：CoreCLR 5.0,.NET Framework 2.0/4.0/5.0
    功能描述：Socket类封装
================================================================================
 
    Copyright 2017 XieChaoyi

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
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using Wlniao.Runtime;

namespace Wlniao.Net
{
    /// <summary>
    /// Redis客户端
    /// </summary>
    public class RedisClient
    {
        /// <summary>
        /// 当前选中的数据库
        /// </summary>
        public int SelectDB = 0;
        /// <summary>
        /// 
        /// </summary>
        public string Username = null;
        /// <summary>
        /// 
        /// </summary>
        public string Password = null;
        /// <summary>
        /// 数据处理编码
        /// </summary>
        public Encoding Encoding = Encoding.UTF8;
        /// <summary>
        /// Socket 是否正在使用 Nagle 算法。
        /// </summary>
        public bool NoDelaySocket = false;
        /// <summary>
        /// 
        /// </summary>
        private List<WlnSocket> SocketList = new List<WlnSocket>();
        /// <summary>
        /// 连接终结点集合
        /// </summary>
        internal List<EndPoint> EndPointList = new List<EndPoint>();
        /// <summary>
        /// 
        /// </summary>
        public RedisClient()
        {
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"></param>
        public RedisClient(EndPoint endpoint)
        {
            EndPointList.Add(endpoint);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="password"></param>
        public RedisClient(EndPoint endpoint, string password)
        {
            Password = password;
            EndPointList.Add(endpoint);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="nagle"></param>
        public RedisClient(EndPoint endpoint, string username, string password, bool nagle = false)
        {
            Username = username;
            Password = password;
            NoDelaySocket = nagle;
            EndPointList.Add(endpoint);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="endpoint"></param>
        public void AddEndPoint(EndPoint endpoint)
        {
            EndPointList.Add(endpoint);
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <returns></returns>
        public string Get(string key)
        {
            try
            {
                var socket = GetSocket();
                var byteKey = Encoding.UTF8.GetBytes(key);
                return ResToText(SendCommand(socket, RedisCommand.Get, byteKey));
            }
            catch (Exception ex)
            {
                throw new XCoreException("RedisClient.Get => " + ex.Message, ex);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="expire"></param>
        public bool Set(string key, byte[] value, int expire)
        {
            try
            {
                var socket = GetSocket();
                var byteKey = Encoding.UTF8.GetBytes(key);
                if (expire > 0)
                {
                    return ResToBool(SetByPipeline(socket, byteKey, value, expire));
                }
                return ResToBool(SendCommand(socket, RedisCommand.Set, byteKey, value));
            }
            catch (Exception ex)
            {
                throw new XCoreException("RedisClient.Set => " + ex.Message, ex);
            }
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public bool KeyDelete(string key)
        {
            try
            {
                var socket = GetSocket();
                var byteKey = Encoding.UTF8.GetBytes(key);
                return ResToBool(SendCommand(socket, RedisCommand.Del, byteKey));
            }
            catch (Exception ex)
            {
                throw new XCoreException("RedisClient.KeyDelete => " + ex.Message, ex);
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="key"></param>
        public bool KeyExists(string key)
        {
            try
            {
                var socket = GetSocket();
                var byteKey = Encoding.UTF8.GetBytes(key);
                return ResToBool(SendCommand(socket, RedisCommand.Exists, byteKey));
            }
            catch (Exception ex)
            {
                throw new XCoreException("RedisClient.KeyExists => " + ex.Message, ex);
            }
        }


        #region 命令类型定义
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
            /// 关闭当前连接
            /// </summary>
            Quit,
            /// <summary>
            /// 切换到指定的数据库
            /// </summary>
            Select,
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
        #endregion

        #region 通讯处理方法

        /// <summary>
        /// 从连接池获取一个实例
        /// </summary>
        /// <returns></returns>
        private WlnSocket GetSocket()
        {
            lock (Encoding)
            {
            beginCheck:
                foreach (var socket in SocketList.OrderBy(a => a.LastUse))
                {
                    if (socket.Catch || !socket.Connected)
                    {
                        SocketList.Remove(socket);
                        try
                        {
                            if (socket.Connected)
                            {
                                socket.Shutdown(SocketShutdown.Both);
                            }
                            socket.Close();
                        }
                        catch { }
                        goto beginCheck;
                    }
                    if (!socket.Using && socket.Connected)
                    {
                        socket.Using = true;
                        socket.LastUse = DateTime.Now.Ticks;
                        return socket;
                    }
                }
                var connMsg = "Redis connection configuration error: not config server";
                var newsocket = new WlnSocket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                newsocket.Using = true;
                newsocket.LastUse = DateTime.Now.Ticks;
                newsocket.NoDelay = NoDelaySocket;
                //newsocket.SendTimeout = TimeOutSeconds * 1000;  //10s
                //newsocket.ReceiveTimeout = TimeOutSeconds * 1000;  //10s
                try
                {
                    foreach (var endPoint in EndPointList)
                    {
                        newsocket.Connect(endPoint);
                        if (newsocket.Connected)
                        {
                            break;
                        }
                    }
                }
                catch (Exception ex)
                {
                    connMsg = "Redis connection configuration error: " + ex.Message;
                    Log.Loger.Error(connMsg);
                }
                if (newsocket.Connected)
                {
                    if (!string.IsNullOrEmpty(Password))
                    {
                        if (string.IsNullOrEmpty(Username))
                        {
                            Username = "default";
                        }
                        if (!ResToBool(SendCommand(newsocket, RedisCommand.Auth, Encoding.GetBytes(Username), Encoding.GetBytes(Password))))
                        {
                            if (newsocket.Connected)
                            {
                                newsocket.Shutdown(SocketShutdown.Both);
                            }
                            newsocket.Close();
                            throw new XCoreException("Redis: client password is error!");
                        }
                    }
                    if (SelectDB > 0)
                    {
                        if (!ResToBool(SendCommand(newsocket, RedisCommand.Select, Encoding.GetBytes(SelectDB.ToString()))))
                        {
                            if (newsocket.Connected)
                            {
                                newsocket.Shutdown(SocketShutdown.Both);
                            }
                            newsocket.Close();
                            throw new XCoreException("Redis: client database select error!");
                        }
                    }
                    SocketList.Add(newsocket);
                    return newsocket;
                }
                else
                {
                    throw new XCoreException(connMsg);
                }
            }
        }
        /// <summary>
        /// 创建事务
        /// </summary>
        /// <param name="socket"></param>
        private byte[] Multi(WlnSocket socket)
        {
            return SendData(socket, RedisCommand.Multi, new byte[][] { }, true);
        }
        /// <summary>
        /// 执行事务
        /// </summary>
        /// <param name="socket"></param>
        /// <returns></returns>
        private byte[] Exec(WlnSocket socket)
        {
            return SendData(socket, RedisCommand.Exec, new byte[][] { }, false);
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="second"></param>
        /// <returns></returns>
        private byte[] SetByPipeline(WlnSocket socket, byte[] key, byte[] value, int second)
        {
            this.Multi(socket);
            this.AddCommand(socket, RedisCommand.Set, key, value);
            this.AddCommand(socket, RedisCommand.Expire, key, Encoding.GetBytes(second.ToString()));
            return this.Exec(socket);
        }
        /// <summary>
        /// 添加命令
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        /// <returns></returns>
        private byte[] AddCommand(WlnSocket socket, RedisCommand command, params byte[][]args)
        {
            return SendData(socket, command, args, true);
        }
        /// <summary>
        /// 发送命令
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="command"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private byte[] SendCommand(WlnSocket socket, RedisCommand command, params byte[][] data)
        {
            return SendData(socket, command, data, false);
        }
        /// <summary>
        /// 数据发送方法
        /// </summary>
        /// <param name="socket"></param>
        /// <param name="command"></param>
        /// <param name="args"></param>
        /// <param name="keepConnect"></param>
        /// <returns></returns>
        private byte[] SendData(WlnSocket socket, RedisCommand command, byte[][] args, bool keepConnect)
        {
            // 参数命令
            var cmd = command.ToString();
            // 数据缓冲区
            var buffer = new List<byte>();
            // 请求头部格式， *<number of arguments>\r\n
            var headstr = Encoding.GetBytes(string.Format("*{0}\r\n", args.Length + 1));
            // 参数信息       $<number of bytes of argument N>\r\n<argument data>\r\n
            var bulkstr = Encoding.GetBytes(string.Format("${0}\r\n{1}\r\n", cmd.Length, cmd));
            buffer.AddRange(headstr);
            buffer.AddRange(bulkstr);
            foreach (var arg in args)
            {
                buffer.AddRange(Encoding.GetBytes(string.Format("${0}\r\n", arg.Length)));
                buffer.AddRange(arg);
                buffer.AddRange(Encoding.GetBytes("\r\n"));
            }  
            socket.Send(buffer.ToArray());
            buffer = new List<byte>();
            while (true)
            {
                var rev = new byte[4086];
                var count = socket.Receive(rev, rev.Length, SocketFlags.None);
                if (count > 0)
                {
                    buffer.AddRange(rev.Take(count));
                }
                if (count < rev.Length)
                {
                    break;
                }
            }
            if (!keepConnect)
            {
                socket.Using = false;
            }
            return buffer.ToArray();
        }
        #endregion


        #region 数据处理方法
        private string ResToText(byte[] res)
        {
            if (res.Length > 0)
            {
                var temp = Encoding.GetString(res, 0, res.Length > 16 ? 16 : res.Length);
                if (temp[0] == '-')
                {
                    temp = Encoding.GetString(res);
                    throw new XCoreException("Redis: " + temp.Substring(1, temp.LastIndexOf('\r')));
                }
                else if (temp[0] == '$')
                {
                    var length = Convert.ToInt(temp.Substring(1, temp.IndexOf('\r') - 1));
                    if (length > 0)
                    {
                        return Encoding.GetString(res, temp.IndexOf('\n') + 1, length);
                    }
                }
            }
            return "";
        }
        private bool ResToBool(byte[] res)
        {
            var temp = Encoding.GetString(res, 0, res.Length > 16 ? 16 : res.Length);
            if (temp.StartsWith("+OK"))
            {
                return true;
            }
            else if (temp[0] == '*')
            {
                var lines = temp.Split(new char[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                if (lines.Length == 3 && lines[1].StartsWith("+OK"))
                {
                    return true;
                }
                return false;
            }
            else if (temp[0] == '-')
            {
                temp = Encoding.GetString(res);
                throw new XCoreException("Redis: " + temp.Substring(1, temp.LastIndexOf('\r')));
            }
            else
            {
                return false;
            }
        }
        #endregion

    }
}