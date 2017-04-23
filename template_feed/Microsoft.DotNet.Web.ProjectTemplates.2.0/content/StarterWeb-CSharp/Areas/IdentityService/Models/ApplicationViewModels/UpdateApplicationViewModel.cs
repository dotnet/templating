using Microsoft.AspNetCore.Identity.Service;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Company.WebApplication1.Identity.Models.ApplicationViewModels
{
    public class UpdateApplicationViewModel
    {
        public UpdateApplicationViewModel()
        {
            RedirectUris = new List<string>();
            UpdateRegisteredUri = new List<UpdateRegisteredRedirectUriViewModel>();
            UnregisterRedirectUri = new List<UnregisterRedirectUriViewModel>();
            RegisterRedirectUri = new RegisterRedirectUriViewModel();
            AddScope = new AddScopeViewModel();
            RemoveScope = new List<RemoveScopeViewModel>();
        }

        public UpdateApplicationViewModel(
            IdentityServiceApplication app,
            IEnumerable<string> redirectUris,
            IEnumerable<string> scopes)
        {
            Id = app.Id;
            Name = app.Name;
            ClientId = app.ClientId;
            RedirectUris = redirectUris.OrderBy(r => r).ToList();
            Scopes = scopes.OrderBy(r => r).ToList();
            HasClientSecret = !string.IsNullOrWhiteSpace(app.ClientSecretHash);

            UpdateRegisteredUri = RedirectUris.Select((ru, i) => new UpdateRegisteredRedirectUriViewModel
            {
                Index = i,
                RegisteredRedirectUri = ru
            }).ToList();

            UnregisterRedirectUri = RedirectUris.Select((ru,i) => new UnregisterRedirectUriViewModel
            {
                Index = i,
                RegisteredRedirectUri = ru
            }).ToList();

            RemoveScope = Scopes.Select((s, i) => new RemoveScopeViewModel
            {
                Index = i,
                Scope = s
            }).ToList();

            RegisterRedirectUri = new RegisterRedirectUriViewModel();
        }

        public string Id { get; set; }
        public string Name { get; set; }
        public string ClientId { get; set; }
        public bool HasClientSecret { get; set; }
        public IList<string> RedirectUris { get; set; }
        public IList<string> Scopes { get; set; }
        public IList<UpdateRegisteredRedirectUriViewModel> UpdateRegisteredUri {get; set;}
        public IList<UnregisterRedirectUriViewModel> UnregisterRedirectUri {get; set;}
        public RegisterRedirectUriViewModel RegisterRedirectUri { get; set; }
        public AddScopeViewModel AddScope { get; set; }
        public IList<RemoveScopeViewModel> RemoveScope { get; set; }
    }
}
