using NUnit.Framework;
using System.Collections.Generic;

namespace Wlniao.XCore.Test
{
    public class ApiClientTest
    {
        [SetUp]
        public void Setup()
        {

        }

        [Test]
        public void Get()
        {
            Config.SetConfigs("Webroxy", "https://webroxy.wlniao.cn");
            var data="{\"sign\":\"c418d862a853b73612f0ccca92e2bb064b1eee7eae1f1e609b108827dcbe4aec\",\"data\":\"c6cb35907b73a5a2191d18a75bd03a83addd5a8f72c4958232c49e1435bd3aa6add8ea74fb8dc8be2a5407817a6b78c75cad640428b90692ed635de0c84bbdaf095cd20cb0df11cc23b0cc7e327fee40\",\"trace\":\"202406121624111490320000080901604465\",\"timestamp\":\"1718180651\"}";
            var isdev = Wlniao.Net.ApiClient.Post("https://capay-api-mzlakgugnv.cn-qingdao.fcapp.run/console/merchant/list", data);
            Assert.GreaterOrEqual(isdev, "false");
        }
    }
}