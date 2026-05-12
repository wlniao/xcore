using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Emit;
using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Unicode;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Wlniao;
using Wlniao.XServer;
using static System.Collections.Specialized.BitVector32;

namespace Wlniao.XCenter
{
    /// <summary>
    /// Emi应用基础Controller
    /// </summary>
    public class EmiJsController : EmiController
    {
        /// <summary>
        /// 获取常用日期段
        /// </summary>
        /// <returns></returns>
        public IActionResult dates()
        {
            var now = DateTools.GetNow();
            var quarter = now.Month / 3 * 3;
            if (now.Month % 3 == 0)
            {
                quarter -= 3;
            }
            var Today = now.ToString("yyyy-MM-dd");
            var Yeserday = now.AddDays(-1).ToString("yyyy-MM-dd");
            var MonthStart = now.ToString("yyyy-MM-01");
            var MonthEnd = DateTools.Convert(now.ToString("yyyy-MM-01 12:00:00")).AddMonths(1).AddDays(-1).ToString("yyyy-MM-dd");
            var QuarterStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(quarter).ToString("yyyy-MM-01");
            var QuarterEnd = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(quarter + 3).AddDays(-1).ToString("yyyy-MM-dd");
            var YearStart = now.ToString("yyyy-01-01");
            var YearEnd = now.ToString("yyyy-12-31");
            var PrevYearStart = now.AddYears(-1).ToString("yyyy-01-01");
            var PrevYearEnd = now.AddYears(-1).ToString("yyyy-12-31");
            var PrevMonthStart = now.AddMonths(-1).ToString("yyyy-MM-01");
            var PrevMonthEnd = DateTools.Convert(now.ToString("yyyy-MM-01 12:00:00")).AddDays(-1).ToString("yyyy-MM-dd");
            var PrevQuarterStart = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(quarter - 3).ToString("yyyy-MM-01");
            var PrevQuarterEnd = new DateTime(now.Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified).AddMonths(quarter).AddDays(-1).ToString("yyyy-MM-dd");
            return Json(new
            {
                Today,
                Yeserday,
                MonthStart,
                MonthEnd,
                QuarterStart,
                QuarterEnd,
                YearStart,
                YearEnd,
                PrevYearStart,
                PrevYearEnd,
                PrevMonthStart,
                PrevMonthEnd,
                PrevQuarterStart,
                PrevQuarterEnd,
                success = true
            });
        }
        /// <summary>
        /// 获取枚举数据
        /// </summary>
        /// <returns></returns>
        public IActionResult enums()
        {
            return CheckAuth(ctx =>
            {
                var key = GetRequest("key");
                var list = new List<object>();
                var rlt = ctx.EmiGet<List<Dictionary<string, object>>>("app", "getenumlist", new KeyValuePair<string, string>("parent", key));
                if (rlt.success && rlt.data != null)
                {
                    foreach (var row in rlt.data)
                    {
                        list.Add(new { value = row.GetString("key"), label = row.GetString("label"), description = row.GetString("description") });
                    }
                }
                return Json(list);
            });
        }
        /// <summary>
        /// 获取UI标签
        /// </summary>
        /// <returns></returns>
        public IActionResult label()
        {
            return CheckAuth(ctx =>
            {
                var ht = new System.Collections.Hashtable();
                var keys = PostRequest("keys").SplitBy(",", "|");
                foreach (var key in keys)
                {
                    ht.Add(key, ctx.GetLabel(key));
                }
                ht.Add("success", true);
                return Json(ht);
            });
        }
        /// <summary>
        /// 权限检查
        /// </summary>
        /// <returns></returns>
        public IActionResult permission()
        {
            return CheckSession((xsession, ctx) =>
            {
                var ht = new System.Collections.Hashtable();
                var codes = PostRequest("code").SplitBy(",", "|");
                var organ = PostRequest("organ");
                var allow = Config.GetConfigs("AllowPermission").ToUpper();
                if (codes.Length == 0)
                {
                    ht.Add("success", false);
                }
                else if (string.IsNullOrEmpty(organ))
                {
                    for (var i = 0; i < codes.Length; i++)
                    {
                        var auth = allow == "ALL" || allow == codes[i].ToUpper() ? true : ctx.Permission(xsession.UserSid, codes[i]);
                        ht.Add(codes[i], auth);
                        if (i == 0)
                        {
                            ht.Add("success", auth);
                        }
                    }
                }
                else
                {
                    for (var i = 0; i < codes.Length; i++)
                    {
                        var auth = allow == "ALL" || allow == codes[i].ToUpper() ? true : ctx.PermissionOrgan(xsession.UserSid, codes[i], organ);
                        ht.Add(codes[i], auth);
                        if (i == 0)
                        {
                            ht.Add("success", auth);
                        }
                    }
                }
                return Json(ht);
            });
        }
    }
}