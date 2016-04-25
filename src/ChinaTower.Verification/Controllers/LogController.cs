using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Authorization;
using Microsoft.Data.Entity;

namespace ChinaTower.Verification.Controllers
{
    [Authorize]
    public class LogController : BaseController
    {
        public IActionResult Index()
        {
            var logs = DB.Logs
                .Include(x => x.User)
                .Include(x => x.Form)
                .OrderByDescending(x => x.Id);
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
