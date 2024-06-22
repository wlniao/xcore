using NUnit.Framework;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Wlniao.XCore.Test
{
    public class LogTest
    {
        [SetUp]
        public void Setup()
        {
            Wlniao.Config.SetConfigs("WLN_LOG_TYPE", "loki");
            Wlniao.Config.SetConfigs("WLN_LOG_ORGID", "2");
            Wlniao.Config.SetConfigs("WLN_LOG_SERVER", "http://192.168.31.230:3100");
        }

        [Test]
        public void LokiOrg()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Wlniao.log.Debug(strUtil.CreateRndStrE(50));
                    Wlniao.log.Info(strUtil.CreateRndStrE(50));
                    Wlniao.log.Warn(strUtil.CreateRndStrE(50));
                    Wlniao.log.Error(strUtil.CreateRndStrE(50));
                    Wlniao.log.Fatal(strUtil.CreateRndStrE(50));
                    Wlniao.log.Topic("org:" + Wlniao.Config.GetConfigs("WLN_LOG_ORGID"), strUtil.CreateRndStrE(50));
                    System.Threading.Thread.Sleep(200);
                }
            });
            System.Threading.Thread.Sleep(360000000);
            Assert.Pass();
        }

        /// <summary>
        /// Loki—π¡¶≤‚ ‘
        /// </summary>
        [Test]
        public void LokiPushTime()
        {
            Task.Run(() =>
            {
                while (true)
                {
                    Wlniao.log.Topic("test1", strUtil.CreateRndStrE(50));
                    System.Threading.Thread.Sleep(167);
                }
            });
            Task.Run(() =>
            {
                while (true)
                {
                    Wlniao.log.Topic("test2", strUtil.CreateRndStrE(50));
                    System.Threading.Thread.Sleep(173);
                }
            });
            Task.Run(() =>
            {
                while (true)
                {
                    Wlniao.log.Topic("test3", strUtil.CreateRndStrE(50));
                    System.Threading.Thread.Sleep(123);
                }
            });
            Task.Run(() =>
            {
                while (true)
                {
                    Wlniao.log.Topic("test4", strUtil.CreateRndStrE(50));
                    System.Threading.Thread.Sleep(121);
                }
            });
            Task.Run(() =>
            {
                while (true)
                {
                    Wlniao.log.Topic("test5", strUtil.CreateRndStrE(50));
                    System.Threading.Thread.Sleep(191);
                }
            });
            Task.Run(() =>
            {
                while (true)
                {
                    Wlniao.log.Topic("test6", strUtil.CreateRndStrE(50));
                    System.Threading.Thread.Sleep(130);
                }
            });
            Task.Run(() =>
            {
                while (true)
                {
                    Wlniao.log.Topic("test7", strUtil.CreateRndStrE(50));
                    System.Threading.Thread.Sleep(73);
                }
            });
            Task.Run(() =>
            {
                while (true)
                {
                    Wlniao.log.Topic("test8", strUtil.CreateRndStrE(50));
                    System.Threading.Thread.Sleep(91);
                }
            });
            Task.Run(() =>
            {
                while (true)
                {
                    Wlniao.log.Topic("test9", strUtil.CreateRndStrE(50));
                    System.Threading.Thread.Sleep(130);
                }
            });

            Task.Run(() =>
            {
                while (true)
                {
                    Wlniao.log.Topic("test10", strUtil.CreateRndStrE(50));
                    //System.Threading.Thread.Sleep(91);
                }
            });
            System.Threading.Thread.Sleep(3600000);
            Assert.Pass();
            //Assert.Pass();
        }

    }
}