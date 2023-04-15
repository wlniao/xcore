using System;
using System.Linq;
using System.Collections.Generic;
using StackExchange.Redis;
using System.Threading.Tasks;
using Wlniao;
using Wlniao.Caching;
using Microsoft.EntityFrameworkCore.Metadata.Internal;
using System.Security.Cryptography;
using System.IO;
using static Microsoft.AspNetCore.Razor.Language.TagHelperMetadata;

namespace Wlniao
{
    /// <summary>
    /// 延迟执行的队列任务
    /// </summary>
    public class Tasker
    {
        /// <summary>
        /// 标识是否默认获取的实例
        /// </summary>
        private static bool instanceDefault = true;
        /// <summary>
        /// Instance内部字段
        /// </summary>
        private static ConnectionMultiplexer instance = null;
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
                    lock (XCore.Lock)
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
        /// Redis/kakfa实例
        /// </summary>
        public static ConnectionMultiplexer Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = Redis.Reconnect();
                }
                return instance;
            }
            set
            {
                instance = value;
                instanceDefault = false;
            }
        }
        /// <summary>
        /// 任务消息
        /// </summary>
        public class Context
        {
            /// <summary>
            /// 任务编号
            /// </summary>
            public string key { get; set; }
            /// <summary>
            /// 任务主题
            /// </summary>
            public string topic { get; set; }
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
        /// <param name="topics">多个主题可通过“,”分割</param>
        /// <param name="begin">开始触发时间</param>
        /// <param name="delay">队列检查时间间隔（单位：毫秒）</param>
        /// <param name="maxqueue">每个时间区间拉取任务的最大数量</param>
        public static void Trigger(string topics, long begin = 0, int delay = 1000, int maxqueue = 100)
        {
            if (string.IsNullOrEmpty(topics))
            {
                return;
            }
            Task.Run(() =>
            {
                while (Instance == null)
                {
                    Task.Delay(300).Wait();
                }
                foreach (var topic in topics.SplitBy())
                {
                    Task.Run(() =>
                    {
                        var index = "tasker_queue_" + topic;
                        while (true)
                        {
                            try
                            {
                                if (Instance != null && instance.IsConnected)
                                {
                                    var db = instance.GetDatabase(Redis.Select);
                                    var now = DateTools.GetUnix();
                                    var tran = db.CreateTransaction();
                                    foreach (var s in db.SortedSetRangeByRankWithScores(index, 0, maxqueue, Order.Ascending))
                                    {
                                        if (s.Score <= now)
                                        {
                                            var next = 0L;
                                            var jobid = s.Element.ToString();
                                            try
                                            {
                                                var max = 0L;
                                                var times = new List<string>();
                                                var infokey = "tasker_tl" + topic + "_" + jobid;
                                                var value = db.StringGet(infokey).ToString();
                                                if (!string.IsNullOrEmpty(value))
                                                {
                                                    foreach (var item in value.SplitBy())
                                                    {
                                                        max = cvt.ToLong(item);
                                                        if (next == 0 && max > now)
                                                        {
                                                            next = max;
                                                            times.Add(item);
                                                        }
                                                        else if (max > now)
                                                        {
                                                            times.Add(item);
                                                        }
                                                    }
                                                }
                                                if (next > 0)
                                                {
                                                    tran.StringSetAsync(infokey, string.Join(",", times), TimeSpan.FromSeconds(max - now + 10));
                                                    tran.SortedSetAddAsync(index, jobid, next);
                                                }
                                                else if (max > 0)
                                                {
                                                    tran.KeyDeleteAsync(infokey);
                                                }
                                            }
                                            catch { }
                                            if (next <= now)
                                            {
                                                tran.SortedSetRemoveAsync(index, jobid);
                                            }
                                            if (s.Score > begin)
                                            {
                                                tran.PublishAsync(index, jobid);
                                            }
                                        }
                                        else
                                        {
                                            break;
                                        }
                                    }
                                    tran.Execute();
                                }
                            }
                            catch (Exception ex)
                            {
                                log.Topic("Tasker", ex.Message);
                            }
                            finally
                            {
                                if (delay > 0 && delay < 86401)
                                {
                                    Task.Delay(delay).Wait();
                                }
                            }
                        }
                    });
                }
            });
        }
        /// <summary>
        /// 订阅事件
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="func"></param>
        public static void Subscribe(string topic, Func<Context, Boolean> func)
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
                        if (watcher[topic] < DateTime.Now.AddMinutes(-30))
                        {
                            if (subscriber != null)
                            {
                                log.Topic("Tasker", "Tasker subscribe " + topic + " restart at " + DateTools.Format());
                                subscriber.Unsubscribe(channel);
                            }
                            subscriber = null;  //默认30分钟异常周期，超过时清除subscriber并在后续重新发起订阅
                        }
                        if (subscriber == null)
                        {
                            watcher[topic] = DateTime.Now;
                            subscriber = Instance.GetSubscriber();
                            subscriber.Subscribe(channel, (rchannel, message) =>
                            {
                                watcher[topic] = DateTime.Now;
                                func.Invoke(new Context
                                {
                                    key = message,
                                    topic = topic,
                                });
                            });
                        }
                        else if (!subscriber.IsConnected(channel))
                        {
                            log.Topic("Tasker", "Tasker subscribe " + topic + " link has been disconnected.");
                        }
                        else
                        {
                            watcher[topic] = DateTime.Now;
                        }
                    }
                    catch
                    {
                        subscriber = null;
                        log.Topic("Tasker", "Tasker subscribe " + topic + " error, please check if Redis connection is correct.");
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
                    log.Topic("Tasker", "Tasker subscribe " + topic + " stop.");
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
                if (Instance != null && instance.IsConnected)
                {
                    var db = instance.GetDatabase(Redis.Select);
                    var tran = db.CreateTransaction();
                    foreach (var topic in topics.SplitBy())
                    {
                        tran.SortedSetRemoveAsync("tasker_queue_" + topic, jobId);
                        tran.KeyDeleteAsync("tasker_tl" + topic + "_" + jobId);
                    }
                    return tran.Execute();
                }
            }
            catch
            {
                if (instanceDefault)
                {
                    instance = null;
                }
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
                    var tran = instance.GetDatabase(Redis.Select).CreateTransaction();
                    foreach (var topic in topics.SplitBy())
                    {
                        tran.SortedSetAddAsync("tasker_queue_" + topic, key, runtime > 0 ? runtime : XCore.NowUnix);
                    }
                    return tran.Execute();
                }
            }
            catch
            {
                if (instanceDefault)
                {
                    instance = null;
                }
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
                    var now = XCore.NowUnix;
                    var times = new List<long>();
                    if (delays == null || delays.Length == 0)
                    {
                        delays = Delays;
                    }
                    foreach (var delay in delays)
                    {
                        times.Add(now + delay);
                    }
                    var tran = Instance.GetDatabase(Redis.Select).CreateTransaction();
                    foreach (var topic in topics.SplitBy())
                    {
                        if (topic == "queue")
                        {
                            log.Error("tasker error, topic cannot use:" + topic);
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
                if (instanceDefault)
                {
                    instance = null;
                }
            }
            return false;
        }
    }
}