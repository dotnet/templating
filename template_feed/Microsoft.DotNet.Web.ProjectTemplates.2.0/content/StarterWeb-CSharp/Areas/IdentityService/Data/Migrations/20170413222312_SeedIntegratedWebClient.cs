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
    [DbContext(typeof(IdentityServiceDbContext))]
    [Migration("20170413222312_SeedIntegratedWebClient")]
    public class SeedIntegratedWebClient : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            SeedIntegratedApplication.SeedApplication(migrationBuilder);
        }
    }
}
