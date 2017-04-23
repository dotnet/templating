
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using System.Threading.Tasks;
using Company.WebApplication1.Identity.Models;

namespace Company.WebApplication1.Identity.Controllers
{
    [Area(IdentityServiceConstants.Tenant)]
    [Route(IdentityServiceConstants.RoutePrefix + "/[Area]/" + IdentityServiceConstants.DefaultPolicy)]
    public class IdentityServiceConfigurationController : Controller
    {
        private static readonly string IdentityService = nameof(IdentityServiceController)
            .Replace("Controller", "");

        private static readonly string IdentityServiceConfiguration = nameof(IdentityServiceConfigurationController)
            .Replace("Controller", "");

        private readonly ITokenManager _tokenManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationManager<IdentityServiceApplication> _applicationManager;
        private readonly IUserClaimsPrincipalFactory<ApplicationUser> _userClaimsFactory;
        private readonly IApplicationClaimsPrincipalFactory<IdentityServiceApplication> _applicationClaimsFactory;
        private readonly IConfigurationManager _configurationProvider;
        private readonly IKeySetMetadataProvider _keySetProvider;
        private readonly IAuthorizationResponseFactory _authorizationResponseFactory;
        private readonly ITokenResponseFactory _tokenResponseFactory;

        public IdentityServiceConfigurationController(
            IConfigurationManager configurationProvider,
            IOptions<IdentityServiceOptions> options,
            IKeySetMetadataProvider keySetProvider,
            ITokenManager tokenManager,
            UserManager<ApplicationUser> userManager,
            IUserClaimsPrincipalFactory<ApplicationUser> userClaimsFactory,
            ApplicationManager<IdentityServiceApplication> applicationManager,
            IApplicationClaimsPrincipalFactory<IdentityServiceApplication> applicationClaimsFactory,
            ITokenResponseFactory tokenResponseFactory,
            IAuthorizationResponseFactory authorizationResponseFactory)
        {
            _configurationProvider = configurationProvider;
            _keySetProvider = keySetProvider;
            _tokenManager = tokenManager;
            _userManager = userManager;
            _userClaimsFactory = userClaimsFactory;
            _applicationManager = applicationManager;
            _applicationClaimsFactory = applicationClaimsFactory;
            _authorizationResponseFactory = authorizationResponseFactory;
            _tokenResponseFactory = tokenResponseFactory;
        }

        [HttpGet("v" + IdentityServiceConstants.Version + "/.well-known/openid-configuration")]
        [Produces("application/json")]
        public async Task<IActionResult> MetadataAsync()
        {
            const string Token = nameof(IdentityServiceController.TokenAsync);
            const string Authorize = nameof(IdentityServiceController.AuthorizeAsync);
            const string Logout = nameof(IdentityServiceController.LogOutAsync);

            var configurationContext = new ConfigurationContext();
            configurationContext.Id = $"{IdentityServiceConstants.Tenant}:{IdentityServiceConstants.DefaultPolicy}";
            configurationContext.HttpContext = HttpContext;
            configurationContext.AuthorizationEndpoint = Link(Authorize, IdentityService);
            configurationContext.TokenEndpoint = Link(Token, IdentityService);
            configurationContext.JwksUriEndpoint = Link(nameof(KeysAsync), IdentityServiceConfiguration);
            configurationContext.EndSessionEndpoint = Link(Logout, IdentityService);

            return Ok(await _configurationProvider.GetConfigurationAsync(configurationContext));
            string Link(string action, string controller) =>
                Url.Action(action, controller, null, Request.Scheme, Request.Host.Value);
        }

        [HttpGet("discovery/v" + IdentityServiceConstants.Version + "/keys")]
        [Produces("application/json")]
        public async Task<IActionResult> KeysAsync()
        {
            return Ok(await _keySetProvider.GetKeysAsync());
        }
    }
}
