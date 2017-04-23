using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.Service;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Linq;
using System.Threading.Tasks;
using Company.WebApplication1.Identity.Models;
using Company.WebApplication1.Identity.Models.ApplicationViewModels;

namespace Company.WebApplication1.Identity.Controllers
{
    [Authorize(IdentityServiceOptions.LoginPolicyName)]
    [Area(IdentityServiceConstants.Tenant)]
    [Route(IdentityServiceConstants.RoutePrefix + "/[area]/[controller]")]
    [RequireHttps]
    public class ApplicationsController : Controller
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationManager<IdentityServiceApplication> _applicationManager;

        public ApplicationsController(
            UserManager<ApplicationUser> userManager,
            ApplicationManager<IdentityServiceApplication> applicationManager)
        {
            _userManager = userManager;
            _applicationManager = applicationManager;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var id = _userManager.GetUserId(User);
            var applications = await _applicationManager.FindByUserIdAsync(id);
            return View(applications);
        }

        [HttpGet("New")]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost("New")]
        public async Task<IActionResult> Create(CreateApplicationViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            var application = new IdentityServiceApplication
            {
                Name = model.Name,
                ClientId = Guid.NewGuid().ToString(),
                UserId = user.Id
            };

            await _applicationManager.CreateAsync(application);
            await _applicationManager.AddScopeAsync(application, OpenIdConnectScope.OpenId);
            await _applicationManager.AddScopeAsync(application, "offline_access");

            return RedirectToAction("Edit", new { id = application.Id });
        }

        [HttpPost("{id}/[action]")]
        public async Task<IActionResult> AddScope(
            [FromRoute]string id,
            [FromForm] UpdateApplicationViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);

            var application = await _applicationManager.FindByIdAsync(id);

            var result = await _applicationManager.AddScopeAsync(application, model.AddScope.NewScope);
            if (!result.Succeeded)
            {
                MapErrorsToModelState("AddScope.NewScope", result);
                var redirectUris = await _applicationManager.FindRegisteredUrisAsync(application);
                var scopes = await _applicationManager.FindScopesAsync(application);
                return View("Edit", new UpdateApplicationViewModel(application, redirectUris, scopes)
                {
                    AddScope = model.AddScope
                });
            }

            return RedirectToAction("Edit", new { id = application.Id });
        }

        [HttpPost("{id}/[action]")]
        public async Task<IActionResult> RemoveScope(
            [FromRoute]string id,
            [FromForm] UpdateApplicationViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var userId = _userManager.GetUserId(User);

            IdentityServiceResult result = await _applicationManager.CheckUserPermissionsAsync(application, userId);
            if (!result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.AccessDenied), "Account");
            }

            var element = model.RemoveScope.Single();

            var updateResult = await _applicationManager.RemoveScopeAsync(
                application,
                element.Scope);

            if (!updateResult.Succeeded)
            {
                var scopes = (await _applicationManager.FindScopesAsync(application))
                    .OrderBy(s => s)
                    .ToArray();

                var redirectUris = await _applicationManager.FindRegisteredUrisAsync(application);

                if (scopes.Length > element.Index ||
                    element.Scope.Equals(
                        scopes[element.Index],
                        StringComparison.OrdinalIgnoreCase))
                {
                    MapErrorsToModelState($"RegisterRedirectUri.UnregisterRedirectUri[{element.Index}]", updateResult);
                    return View("Edit", new UpdateApplicationViewModel(application, redirectUris, scopes));
                }
                else
                {
                    MapErrorsToModelState($"RegisterRedirectUri.UnregisterRedirectUri", updateResult);
                    return View("Edit", new UpdateApplicationViewModel(application, redirectUris, scopes));
                }
            }

            return RedirectToAction("Edit", new { id = application.Id });
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details([FromRoute]string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var userId = _userManager.GetUserId(User);

            IdentityServiceResult result = await _applicationManager.CheckUserPermissionsAsync(application, userId);
            if (!result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.AccessDenied), "Account");
            }

            var redirectUris = await _applicationManager.FindRegisteredUrisAsync(application);

            return View(new ApplicationDetailsViewModel
            {
                Id = application.Id,
                Name = application.Name,
                ClientId = application.ClientId,
                RedirectUris = redirectUris
            });
        }

        [HttpGet("{id}/[action]")]
        public async Task<IActionResult> Edit([FromRoute]string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var userId = _userManager.GetUserId(User);

            IdentityServiceResult result = await _applicationManager.CheckUserPermissionsAsync(application, userId);
            if (!result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.AccessDenied), "Account");
            }

            var redirectUris = await _applicationManager.FindRegisteredUrisAsync(application);
            var scopes = await _applicationManager.FindScopesAsync(application);

            return View(new UpdateApplicationViewModel(application, redirectUris, scopes));
        }

        [HttpPost("{id}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeName([FromRoute]string id, UpdateApplicationViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(model.Id);
            var userId = _userManager.GetUserId(User);

            IdentityServiceResult result = await _applicationManager.CheckUserPermissionsAsync(application, userId);
            if (!result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.AccessDenied), "Account");
            }

            application.Name = model.Name;

            var updateResult = await _applicationManager.UpdateAsync(application);
            if (!updateResult.Succeeded)
            {
                MapErrorsToModelState("Name", updateResult);
                var redirectUris = await _applicationManager.FindRegisteredUrisAsync(application);
                var scopes = await _applicationManager.FindScopesAsync(application);
                return View("Edit", new UpdateApplicationViewModel(application, redirectUris, scopes));
            }

            return RedirectToAction("Details", new { id = application.Id });
        }

        [HttpPost("{id}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> GenerateClientSecret([FromRoute]string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var userId = _userManager.GetUserId(User);

            IdentityServiceResult result = await _applicationManager.CheckUserPermissionsAsync(application, userId);
            if (!result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.AccessDenied), "Account");
            }

            var clientSecret = await _applicationManager.GenerateClientSecretAsync();
            var addSecretResult = await _applicationManager.AddClientSecretAsync(application, clientSecret);

            if (!addSecretResult.Succeeded)
            {
                MapErrorsToModelState("", addSecretResult);
                var redirectUris = await _applicationManager.FindRegisteredUrisAsync(application);
                var scopes = await _applicationManager.FindScopesAsync(application);
                return View("Edit", new UpdateApplicationViewModel(application, redirectUris, scopes));
            }

            return View("ClientSecret", new CredentialsViewModel(application, clientSecret));
        }

        [HttpPost("{id}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegenerateClientSecret([FromRoute]string id)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var userId = _userManager.GetUserId(User);

            IdentityServiceResult result = await _applicationManager.CheckUserPermissionsAsync(application, userId);
            if (!result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.AccessDenied), "Account");
            }

            var clientSecret = await _applicationManager.GenerateClientSecretAsync();
            var addSecretResult = await _applicationManager.ChangeClientSecretAsync(application, clientSecret);

            if (!addSecretResult.Succeeded)
            {
                MapErrorsToModelState("", addSecretResult);
                var redirectUris = await _applicationManager.FindRegisteredUrisAsync(application);
                var scopes = await _applicationManager.FindScopesAsync(application);
                return View("Edit", new UpdateApplicationViewModel(application, redirectUris, scopes));
            }

            return View("ClientSecret", new CredentialsViewModel(application, clientSecret));
        }

        private void MapErrorsToModelState(string key, IdentityServiceResult updateResult)
        {
            foreach (var error in updateResult.Errors)
            {
                ModelState.AddModelError(key, error.Description);
            }
        }

        [HttpPost("{id}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RegisterRedirectUri(
            [FromRoute]string id,
            [FromForm] UpdateApplicationViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var userId = _userManager.GetUserId(User);

            IdentityServiceResult result = await _applicationManager.CheckUserPermissionsAsync(application, userId);
            if (!result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.AccessDenied), "Account");
            }

            var registrationResult = await _applicationManager.RegisterRedirectUriAsync(
                application,
                model.RegisterRedirectUri.NewRedirectUri);

            if (!registrationResult.Succeeded)
            {
                MapErrorsToModelState("RegisterRedirectUri.NewRedirectUri", registrationResult);
                var redirectUris = await _applicationManager.FindRegisteredUrisAsync(application);
                var scopes = await _applicationManager.FindScopesAsync(application);
                return View("Edit", new UpdateApplicationViewModel(application, redirectUris, scopes)
                {
                    RegisterRedirectUri = model.RegisterRedirectUri
                });
            }

            return RedirectToAction("Edit", new { id = application.Id });
        }

        [HttpPost("{id}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateRegisteredUri(
            [FromRoute]string id,
            UpdateApplicationViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var userId = _userManager.GetUserId(User);

            IdentityServiceResult result = await _applicationManager.CheckUserPermissionsAsync(application, userId);
            if (!result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.AccessDenied), "Account");
            }

            var element = model.UpdateRegisteredUri.Single();

            var updateResult = await _applicationManager.UpdateRedirectUriAsync(
                application,
                element.RegisteredRedirectUri,
                element.UpdatedRedirectUri);

            if (!updateResult.Succeeded)
            {
                var scopes = await _applicationManager.FindScopesAsync(application);

                var redirectUris = (await _applicationManager
                    .FindRegisteredUrisAsync(application))
                    .OrderBy(uri => uri)
                    .ToArray();

                if (redirectUris.Length > element.Index ||
                    element.RegisteredRedirectUri.Equals(
                        redirectUris[element.Index],
                        StringComparison.OrdinalIgnoreCase))
                {
                    MapErrorsToModelState($"RegisterRedirectUri.UpdateRegisteredUri[{element.Index}]", updateResult);
                    return View("Edit", new UpdateApplicationViewModel(application, redirectUris, scopes));
                }
                else
                {
                    MapErrorsToModelState($"RegisterRedirectUri.UpdateRegisteredUri", updateResult);
                    return View("Edit", new UpdateApplicationViewModel(application, redirectUris, scopes));
                }
            }

            return RedirectToAction("Edit", new { id = application.Id });
        }

        [HttpPost("{id}/[action]")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UnregisterRedirectUri(
            [FromRoute] string id,
            UpdateApplicationViewModel model)
        {
            var application = await _applicationManager.FindByIdAsync(id);
            var userId = _userManager.GetUserId(User);

            IdentityServiceResult result = await _applicationManager.CheckUserPermissionsAsync(application, userId);
            if (!result.Succeeded)
            {
                return RedirectToAction(nameof(AccountController.AccessDenied), "Account");
            }

            var element = model.UnregisterRedirectUri.Single();

            var updateResult = await _applicationManager.UnregisterRedirectUriAsync(
                application,
                element.RegisteredRedirectUri);

            if (!updateResult.Succeeded)
            {
                var scopes = await _applicationManager.FindScopesAsync(application);

                var redirectUris = (await _applicationManager
                    .FindRegisteredUrisAsync(application))
                    .OrderBy(uri => uri)
                    .ToArray();

                if (redirectUris.Length > element.Index ||
                    element.RegisteredRedirectUri.Equals(
                        redirectUris[element.Index],
                        StringComparison.OrdinalIgnoreCase))
                {
                    MapErrorsToModelState($"RegisterRedirectUri.UnregisterRedirectUri[{element.Index}]", updateResult);
                    return View("Edit", new UpdateApplicationViewModel(application, redirectUris, scopes));
                }
                else
                {
                    MapErrorsToModelState($"RegisterRedirectUri.UnregisterRedirectUri", updateResult);
                    return View("Edit", new UpdateApplicationViewModel(application, redirectUris, scopes));
                }
            }

            return RedirectToAction("Edit", new { id = application.Id });
        }
    }
}
