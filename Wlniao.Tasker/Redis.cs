using System;
using System.Linq;
using System.Collections.Generic;
using StackExchange.Redis;
using System.Threading.Tasks;
using Wlniao;
using Wlniao.Caching;
using System.Security.Cryptography;
using System.IO;
using static System.Net.Mime.MediaTypeNames;
using Wlniao.Log;

namespace Wlniao.Tasker
{
    /// <summary>
    /// 延迟执行的队列任务
    /// </summary>
    public class Redis
    {
        /// <summary>
        /// 订阅任务监视缓存
        /// </summary>
        private static Dictionary<String, DateTime> watcher = new Dictionary<String, DateTime>();
        /// <summary>
        /// Delays内部字段
        /// </summary>
        private static int[] delays = null;
        /// <summary>
        /// 根据WLN_TASKER_DELAYS变量获取时间间隔
        /// </summary>
        private static int[] Delays
        {
            get
            {
                if (delays == null)
                {
                    lock (watcher)
                    {
                        try
                        {
                            var list = new List<int>();
                            foreach (var delay in Wlniao.Config.GetSetting("WLN_TASKER_DELAYS", "10,30,60,180,600,1800").SplitBy())
                            {
                                list.Add(cvt.ToInt(delay));
                            }
                            delays = list.ToArray();
                        }
                        catch { }
                    }
                }
                return delays;
            }
        }

        /// <summary>
        /// Instance内部字段
        /// </summary>
        private static ConnectionMultiplexer? instance = null;
        /// <summary>
        /// Redis/kakfa实例
        /// </summary>
        public static ConnectionMultiplexer Instance
        {
            get
            {
                try
                {
                    if (instance == null && nextconnect < DateTime.Now)
                    {
                        nextconnect = DateTime.Now.AddSeconds(1);
                        instance = ConnectionMultiplexer.Connect(Wlniao.Caching.Redis.ConnStr);
                    }
                }
                catch
                {
                    Loger.Warn("Tasker redis connect error");
                }
                return instance;
            }
            set
            {
                instance = value;
            }
        }
        /// <summary>
        /// 下次尝试链接时间
        /// </summary>
        private static DateTime nextconnect = DateTime.MinValue;
        /// <summary>
        /// 重新链接
        /// </summary>
        public static ConnectionMultiplexer Reconnect()
        {
            instance = null;
            nextconnect = DateTime.MinValue;
            return Instance;
        }
        /// <summary>
        /// 延时任务实体
        /// </summary>
        public class DelayJob
        {
            /// <summary>
            /// 任务标识
            /// </summary>
            public String key { get; set; }
            /// <summary>
            /// 任务主题
            /// </summary>
            public string topics { get; set; }
            /// <summary>
            /// 任务的执行时间点，如当前unixtime后的10秒/30秒/1分钟/3分钟/5分钟/10分钟/30分钟
            /// </summary>
            public List<long> times { get; set; }
        }


