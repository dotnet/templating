using Microsoft.AspNetCore.Identity.Service;
using Microsoft.AspNetCore.Identity.Service.IntegratedWebClient;
using Microsoft.AspNetCore.Identity.Service.Mvc;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System.Security.Claims;
using System.Threading.Tasks;
using Company.WebApplication1.Identity.Models;

namespace Company.WebApplication1.Identity.Controllers
{
    [RequireHttps]
    [Area(IdentityServiceConstants.Tenant)]
    [Route("tfp/[area]/" + IdentityServiceConstants.DefaultPolicy + "/oauth2/v" + IdentityServiceConstants.Version)]
    public class IdentityServiceController : Controller
    {
        private readonly IOptions<IdentityServiceOptions> _options;
        private readonly ITokenManager _tokenManager;
        private readonly SessionManager<ApplicationUser, IdentityServiceApplication> _sessionManager;
        private readonly IAuthorizationResponseFactory _authorizationResponseFactory;
        private readonly ITokenResponseFactory _tokenResponseFactory;

        public IdentityServiceController(
            IOptions<IdentityServiceOptions> options,
            ITokenManager tokenManager,
            SessionManager<ApplicationUser, IdentityServiceApplication> sessionManager,
            IAuthorizationResponseFactory authorizationResponseFactory,
            ITokenResponseFactory tokenResponseFactory)
        {
            _options = options;
            _tokenManager = tokenManager;
            _sessionManager = sessionManager;
            _authorizationResponseFactory = authorizationResponseFactory;
            _tokenResponseFactory = tokenResponseFactory;
        }

        [AcceptVerbs("GET", "POST", Route = "authorize")]
        public async Task<IActionResult> AuthorizeAsync(
            [EnableIntegratedWebClient, ModelBinder(typeof(AuthorizationRequestModelBinder))] AuthorizationRequest authorization)
        {
            if (!authorization.IsValid)
            {
                return this.InvalidAuthorization(authorization.Error);
            }

            var authorizationResult = await _sessionManager.IsAuthorizedAsync(authorization);
            if (authorizationResult.Status == AuthorizationStatus.Forbidden)
            {
                return this.InvalidAuthorization(authorizationResult.Error);
            }

            if (authorizationResult.Status == AuthorizationStatus.LoginRequired)
            {
                return RedirectToLogin(nameof(AccountController.Login), "Account", authorization.Message);
            }

            var context = authorization.CreateTokenGeneratingContext(
                authorizationResult.User,
                authorizationResult.Application);

            context.AmbientClaims.Add(new Claim("policy", IdentityServiceConstants.DefaultPolicy));
            context.AmbientClaims.Add(new Claim("version", "1.0"));

            await _tokenManager.IssueTokensAsync(context);
            var response = await _authorizationResponseFactory.CreateAuthorizationResponseAsync(context);

            await _sessionManager.StartSessionAsync(authorizationResult.User, authorizationResult.Application);

            return this.ValidAuthorization(response);
        }

        private IActionResult RedirectToLogin(string action, string controller, OpenIdConnectMessage message)
        {
            var copy = message.Clone();
            copy.Prompt = null;

            var parameters = new
            {
                ReturnUrl = Url.Action(nameof(AuthorizeAsync), "IdentityService", copy.Parameters)
            };

            return RedirectToAction(action, controller, parameters);
        }

        [HttpPost("token")]
        [Produces("application/json")]
        public async Task<IActionResult> TokenAsync(
            [ModelBinder(typeof(TokenRequestModelBinder))] TokenRequest request)
        {
            if (!request.IsValid)
            {
                return BadRequest(request.Error.Parameters);
            }

            var session = await _sessionManager.CreateSessionAsync(request.UserId, request.ClientId);

            var context = request.CreateTokenGeneratingContext(session.User, session.Application);
            context.AmbientClaims.Add(new Claim("policy", IdentityServiceConstants.DefaultPolicy));
            context.AmbientClaims.Add(new Claim("version", "1.0"));

            await _tokenManager.IssueTokensAsync(context);
            var response = await _tokenResponseFactory.CreateTokenResponseAsync(context);
            return Ok(response.Parameters);
        }

        [HttpGet("logout")]
        public async Task<IActionResult> LogOutAsync(
            [EnableIntegratedWebClient, ModelBinder(typeof(LogoutRequestModelBinder))] LogoutRequest request)
        {
            if (!request.IsValid)
            {
                return View("InvalidLogoutRedirect");
            }

            var endSessionResult = await _sessionManager.EndSessionAsync(request);
            if (endSessionResult.Status == LogoutStatus.RedirectToLogoutUri)
            {
                return Redirect(endSessionResult.LogoutRedirect);
            }
            else
            {
                return View("LoggedOut", request);
            }
        }
    }
}
