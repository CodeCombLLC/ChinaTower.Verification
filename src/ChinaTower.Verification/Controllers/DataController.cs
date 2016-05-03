using System;
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
using CodeComb.Data.Verification;
using CodeComb.Data.Verification.EntityFramework;
using CodeComb.Net.EmailSender;
using Newtonsoft.Json;
using Npgsql;
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
            DB.Database.ExecuteSqlCommand("UPDATE \"Form\" SET \"Status\"={0}, \"VerificationJson\" = {1} WHERE \"Type\" = {2}", VerificationStatus.Pending, "[]", type);
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
            if (flag)
                DB.Database.ExecuteSqlCommand("UPDATE \"Form\" SET \"Status\"={0}, \"VerificationJson\" = {1} WHERE \"Type\" = {2}", VerificationStatus.Pending, "[]", rule.Type);
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
            DB.Database.ExecuteSqlCommand("UPDATE \"Form\" SET \"Status\"={0}, \"VerificationJson\" = {1} WHERE \"Type\" = {2}", VerificationStatus.Pending, "[]", rule.Type);
            return Content("ok");
        }

        [HttpGet]
        public IActionResult Import()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Import(FormType type, IFormFile file, [FromServices] IApplicationEnvironment env)
        {
            using (var serviceScope = Resolver.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var userEmail = User.Current.Email;
                var IsRoot = User.IsInRole("Root");
                var cities = (await UserManager.GetClaimsAsync(User.Current))
                    .Where(x => x.Type == "管辖市区")
                    .Select(x => x.Value)
                    .ToList();
                var allc = DB.Cities
                    .Select(x => x.Id)
                    .ToList();

                var directory = System.IO.Path.Combine(env.ApplicationBasePath, "Upload");
                if (!System.IO.Directory.Exists(directory))
                    System.IO.Directory.CreateDirectory(directory);
                var fname = System.IO.Path.Combine(directory, Guid.NewGuid() + ".xlsx");
                file.SaveAs(fname);

                Task.Factory.StartNew(async () =>
                {
                    var count = 0;
                    var addition = 0;
                    var updated = 0;
                    var ignored = 0;

                    using (var db = serviceScope.ServiceProvider.GetService<ChinaTowerContext>())
                    {
                        var dvrm = serviceScope.ServiceProvider.GetRequiredService<DataVerificationRuleManager>();
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
                                        count++;
                                        var fields = new List<string>();
                                        for (var i = 0; i < Hash.Headers[type].Count(); i++)
                                        {
                                            if (i < row.Count)
                                                fields.Add(row[i]);
                                            else
                                                fields.Add("");
                                        }
                                        var verifyResult = new VerifyResult { IsSuccess = true };
                                        try
                                        {
                                            foreach (var r in rules)
                                            {
                                                var res = dvrm.Verify(r.RuleId, fields.ToArray());
                                                if (!res.IsSuccess)
                                                {
                                                    verifyResult.IsSuccess = false;
                                                }
                                            }
                                        }
                                        catch
                                        {
                                            verifyResult.Information = "未知错误";
                                            verifyResult.IsSuccess = false;
                                        }
                                        
                                        // 如果没有校验规则
                                        if (rules.Count == 0)
                                        {
                                            verifyResult.IsSuccess = true;
                                            verifyResult.Information = "";
                                            verifyResult.FailedRules = new List<Rule>();
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
                                            StationKey = type == FormType.站址 ? Convert.ToInt64(fields[Hash.UniqueKey[type]]) : Convert.ToInt64(fields[Hash.StationId[type].Value]),
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
                                        // 处理市、区信息
                                        if (Hash.City[type].HasValue)
                                            form.City = fields[Hash.City[type].Value];
                                        if (Hash.District[type].HasValue)
                                            form.District = fields[Hash.District[type].Value];
                                        if (!IsRoot && !cities.Contains(form.City) && allc.Contains(form.City))
                                        {
                                            ignored++;
                                            continue;
                                        }

                                        // 如果是站址需要额外判断
                                        if (type == FormType.站址)
                                        {
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
                                        var existedForm = db.Forms
                                            .AsNoTracking()
                                            .SingleOrDefault(x => x.UniqueKey == form.UniqueKey && x.Type == type);
                                        // 如果数据库中没有这条数据，则写入
                                        if (existedForm == null)
                                        {
                                            addition++;
                                            try
                                            {
                                                db.Database.ExecuteSqlCommand("INSERT INTO \"Form\" (\"FormJson\", \"StationKey\", \"Type\", \"UniqueKey\", \"VerificationJson\", \"VerificationTime\",\"Status\", \"Lon\", \"Lat\", \"City\", \"District\", \"Name\") VALUES ({0}, {1}, {2}, {3}, {4}, {5}, {6}, {7}, {8}, {9}, {10}, {11});", form.FormJson, form.StationKey, form.Type, form.UniqueKey, form.VerificationJson, form.VerificationTime, (int)form.Status, form.Lon, form.Lat, form.City, form.District, form.Name);
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex.ToString());
                                            }
                                        }
                                        // 否则更新
                                        else
                                        {
                                            updated++;
                                            try
                                            {
                                                db.Database.ExecuteSqlCommand("UPDATE \"Form\" SET \"Name\" = {0}, \"Lon\" = {1}, \"Lat\" = {2}, \"StationKey\" = {3}, \"Status\" = {4}, \"Type\" = {5}, \"VerificationJson\" = {6}, \"VerificationTime\" = {7}, \"UniqueKey\" = {8}, \"City\" = {9}, \"District\" = {10} WHERE \"UniqueKey\" = {11}",
                                                    form.Name,
                                                    form.Lon,
                                                    form.Lat,
                                                    form.StationKey,
                                                    form.Status,
                                                    (int)form.Type,
                                                    form.VerificationJson,
                                                    form.VerificationTime,
                                                    form.UniqueKey,
                                                    form.City,
                                                    form.District,
                                                    form.UniqueKey);
                                            }
                                            catch (Exception ex)
                                            {
                                                Console.WriteLine(ex.ToString());
                                            }
                                        }
                                    }
                                }
                            }
                        }
                        var email = serviceScope.ServiceProvider.GetRequiredService<IEmailSender>();
                        await email.SendEmailAsync(userEmail, "导入数据完成", $"本次导入数据已于 { DateTime.Now.ToString("yyyy年MM月dd日 HH时mm分") } 完成，共 {count} 条数据。其中新增 {addition} 条，更新 {updated} 条{ (ignored > 0 ? $"，有 { ignored } 条数据不属于您的管辖区，因此系统没有允许您导入这些数据" : "") }。");
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

        /// <summary>
        /// 展示校验管理界面
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public async Task<IActionResult> Verify()
        {
            IEnumerable<Form> ret = DB.Forms
                .Where(x => x.Status != VerificationStatus.Accepted);
            if (!User.IsInRole("Root"))
            {
                var cities = (await UserManager.GetClaimsAsync(User.Current))
                    .Where(x => x.Type == "管辖市区")
                    .Select(x => x.Value)
                    .ToList();
                var allc = DB.Cities.Select(x => x.Id).ToList();
                ret = ret.Where(x => cities.Contains(x.City) || !cities.Contains(x.City) && !allc.Contains(x.City));
            }
            ret = ret.OrderBy(x => x.City)
                .ThenBy(x => x.District);
            return PagedView(ret);
        }

        /// <summary>
        /// 对用户所选定范围进行数据校验
        /// </summary>
        /// <param name="PendingOnly"></param>
        /// <returns></returns>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Verify(bool PendingOnly)
        {
            using (var serviceScope = Resolver.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                var userEmail = User.Current.Email;
                var uid = User.Current.Id;
                Task.Factory.StartNew(()=>
                {
                    using (var db = serviceScope.ServiceProvider.GetService<ChinaTowerContext>())
                    {
                        var dvrm = serviceScope.ServiceProvider.GetRequiredService<DataVerificationRuleManager>();
                        var total = 0;
                        var failed = 0;
                        if (!PendingOnly)
                        {
                            if (User.IsInRole("Root"))
                            {
                                db.Database.ExecuteSqlCommand("UPDATE \"Form\" SET \"Status\" = {0} WHERE \"Status\" <> {0}", 2);
                            }
                            else
                            {
                                db.Database.ExecuteSqlCommand("UPDATE \"Form\" SET \"Status\" = {0} WHERE \"Status\" <> {0} AND \"City\" IN (SELECT \"ClaimValue\" FROM \"AspNetUserClaims\" WHERE \"UserId\" = {1} AND \"ClaimType\" = {2})", 2, uid, "管辖市区");
                            }
                        }
                        var Cities = db.Cities.ToList();
                        var Rules = db.VerificationRules.ToList();
                        using (var conn = new NpgsqlConnection(Startup.ConnectionString))
                        {
                            conn.Open();
                            int count;
                            do
                            {
                                count = 0;
                                using (var cmd = new NpgsqlCommand("SELECT \"Id\",\"FormJson\", \"Type\" FROM \"Form\" WHERE \"Status\" = 2 LIMIT 100", conn))
                                using (var reader = cmd.ExecuteReader())
                                {
                                    while (reader.Read())
                                    {
                                        total++;
                                        count++;
                                        var type = (FormType)reader["Type"];
                                        var result = new VerifyResult { IsSuccess = true, Information = "", FailedRules = new List<Rule>() };
                                        var json = reader["FormJson"].ToString();
                                        var fields = JsonConvert.DeserializeObject<string[]>(json);
                                        var status = VerificationStatus.Accepted;
                                        var rules = Rules
                                            .Where(x => x.Type == type)
                                            .ToList();
                                        foreach (var x in rules)
                                        {
                                            try
                                            {
                                                var res = dvrm.Verify(x.RuleId, fields);
                                                if (!res.IsSuccess)
                                                {
                                                    result.IsSuccess = false;
                                                    result.Information += res.Information;
                                                    result.FailedRules.AddRange(res.FailedRules);
                                                    status = VerificationStatus.Wrong;
                                                }
                                            }
                                            catch
                                            {
                                                result.IsSuccess = false;
                                                result.Information += "未知原因";
                                                status = VerificationStatus.Wrong;
                                            }
                                        }
                                        var logs = new List<VerificationLog>();
                                        foreach (var x in result.FailedRules)
                                        {
                                            logs.Add(new VerificationLog { Field = Hash.Headers[type][x.ArgumentIndex], FieldIndex = x.ArgumentIndex, Reason = $"{ Hash.Headers[type][x.ArgumentIndex] }字段没有通过校验" });
                                        }
                                        if (type == FormType.站址)
                                        {
                                            var city = Cities.SingleOrDefault(x => x.Id == fields[3]);
                                            // 1. 判断城市是否合法
                                            if (city == null)
                                            {
                                                logs.Add(new VerificationLog { Time = DateTime.Now, Field = Hash.Headers[type][3], FieldIndex = 3, Reason = $"不存在城市{fields[3]}" });
                                                status = VerificationStatus.Wrong;
                                            }
                                            // 2. 判断区县是否合法
                                            else if (!city.Districts.Contains(fields[4]))
                                            {
                                                logs.Add(new VerificationLog { Time = DateTime.Now, Field = Hash.Headers[type][4], FieldIndex = 4, Reason = $"{city.Id}中不存在区县{fields[4]}" });
                                                status = VerificationStatus.Wrong;
                                            }
                                            // 3. 判断经纬度是否合法
                                            else if (!city.Edge.IsInPolygon(new CodeComb.Algorithm.Geography.Point { X = Convert.ToDouble(fields[Hash.Lon[FormType.站址].Value]), Y = Convert.ToDouble(fields[Hash.Lat[FormType.站址].Value]), Type = CodeComb.Algorithm.Geography.PointType.WGS }))
                                            {
                                                logs.Add(new VerificationLog { Time = DateTime.Now, Field = Hash.Headers[type][Hash.Lon[type].Value], FieldIndex = Hash.Lon[type].Value, Reason = $"({Convert.ToDouble(fields[Hash.Lon[FormType.站址].Value])}, {Convert.ToDouble(fields[Hash.Lat[FormType.站址].Value])})不属于{fields[3]}" });
                                                logs.Add(new VerificationLog { Time = DateTime.Now, Field = Hash.Headers[type][Hash.Lat[type].Value], FieldIndex = Hash.Lat[type].Value, Reason = $"({Convert.ToDouble(fields[Hash.Lon[FormType.站址].Value])}, {Convert.ToDouble(fields[Hash.Lat[FormType.站址].Value])})不属于{fields[3]}" });
                                                status = VerificationStatus.Wrong;
                                            }
                                        }

                                        // 计数
                                        if (status == VerificationStatus.Wrong)
                                            failed++;

                                        // 写入数据库
                                        using (var conn2 = new NpgsqlConnection(Startup.ConnectionString))
                                        {
                                            conn2.Open();
                                            using (var cmd2 = new NpgsqlCommand("UPDATE \"Form\" SET \"VerificationTime\" = @p0, \"Status\" = @p1, \"VerificationJson\" = @p2 WHERE \"Id\" = @p3;", conn2))
                                            {
                                                cmd2.Parameters.Add(new NpgsqlParameter { ParameterName = "@p0", Value = DateTime.Now });
                                                cmd2.Parameters.Add(new NpgsqlParameter { ParameterName = "@p1", Value = (int)status });
                                                cmd2.Parameters.Add(new NpgsqlParameter { ParameterName = "@p2", Value = JsonConvert.SerializeObject(logs) });
                                                cmd2.Parameters.Add(new NpgsqlParameter { ParameterName = "@p3", Value = Convert.ToInt64(reader["Id"]) });
                                                cmd2.ExecuteNonQuery();
                                            }
                                        }
                                    }
                                }
                                GC.Collect();
                            }
                            while (count > 0);
                        }
                        var email = serviceScope.ServiceProvider.GetService<IEmailSender>();
                        email.SendEmailAsync(userEmail, "数据校验完成", $"数据已经校验完成，总共校验 {total} 条数据，其中 {failed} 条没有通过校验。");
                    }
                });
            }
            return Prompt(x =>
            {
                x.Title = "正在校验";
                x.Details = "系统正在校验数据，在完成校验后系统将给您发送一封电子邮件通知您，请您耐心等待！";
            });
        }

        [HttpGet]
        [AnyRoles("Root")]
        public IActionResult Clear()
        {
            return View();
        }

        [HttpPost]
        [AnyRoles("Root")]
        [ValidateAntiForgeryToken]
        public IActionResult Clear(FormType[] types)
        {
            foreach(var x in types)
            {
                DB.Database.ExecuteSqlCommand("DELETE FROM \"Form\" WHERE \"Type\" = {0}", (int)x);
            }
            return Prompt(x =>
            {
                x.Title = "清空成功";
                x.Details = "您所选择的表单已经全部清空";
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Export([FromServices] IApplicationEnvironment env)
        {
            var url = Request.Scheme + "://" + Request.Host + "/Home/Download/";
            var uid = User.Current.Id;
            var userEmail = User.Current.Email;
            var allc = DB.Cities.Select(x => x.Id).ToList();
            var isRoot = User.IsInRole("Root");
            var cities = (await UserManager.GetClaimsAsync(User.Current)).Where(x => x.Type == "管辖市区").Select(x => x.Value).ToList();
            var directory = System.IO.Path.Combine(env.ApplicationBasePath, "Export");
            if (!System.IO.Directory.Exists(directory))
                System.IO.Directory.CreateDirectory(directory);
            var fname = System.IO.Path.Combine(directory, Guid.NewGuid() + ".xlsx");
            using (var serviceScope = Resolver.GetRequiredService<IServiceScopeFactory>().CreateScope())
            {
                Task.Factory.StartNew(async ()=> 
                {
                    var db = serviceScope.ServiceProvider.GetRequiredService<ChinaTowerContext>();
                    var email = serviceScope.ServiceProvider.GetRequiredService<IEmailSender>();
                    using (var excel = ExcelStream.Create(fname))
                    using (var sheet1 = excel.LoadSheet(1))
                    {
                        var tmp = db.Forms
                            .Where(x => x.Status == VerificationStatus.Wrong);
                        if (!isRoot)
                            tmp = tmp.Where(x => cities.Contains(x.City) || (!cities.Contains(x.City) && !allc.Contains(x.City)));
                        var g = tmp.GroupBy(x => x.StationKey)
                            .Select(x => new { Key = x.Key, Count = x.Count(), Details = x.Select(y => new { Type = y.Type, Logs = y.VerificationJson }) })
                            .ToList();
                        var ids = g.Where(x => x.Key.HasValue)
                            .Select(x => x.Key.Value.ToString())
                            .Distinct()
                            .ToList();
                        var dic = db.Forms
                            .Where(x => x.Type == FormType.站址 && ids.Contains(x.UniqueKey))
                            .ToDictionary(x => x.UniqueKey, x => x.Name);
                        foreach (var x in g.Where(x => x.Key.HasValue))
                        {
                            if (!dic.ContainsKey(x.Key.Value.ToString()))
                                continue;
                            sheet1.Add(new CodeComb.Data.Excel.Infrastructure.Row { $"【{dic[x.Key.Value.ToString()]}】 站址编码：{x.Key.Value} 错误表单：{x.Count}"});
                            foreach (var y in x.Details)
                            {
                                try
                                {
                                    var log = JsonConvert.DeserializeObject<ICollection<VerificationLog>>(y.Logs);
                                    foreach (var z in log)
                                        foreach (var line in z.Reason.Split('\n'))
                                            sheet1.Add(new CodeComb.Data.Excel.Infrastructure.Row { $"┝ ◇[{y.Type}]{line}" });
                                }
                                catch (Exception ex)
                                {
                                    Console.WriteLine(ex.ToString());
                                }
                            }
                        }
                        sheet1.SaveChanges();
                    }
                    var blob = System.IO.File.ReadAllBytes(fname);
                    var b = new Blob
                    {
                        Content = blob,
                        ContentType = "application/vnd.ms-excel",
                        ContentLength = blob.Length,
                        FileName = $"校验结果导出{ DateTime.Now.ToString("yyyyMMddHHmmss") }.xlsx",
                        Time = DateTime.Now,
                        UserId = uid
                    };
                    db.Blobs.Add(b);
                    db.SaveChanges();
                    await email.SendEmailAsync(userEmail, "校验结果导出完毕", $"<a href=\"{ url + b.Id }\">校验结果导出{ DateTime.Now.ToString("yyyyMMddHHmmss") }.xlsx</a>");
                });
            }
            return Prompt(x =>
            {
                x.Title = "正在导出";
                x.Details = "系统正在为您将校验结果导出到Excel，在导出完毕后，您将收到带有Excel表格附件的电子邮件，请稍候。";
            });
        }
    }
}
