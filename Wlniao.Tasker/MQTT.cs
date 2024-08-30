using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Client.Connecting;
using MQTTnet.Client.Disconnecting;
using MQTTnet.Client.Options;
using MQTTnet.Client.Receiving;
using MQTTnet.Client.Unsubscribing;
using MQTTnet.Server;
using Org.BouncyCastle.Utilities.Encoders;
using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Wlniao.Crypto;

namespace Wlniao.Tasker
{
    public class MQTT
    {
        IMqttClient? client;
        public string Port = Config.GetConfigs("MQTT_SERVER_PORT", "1883");
        public string Server = Config.GetConfigs("MQTT_SERVER_HOST", "127.0.0.1");
        public string UserName = Config.GetConfigs("MQTT_KEY_UID");
        public string Password = Config.GetConfigs("MQTT_KEY_PWD");
        public bool Normal = Config.GetConfigs("MQTT_NORMAL", "false") == "true";
        private bool Connecting = false;
        /// <summary>
        /// SM2加密实例
        /// </summary>
        private static SM2 sm2 = new SM2(null, null);
        /// <summary>
        /// 订阅任务缓存
        /// </summary>
        private Dictionary<String, Action<Context>> subscribe = new Dictionary<String, Action<Context>>();
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="func"></param>
        public void Subscribe(string topic, Action<Context> func)
        {
            if (string.IsNullOrEmpty(topic))
            {
                return;
            }
            else if (topic.IndexOf(',') >= 0 || topic.IndexOf(';') >= 0 || topic.IndexOf('#') >= 0 || topic.IndexOf('/') >= 0 || topic.IndexOf('\\') >= 0)
            {
                throw new Exception("事件名称包含不允许的特殊字符");
            }
            else if (subscribe.ContainsKey(topic))
            {
                subscribe[topic] = func;
            }
            else
            {
                subscribe.TryAdd(topic, func);
            }
        }
        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="topic"></param>
        public bool UnSubscribe(string topic)
        {
            try
            {
                if (subscribe.ContainsKey(topic))
                {
                    if (client != null && client.IsConnected)
                    {
                        client.UnsubscribeAsync(new MqttClientUnsubscribeOptionsBuilder().WithTopicFilter(topic).Build()).Wait();
                    }
                    return subscribe.Remove(topic);
                }
            }
            catch { }
            return false;
        }
        /// <summary>
        /// 构造发送消息
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        private MqttApplicationMessage BuildMessage(string topic, object data)
        {
            var msg = Wlniao.Json.ToString(data);
            var buffer = sm2.Encrypt(Encoding.UTF8.GetBytes(msg));
            var builder = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithExactlyOnceQoS()
            .WithPayload(Hex.ToHexString(buffer));
            return builder.Build();
        }

