﻿using System.Collections.Generic;

namespace Company.WebApplication1.Identity.Models.ApplicationViewModels
{
    public class CreateLogoutUriViewModel
    {
        public CreateLogoutUriViewModel()
        {
        }

        public CreateLogoutUriViewModel(string id, string applicationName, IEnumerable<string> logoutUris)
        {
            Id = id;
            Name = applicationName;
            LogoutUris = logoutUris;
        }

        public string Id { get; set; }
        public string Name { get; }
        public IEnumerable<string> LogoutUris { get; }
        public string NewLogoutUri { get; set; }
    }
}
