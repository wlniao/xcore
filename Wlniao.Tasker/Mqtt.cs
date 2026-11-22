using MQTTnet;
using MQTTnet.Protocol;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using System.Buffers;
using Wlniao.Crypto;
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
        private static SM2 _sm2 = new(null, null);
        /// <summary>
        /// 订阅任务缓存
        /// </summary>
        private readonly ConcurrentDictionary<string, Action<Context>> _subscribe = [];
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
                throw new ArgumentException("事件名称包含不允许的特殊字符", nameof(topic));
            }
            else
            {
                _subscribe[topic] = func;
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

                    return _subscribe.TryRemove(topic, out _);
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
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL2026", Justification = "Publish 载荷为 object 运行时类型，走 Json 反射逃生舱（CoreCLR 下行为不变）；NativeAOT 需调用方保证载荷类型已登记到源生成上下文")]
        [UnconditionalSuppressMessage("ReflectionAnalysis", "IL3050", Justification = "Publish 载荷为 object 运行时类型，走 Json 反射逃生舱（CoreCLR 下行为不变）；NativeAOT 需调用方保证载荷类型已登记到源生成上下文")]
        private MqttApplicationMessage BuildMessage(string topic, object data)
        {
            var msg = Json.Serialize(data);
            var buffer = _sm2.Encrypt(Encoding.UTF8.GetBytes(msg));
            var builder = new MqttApplicationMessageBuilder()
            .WithTopic(topic)
            .WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce)
            .WithPayload(System.Convert.ToHexString(buffer).ToLowerInvariant());
            return builder.Build();
        }
        /// <summary>
        /// 
        /// </summary>
        public void Start()
        {
            Task.Run(async () =>
            {
                var pubkey = Config.GetConfigs("MQTT_KEY_PUBLIC");
                var optionsBuilder = new MqttClientOptionsBuilder();
                optionsBuilder.WithProtocolVersion(MQTTnet.Formatter.MqttProtocolVersion.V500);
                optionsBuilder.WithKeepAlivePeriod(TimeSpan.FromSeconds(180));
                optionsBuilder.WithSessionExpiryInterval(180);
                optionsBuilder.WithTcpServer(Server, Wlniao.Convert.ToInt(Port)); //设置MQTT服务器地址
                if(string.IsNullOrEmpty(pubkey))
                {
                    var key = new KeyTool();
                    _sm2 = new SM2(key.PublicKey, key.PrivateKey, SM2Mode.C1C3C2);
                }
                else
                {
                    _sm2 = new SM2(Helper.Decode(pubkey), [], SM2Mode.C1C3C2);
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
                        Password = System.Convert.ToHexString(_sm2.Encrypt(Encoding.UTF8.GetBytes(clientId))).ToLowerInvariant();
                    }
                    optionsBuilder.WithCredentials(UserName, Password);  // 设置鉴权参数
                }
                if (Normal && !string.IsNullOrEmpty(pubkey))
                {
                    //使用普通服务器时，设置掉线遗嘱消息
                    var buffer = _sm2.Encrypt(Encoding.UTF8.GetBytes(Json.Serialize(new Dictionary<string, object>
                    {
                        ["ClientId"] = clientId
                    }, Wlniao.JsonContext.Instance.DictionaryStringObject)));
                    var willBuilder = new MqttApplicationMessageBuilder();
                    willBuilder.WithTopic("watcher/disconnected");
                    willBuilder.WithQualityOfServiceLevel(MqttQualityOfServiceLevel.ExactlyOnce);
                    willBuilder.WithPayload(System.Convert.ToHexString(buffer).ToLowerInvariant());
                    optionsBuilder.WithWillTopic(willBuilder.Build().Topic)
                        .WithWillPayload(willBuilder.Build().Payload.ToArray())
                        .WithWillQualityOfServiceLevel(willBuilder.Build().QualityOfServiceLevel);
                }
                optionsBuilder.WithClientId(clientId); //设置客户端序列号
                var failures = 0;
                while (true)
                {
                    try
                    {
                        if (_client is not { IsConnected: true })
                        {
                            failures++;
                            try
                            {
                                _client?.Dispose();
                            }
                            catch { }
                            var factory = new MqttClientFactory();
                            var client = factory.CreateMqttClient();
                            client.ConnectedAsync += OnMqttClientConnected;
                            client.DisconnectedAsync += OnMqttClientDisconnected;
                            client.ApplicationMessageReceivedAsync += OnMqttClientApplicationMessageReceived;
                            _client = client;
                            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(10));
                            var result = await client.ConnectAsync(optionsBuilder.Build(), timeout.Token).ConfigureAwait(false);
                            if (result.ResultCode == MqttClientConnectResultCode.Success)
                            {
                                failures = 0;
                                if (string.IsNullOrEmpty(pubkey) && !string.IsNullOrEmpty(result.ReasonString))
                                {
                                    Normal = false;
                                    pubkey = result.ReasonString;
                                    _sm2 = new SM2(Helper.Decode(pubkey), new byte[0], SM2Mode.C1C3C2);
                                }
                                if (Normal)
                                {
                                    await client.PublishAsync(BuildMessage("watcher/connected", new { ClientId = clientId })).ConfigureAwait(false);
                                }
                                foreach (var topic in _subscribe.Keys)
                                {
                                    await client.SubscribeAsync(topic, MqttQualityOfServiceLevel.AtLeastOnce).ConfigureAwait(false);
                                    if (Normal)
                                    {
                                        await client.PublishAsync(BuildMessage("watcher/subscribe", new { ClientId = clientId, Topic = topic })).ConfigureAwait(false);
                                    }
                                }
                            }
                        }
                    }
                    catch (MQTTnet.Exceptions.MqttProtocolViolationException ex)
                    {
                        if (failures == 1)
                        {
                            Loger.Error($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                        }
                    }
                    catch (Exception ex)
                    {
                        if (failures == 1)
                        {
                            Loger.Error($"{ex.Message}{Environment.NewLine}{ex.StackTrace}");
                        }
                    }
                    finally
                    {
                        var delay = _client is { IsConnected: true }
                            ? 5000
                            : Math.Min(200 * Math.Pow(2, Math.Max(0, failures - 1)), 60000);
                        await Task.Delay(TimeSpan.FromMilliseconds(delay)).ConfigureAwait(false);
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
            _subscribe.TryGetValue(topic, out var func);
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