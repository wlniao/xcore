using NUnit.Framework;

namespace Wlniao.XCore.Test
{
    public class Tests
    {
        [SetUp]
        public void Setup()
        {
        }

        [Test]
        public void Loger()
        {
            for(var i = 0; i < 10000; i++)
            {
                Wlniao.log.Topic("test", DateTools.GetUnix() + strUtil.CreateRndStrE(16));
            }
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