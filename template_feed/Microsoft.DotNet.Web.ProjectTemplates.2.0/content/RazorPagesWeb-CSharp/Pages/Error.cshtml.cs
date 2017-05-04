using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Company.WebApplication1.Pages
{
    public class ErrorModel : PageModel
    {
        public string CorrelationId { get; set; }

        public bool ShowCorrelationId => !string.IsNullOrEmpty(CorrelationId);

        public void OnGet()
        {
            CorrelationId = Activity.Current?.Id;
        }
    }
}
