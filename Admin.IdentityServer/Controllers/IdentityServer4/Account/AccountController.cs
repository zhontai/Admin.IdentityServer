// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using Admin.IdentityServer.Configs;
using Admin.IdentityServer.Domain.Admin;
using Admin.IdentityServer.Utils;
using FreeSql;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;

namespace Admin.IdentityServer
{
    /// <summary>
    /// This sample controller implements a typical login/logout/provision workflow for local and external accounts.
    /// The login service encapsulates the interactions with the user data store. This data store is in-memory only and cannot be used for production!
    /// The interaction service provides a way for the UI to communicate with identityserver for validation and context retrieval
    /// </summary>
    [SecurityHeaders]
    [AllowAnonymous]
    public class AccountController : Controller
    {
        private readonly IBaseRepository<UserEntity> _userRepository;
        private readonly IBaseRepository<LoginLogEntity> _loginLogRepository;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IClientStore _clientStore;
        private readonly IAuthenticationSchemeProvider _schemeProvider;
        private readonly IEventService _events;
        //private readonly IHttpClientFactory _httpClientFactory;
        private readonly AppSettings _appSettings;
        private readonly IPHelper _iPHelper;

        public AccountController(
            IBaseRepository<UserEntity> userRepository,
            IBaseRepository<LoginLogEntity> loginLogRepository,
            IIdentityServerInteractionService interaction,
            IClientStore clientStore,
            IAuthenticationSchemeProvider schemeProvider,
            IEventService events,
            //IHttpClientFactory httpClientFactory,
            AppSettings appSettings,
            IPHelper iPHelper)
        {
            // if the TestUserStore is not in DI, then we'll just use the global users collection
            // this is where you would plug in your own custom identity management library (e.g. ASP.NET Identity)
            _userRepository = userRepository;
            _loginLogRepository = loginLogRepository;

            _interaction = interaction;
            _clientStore = clientStore;
            _schemeProvider = schemeProvider;
            _events = events;
            //_httpClientFactory = httpClientFactory;
            _appSettings = appSettings;
            _iPHelper = iPHelper;
        }

        /// <summary>
        /// Entry point into the login workflow
        /// </summary>
        [HttpGet]
        [Route("user/login")]
        public async Task<IActionResult> Login(string returnUrl)
        {
            // build a model so we know what to show on the login page
            var vm = await BuildLoginViewModelAsync(returnUrl);

            if (vm.IsExternalLoginOnly)
            {
                // we only have one option for logging in and it's an external provider
                return RedirectToAction("Challenge", "External", new { provider = vm.ExternalLoginScheme, returnUrl });
            }

            return View(vm);

        }

        
        /// <summary>
        /// Handle postback from username/password login
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("user/login")]
        public async Task<IActionResult> Login(LoginInputModel input)
        {
            var sw = new Stopwatch();
            sw.Start();

            // check if we are in the context of an authorization request
            var context = await _interaction.GetAuthorizationContextAsync(input.ReturnUrl);
            
            if (ModelState.IsValid)
            {
                var user = await _userRepository.Select.Where(a => a.UserName == input.UserName).ToOneAsync(a => new { a.Id, a.Password, a.NickName });

                var isValid = false;
                if (user != null)
                {
                    var password = MD5Encrypt.Encrypt32(input.Password);
                    if (user.Password == password)
                    {
                        isValid = true;
                    }
                }

                if (isValid)
                {
                    await _events.RaiseAsync(new UserLoginSuccessEvent(input.UserName, user.Id.ToString(), input.UserName, clientId: context?.ClientId));

                    // only set explicit expiration here if user chooses "remember me". 
                    // otherwise we rely upon expiration configured in cookie middleware.
                    AuthenticationProperties props = null;
                    if (AccountOptions.AllowRememberLogin && input.RememberLogin)
                    {
                        props = new AuthenticationProperties
                        {
                            IsPersistent = true,
                            ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                        };
                    };

                    // issue authentication cookie with subject ID and username
                    var isuser = new IdentityServerUser(user.Id.ToString())
                    {
                        DisplayName = input.UserName
                    };

                    await HttpContext.SignInAsync(isuser, props);

                    sw.Stop();
                    var loginLogEntity = new LoginLogEntity()
                    {
                        CreatedUserId = user.Id,
                        NickName = user.NickName,
                        CreatedUserName = input.UserName,
                        ElapsedMilliseconds = sw.ElapsedMilliseconds,
                        Status = true
                    };
                    await AddLoginLog(loginLogEntity);

                    if (context != null)
                    {
                        if (await _clientStore.IsPkceClientAsync(context.ClientId))
                        {
                            // if the client is PKCE then we assume it's native, so this change in how to
                            // return the response is for better UX for the end user.
                            return this.LoadingPage("Redirect", input.ReturnUrl);
                        }

                        // we can trust model.ReturnUrl since GetAuthorizationContextAsync returned non-null
                        return Redirect(input.ReturnUrl);
                    }

                    // request for a local page
                    if (Url.IsLocalUrl(input.ReturnUrl))
                    {
                        return Redirect(input.ReturnUrl);
                    }
                    else if (string.IsNullOrEmpty(input.ReturnUrl))
                    {
                        return Redirect("~/");
                    }
                    else
                    {
                        // user might have clicked on a malicious link - should be logged
                        throw new Exception("invalid return URL");
                    }
                }

                await _events.RaiseAsync(new UserLoginFailureEvent(input.UserName, "invalid credentials", clientId: context?.ClientId));
                ModelState.AddModelError(string.Empty, AccountOptions.InvalidCredentialsErrorMessage);
            }

            // something went wrong, show form with error
            var vm = await BuildLoginViewModelAsync(input);
            return View(vm);
        }

