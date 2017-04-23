using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Company.WebApplication1.Identity.Models.ApplicationViewModels
{
    public class UpdateRegisteredRedirectUriViewModel
    {
        public string RegisteredRedirectUri { get; set; }
        public string UpdatedRedirectUri { get; set; }
        public int Index { get; set; }
    }
}
