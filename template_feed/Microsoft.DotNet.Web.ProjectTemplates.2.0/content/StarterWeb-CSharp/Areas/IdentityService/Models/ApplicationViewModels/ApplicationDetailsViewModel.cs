using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Company.WebApplication1.Identity.Models.ApplicationViewModels
{
    public class ApplicationDetailsViewModel
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string ClientId { get; set; }
        public IEnumerable<string> RedirectUris { get; set; }
    }
}
