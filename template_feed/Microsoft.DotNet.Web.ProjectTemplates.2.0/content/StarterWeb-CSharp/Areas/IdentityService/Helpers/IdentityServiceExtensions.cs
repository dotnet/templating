using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.Service;
using Microsoft.AspNetCore.Identity.Service.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.Service.IntegratedWebClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Company.WebApplication1.Data;
using Company.WebApplication1.Identity;
using Company.WebApplication1.Identity.Models;
using Company.WebApplication1.Services;

namespace Microsoft.Extensions.DependencyInjection
{
    public static class IdentityServiceExtensions
    {
        public static IServiceCollection AddIdentityService(this IServiceCollection services, IConfiguration config)
        {
            // Add framework services.
            services.AddDbContext<IdentityServiceDbContext>(options =>
  #if (UseLocalDB)
                options.UseSqlServer(config.GetConnectionString("DefaultConnection")));
  #else
                options.UseSqlite(config.GetConnectionString("DefaultConnection")));
  #endif

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddEntityFrameworkStores<IdentityServiceDbContext>()
                .AddDefaultTokenProviders();

            var certificates = CertificateLoader.LoadCertificates(config.GetSection("Certificates"));

            services.AddIdentityService<ApplicationUser, IdentityServiceApplication>(
                options =>
                {
                    options.IdTokenOptions.ContextClaims.AddSingle("tfp", "policy");
                    options.IdTokenOptions.ContextClaims.AddSingle("ver", "version");
                    options.AccessTokenOptions.ContextClaims.AddSingle("tfp", "policy");
                    options.AccessTokenOptions.ContextClaims.AddSingle("ver", "version");

                    options.Issuer = "http://www.example.com/WebApplication";
                })
                .AddEntityFrameworkStores<IdentityServiceDbContext>()
                .AddSigningCertificates(certificates);

            // Add external authentication handlers below. To configure them please see https://go.microsoft.com/fwlink/?LinkID=532715

            services.AddIntegratedWebClient(config.GetSection("Authentication"));

            services.AddTransient<IEmailSender, AuthMessageSender>();
            services.AddTransient<ISmsSender, AuthMessageSender>();

            return services;
        }

        public static IServiceCollection AddIdentityServiceAuthentication(this IServiceCollection services)
        {
            services.AddAuthentication(sharedOptions =>
            {
                sharedOptions.DefaultSignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                sharedOptions.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            });

            services.AddCookieAuthentication();

            return services.AddScheme<OpenIdConnectOptions, OpenIdConnectHandler>(
                authenticationScheme: OpenIdConnectDefaults.AuthenticationScheme,
                displayName: null,
                configureOptions: _ => { });
        }
    }
}
