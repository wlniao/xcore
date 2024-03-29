using NUnit.Framework;

namespace Wlniao.XCore.Test
{
    public class ConfigTest
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void ConfigFile()
        {
            var isdev = Config.GetConfigs("ISDEV");
            Assert.GreaterOrEqual(isdev, "true");
        }
        [Test]
        public void MysqlConnStr()
        {
            var rw_connstr = Wlniao.DbConnectInfo.WLN_CONNSTR_MYSQL;
            var ro_connstr = Wlniao.DbConnectInfo.WLN_CONNSTR_MYSQL_READONLY;
            System.Console.WriteLine("rw_connstr:" + rw_connstr);
            System.Console.WriteLine("ro_connstr:" + ro_connstr);
            Assert.GreaterOrEqual(rw_connstr, ro_connstr);
            //Assert.Pass();
        }
    }
}