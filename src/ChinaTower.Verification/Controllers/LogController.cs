using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Authorization;
using Microsoft.Data.Entity;
using ChinaTower.Verification.Models;

namespace ChinaTower.Verification.Controllers
{
    [Authorize]
    public class LogController : BaseController
    {
        public IActionResult Index()
        {
            IEnumerable<Log> logs = DB.Logs
                .Include(x => x.User)
                .Include(x => x.Form)
                .OrderByDescending(x => x.Id);
            if (!User.IsInRole("Root"))
                logs = logs.Where(x => x.UserId == User.Current.Id);
            return PagedView(logs);
        }

        public IActionResult Detail(Guid id)
        {
            var log = DB.Logs
                .Include(x => x.Form)
                .SingleOrDefault(x => x.Id == id);
            if (log == null)
                return Prompt(x =>
                {
                    x.Title = "没有找到日志";
                    x.Details = "没有找到指定的日志，请返回重新尝试！";
                    x.StatusCode = 404;
                });
            return View(log);
        }
    }
}
