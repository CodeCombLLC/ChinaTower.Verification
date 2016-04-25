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
    }
}