        public void Start()
        {
            Task.Run(() =>
            {
                var pubkey = Config.GetConfigs("MQTT_KEY_PUBLIC");
                var optionsBuilder = new MqttClientOptionsBuilder();
                optionsBuilder.WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500);
                optionsBuilder.WithKeepAlivePeriod(TimeSpan.FromSeconds(180));
                optionsBuilder.WithSessionExpiryInterval(180);
                optionsBuilder.WithTcpServer(this.Server, cvt.ToInt(this.Port)); //设置MQTT服务器地址
                if(string.IsNullOrEmpty(pubkey))
                {
                    var key = new KeyTool();
                    sm2 = new SM2(key.PublicKey, key.PrivateKey, SM2Mode.C1C3C2);
                }
                else
                {
                    sm2 = new SM2(Helper.Decode(pubkey), new byte[0], SM2Mode.C1C3C2);
                }
                if (Port == "8883" || Config.GetConfigs("MQTT_SERVER_TLS", "false") == "true")
                {
                    optionsBuilder.WithTls(); //启用SSL加密通讯
                }
                var ClientId = Config.GetConfigs("MQTT_CLIENT_ID");
                if (string.IsNullOrEmpty(ClientId))
                {
                    ClientId = strUtil.CreateRndStrE(8);
                }
                if (!string.IsNullOrEmpty(this.UserName))
                {
                    ClientId = this.UserName + "@" + ClientId;
                    if (string.IsNullOrEmpty(this.Password) && !string.IsNullOrEmpty(pubkey))
                    {
                        this.Password = Hex.ToHexString(sm2.Encrypt(UTF8Encoding.UTF8.GetBytes(ClientId)));
                    }
                    optionsBuilder.WithCredentials(this.UserName, this.Password);  // 设置鉴权参数
                }
                if (Normal && !string.IsNullOrEmpty(pubkey))
                {
                    //使用普通服务器时，设置掉线遗嘱消息
                    var buffer = sm2.Encrypt(Encoding.UTF8.GetBytes(Json.ToString(new { ClientId })));
                    var willBuilder = new MqttApplicationMessageBuilder();
                    willBuilder.WithTopic("watcher/disconnected");
                    willBuilder.WithExactlyOnceQoS();
                    willBuilder.WithPayload(Hex.ToHexString(buffer));
                    optionsBuilder.WithWillMessage(willBuilder.Build());
                }
                optionsBuilder.WithClientId(ClientId); //设置客户端序列号
                var time = 0;
                while (true)
                {
                    try
                    {
                        if (client == null || !client.IsConnected)
                        {
                            time++;
                            client = new MqttFactory().CreateMqttClient();
                            client.ConnectedHandler = new MqttClientConnectedHandlerDelegate(OnMqttClientConnected);
                            client.DisconnectedHandler = new MqttClientDisconnectedHandlerDelegate(OnMqttClientDisconnected);
                            client.ApplicationMessageReceivedHandler = new MqttApplicationMessageReceivedHandlerDelegate(OnMqttClientApplicationMessageReceived);
                            var result = client.ConnectAsync(optionsBuilder.Build(), CancellationToken.None).Result;
                            if (result.ResultCode == MqttClientConnectResultCode.Success)
                            {
                                time = 0;
                                if (string.IsNullOrEmpty(pubkey) && !string.IsNullOrEmpty(result.ReasonString))
                                {
                                    Normal = false;
                                    pubkey = result.ReasonString;
                                    sm2 = new SM2(Helper.Decode(pubkey), new byte[0], SM2Mode.C1C3C2);
                                }
                                if (Normal)
                                {
                                    client.PublishAsync(BuildMessage("watcher/connected", new { ClientId }));
                                }
                                foreach (var topic in subscribe.Keys)
                                {
                                    client.SubscribeAsync(topic, MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);
                                    if (Normal)
                                    {
                                        client.PublishAsync(BuildMessage("watcher/subscribe", new { ClientId, Topic = topic }));
                                    }
                                }
                            }

                        }
                    }
                    catch (MQTTnet.Exceptions.MqttProtocolViolationException ex)
                    {
                        if (time == 1)
                        {
                            log.Error(ex.Message);
                        }
                    }
                    catch (Exception ex)
                    {
                        if (time == 1)
                        {
                            log.Error(ex.Message);
                        }
                    }
                    finally
                    {
                        Thread.Sleep(200);
                    }
                }
            });
        }
        private void OnMqttClientConnected(MqttClientConnectedEventArgs e)
        {
            if (!Connecting)
            {
                Connecting = true;
                log.Debug("MQ服务端已连接!!!");
            }
        }
        private void OnMqttClientDisconnected(MqttClientDisconnectedEventArgs e)
        {
            if (Connecting)
            {
                Connecting = false;
                log.Debug("MQ服务端已断开!!!");
            }
        }
        private void OnMqttClientApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic;
            var func = subscribe.GetValueOrDefault(topic);
            if (func == null)
            {
                log.Warn($"{e.ApplicationMessage.Topic}暂未注册");
            }
            else if (!sm2.VerifySign(e.ApplicationMessage.Payload, e.ApplicationMessage.CorrelationData, null))
            {
                log.Warn($"{e.ApplicationMessage.Topic}数据验签失败");
            }
            else
            {
                var message = UTF8Encoding.UTF8.GetString(e.ApplicationMessage.Payload);
                log.Debug(message);
                var obj = Json.StringToDic(message);
                func(new Context
                {
                    topic = topic,
                    key = obj.GetString("key"),
                    appid = obj.GetString("appid"),
                    clientid = obj.GetString("clientid")
                });
            }
        }
    }
}
