using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using CodeComb.Algorithm.Geography;
using Newtonsoft.Json;
using ChinaTower.Verification.Models;

namespace ChinaTower.Verification.Controllers
{
    public class CityController : BaseController
    {
        [HttpGet]
        public IActionResult Index(string city)
        {
            IEnumerable<City> ret = DB.Cities;
            if (!string.IsNullOrEmpty(city))
                ret = ret.Where(x => x.Id.Contains(city) || city.Contains(x.Id));
            return PagedView(ret);
        }

        [HttpPost]
        [AnyRoles("Root")]
        [ValidateAntiForgeryToken]
        public IActionResult Insert(string city, [FromHeader] string Referer)
        {
            if (DB.Cities.SingleOrDefault(x => x.Id == city) != null)
                return Prompt(x =>
                {
                    x.Title = "添加失败";
                    x.Details = $"系统中已经存在了名为{city}的城市，请勿重复添加！";
                    x.StatusCode = 400;
                });
            DB.Cities.Add(new City { Id = city });
            DB.SaveChanges();
            return Redirect(Referer);
        }


        [HttpPost]
        [AnyRoles("Root")]
        [ValidateAntiForgeryToken]
        public IActionResult Delete(string city, [FromHeader] string Referer)
        {
            var c = DB.Cities.Single(x => x.Id == city);
            DB.Cities.Remove(c);
            DB.SaveChanges();
            return Redirect(Referer);
        }

        [HttpGet]
        [AnyRoles("Root")]
        public IActionResult Edge(string id)
        {
            var city = DB.Cities
                .Where(x => x.Id == id)
                .SingleOrDefault();
            return View(city);
        }

        [HttpPost]
        [AnyRoles("Root")]
        [ValidateAntiForgeryToken]
        public IActionResult Edge(string id, string edge)
        {
            var tmp1 = edge.Split('|');
            var polygon = new Polygon();
            foreach (var str in tmp1)
            {
                var tmp2 = str.Split(',');
                var p = new Point { X = Convert.ToDouble(tmp2[0]), Y = Convert.ToDouble(tmp2[1]), Type = PointType.Baidu };
                polygon.Add(p.ToWgsPoint());
            }
            var city = DB.Cities.Single(x => x.Id == id);
            city.EdgeJson = JsonConvert.SerializeObject(polygon);
            DB.SaveChanges();
            return Prompt(x =>
            {
                x.Title = "修改成功";
                x.Details = "新的城市边界已经保存！";
                x.RedirectText = "返回城市列表";
                x.RedirectUrl = Url.Action("Index", "City");
            });
        }
    }
}