using System;
using System.Linq;
using System.Collections.Generic;
using StackExchange.Redis;
using System.Threading.Tasks;
using Wlniao;
using Wlniao.Caching;

namespace Wlniao
{
    /// <summary>
    /// 延迟执行的队列任务
    /// </summary>
    public class Tasker
    {
        private static bool instanceDefault = true;
        /// <summary>
        /// Store内部字段
        /// </summary>
        private static ConnectionMultiplexer instance = null;
        /// <summary>
        /// Delays内部字段
        /// </summary>
        private static int[] delays = new int[0];
        /// <summary>
        /// 根据WLN_TASKER_DELAYS变量获取时间间隔
        /// </summary>
        private static int[] Delays
        {
            get
            {
                if (delays.Length == 0)
                {
                    try
                    {
                        var list = new List<int>();
                        foreach (var delay in Wlniao.Config.GetSetting("WLN_TASKER_DELAYS", "5,30,60,180,600,1800").SplitBy())
                        {
                            list.Add(cvt.ToInt(delay));
                        }
                        delays = list.ToArray();
                    }
                    catch { }
                    if (delays.Length == 0)
                    {
                        delays = new int[] { 5, 30, 60, 180, 600, 1800 };
                    }
                }
                return delays;
            }
        }
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
            /// 任务的执行时间点，如当前unixtime后的5秒/30秒/1分钟/3分钟/5分钟/10分钟/30分钟
            /// </summary>
            public List<long> times { get; set; }
        }
        /// <summary>
        /// 向后滚发任务
        /// </summary>
        public static bool Trigger(string key, string topics)
        {
            try
            {
                if (Instance != null)
                {
                    var tran = instance.GetDatabase(Redis.Select).CreateTransaction();
                    foreach (var topic in topics.SplitBy())
                    {
                        if (topic == "queue")
                        {
                            log.Error("tasker error, topic cannot use:" + topic);
                            return false;
                        }
                        var times = new List<long>();
                        var str = tran.StringGetAsync("tasker_" + topic + "_" + key).Result.ToString();
                        foreach (var time in str.SplitBy())
                        {
                            times.Add(cvt.ToLong(time));
                        }
                        if (times.Count > 1)
                        {
                            tran.StringSetAsync("tasker_tl" + topic + "_" + key, string.Join(",", times), TimeSpan.FromSeconds(times.LastOrDefault() - XCore.NowUnix + 300));
                            tran.SortedSetAddAsync("tasker_queue_" + topic, key, times[0]);
                        }
                        else
                        {
                            tran.StringSetAsync("tasker_tl" + topic + "_" + key, string.Join(",", times), TimeSpan.FromSeconds(times.LastOrDefault() - XCore.NowUnix + 300));
                            tran.SortedSetRemoveAsync("tasker_queue_" + topic, key);
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
        /// <summary>
        /// 放入任务队列
        /// </summary>
        /// <param name="key">任务编号</param>
        /// <param name="topics">任务类型</param>
        /// <param name="runtime">执行时间</param>
        /// <returns></returns>
        public static bool PushQueue(string key, string topics, long runtime = 0)
        {
            try
            {
                var tran = Instance.GetDatabase(Redis.Select).CreateTransaction();
                foreach (var topic in topics.SplitBy())
                {
                    tran.SortedSetAddAsync("tasker_queue_" + topic, key, runtime > 0 ? runtime : XCore.NowUnix);
                }
                return tran.Execute();
            }
            catch
            {
                if (instanceDefault)
                {
                    instance = null;
                }
                return false;
            }
        }
        /// <summary>
        /// 放入任务队列
        /// </summary>
        /// <param name="key">任务编号</param>
        /// <param name="topics">任务类型</param>
        /// <param name="runtime">执行时间</param>
        /// <returns></returns>
        public static bool PushDelayQueue(string key, string topics, long runtime = 0)
        {
            try
            {
                var times = new List<long>();
                runtime = runtime > 0 ? runtime : XCore.NowUnix;
                foreach (var delay in Delays)
                {
                    times.Add(runtime + delay);
                }
                var tran = Instance.GetDatabase(Redis.Select).CreateTransaction();
                foreach (var topic in topics.SplitBy())
                {
                    if (topic == "queue")
                    {
                        log.Error("tasker error, topic cannot use:" + topic);
                        return false;
                    }
                    tran.SortedSetAddAsync("tasker_queue_" + topic, key, runtime);
                    if (times.Count > 1)
                    {
                        tran.StringSetAsync("tasker_tl" + topic + "_" + key, string.Join(",", times), TimeSpan.FromSeconds(times[0] - XCore.NowUnix + 300));
                    }
                }
                return tran.Execute();
            }
            catch
            {
                if (instanceDefault)
                {
                    instance = null;
                }
                return false;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="maxconcurrency"></param>
        public static void Publish(string topic, Int32 maxconcurrency = 1000)
        {
            if (string.IsNullOrEmpty(topic))
            {
                return;
            }
            else
            {
                topic = "tasker_queue_" + topic;
            }
            Task.Run(() =>
            {
                while (true)
                {
                    try
                    {
                        if (Instance != null)
                        {
                            var db = instance.GetDatabase(Redis.Select);
                            foreach (var item in db.SortedSetRangeByRankWithScores(topic, 0, maxconcurrency, Order.Ascending))
                            {
                                db.Publish(topic, item.Element.ToString());
                            }
                        }
                    }
                    catch { }
                    Task.Delay(1000).Wait();
                }
            });
        }
        /// <summary>
        /// 
        /// </summary>
        /// <param name="topic"></param>
        /// <param name="func"></param>
        public static void Subscribe(string topic, Func<Context, Boolean> func)
        {
            Task.Run(() =>
            {
                try
                {
                    if (Instance != null)
                    {
                        instance.GetSubscriber().Subscribe("tasker_queue_" + topic, (channel, message) =>
                        {
                            func.Invoke(new Context
                            {
                                topic = topic
                            });
                        });
                        return;
                    }
                }
                catch
                {
                    log.Error("Tasker subscribe " + topic + " error, please check if Redis connection is correct.");
                }
            });
        }
    }
}