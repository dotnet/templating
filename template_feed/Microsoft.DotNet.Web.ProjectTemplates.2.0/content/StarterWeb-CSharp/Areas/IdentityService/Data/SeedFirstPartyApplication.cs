using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Company.WebApplication1.Data;
using JetBrains.Annotations;

namespace Company.WebApplication1.IdentityService.Data.Migrations
{

    public static class SeedIntegratedApplication
    { 
        public static void SeedApplication(MigrationBuilder migrationBuilder)
        {
            var applicationColumns = new string[] { "Id", "ClientId", "Name" };
            var appId = Guid.NewGuid();
            var clientId = "11111111-1111-1111-11111111111111111";

            var applicationValues = new object[1, 3]
            {
                {appId, clientId, "IntegratedWebApplication" }
            };

            migrationBuilder.Insert("AspNetApplications", null, applicationColumns, applicationValues);

            var scopesColumns = new string[] { "Id", "ApplicationId", "Value" };
            var applicationScopes = new object[1, 3]
            {
                {0, appId, "openid" },
            };

            migrationBuilder.Insert("AspNetScopes", null, scopesColumns, applicationScopes);

            var redirectUriColumns = new string[] { "Id", "ApplicationId", "IsLogout", "Value" };
            var redirectUriValues = new object[2, 4]
            {
                {Guid.NewGuid(), appId, true, "urn:self:aspnet:identity:integrated" },
                {Guid.NewGuid(), appId, false, "urn:self:aspnet:identity:integrated" }
            };

            migrationBuilder.Insert("AspNetRedirectUris", null, redirectUriColumns, redirectUriValues);
        }
    }
}
