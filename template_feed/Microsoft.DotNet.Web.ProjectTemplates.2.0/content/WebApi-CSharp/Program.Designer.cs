using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace Company.WebApplication1
{
    public class DesignTimeServiceProviderFactory : IDesignTimeServiceProviderFactory
    {
        public IServiceProvider CreateServiceProvider(string[] args) =>
            Program.BuildWebHost(args).Services;
    }
}
