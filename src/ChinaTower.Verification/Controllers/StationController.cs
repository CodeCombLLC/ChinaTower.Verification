using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Extensions.PlatformAbstractions;
using CodeComb.Data.Excel;
using ChinaTower.Verification.Models;
using ChinaTower.Verification.Models.Infrastructures;

namespace ChinaTower.Verification.Controllers
{
    public class StationController : BaseController
    {
        public async Task<IActionResult> Index(string StationId, string StationName,string City, string District, bool ErrorOnly, [FromServices] IApplicationEnvironment env, bool raw = false)
        {
            ViewBag.Cities = DB.Cities.ToList();
            var ret = DB.Forms
                .Where(x => x.Type == Models.Infrastructures.FormType.站址);
            if (ErrorOnly)
                ret = ret.Where(x => x.Status == Models.Infrastructures.VerificationStatus.Wrong);
            if (!string.IsNullOrEmpty(City))
                ret = ret.Where(x => x.City == City);
            if (!string.IsNullOrEmpty(District))
                ret = ret.Where(x => x.District == District);
            if (!string.IsNullOrEmpty(StationId))
                ret = ret.Where(x => x.UniqueKey == StationId);
            if (!string.IsNullOrEmpty(StationName))
                ret = ret.Where(x => x.Name.Contains(StationName) || StationName.Contains(x.Name));
            if (!User.IsInRole("Root"))
            {
                var cities = (await UserManager.GetClaimsAsync(User.Current))
                    .Where(x => x.Type == "管辖市区")
                    .Select(x => x.Value)
                    .ToList();
                ret = ret.Where(x => cities.Contains(x.City));
            }
            
            // 处理导出逻辑
            if (raw)
            {
                var directory = System.IO.Path.Combine(env.ApplicationBasePath, "Export");
                if (!System.IO.Directory.Exists(directory))
                    System.IO.Directory.CreateDirectory(directory);
                var fname = System.IO.Path.Combine(directory, Guid.NewGuid() + ".xlsx");
                using (var excel = ExcelStream.Create(fname))
                using (var sheet = excel.CreateSheet("Sheet1"))
                {
                    var src = ret.ToList();
                    var header = new CodeComb.Data.Excel.Infrastructure.Row();
                    foreach (var x in Hash.Headers[FormType.站址])
                        header.Add(x);
                    sheet.Add(header);
                    foreach(var x in src)
                    {
                        var row = new CodeComb.Data.Excel.Infrastructure.Row();
                        row.AddRange(x.FormStringArray);
                        sheet.Add(row);
                    }
                    sheet.SaveChanges();
                }
                var blob = System.IO.File.ReadAllBytes(fname);
                System.IO.File.Delete(fname);
                return File(blob, "application/vnd.ms-excel", "stations.xlsx");
            }

            return PagedView(ret);
        }

        public IActionResult Show(long id)
        {
            var form = DB.Forms.Single(x => x.Id == id);
            if (form.Type != FormType.站址)
                return Prompt(x =>
                {
                    x.Title = "非法操作";
                    x.Details = "您的请求不正确，请返回重试！";
                    x.StatusCode = 401;
                });
            ViewBag.RelatedForms = DB.Forms
                .Where(x => x.Type != FormType.站址 && x.StationKey == form.Id)
                .ToList();
            return View(form);
        }
    }
}
