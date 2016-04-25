using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Authorization;
using Microsoft.Data.Entity;

namespace ChinaTower.Verification.Controllers
{
    [Authorize]
    public class AccountController : BaseController
    {
        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string username, string password)
        {
            var result = await SignInManager.PasswordSignInAsync(username, password, false, false);
            if (result.Succeeded)
                return RedirectToAction("Index", "Home");
            else
                return Prompt(x =>
                {
                    x.Title = "登录失败";
                    x.Details = "用户名或密码不正确，请返回重新登录！";
                    x.StatusCode = 401;
                });
        }

        [AnyRoles("Root")]
        public IActionResult Index()
        {
            var users = UserManager.Users
                .Include(x => x.Claims)
                .Include(x => x.Roles)
                .ToList();
            return View(users);
        }
    }
}
