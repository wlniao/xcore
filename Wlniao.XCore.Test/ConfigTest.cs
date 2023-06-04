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