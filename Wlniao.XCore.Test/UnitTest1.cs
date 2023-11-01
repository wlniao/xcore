using NUnit.Framework;
using System.Collections.Generic;
using System;
using Wlniao.XServer;
using System.Threading.Tasks;
using System.Text;

namespace Wlniao.XCore.Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Redis()
        {
            var encode = System.Text.UTF8Encoding.UTF8;
            Wlniao.Cache.UseRedis("192.168.31.254:6379,password=123456");
            for(var c = 0; c < 1000; c++)
            {
                Task.Run(() =>
                {
                    var data = new List<KeyValuePair<string, string>>();
                    for (var i = 0; i < 100; i++)
                    {
                        data.Add(new KeyValuePair<string, string>(strUtil.CreateRndStrE(10), strUtil.CreateRndStrE(32)));
                    }
                    var start = DateTime.Now;
                    foreach (var kv in data)
                    {
                        Wlniao.Caching.Redis.Set(kv.Key, kv.Value, -1);
                    }
                    log.Topic("utest", "Set:" + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms");
                    start = DateTime.Now;
                    foreach (var kv in data)
                    {
                        var tmp = Wlniao.Caching.Redis.Get(kv.Key);
                        if (tmp != kv.Value)
                        {
                            log.Topic("error", "Get: error => " + kv.Key + " => " + kv.Value + " => " + tmp);
                        }
                    }
                    log.Topic("utest", "Get:" + DateTime.Now.Subtract(start).TotalMilliseconds.ToString("F2") + "ms");
                });
            }
            System.Threading.Thread.Sleep(120000);
            var value = Wlniao.Cache.Get("test");
            //         Wlniao.Cache.Set("test1", strUtil.CreateRndStrE(8), -1);
            //Wlniao.Cache.Set("test2", strUtil.CreateRndStrE(8), 300);
            //Wlniao.Cache.Set("test3", Json.ToString(new
            //{
            //	test = 123,
            //	daddlg = "sdgew",
            //	tesdgw = new Dictionary<string, string>
            //	{
            //		{ "a1","23"},
            //		{ "a2","dsdr"}
            //	},
            //	tesdgsdgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tesd224gsdgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tesdgs221dgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tesdgs34dgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tes2dgsdgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tesdg9sdgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tesdg11sdgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tesd44gsdgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tes22dgsdgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tesdgsd55gw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tesdg22sdgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tesdg34sdgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tesdgs4dgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tesd126gsdgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tesdgs2dgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tesd45gsdgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 },
            //	tesd32gsdgw = new Dictionary<string, object>
            //			 {
            //				 { "o1",442},
            //				 { "o2","dsdr"},
            //				 { "o3",false},
            //				 { "o4",39293.313m }
            //			 }
            //}), 300);
            Assert.IsTrue(value.IsNotNullAndEmpty());
        }

        [Test]
        public void IsIdentity()
        {
            var result = Wlniao.strUtil.IsIdentity("51020219640204655X");
            Assert.IsTrue(result);
		}

		[Test]
		public void FileLoger()
		{
			log.Topic("test1", strUtil.CreateRndStrE(50));
            System.Threading.Thread.Sleep(3600000);
        }

		[Test]
		public void Loger()
		{
			Task.Run(() =>
			{
				while (true)
				{
					Wlniao.log.Topic("test1", strUtil.CreateRndStrE(50));
				}
			});
			Task.Run(() =>
			{
				while (true)
				{
					Wlniao.log.Topic("test2", strUtil.CreateRndStrE(50));
					//System.Threading.Thread.Sleep(73);
				}
			});
			Task.Run(() =>
			{
				while (true)
				{
					Wlniao.log.Topic("test1", strUtil.CreateRndStrE(50));
				}
			});
			Task.Run(() =>
			{
				while (true)
				{
					Wlniao.log.Topic("test3", strUtil.CreateRndStrE(50));
					//System.Threading.Thread.Sleep(21);
				}
			});
			Task.Run(() =>
			{
				while (true)
				{
					Wlniao.log.Topic("test5", strUtil.CreateRndStrE(50));
					//System.Threading.Thread.Sleep(91);
				}
			});
			Task.Run(() =>
			{
				while (true)
				{
					Wlniao.log.Topic("test4", strUtil.CreateRndStrE(50));
					//System.Threading.Thread.Sleep(130);
				}
			});
			Task.Run(() =>
			{
				while (true)
				{
					Wlniao.log.Topic("test2", strUtil.CreateRndStrE(50));
					//System.Threading.Thread.Sleep(73);
				}
			});
			Task.Run(() =>
			{
				while (true)
				{
					Wlniao.log.Topic("test5", strUtil.CreateRndStrE(50));
					//System.Threading.Thread.Sleep(91);
				}
			});
			Task.Run(() =>
			{
				while (true)
				{
					Wlniao.log.Topic("test4", strUtil.CreateRndStrE(50));
					//System.Threading.Thread.Sleep(130);
				}
			});
			Task.Run(() =>
			{
				while (true)
				{
					Wlniao.log.Topic("test5", strUtil.CreateRndStrE(50));
					//System.Threading.Thread.Sleep(91);
				}
			});
            System.Threading.Thread.Sleep(3600000);
            Assert.Pass();
            //Assert.Pass();
        }
        [Test]
        public void Test1()
        {
            var str = Wlniao.XServer.Common.GetResponseString("http://jinke.qihui365.com/login", 1, 60);
            var length = str.Length;
            Assert.GreaterOrEqual(length, 0);
            //Assert.Pass();
        }

        [Test]
        public void Test2()
        {
            var str = Wlniao.XServer.Common.GetResponseString("http://jinke.qihui365.com/js/md5.js", 1, 60);
            var length = str.Length;
            Assert.GreaterOrEqual(length, 0);
            //Assert.Pass();
        }

        [Test]
        public void Test3()
        {
            var str = Wlniao.XServer.Common.GetResponseString("http://wx.jeffhouse.net/js/chunk-vendors.js?v=2105182354", 1);
            var length = str.Length;
            System.Console.WriteLine(str);
            Assert.GreaterOrEqual(length, 0);
            //Assert.Pass();
        }
    }
}