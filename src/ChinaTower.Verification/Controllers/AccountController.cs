using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNet.Mvc;
using Microsoft.AspNet.Authorization;
using Microsoft.Data.Entity;
using ChinaTower.Verification.Models;

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

        [HttpPost]
        [AnyRoles("Root")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (id == User.Current.Id)
                return Prompt(x =>
                {
                    x.Title = "删除失败";
                    x.Details = "这个用户不能被删除，请返回！";
                    x.StatusCode = 400;
                });
            var user = await UserManager.FindByIdAsync(id);
            await UserManager.DeleteAsync(user);
            return Content("ok");
        }

        [HttpPost]
        [AnyRoles("Root")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(string username, string password, string role, string email, string cities)
        {
            var user = new User { UserName = username, Email = email };
            await UserManager.CreateAsync(user, password);
            await UserManager.AddToRoleAsync(user, role);
            if (role == "Member")
            {
                var tmp = cities.Split(',');
                foreach(var x in tmp)
                {
                    var city = x.Trim();
                    if (!string.IsNullOrEmpty(city))
                    {
                        await UserManager.AddClaimAsync(user, new System.Security.Claims.Claim("管辖市区", x));
                    }
                }
            }
            return RedirectToAction("Index", "Account");
        }

        [HttpGet]
        [AnyRoles("Root")]
        public async Task<IActionResult> Detail(string id)
        {
            var user = await UserManager.FindByIdAsync(id);
            return View(user);
        }

        [HttpPost]
        [AnyRoles("Root")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, string email, string cities, string role, string password)
        {
            var user = DB.Users.Single(x => x.Id == id);
            user.Email = email;
            DB.SaveChanges();
            await UserManager.RemoveFromRolesAsync(user, await UserManager.GetRolesAsync(user));
            await UserManager.AddToRoleAsync(user, role);
            if (!string.IsNullOrEmpty(password))
            {
                var token = await UserManager.GeneratePasswordResetTokenAsync(user);
                await UserManager.ResetPasswordAsync(user, token, password);
            }
            if (role == "Member")
            {
                await UserManager.RemoveClaimsAsync(user, (await UserManager.GetClaimsAsync(user)).Where(x => x.Type == "管辖市区"));
                var tmp = cities.Split(',');
                foreach(var x in tmp)
                {
                    var city = x.Trim();
                    if (!string.IsNullOrEmpty(city))
                        await UserManager.AddClaimAsync(user, new System.Security.Claims.Claim("管辖市区", city));
                }
            }
            return Prompt(x =>
            {
                x.Title = "修改成功";
                x.Details = "用户信息已经保存成功！";
                x.HideBack = true;
                x.RedirectText = "返回用户列表";
                x.RedirectUrl = Url.Action("Index", "Account");
            });
        }

        [HttpGet]
        public IActionResult Password()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Password(string old, string @new, string confirm)
        {
            if (@new != confirm)
                return Prompt(x =>
                {
                    x.Title = "修改失败";
                    x.Details = "两次输入的密码不一致！";
                });
            if (string.IsNullOrEmpty(confirm))
                return Prompt(x =>
                {
                    x.Title = "修改失败";
                    x.Details = "密码不能为空";
                });
            var result = await UserManager.ChangePasswordAsync(User.Current, old, @new);
            if (!result.Succeeded)
                return Prompt(x =>
                {
                    x.Title = "修改失败";
                    x.Details = result.Errors.First().Description;
                });
            return Prompt(x =>
            {
                x.Title = "修改成功";
                x.Details = "密码修改成功，新密码已经生效";
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await SignInManager.SignOutAsync();
            return RedirectToAction("Login", "Account");
        }
    }
}
