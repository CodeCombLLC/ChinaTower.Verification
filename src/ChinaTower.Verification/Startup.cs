using System.Threading;
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
        public static readonly string ConnectionString = "User ID=postgres;Password=123456;Host=localhost;Port=5432;Database=ctv;";
        private Timer ExportGC;

        public void ConfigureServices(IServiceCollection services)
        {
            services.AddEntityFramework()
                .AddDbContext<ChinaTowerContext>(x => x.UseNpgsql(ConnectionString))
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
        }

        public static void Main(string[] args) => WebApplication.Run<Startup>(args);
    }
}
