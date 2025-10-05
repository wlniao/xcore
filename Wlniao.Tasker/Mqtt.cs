using MQTTnet;
using MQTTnet.Protocol;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;
using Wlniao.Crypto;
using Org.BouncyCastle.Utilities.Encoders;
using Wlniao.Log;
using Wlniao.Text;
using Encoding = System.Text.Encoding;

namespace Wlniao.Tasker
{
    /// <summary>
    /// 
    /// </summary>
    public class Mqtt
    {
        /// <summary>
        /// 
        /// </summary>
        private IMqttClient? _client;
        /// <summary>
        /// 
        /// </summary>
        public string Port = Config.GetConfigs("MQTT_SERVER_PORT", "1883");
        /// <summary>
        /// 
        /// </summary>
        public string Server = Config.GetConfigs("MQTT_SERVER_HOST", "127.0.0.1");
        /// <summary>
        /// 
        /// </summary>
        public string UserName = Config.GetConfigs("MQTT_KEY_UID");
        /// <summary>
        /// 
        /// </summary>
        public string Password = Config.GetConfigs("MQTT_KEY_PWD");
        /// <summary>
        /// 
        /// </summary>
        public bool Normal = Config.GetConfigs("MQTT_NORMAL", "false") == "true";
        /// <summary>
        /// 是否已连接
        /// </summary>
        public bool Connecting { get; private set; }
        /// <summary>
        /// SM2加密实例
        /// </summary>
        private static SM2 _sm2 = new SM2(null, null);
        /// <summary>
        /// 订阅任务缓存
        /// </summary>
        private Dictionary<string, Action<Context>> _subscribe = new Dictionary<string, Action<Context>>();
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="func"></param>
        public void Subscribe(string topic, Action<Context> func)
        {
            if (string.IsNullOrEmpty(topic))
            {
            }
            else if (topic.IndexOf(',') >= 0 || topic.IndexOf(';') >= 0 || topic.IndexOf('#') >= 0 || topic.IndexOf('/') >= 0 || topic.IndexOf('\\') >= 0)
            {
                throw new Exception("事件名称包含不允许的特殊字符");
            }
            else if (_subscribe.ContainsKey(topic))
            {
                _subscribe[topic] = func;
            }
            else
            {
                _subscribe.TryAdd(topic, func);
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
                if (_subscribe.ContainsKey(topic))
                {
                    if (_client is { IsConnected: true })
                    {
                        var unsubscribeOptions = new MqttClientUnsubscribeOptionsBuilder()
                            .WithTopicFilter(topic)
                            .Build();
                        _client.UnsubscribeAsync(unsubscribeOptions).Wait();
                    }

                    return _subscribe.Remove(topic);
                }
            }
            catch (Exception ex)
            {
                Loger.Error($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
            }
            return false;
        }
        /// <summary>
        /// 构造发送消息
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="data"></param>
        /// <returns></returns>
        private MqttApplicationMessage BuildMessage(string topic, object data)
        {
            var msg = Json.Serialize(data);
            var buffer = _sm2.Encrypt(Encoding.UTF8.GetBytes(msg));
            var builder = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
            .WithPayload(Hex.ToHexString(buffer));
            return builder.Build();
        }
        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            Task.Run(() =>
            {
                var pubkey = Config.GetConfigs("MQTT_KEY_PUBLIC");
                var optionsBuilder = new MqttClientOptionsBuilder();
                optionsBuilder.WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500);
                optionsBuilder.WithKeepAlivePeriod(TimeSpan.FromSeconds(180));
                optionsBuilder.WithSessionExpiryInterval(180);
                optionsBuilder.WithTcpServer(Server, int.Parse(Port)); //设置MQTT服务器地址
                if(string.IsNullOrEmpty(pubkey))
                {
                    var key = new KeyTool();
                    _sm2 = new SM2(key.PublicKey, key.PrivateKey, SM2Mode.C1C3C2);
                }
                else
                {
                    _sm2 = new SM2(Helper.Decode(pubkey), new byte[0], SM2Mode.C1C3C2);
                }
                if (Port == "8883" || Config.GetConfigs("MQTT_SERVER_TLS", "false") == "true")
                {
                    optionsBuilder.WithTlsOptions(o => o.UseTls()); //启用SSL加密通讯
                }
                var clientId = Config.GetConfigs("MQTT_CLIENT_ID");
                if (string.IsNullOrEmpty(clientId))
                {
                    clientId = StringUtil.CreateRndStrE(8);
                }
                if (!string.IsNullOrEmpty(UserName))
                {
                    clientId = UserName + "@" + clientId;
                    if (string.IsNullOrEmpty(Password) && !string.IsNullOrEmpty(pubkey))
                    {
                        Password = Hex.ToHexString(_sm2.Encrypt(Encoding.UTF8.GetBytes(clientId)));
                    }
                    optionsBuilder.WithCredentials(UserName, Password);  // 设置鉴权参数
                }
                if (Normal && !string.IsNullOrEmpty(pubkey))
                {
                    //使用普通服务器时，设置掉线遗嘱消息
                    var buffer = _sm2.Encrypt(Encoding.UTF8.GetBytes(Json.Serialize(new { ClientId = clientId })));
                    var willBuilder = new MqttApplicationMessageBuilder();
                    willBuilder.WithTopic("watcher/disconnected");
                    willBuilder.WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);
                    willBuilder.WithPayload(Hex.ToHexString(buffer));
                    optionsBuilder.WithWillTopic(willBuilder.Build().Topic)
                        .WithWillPayload(willBuilder.Build().Payload.ToArray())
                        .WithWillQualityOfServiceLevel(willBuilder.Build().QualityOfServiceLevel);
                }
                optionsBuilder.WithClientId(clientId); //设置客户端序列号
                var time = 0;
                while (true)
                {
                    try
                    {
                        if (_client is not { IsConnected: true })
                        {
                            time++;
                            var factory = new MqttClientFactory();
                            _client = factory.CreateMqttClient();
                            _client.ConnectedAsync += OnMqttClientConnected;
                            _client.DisconnectedAsync += OnMqttClientDisconnected;
                            _client.ApplicationMessageReceivedAsync += OnMqttClientApplicationMessageReceived;
                            var result = _client.ConnectAsync(optionsBuilder.Build(), CancellationToken.None).Result;
                            if (result.ResultCode == MqttClientConnectResultCode.Success)
                            {
                                time = 0;
                                if (string.IsNullOrEmpty(pubkey) && !string.IsNullOrEmpty(result.ReasonString))
                                {
                                    Normal = false;
                                    pubkey = result.ReasonString;
                                    _sm2 = new SM2(Helper.Decode(pubkey), new byte[0], SM2Mode.C1C3C2);
                                }
                                if (Normal)
                                {
                                    _client.PublishAsync(BuildMessage("watcher/connected", new { ClientId = clientId }));
                                }
                                foreach (var topic in _subscribe.Keys)
                                {
                                    _client.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce);
                                    if (Normal)
                                    {
                                        _client.PublishAsync(BuildMessage("watcher/subscribe", new { ClientId = clientId, Topic = topic }));
                                    }
                                }
                            }

                        }
                    }
                    catch (MQTTnet.Exceptions.MqttProtocolViolationException ex)
                    {
                        if (time == 1)
                        {
                            Loger.Error($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (time == 1)
                        {
                            Loger.Error($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                        }
                    }
                    finally
                    {
                        Thread.Sleep(200);
                    }
                }
            });
        }
        private Task OnMqttClientConnected(MqttClientConnectedEventArgs e)
        {
            if (!Connecting)
            {
                Connecting = true;
                Loger.Debug("MQ服务端已连接!!!");
            }
            return Task.CompletedTask;
        }
        private Task OnMqttClientDisconnected(MqttClientDisconnectedEventArgs e)
        {
            if (Connecting)
            {
                Connecting = false;
                Loger.Debug("MQ服务端已断开!!!");
            }
            return Task.CompletedTask;
        }
        private Task OnMqttClientApplicationMessageReceived(MqttApplicationMessageReceivedEventArgs e)
        {
            var topic = e.ApplicationMessage.Topic;
            var func = _subscribe.GetValueOrDefault(topic);
            if (func == null)
            {
                Loger.Warn($"{e.ApplicationMessage.Topic}暂未注册");
            }
            else if (!_sm2.VerifySign(e.ApplicationMessage.Payload.ToArray(), e.ApplicationMessage.CorrelationData))
            {
                Loger.Warn($"{e.ApplicationMessage.Topic}数据验签失败");
            }
            else
            {
                var message = Encoding.UTF8.GetString(e.ApplicationMessage.Payload.ToArray());
                Loger.Debug(message);
                var obj = Json.DeserializeToDic(message);
                func(new Context
                {
                    topic = topic,
                    key = obj.GetString("key"),
                    appid = obj.GetString("appid"),
                    clientid = obj.GetString("clientid")
                });
            }
            return Task.CompletedTask;
        }
    }
}