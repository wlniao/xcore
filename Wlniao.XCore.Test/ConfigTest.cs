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
    }
}