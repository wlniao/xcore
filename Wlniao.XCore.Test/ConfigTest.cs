using NUnit.Framework;
using System.Collections.Generic;

namespace Wlniao.XCore.Test
{
    public class ConfigTest
    {
        [SetUp]
        public void Setup()
        {
            Wlniao.Config.Secret = "1234567898765432";
            Wlniao.Config.SetEncrypt("WLN_CONNSTR_PWD", "123456", Wlniao.Config.Secret);
        }

        [Test]
        public void ConfigPut()
        {
            var cfgs = new Dictionary<string, string>();
            cfgs.PutValue("test1", "");
            cfgs.PutValue("test2", null);
            cfgs.PutValue("test3", "test3");
            var v1 = cfgs.GetString("test1", "de1", false);
            var v2 = cfgs.GetString("test2", "de2", false);
            var v3 = cfgs.GetString("test3", "de3", false);
            if (v1 == v2 && v2 == v3)
            {

            }
            cfgs.PutOnlyEmpty("test0", "up0");
            cfgs.PutOnlyEmpty("test1", "up1");
            cfgs.PutOnlyEmpty("test2", "up2");
            cfgs.PutOnlyEmpty("test3", "up3");


            Assert.GreaterOrEqual(cfgs.Count, "false");
        }

        [Test]
        public void ConfigFile()
        {
            var isdev = Config.GetConfigs("ISDEV");
            Assert.GreaterOrEqual(isdev, "false");
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