using NUnit.Framework;
using System;
using System.Collections.Generic;
using Wlniao;

namespace Wlniao.XCore.Test
{
    public class TaskerTest
    {
        [SetUp]
        public void Setup()
        {
            Tasker.Redis.Subscribe("test", (ctx) =>
            {
                Console.WriteLine(ctx.topic + ":" + ctx.key);
            });
            Tasker.Redis.Trigger("test");
        }

        [Test]
        public void Insert()
        {
            Tasker.Redis.InsertDelayQueue("abc111", "test");
            Tasker.Redis.RemoveJob("abc111", "test");
            Tasker.Redis.InsertDelayQueue("test111", "test", 2, 8, 12, 20, 30, 50);
            Tasker.Redis.InsertDelayQueue("test222", "test", 2, 8, 12, 20, 30, 50);
            Tasker.Redis.InsertDelayQueue("test333", "test", 2, 8, 12, 20, 30, 50);
            System.Threading.Thread.Sleep(10000);
            Tasker.Redis.RemoveJob("test*", "test");
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