namespace Wlniao.XCore.NTest
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
            Wlniao.Config.SetConfigs("WLN_LOG_TYPE", "loki");
            Wlniao.Config.SetConfigs("WLN_LOG_SERVER", "http://192.168.31.230:3100");
        }

        [Test]
        public void TestOrg()
        {
            Wlniao.Config.SetConfigs("WLN_LOG_ORGID", "2");
            Wlniao.Log.Loger.Debug(strUtil.CreateRndStrE(50));
            Wlniao.Log.Loger.Info(strUtil.CreateRndStrE(50));
            Wlniao.Log.Loger.Warn(strUtil.CreateRndStrE(50));
            Wlniao.Log.Loger.Error(strUtil.CreateRndStrE(50));
            Wlniao.Log.Loger.Fatal(strUtil.CreateRndStrE(50));
            System.Threading.Thread.Sleep(8000);
            //Assert.Fail("Fail");
            Assert.Pass("Success");
        }
    }
}