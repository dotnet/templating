
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
#if (IndividualLocalAuth)
using Company.WebApplication1.Identity.Data;
using Company.WebApplication1.Identity.Models;
using Company.WebApplication1.Identity.Services;
#endif
#if (OrganizationalAuth || IndividualB2CAuth)
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
#endif
using Microsoft.AspNetCore.Builder;
#if (IndividualLocalAuth)
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Service.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity.Service.Extensions;
using Microsoft.AspNetCore.Identity.Service.IntegratedWebClient;
using Microsoft.AspNetCore.Diagnostics.EntityFrameworkCore;
using Microsoft.AspNetCore.Diagnostics.Identity.Service;
#endif
#if (OrganizationalAuth || IndividualAuth)
using Microsoft.AspNetCore.Http;
#endif
using Microsoft.AspNetCore.Hosting;
#if (OrganizationalAuth || IndividualAuth)
using Microsoft.AspNetCore.Rewrite;
#endif
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
#if (OrganizationalAuth || IndividualAuth)
using Microsoft.Extensions.Options;
#endif
#if (OrganizationalAuth && OrgReadAccess)
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
#endif
#if (MultiOrgAuth)
using Microsoft.IdentityModel.Tokens;
#endif

namespace Company.WebApplication1
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
#if (IndividualLocalAuth)
            services.AddDbContext<IdentityServiceDbContext>(options =>
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection")));

            services.AddIdentity<ApplicationUser, IdentityRole>()
                .AddDefaultTokenProviders()
                .AddApplications()
                .AddEntityFrameworkStores<IdentityServiceDbContext>()
                .AddClientExtensions();

#elseif (IndividualB2CAuth)
            services.AddAzureAdB2CAuthentication();
#elseif (OrganizationalAuth)
            services.AddAzureAdAuthentication();
#endif
#if (IndividualAuth)
            services.AddOpenIdConnectAuthentication()
                .WithIntegratedWebClient();
            
            services.AddCookieAuthentication();
#elseif (OrganizationalAuth)
            services.AddOpenIdConnectAuthentication();            
            services.AddCookieAuthentication();
#endif
            services.AddMvc();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IHostingEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
#if (IndividualLocalAuth)
                app.UseDatabaseErrorPage();
                app.UseDevelopmentCertificateErrorPage();
#endif
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

#if (OrganizationalAuth || IndividualAuth)
            app.UseRewriter(new RewriteOptions().AddIISUrlRewrite(env.ContentRootFileProvider, "urlRewrite.config"));
#endif
            
            app.UseStaticFiles();

#if (OrganizationalAuth || IndividualAuth)
            app.UseAuthentication();
#endif
            app.UseMvc(routes =>
            {
                routes.MapRoute(
                    name: "default",
                    template: "{controller=Home}/{action=Index}/{id?}");
            });
        }
    }
}
