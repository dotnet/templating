using Microsoft.AspNetCore.Identity.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Company.WebApplication1.Identity.Models.ApplicationViewModels
{
    public class CredentialsViewModel
    {
        public CredentialsViewModel(IdentityServiceApplication application, string clientSecret)
        {
            Id = application.Id;
            ClientId = application.ClientId;
            ClientSecret = clientSecret;
        }

        public string Id { get; set; }
        public string ClientId { get; set; }
        [BindingBehavior(BindingBehavior.Never)]
        public string ClientSecret { get; set; }
    }
}
