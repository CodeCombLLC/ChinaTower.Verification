using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using ChinaTower.Verification.Models;

namespace ChinaTower.Verification.Controllers
{
    public class BaseController : BaseController<ChinaTowerContext, User, string>
    {
    }
}
