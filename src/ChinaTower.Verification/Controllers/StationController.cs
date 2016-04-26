using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.Data.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Microsoft.AspNet.Authorization;
using CodeComb.Data.Excel;
using CodeComb.Data.Verification;
using CodeComb.Net.EmailSender;
using Newtonsoft.Json;
using ChinaTower.Verification.Models;
using ChinaTower.Verification.Models.Infrastructures;

namespace ChinaTower.Verification.Controllers
{
    [Authorize]
    public class StationController : BaseController
    {
        public async Task<IActionResult> Index(string StationId, string StationName,string City, string District, VerificationStatus? Status, [FromServices] IApplicationEnvironment env, bool raw = false)
        {
            ViewBag.Cities = DB.Cities.ToList();
            var ret = DB.Forms
                .Where(x => x.Type == FormType.站址);
            if (Status.HasValue)
                ret = ret.Where(x => x.Status == Status.Value);
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
                var allc = DB.Cities.Select(x => x.Id).ToList();
                ret = ret.Where(x => cities.Contains(x.City) || !cities.Contains(x.City) && !allc.Contains(x.City));
            }
            return PagedView(ret);
        }

        public IActionResult Show(long? id, long? sid)
        {
            if (id.HasValue)
            {
                var form = DB.Forms.Single(x => x.Id == id.Value);
                if (form.Type != FormType.站址)
                    return Prompt(x =>
                    {
                        x.Title = "非法操作";
                        x.Details = "您的请求不正确，请返回重试！";
                        x.StatusCode = 401;
                    });
                ViewBag.RelatedForms = DB.Forms
                    .Where(x => x.Type != FormType.站址 && x.StationKey.ToString() == form.UniqueKey)
                    .ToList();
                return View(form);
            }
            else
            {
                var formId = DB.Forms
                    .Where(x => x.UniqueKey == sid.Value.ToString() && x.Type == FormType.站址)
                    .Select(x => x.Id)
                    .Single();
                return RedirectToAction("Show", "Station", new { id = formId });
            }
        }
        
        public IActionResult FIelds(long id)
        {
            var form = DB.Forms.Single(x => x.Id == id);
            ViewBag.Headers = Hash.Headers[form.Type];
            ViewBag.Rules = DB.VerificationRules
                .Include(x => x.Rule)
                .Where(x => x.Type == form.Type)
                .ToList();
            return View(form);
        }

        public IActionResult Verify(FormType type, string[] fields)
        {
            var rules = DB.VerificationRules
                .Where(x => x.Type == type)
                .ToList();
            var result = new VerifyResult { IsSuccess = true, Information = "", FailedRules = new List<Rule>() };
            foreach(var x in rules)
            {
                var res = DataVerificationRuleManager.Verify(x.RuleId, fields);
                if (!res.IsSuccess)
                {
                    result.IsSuccess = false;
                    result.Information += res.Information;
                    result.FailedRules.AddRange(res.FailedRules);
                }
            }
            return Json(result.FailedRules.Select(x => x.ArgumentIndex));
        }

        public IActionResult Log(long id)
        {
            var form = DB.Forms.Single(x => x.Id == id);
            ViewBag.Rules = DB.VerificationRules
                .Where(x => x.Type == form.Type)
                .ToList();
            return View(form);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Edit(long id, string[] fields, [FromHeader] string Referer)
        {
            // 准备工作
            var gpsPosition = true;
            var form = DB.Forms.Single(x => x.Id == id);
            var modifyLog = new Log
            {
                Time = DateTime.Now,
                UserId = User.Current.Id,
                FormId = form.Id,
                OriginJson = form.FormJson
            };
            var type = form.Type;
            var result = new VerifyResult { IsSuccess = true, Information = "", FailedRules = new List<Rule>() };

            // 赋值
            try
            {
                form.Lon = Convert.ToDouble(fields[Hash.Lon[type].Value]);
                form.Lat = Convert.ToDouble(fields[Hash.Lat[type].Value]);
            }
            catch
            {
                gpsPosition = false;
            }
            if (Hash.City[type].HasValue)
                form.City = fields[Hash.City[type].Value];
            if (Hash.District[type].HasValue)
                form.District = fields[Hash.District[type].Value];
            if (type == FormType.站址)
            {
                form.Name = fields[0];
            }
            else
            {
                form.StationKey = Convert.ToInt64(fields[Hash.StationId[type].Value]);
            }
            form.UniqueKey = fields[Hash.UniqueKey[type]];
            form.FormJson = JsonConvert.SerializeObject(fields);

            // 根据校验规则进行校验
            var rules = DB.VerificationRules
                .Where(x => x.Type == form.Type)
                .ToList();
            foreach (var x in rules)
            {
                var res = DataVerificationRuleManager.Verify(x.RuleId, fields);
                if (!res.IsSuccess)
                {
                    result.IsSuccess = false;
                    result.Information += res.Information;
                    result.FailedRules.AddRange(res.FailedRules);
                }
            }
            var logs = new List<VerificationLog>();
            foreach(var x in result.FailedRules)
            {
                logs.Add(new VerificationLog { Field = Hash.Headers[type][x.ArgumentIndex], FieldIndex = x.ArgumentIndex, Reason = $"{ Hash.Headers[type][x.ArgumentIndex] }字段没有通过校验" });
            }
            form.Status = result.IsSuccess ? VerificationStatus.Accepted : VerificationStatus.Wrong;
            form.VerificationTime = DateTime.Now;
            // 如果表单是站址，则需要额外校验
            if (form.Type == FormType.站址)
            {
                var city = DB.Cities.SingleOrDefault(x => x.Id == form.City);
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
            modifyLog.ModifiedJson = form.FormJson;
            DB.Logs.Add(modifyLog);
            DB.SaveChanges();
            return Prompt(x =>
            {
                x.Title = "修改成功";
                x.Details = "表单信息已经成功保存！";
                x.HideBack = true;
                x.RedirectText = "返回上一页";
                x.RedirectUrl = Referer;
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Pano(long id, string url)
        {
            var form = DB.Forms.Single(x => x.Id == id);
            form.PanoUrl = url;
            DB.SaveChanges();
            return RedirectToAction("Show", "Station", new { id = id });
        }
    }
}
