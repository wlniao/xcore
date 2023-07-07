using NUnit.Framework;
using System;
using System.Collections.Generic;

namespace Wlniao.XCore.Test
{
    public class TaskerTest
    {
        [SetUp]
        public void Setup()
        {
            Tasker.Subscribe("test", (ctx) =>
            {
                Console.WriteLine(ctx.topic + ":" + ctx.key);
            });
            Tasker.Trigger("test");
        }

        [Test]
        public void Insert()
		{
			Tasker.InsertDelayQueue("abc111", "test");
			Tasker.RemoveJob("abc111", "test");
            Tasker.InsertDelayQueue("test111", "test", 2, 8, 12, 20, 30, 50);
			Tasker.InsertDelayQueue("test222", "test", 2, 8, 12, 20, 30, 50);
			Tasker.InsertDelayQueue("test333", "test", 2, 8, 12, 20, 30, 50);
			System.Threading.Thread.Sleep(10000);
			Tasker.RemoveJob("test*", "test");
			Assert.Warn("test");
		}
        [Test]
        public void MysqlConnStr()
		{
			var rw_connstr = Wlniao.DbConnectInfo.WLN_CONNSTR_MYSQL;
            var ro_connstr = Wlniao.DbConnectInfo.WLN_CONNSTR_READONLY;
            System.Console.WriteLine("rw_connstr:" + rw_connstr);
            System.Console.WriteLine("ro_connstr:" + ro_connstr);
            Assert.GreaterOrEqual(rw_connstr, ro_connstr);
        }
    }
}