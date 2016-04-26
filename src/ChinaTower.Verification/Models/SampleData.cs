using System;
using System.Threading.Tasks;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;

namespace ChinaTower.Verification.Models
{
    public static class SampleData
    {
        public static async Task InitDB(this IServiceProvider services)
        {
            var DB = services.GetRequiredService<ChinaTowerContext>();
            var UserManager = services.GetRequiredService<UserManager<User>>();
            var RoleManager = services.GetRequiredService<RoleManager<IdentityRole>>();

            DB.Database.EnsureCreated();

            await RoleManager.CreateAsync(new IdentityRole("Root"));
            await RoleManager.CreateAsync(new IdentityRole("Member"));

            var user = new User { UserName = "root", Email = "911574351@qq.com" };
            await UserManager.CreateAsync(user, "123456");
            await UserManager.AddToRoleAsync(user, "Root");
        }
    }
}
