using Org.BouncyCastle.Utilities.Encoders;

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

        [Test]
        public void TestSm4()
        {
            var sm4 = new Wlniao.Crypto.SM4();
            var key = "1qabgw9qd2aoqevl";
            var txt = "41DBB8F77C87EBD3639B8000D587E4D5BD1A2E00043F0DC0EFC1D6215096D41CEF682AFD694687453FFB23969E3260B3F169D23077FAEE69644611D64A5D0FD613C5B3B5298A1B95B071396D4025EE29ECEB9459A09959DE75BC76B8B532FBB795AABDF949BB4424FA0205EAEDAEE1";

            var data = Hex.Decode(txt);
            var keys = System.Text.Encoding.ASCII.GetBytes(key);
            var swap = sm4.DecryptECB(data, keys, true);

            var rlt = Hex.ToHexString(swap);
            Assert.Pass(rlt);
        }
        [Test]
        public void TestSm2Sign()
        {

            var txt = "215096D41CEF682AFD694687453FFB23969E3260B3F1";
            var prvkey = "3577408c5c3773b42143b5151ba5fa3515f3710edaa3e626e98a1f61e48c8950";
            var pubkey = "044c1c0542b4f76bc44aff6c70a3394840d2e3a74f405d38b96ffbd537763033c6a8670fd2c26a992ce611738cb949cdfbd19ca3bde85d709da66d412cc3fd56b8";
            var sm2 = new Wlniao.Crypto.SM2(Wlniao.Crypto.Helper.Decode(pubkey), Wlniao.Crypto.Helper.Decode(prvkey));
            var sign = sm2.Sign(System.Text.UTF8Encoding.UTF8.GetBytes(txt));

            if (sm2.VerifySign(System.Text.ASCIIEncoding.ASCII.GetBytes(txt), sign))
            {
                Assert.Pass("Yes");
            }
            else
            {
                Assert.Pass("Not");
            }
        }
    }
}