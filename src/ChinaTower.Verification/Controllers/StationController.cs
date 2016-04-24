using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace ChinaTower.Verification.Controllers
{
    public class StationController : BaseController
    {
        public async Task<IActionResult> Index(string StationId, string StationName,string City, string District, bool ErrorOnly)
        {
            ViewBag.Cities = DB.Cities.ToList();
            var ret = DB.Forms
                .Where(x => x.Type == Models.Infrastructures.FormType.站址);
            if (ErrorOnly)
                ret = ret.Where(x => x.Status == Models.Infrastructures.VerificationStatus.Wrong);
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
                ret = ret.Where(x => cities.Contains(x.City));
            }
            return PagedView(ret);
        }
    }
}