        /// <summary>
        /// Show logout page
        /// </summary>
        [HttpGet]
        [Route("user/logout")]
        public async Task<IActionResult> Logout(string logoutId)
        {
            // build a model so the logout page knows what to display
            var vm = await BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                // if the request for logout was properly authenticated from IdentityServer, then
                // we don't need to show the prompt and can just log the user out directly.
                return await Logout(vm);
            }

            return View(vm);
        }

        /// <summary>
        /// Handle logout page postback
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("user/logout")]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            // build a model so the logged out page knows what to display
            var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

            if (User?.Identity.IsAuthenticated == true)
            {
                // delete local authentication cookie
                await HttpContext.SignOutAsync();

                // raise the logout event
                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }

            // check if we need to trigger sign-out at an upstream identity provider
            if (vm.TriggerExternalSignout)
            {
                // build a return URL so the upstream provider will redirect back
                // to us after the user has logged out. this allows us to then
                // complete our single sign-out processing.
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

                // this triggers a redirect to the external provider for sign-out
                return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
            }

            if(vm.AutomaticRedirectAfterSignOut)
            {
                return Redirect(vm.PostLogoutRedirectUri);
            }

            return View("LoggedOut", vm);
        }

        /*
        [HttpPost]
        [Route("user/login")]
        public async Task<IResponseOutput> Login([FromBody]LoginInputModel input)
        {
            HttpClient client = _httpClientFactory.CreateClient();

            DiscoveryDocumentResponse disco = await client.GetDiscoveryDocumentAsync(new DiscoveryDocumentRequest
            {
                Address = _appSettings.Urls,
                Policy = {
                        RequireHttps = false
                    }
            });

            if (disco.IsError)
            {
                return ResponseOutput.NotOk(disco.Error);
            }

            TokenResponse response = await client.RequestTokenAsync(new PasswordTokenRequest()
            {
                Address = disco.TokenEndpoint,
                GrantType = GrantType.ResourceOwnerPassword,
                ClientId = "admin.ui",
                ClientSecret = "secret",
                Parameters = { 
                    { "UserName", input.UserName },
                    { "Password", input.Password }
                },
                Scope = "openid profile admin.server.api",
            });

            if (response.IsError)
            {
                return ResponseOutput.NotOk(response.ErrorDescription);
            }
            return ResponseOutput.Ok(response.Json);
        }
        */

        /*
        [HttpPost]
        [Route("user/login")]
        public async Task<IResponseOutput> Login([FromBody] LoginInputModel model)
        {
            model.ReturnUrl = model.ReturnUrl.Replace("https://localhost:5000", "");
            // check if we are in the context of an authorization request
            var context = await _interaction.GetAuthorizationContextAsync(model.ReturnUrl);

            if (!ModelState.IsValid)
            {
                return ResponseOutput.NotOk(ModelState.Values.First().Errors[0].ErrorMessage);
            }

            var user = await _userRepository.Select.Where(a => a.UserName == model.UserName).ToOneAsync(a => new { a.Id, a.Password });

            if (!(user?.Id > 0))
            {
                return ResponseOutput.NotOk("’À∫≈ ‰»Î”–ŒÛ!", 3);
            }

            var password = MD5Encrypt.Encrypt32(model.Password);
            if (user.Password != password)
            {
                return ResponseOutput.NotOk("√‹¬Î ‰»Î”–ŒÛ£°", 4);
            }

            await _events.RaiseAsync(new UserLoginSuccessEvent(model.UserName, user.Id.ToString(), model.UserName, clientId: context?.ClientId));

            // only set explicit expiration here if user chooses "remember me". 
            // otherwise we rely upon expiration configured in cookie middleware.
            AuthenticationProperties props = null;
            if (AccountOptions.AllowRememberLogin && model.RememberLogin)
            {
                props = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(AccountOptions.RememberMeLoginDuration)
                };
            };

            // issue authentication cookie with subject ID and username
            var isuser = new IdentityServerUser(user.Id.ToString())
            {
                DisplayName = model.UserName
            };

            await HttpContext.SignInAsync(isuser, props);

            if (context != null)
            {
                var b = await _clientStore.IsPkceClientAsync(context.ClientId);
            }
            return ResponseOutput.Ok();
            //await _events.RaiseAsync(new UserLoginFailureEvent(model.UserName, "invalid credentials", clientId: context?.ClientId));
            //AccountOptions.InvalidCredentialsErrorMessage
        }
        */

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }


        /*****************************************/
        /* helper APIs for the AccountController */
        /*****************************************/
        private async Task<LoginViewModel> BuildLoginViewModelAsync(string returnUrl)
        {
            var context = await _interaction.GetAuthorizationContextAsync(returnUrl);
            if (context?.IdP != null && await _schemeProvider.GetSchemeAsync(context.IdP) != null)
            {
                var local = context.IdP == IdentityServer4.IdentityServerConstants.LocalIdentityProvider;

                // this is meant to short circuit the UI and only trigger the one external IdP
                var vm = new LoginViewModel
                {
                    EnableLocalLogin = local,
                    ReturnUrl = returnUrl,
                    UserName = context?.LoginHint,
                };

                if (!local)
                {
                    vm.ExternalProviders = new[] { new ExternalProvider { AuthenticationScheme = context.IdP } };
                }

                return vm;
            }

            var schemes = await _schemeProvider.GetAllSchemesAsync();

            var providers = schemes
                .Where(x => x.DisplayName != null ||
                            (x.Name.Equals(AccountOptions.WindowsAuthenticationSchemeName, StringComparison.OrdinalIgnoreCase))
                )
                .Select(x => new ExternalProvider
                {
                    DisplayName = x.DisplayName ?? x.Name,
                    AuthenticationScheme = x.Name
                }).ToList();

            var allowLocal = true;
            if (context?.ClientId != null)
            {
                var client = await _clientStore.FindEnabledClientByIdAsync(context.ClientId);
                if (client != null)
                {
                    allowLocal = client.EnableLocalLogin;

                    if (client.IdentityProviderRestrictions != null && client.IdentityProviderRestrictions.Any())
                    {
                        providers = providers.Where(provider => client.IdentityProviderRestrictions.Contains(provider.AuthenticationScheme)).ToList();
                    }
                }
            }

            return new LoginViewModel
            {
                AllowRememberLogin = AccountOptions.AllowRememberLogin,
                EnableLocalLogin = allowLocal && AccountOptions.AllowLocalLogin,
                ReturnUrl = returnUrl,
                UserName = context?.LoginHint,
                ExternalProviders = providers.ToArray()
            };
        }

        private async Task<LoginViewModel> BuildLoginViewModelAsync(LoginInputModel model)
        {
            var vm = await BuildLoginViewModelAsync(model.ReturnUrl);
            vm.UserName = model.UserName;
            vm.RememberLogin = model.RememberLogin;
            return vm;
        }

        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

            if (User?.Identity.IsAuthenticated != true)
            {
                // if the user is not authenticated, then just show logged out page
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                // it's safe to automatically sign-out
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            // show the logout prompt. this prevents attacks where the user
            // is automatically signed out by another malicious web page.
            return vm;
        }

        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
            // get context information (client name, post logout redirect URI and iframe for federated signout)
            var logout = await _interaction.GetLogoutContextAsync(logoutId);

            var vm = new LoggedOutViewModel
            {
                AutomaticRedirectAfterSignOut = AccountOptions.AutomaticRedirectAfterSignOut,
                PostLogoutRedirectUri = logout?.PostLogoutRedirectUri,
                ClientName = string.IsNullOrEmpty(logout?.ClientName) ? logout?.ClientId : logout?.ClientName,
                SignOutIframeUrl = logout?.SignOutIFrameUrl,
                LogoutId = logoutId
            };

            if (User?.Identity.IsAuthenticated == true)
            {
                var idp = User.FindFirst(JwtClaimTypes.IdentityProvider)?.Value;
                if (idp != null && idp != IdentityServer4.IdentityServerConstants.LocalIdentityProvider)
                {
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {
                        if (vm.LogoutId == null)
                        {
                            // if there's no current logout context, we need to create one
                            // this captures necessary info from the current logged in user
                            // before we signout and redirect away to the external IdP for signout
                            vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            return vm;
        }

        private async Task AddLoginLog(LoginLogEntity entity)
        {
            entity.IP = IPHelper.GetIP(HttpContext?.Request);
            string ua = HttpContext.Request.Headers["User-Agent"];
            if (ua.NotNull())
            {
                var client = UAParser.Parser.GetDefault().Parse(ua);
                var device = client.Device.Family;
                device = device.ToLower() == "other" ? "" : device;
                entity.Browser = client.UA.Family;
                entity.Os = client.OS.Family;
                entity.Device = device;
                entity.BrowserInfo = ua;
            }
            var id = (await _loginLogRepository.InsertAsync(entity)).Id;
        }
    }
}
