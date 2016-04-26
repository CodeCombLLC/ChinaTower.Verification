using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Authorization;
using ChinaTower.Verification.Models.Infrastructures;

namespace ChinaTower.Verification.Controllers
{
    [Authorize]
    public class HomeController : BaseController
    {
        public IActionResult Index()
        {
            var raw = DB.Forms
                .Where(x =>x.Type == FormType.站址)
                .GroupBy(x => new { Status = x.Status, City = x.City })
                .Select(x => new { Key = x.Key, Count = x.Count() })
                .ToList();

            var statistics = new Dictionary<string, Dictionary<VerificationStatus, int>>();
            foreach(var x in raw)
            {
                if (!statistics.ContainsKey(x.Key.City))
                    statistics.Add(x.Key.City, new Dictionary<VerificationStatus, int>());
                statistics[x.Key.City][x.Key.Status] = x.Count;
            }

            return View(statistics);
        }

        [AllowAnonymous]
        public IActionResult Download(Guid id)
        {
            var blob = DB.Blobs.Single(x => x.Id == id);
            return File(blob.Content, blob.ContentType, blob.FileName);
        }

        public IActionResult Image(Guid id)
        {
            ViewBag.Id = id;
            return View();
        }

        [HttpPost]
        public IActionResult RemoveFile(Guid id)
        {
            var blob = DB.Blobs.Single(x => x.Id == id);
            if (blob.UserId != User.Current.Id && !User.IsInRole("Root"))
                return Prompt(x =>
                {
                    x.Title = "权限不足";
                    x.Details = "您没有权限删除这个文件！";
                    x.StatusCode = 403;
                });
            DB.Blobs.Remove(blob);
            DB.SaveChanges();
            return Prompt(x =>
            {
                x.Title = "删除成功";
                x.Details = "这个文件已经删除成功！";
            });
        }
    }
}
