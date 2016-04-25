﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Http;
using Microsoft.AspNet.Authorization;
using Microsoft.Data.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using CodeComb.Data.Excel;
using CodeComb.Data.Verification.EntityFramework;
using Newtonsoft.Json;
using ChinaTower.Verification.Models;
using ChinaTower.Verification.Models.Infrastructures;

namespace ChinaTower.Verification.Controllers
{
    [Authorize]
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
            DB.Database.ExecuteSqlCommand("UPDATE \"Forms\" SET \"Status\"={0}, \"VerificationJson\" = {1} WHERE \"Type\" = {2}", VerificationStatus.Pending, "[]", type);
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
            var flag = false;
            if (RuleJson != rule.Rule.RuleJson)
                flag = true;
            rule.Rule.RuleJson = RuleJson;
            DB.SaveChanges();
            DB.Database.ExecuteSqlCommand("UPDATE \"Forms\" SET \"Status\"={0}, \"VerificationJson\" = {1} WHERE \"Type\" = {2}", VerificationStatus.Pending, "[]", rule.Type);
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
            DB.Database.ExecuteSqlCommand("UPDATE \"Forms\" SET \"Status\"={0}, \"VerificationJson\" = {1} WHERE \"Type\" = {2}", VerificationStatus.Pending, "[]", rule.Type);
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
            using (var serviceScope = Resolver.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                Task.Factory.StartNew(() =>
                {
                    using (var db = serviceScope.ServiceProvider.GetService<ChinaTowerContext>())
                    {
                        var directory = System.IO.Path.Combine(env.ApplicationBasePath, "Upload");
                        if (!System.IO.Directory.Exists(directory))
                            System.IO.Directory.CreateDirectory(directory);
                        var fname = System.IO.Path.Combine(directory, Guid.NewGuid() + ".xlsx");
                        file.SaveAs(fname);
                        var rules = db.VerificationRules
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
                                        // 检查是否为表头
                                        var total = Hash.Headers[type].Count();
                                        var similar = 0;
                                        for (var i = 0; i < row.Count && i < total; i++)
                                            if (Hash.Headers[type][i].Contains(row[i]) || row[i].Contains(Hash.Headers[type][i]))
                                                similar++;
                                        // 如果与表头字段相似度过半则认定为表头
                                        if ((double)similar / (double)total >= 0.5)
                                            continue;
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
                                        // 获取字段校验结果
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
                                            Status = verifyResult.IsSuccess ? VerificationStatus.Accepted : VerificationStatus.Wrong,
                                            City = "",
                                            District = ""
                                        };
                                        // 获取经纬度
                                        var gpsPosition = true;
                                        if (Hash.Lat[type] != null && Hash.Lon[type] != null)
                                        {
                                            try
                                            {
                                                form.Lat = Convert.ToDouble(fields[Hash.Lat[type].Value]);
                                                form.Lon = Convert.ToDouble(fields[Hash.Lon[type].Value]);
                                            }
                                            catch
                                            {
                                                gpsPosition = false;
                                            }
                                        }
                                        // 如果是站址需要额外判断
                                        if (type == FormType.站址)
                                        {
                                            form.City = fields[3];
                                            form.District = fields[4];
                                            form.Name = fields[0];
                                            var city = db.Cities.SingleOrDefault(x => x.Id == form.City);
                                            // 1. 判断城市是否合法
                                            if (city == null)
                                            {
                                                var l = form.VerificationLogs;
                                                l.Add(new VerificationLog { Time = DateTime.Now, Field = Hash.Headers[type][3], FieldIndex = 3, Reason = $"不存在城市{form.City}" });
                                                form.VerificationJson = JsonConvert.SerializeObject(l);
                                                form.Status = VerificationStatus.Wrong;
                                            }
                                            // 2. 判断区县是否合法
                                            else if (!city.Districts.Contains(form.District))
                                            {
                                                var l = form.VerificationLogs;
                                                l.Add(new VerificationLog { Time = DateTime.Now, Field = Hash.Headers[type][4], FieldIndex = 4, Reason = $"{city.Id}中不存在区县{form.District}" });
                                                form.VerificationJson = JsonConvert.SerializeObject(l);
                                                form.Status = VerificationStatus.Wrong;
                                            }
                                            // 3a. 判断经纬度是否合法
                                            else if (!gpsPosition)
                                            {
                                                var l = form.VerificationLogs;
                                                l.Add(new VerificationLog { Time = DateTime.Now, Field = Hash.Headers[type][Hash.Lon[type].Value], FieldIndex = Hash.Lon[type].Value, Reason = $"({form.Lon.Value}, {form.Lat.Value})不属于{form.City}" });
                                                l.Add(new VerificationLog { Time = DateTime.Now, Field = Hash.Headers[type][Hash.Lat[type].Value], FieldIndex = Hash.Lat[type].Value, Reason = $"({form.Lon.Value}, {form.Lat.Value})不属于{form.City}" });
                                                form.VerificationJson = JsonConvert.SerializeObject(l);
                                                form.Status = VerificationStatus.Wrong;
                                            }
                                            // 3b. 判断经纬度是否合法
                                            else if (!city.Edge.IsInPolygon(new CodeComb.Algorithm.Geography.Point { X = form.Lon.Value, Y = form.Lat.Value, Type = CodeComb.Algorithm.Geography.PointType.WGS }))
                                            {
                                                var l = form.VerificationLogs;
                                                l.Add(new VerificationLog { Time = DateTime.Now, Field = Hash.Headers[type][Hash.Lon[type].Value], FieldIndex = Hash.Lon[type].Value, Reason = $"({form.Lon.Value}, {form.Lat.Value})不属于{form.City}" });
                                                l.Add(new VerificationLog { Time = DateTime.Now, Field = Hash.Headers[type][Hash.Lat[type].Value], FieldIndex = Hash.Lat[type].Value, Reason = $"({form.Lon.Value}, {form.Lat.Value})不属于{form.City}" });
                                                form.VerificationJson = JsonConvert.SerializeObject(l);
                                                form.Status = VerificationStatus.Wrong;
                                            }
                                        }
                                        var existedForm = db.Forms.SingleOrDefault(x => x.UniqueKey == form.UniqueKey && x.Type == type);
                                        // 如果数据库中没有这条数据，则写入
                                        if (existedForm == null)
                                        {
                                            if (Hash.Lat[type] != null && Hash.Lon[type] != null)
                                            {
                                                db.Database.ExecuteSqlCommand("INSERT INTO \"Form\" (\"FormJson\", \"StationKey\", \"Type\", \"UniqueKey\", \"VerificationJson\", \"VerificationTime\",\"Status\", \"Lon\", \"Lat\", \"City\", \"District\", \"Name\") VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11});", form.FormJson, form.StationKey, form.Type, form.UniqueKey, form.VerificationJson, form.VerificationTime, (int)form.Status, form.Lon, form.Lat, form.City, form.District, form.Name);
                                            }
                                            else
                                            {
                                                db.Database.ExecuteSqlCommand("INSERT INTO \"Form\" (\"FormJson\", \"StationKey\", \"Type\", \"UniqueKey\", \"VerificationJson\", \"VerificationTime\", \"Status\", \"City, \"District\") VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9});", form.FormJson, form.StationKey, form.Type, form.UniqueKey, form.VerificationJson, form.VerificationTime, (int)form.Status, form.City, form.District, form.Name);
                                            }
                                        }
                                        // 否则更新
                                        else
                                        {
                                            existedForm.Name = form.Name;
                                            existedForm.Lon = form.Lon;
                                            existedForm.Lat = form.Lat;
                                            existedForm.StationKey = form.StationKey;
                                            existedForm.Status = form.Status;
                                            existedForm.Type = form.Type;
                                            existedForm.VerificationJson = form.VerificationJson;
                                            existedForm.VerificationTime = form.VerificationTime;
                                            existedForm.UniqueKey = form.UniqueKey;
                                            existedForm.City = form.City;
                                            existedForm.District = form.District;
                                            db.SaveChanges();
                                        }
                                    }
                                }
                            }
                        }
                        try
                        {
                            System.IO.File.Delete(fname);
                        }
                        catch { }
                    }
                });
            }
            return Prompt(x =>
            {
                x.Title = "正在导入";
                x.Details = "服务器正在导入您的数据并进行相关校验，请您稍后查看即可！";
            });
        }
    }
}
