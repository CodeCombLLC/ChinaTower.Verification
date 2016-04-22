using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using CodeComb.Data.Verification.EntityFramework;
using ChinaTower.Verification.Models;
using ChinaTower.Verification.Models.Infrastructures;

namespace ChinaTower.Verification.Controllers
{
    public class DataController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Rule(FormType? type, string alias)
        {
            IEnumerable<VerificationRule> ret = DB.VerificationRules
                .Include(x => x.Rule);
            if (type.HasValue)
                ret = ret.Where(x => x.Type == type);
            if (!string.IsNullOrEmpty(alias))
                ret = ret.Where(x => alias.Contains(x.Alias) || x.Alias.Contains(alias));
            ret = ret.OrderBy(x => x.Type);
            return PagedView(ret);
        }

        [HttpPost]
        public IActionResult InsertRule(FormType type, string alias)
        {
            var dvr = new DataVerificationRule();
            DB.DataVerificationRules.Add(dvr);
            var vr = new VerificationRule
            {
                Alias = alias,
                RuleId = dvr.Id,
                Type = type
            };
            DB.VerificationRules.Add(vr);
            DB.SaveChanges();
            return RedirectToAction("EditRule", "Data", new { id = vr.Id });
        }

        public IActionResult EditRule(Guid id)
        {
            var rule = DB.VerificationRules
                .Include(x => x.Rule)
                .Single(x => x.Id == id);
            return View(rule);
        }
    }
}
