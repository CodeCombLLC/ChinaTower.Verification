using System;
using System.Threading;
using System.Linq;
using Microsoft.AspNet.Builder;
using Microsoft.AspNet.Hosting;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Data.Entity;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ChinaTower.Verification.Models;

namespace ChinaTower.Verification
{
    public class Startup
    {
        private Timer ExportGC;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEntityFramework()
                .AddDbContext<ChinaTowerContext>(x => x.UseNpgsql("User ID=postgres;Password=123456;Host=localhost;Port=5432;Database=ctv;"))
                .AddNpgsql();

            services.AddIdentity<User, IdentityRole>(x =>
            {
                x.Password.RequireDigit = false;
                x.Password.RequiredLength = 0;
                x.Password.RequireLowercase = false;
                x.Password.RequireNonLetterOrDigit = false;
                x.Password.RequireUppercase = false;
                x.User.AllowedUserNameCharacters = null;
            })
                .AddDefaultTokenProviders()
                .AddEntityFrameworkStores<ChinaTowerContext>();

            services.AddDataVerification()
                .AddEntityFrameworkStores<ChinaTowerContext>();

            services.AddMvc();
            services.AddSignalR();
            services.AddAesCrypto();
            services.AddSmtpEmailSender("smtp.exmail.qq.com", 25, "码锋科技", "service@codecomb.com", "service@codecomb.com", "Yuuko19931101");
            services.AddSmartUser<User, string>();
            services.AddSmartCookies();
        }

        public async void Configure(IApplicationBuilder app, ILoggerFactory logger)
        {
            logger.AddConsole();
            logger.MinimumLevel = LogLevel.Warning;

            app.UseIISPlatformHandler();
            app.UseIdentity();
            app.UseAutoAjax();
            app.UseStaticFiles();
            app.UseSignalR();
            app.UseMvcWithDefaultRoute();

            await SampleData.InitDB(app.ApplicationServices);

            ExportGC = new Timer((e) =>
            {
                var expired = Controllers.StationController.Exports.Where(x => DateTime.Now > x.Expire).ToList();
                foreach (var x in expired)
                    Controllers.StationController.Exports.Remove(x);
                expired.Clear();
                GC.Collect();
            }, null, 0, 1000 * 60 * 20);
        }

        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
