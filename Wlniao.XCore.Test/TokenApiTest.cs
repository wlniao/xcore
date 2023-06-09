using NUnit.Framework;
using System.Collections.Generic;

namespace Wlniao.XCore.Test
{
	public class TokenApiTest
	{
		[SetUp]
		public void Setup()
		{

		}

		[Test]
		public void Connect()
		{
			var rlt = Wlniao.XServer.TokenApi.Request<object>("https://www.jdcfo.com/api/super_set", "80aqt9v3coxtvk5j0wu1rueafle8kixa", new { owner = "1234" });
			rlt = Wlniao.XServer.TokenApi.Request<object>("https://www.jdcfo.com/api/super_set", "80aqt9v3coxtvk5j0wu1rueafle8kixa", new { owner = "1234" });
			rlt = Wlniao.XServer.TokenApi.Request<object>("https://www.jdcfo.com/api/super_set", "80aqt9v3coxtvk5j0wu1rueafle8kixa", new { owner = "1234" });
			Assert.IsTrue(rlt.success);
		}
	}
}
