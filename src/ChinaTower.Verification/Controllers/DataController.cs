﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Http;
using Microsoft.Data.Entity;
using Microsoft.Extensions.PlatformAbstractions;
using CodeComb.Data.Excel;
using CodeComb.Data.Verification.EntityFramework;
using Newtonsoft.Json;
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

        [HttpGet]
        public IActionResult EditRule(Guid id)
        {
            var rule = DB.VerificationRules
                .Include(x => x.Rule)
                .Single(x => x.Id == id);
            return View(rule);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EditRule(Guid id, string RuleJson)
        {
            var rule = DB.VerificationRules
                            .Include(x => x.Rule)
                            .Single(x => x.Id == id);
            rule.Rule.RuleJson = RuleJson;
            DB.SaveChanges();
            return Prompt(x =>
            {
                x.Title = "编辑成功";
                x.Details = "新的校验规则已经生效！";
                x.RedirectText = "返回规则列表";
                x.RedirectUrl = Url.Action("Rule", "Data");
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult DeleteRule(Guid id)
        {
            var rule = DB.VerificationRules
                            .Single(x => x.Id == id);
            DB.VerificationRules.Remove(rule);
            DB.SaveChanges();
            return Content("ok");
        }

        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Import(FormType type, IFormFile file, [FromServices] IApplicationEnvironment env)
        {
            Task.Factory.StartNew(() =>
            {
                var directory = System.IO.Path.Combine(env.ApplicationBasePath, "Upload");
                if (!System.IO.Directory.Exists(directory))
                    System.IO.Directory.CreateDirectory(directory);
                var fname = System.IO.Path.Combine(directory, Guid.NewGuid() + ".xlsx");
                file.SaveAs(fname);
                var rules = DB.VerificationRules
                    .Include(x => x.Rule)
                    .Where(x => x.Type == type)
                    .ToList();
                using (var excel = new ExcelStream(fname))
                {
                    foreach (var book in excel.WorkBook)
                    {
                        using (var data = excel.LoadSheet(book.Id))
                        {
                            foreach (var row in data)
                            {
                                var fields = new List<string>();
                                for (var i = 0; i < Hash.Headers[type].Count(); i++)
                                {
                                    fields.Add(row[i]);
                                }
                                var verifyResult = new CodeComb.Data.Verification.VerifyResult { IsSuccess = true };
                                foreach (var r in rules)
                                {
                                    var res = DataVerificationRuleManager.Verify(r.RuleId, fields.ToArray());
                                    if (!res.IsSuccess)
                                    {
                                        verifyResult.IsSuccess = false;
                                    }
                                }
                                // 如果没有校验规则
                                if (rules.Count == 0)
                                {
                                    verifyResult.IsSuccess = true;
                                    verifyResult.Information = "";
                                    verifyResult.FailedRules = new List<CodeComb.Data.Verification.Rule>();
                                }
                                var logs = new List<VerificationLog>();
                                foreach (var x in verifyResult.FailedRules)
                                    logs.Add(new VerificationLog
                                    {
                                        Field = Hash.Headers[type][x.ArgumentIndex],
                                        Time = DateTime.Now,
                                        Reason = $"{Hash.Headers[type][x.ArgumentIndex]} 字段没有通过校验",
                                        FieldIndex = x.ArgumentIndex
                                    });
                                var form = new Form
                                {
                                    FormJson = JsonConvert.SerializeObject(fields),
                                    StationKey = type == FormType.站址 ? null : (long?)Convert.ToInt64(fields[Hash.StationId[type].Value]),
                                    Type = type,
                                    UniqueKey = fields[Hash.UniqueKey[type]],
                                    VerificationJson = JsonConvert.SerializeObject(logs),
                                    VerificationTime = DateTime.Now,
                                    Status = verifyResult.IsSuccess ? VerificationStatus.Accepted : VerificationStatus.Wrong
                                };
                                if (Hash.Lat[type] != null && Hash.Lon[type] != null)
                                {
                                    form.Lat = Convert.ToDouble(fields[Hash.Lat[type].Value]);
                                    form.Lon = Convert.ToDouble(fields[Hash.Lon[type].Value]);
                                    DB.Database.ExecuteSqlCommand("INSERT INTO \"Form\" (FormJson, StationKey, Type, UniqueKey, VerificationJson, VerificationTime, Status, Lon, Lat) VALUES ('{0}', {1}, {2}, '{3}', '{4}', '{5}', {6}, {7}, {8});", form.FormJson, form.StationKey, form.Type, form.UniqueKey, form.VerificationJson, form.VerificationTime, (int)form.Status, form.Lon, form.Lat);
                                }
                                else
                                {
                                    DB.Database.ExecuteSqlCommand("INSERT INTO \"Form\" (FormJson, StationKey, Type, UniqueKey, VerificationJson, VerificationTime, Status) VALUES ('{0}', {1}, {2}, '{3}', '{4}', '{5}', {6});", form.FormJson, form.StationKey, form.Type, form.UniqueKey, form.VerificationJson, form.VerificationTime, (int)form.Status);
                                }
                            }
                        }
                    }
                }
            });
            return Prompt(x =>
            {
                x.Title = "正在导入";
                x.Details = "服务器正在导入您的数据并进行相关校验，请您稍后查看即可！";
            });
        }
    }
}