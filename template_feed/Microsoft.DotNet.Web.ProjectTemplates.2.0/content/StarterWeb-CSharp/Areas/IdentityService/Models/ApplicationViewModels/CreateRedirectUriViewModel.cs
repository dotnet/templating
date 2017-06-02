﻿using System.Collections.Generic;

namespace Company.WebApplication1.Identity.Models.ApplicationViewModels
{
    public class CreateRedirectUriViewModel
    {
        public CreateRedirectUriViewModel()
        {
        }

        public CreateRedirectUriViewModel(string applicationName, IEnumerable<string> redirectUris)
        {
            Name = applicationName;
            RedirectUris = redirectUris;
        }

        public string Name { get; }
        public IEnumerable<string> RedirectUris { get; }
        public string NewRedirectUri { get; set; }
    }
}