        /// <summary>
        /// 按主题触发任务
        /// </summary>
        /// <param name="topic">多个主题可通过“,”分割</param>
        /// <param name="begin">开始触发时间</param>
        /// <param name="delay">队列检查时间间隔（单位：毫秒）</param>
        /// <param name="maxqueue">每个时间区间拉取任务的最大数量</param>
        public static void Trigger(string topic, long begin = 0, int delay = 1000, int maxqueue = 100)
        {
            if (string.IsNullOrEmpty(topic) || delay <= 0)
            {
                return;
            }
			Task.Run(() =>
			{
				var index = "tasker_queue_" + topic;
				while (true)
				{
					try
					{
						if (Instance != null && instance.IsConnected)
						{
							var db = Instance.GetDatabase(Caching.Redis.Select);
							var now = DateTools.GetUnix();
							var tran = db.CreateTransaction();
							foreach (var s in db.SortedSetRangeByRankWithScores(index, 0, maxqueue, Order.Ascending))
							{
                                if (s.Score < begin)
                                {
                                    tran.SortedSetRemoveAsync(index, s.Element);
                                }
                                else if (s.Score <= now)
                                {
                                    try
                                    {
                                        if (s.Score > begin)
                                        {
                                            db.PublishAsync(new RedisChannel(index, RedisChannel.PatternMode.Auto), s.Element, CommandFlags.None);
                                        }
                                        var key = "tasker_tl" + topic + "_" + s.Element.ToString();
                                        if (db.KeyExists(key))
                                        {
                                            var times = new List<long>();
                                            foreach (var item in db.StringGet(key).ToString().SplitBy())
                                            {
                                                var time = cvt.ToLong(item);
                                                if (time > now)
                                                {
                                                    times.Add(time);
                                                }
                                            }
                                            if (times.Count > 0)
                                            {
                                                var value = string.Join(",", times.OrderBy(o => o).ToArray());
                                                tran.StringSetAsync(key, value, TimeSpan.FromSeconds(times.LastOrDefault() + 1));
                                                tran.SortedSetAddAsync(index, s.Element, times[0]);
                                            }
                                            else
                                            {
                                                tran.KeyDeleteAsync(key);
                                                tran.SortedSetRemoveAsync(index, s.Element);
                                            }
                                        }
                                        else
                                        {
                                            tran.SortedSetRemoveAsync(index, s.Element);
                                        }
                                    }
                                    catch { }
                                }
							}
							tran.Execute();
						}
					}
					catch (Exception ex)
					{
						Loger.Topic("Tasker", ex.Message);
                        Loger.Error("Tasker:" + ex.Message);
					}
					finally
					{
						Task.Delay(delay).Wait();
					}
				}
			});
		}
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="func"></param>
        public static void Subscribe(string topic, Action<Context> func)
        {
            if (string.IsNullOrEmpty(topic))
            {
                return;
            }
            else
            {
                watcher.TryAdd(topic, DateTime.MinValue);
            }
            Task.Run(() =>
            {
                while (Instance == null)
                {
                    Task.Delay(300).Wait();
                }
                ISubscriber subscriber = null;
                var channel = new RedisChannel("tasker_queue_" + topic, RedisChannel.PatternMode.Literal);
                while (watcher.ContainsKey(topic))
                {
                    try
                    {
                        if (watcher[topic] < DateTime.Now.AddMinutes(-1))
                        {
                            if (subscriber != null)
                            {
                                Loger.Topic("Tasker", "Tasker subscribe " + topic + " restart at " + DateTools.Format(), Log.LogLevel.Debug, true);
                                subscriber.Unsubscribe(channel);
                            }
                            subscriber = null;  //默认1分钟异常周期，超过时清除subscriber并在后续重新发起订阅
                        }
                        if (subscriber == null)
                        {
                            watcher[topic] = DateTime.Now;
                            subscriber = Instance.GetSubscriber();
                            subscriber.Subscribe(channel, (rchannel, message) =>
                            {
                                watcher[topic] = DateTime.Now;
                                Loger.Topic("Tasker", "Tasker execute " + message + "[" + topic + "]", Log.LogLevel.Debug, true);
                                func.Invoke(new Context { topic = topic, key = message });
                            });
                        }
                        else if (!subscriber.IsConnected(channel))
                        {
                            Loger.Topic("Tasker", "Tasker subscribe " + topic + " link has been disconnected.", Log.LogLevel.Debug, true);
                        }
                        else
                        {
                            watcher[topic] = DateTime.Now;
                        }
                    }
                    catch
                    {
                        subscriber = null;
                        Loger.Topic("Tasker", "Tasker subscribe " + topic + " error, please check if Redis connection is correct.", Log.LogLevel.Debug, true);
                    }
                    Task.Delay(3000).Wait();
                }
                if (subscriber != null)
                {
                    if (subscriber.IsConnected(channel))
                    {
                        subscriber.Unsubscribe(channel);
                    }
                    subscriber = null;
                    Loger.Topic("Tasker", "Tasker subscribe " + topic + " stop.", Log.LogLevel.Debug, true);
                }
            });
        }
        /// <summary>
        /// 取消订阅
        /// </summary>
        /// <param name="topic"></param>
        public static bool UnSubscribe(string topic)
        {
            if (watcher.ContainsKey(topic))
            {
                return watcher.Remove(topic);
            }
            return false;
        }
        /// <summary>
        /// 删除任务
        /// </summary>
        /// <param name="jobId"></param>
        /// <param name="topics"></param>
        /// <returns></returns>
        public static bool RemoveJob(string jobId, string topics)
        {
            if (string.IsNullOrEmpty(jobId) || string.IsNullOrEmpty(topics))
            {
                return false;
            }
            try
            {
                Loger.Topic("Tasker", "Tasker removed " + jobId + "[" + topics + "]", Log.LogLevel.Debug, true);
                if (Instance != null && instance.IsConnected)
                {
                    var db = Instance.GetDatabase(Caching.Redis.Select);
                    var tran = db.CreateTransaction();
                    foreach (var topic in topics.SplitBy())
					{
                        if (jobId.EndsWith('*'))
						{
							var keys = instance.GetServers().First().Keys(db.Database, "tasker_tl" + topic + "_" + jobId);
							foreach (var key in keys)
							{
                                var mkey = key.ToString().Substring(10 + topic.Length);
								tran.SortedSetRemoveAsync("tasker_queue_" + topic, mkey);
								tran.KeyDeleteAsync(key);
							}
						}
                        else
                        {
                            tran.SortedSetRemoveAsync("tasker_queue_" + topic, jobId);
                            tran.KeyDeleteAsync("tasker_tl" + topic + "_" + jobId);
                        }
                    }
                    return tran.Execute();
                }
            }
            catch
            {
                instance = null;
            }
            return false;
        }
        /// <summary>
        /// 放入任务队列
        /// </summary>
        /// <param name="key">任务编号</param>
        /// <param name="topics">任务类型</param>
        /// <param name="runtime">执行时间</param>
        /// <returns></returns>
        public static bool InsertQueue(string key, string topics, long runtime = 0)
        {
            try
            {
                if (Instance != null && instance.IsConnected)
                {
                    var tran = Instance.GetDatabase(Caching.Redis.Select).CreateTransaction();
                    foreach (var topic in topics.SplitBy())
                    {
                        tran.SortedSetAddAsync("tasker_queue_" + topic, key, runtime > 0 ? runtime : DateTools.GetUnix());
                    }
                    return tran.Execute();
                }
            }
            catch
            {
                instance = null;
            }
            return false;
        }
        /// <summary>
        /// 放入任务队列
        /// </summary>
        /// <param name="key">任务编号</param>
        /// <param name="topics">任务类型</param>
        /// <param name="delays">执行时间</param>
        /// <returns></returns>
        public static bool InsertDelayQueue(string key, string topics, params int[] delays)
        {
            try
            {
                if (Instance != null && instance.IsConnected)
                {
                    var now = DateTools.GetUnix();
                    var times = new List<long>();
                    if (delays == null || delays.Length == 0)
                    {
                        delays = Delays;
                    }
                    foreach (var delay in delays)
                    {
                        times.Add(now + delay);
                    }
                    var tran = Instance.GetDatabase(Caching.Redis.Select).CreateTransaction();
                    foreach (var topic in topics.SplitBy())
                    {
                        if (topic == "queue")
                        {
                            Loger.Error("tasker error, topic cannot use:" + topic);
                            return false;
                        }
                        tran.SortedSetAddAsync("tasker_queue_" + topic, key, times[0]);
                        if (times.Count > 0)
                        {
                            tran.StringSetAsync("tasker_tl" + topic + "_" + key, string.Join(",", times), TimeSpan.FromSeconds(delays.LastOrDefault() + 10));
                        }
                    }
                    return tran.Execute();
                }
            }
            catch
            {
                instance = null;
            }
            return false;
        }
    }
}