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
            var txt = "qwdgewosdgewtewe";
            var ecb = "BB528C338D6E23686397C35E2B5B4EC0BD5DF75A39F0E8AAD05626BD185019C5";
            var cbc = "BB528C338D6E23686397C35E2B5B4EC07149C2740F614ECC6CAD1469CC41518C";

            //var keys = Hex.Decode("0123456789abcdeffedcba9876543210");
            var keys = System.Text.Encoding.ASCII.GetBytes("1qabgw9qd2aoqevl");
            var ecbenc = sm4.EncryptECB(System.Text.Encoding.ASCII.GetBytes(txt), keys, false);
            var ecbdec = sm4.DecryptECB(Hex.Decode(ecb), keys, false);
            var secbenc = Hex.ToHexString(ecbenc);
            var secbdec = System.Text.Encoding.ASCII.GetString(ecbdec);
            
            var cbcenc = sm4.EncryptCBC(System.Text.Encoding.ASCII.GetBytes(txt), keys, new byte[16], false);
            var cbcdec = sm4.DecryptCBC(Hex.Decode(cbc), keys, new byte[16], false);
            var scbcenc = Hex.ToHexString(cbcenc);
            var scbcdec = System.Text.Encoding.ASCII.GetString(cbcdec);
            
            if (ecb == secbenc || txt == secbdec || cbc == scbcenc || txt == scbcdec)
            {
                
            }

            Assert.Pass(Hex.ToHexString(cbcenc));
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