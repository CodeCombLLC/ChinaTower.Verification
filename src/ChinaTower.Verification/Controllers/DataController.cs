using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;

namespace ChinaTower.Verification.Controllers
{
    public class DataController : BaseController
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
