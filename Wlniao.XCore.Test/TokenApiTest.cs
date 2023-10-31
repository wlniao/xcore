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
		public void Json()
		{
			var txt = Wlniao.Json.ToString(new ApiResult<Dictionary<string, object>>
			{
				success = true,
				code = "test",
				data = new Dictionary<string, object> {
					{ "test1","123"},
                    { "test2",555},
                    { "json", new Dictionary<string, object> {
                            {"t1","111" },
                            {"t2","222" },
                        }
                    },
                    { "list", new List<Dictionary<string, object>> {
							new Dictionary<string, object>{ { "t1", "111" }, { "t2", "222" } },
                            new Dictionary<string, object>{ { "c1", "111" }, { "c2", "222" } }
                        }
                    },
                }
			});
			var rlt = Wlniao.Json.ToObject<ApiResult<Dictionary<string, object>>>(txt);
			Assert.IsTrue(rlt.success);
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
