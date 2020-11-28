using System;
using System.Linq;
using System.Diagnostics;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Events;
using IdentityServer4.Extensions;
using IdentityServer4.Services;
using IdentityServer4.Stores;
using FreeSql;
using Admin.IdentityServer.Configs;
using Admin.IdentityServer.Domain.Admin;
using Admin.IdentityServer.Input;
using Admin.IdentityServer.Models;
using Admin.IdentityServer.Output;
using Admin.IdentityServer.Utils;

namespace Admin.IdentityServer
{
    //[ApiExplorerSettings(IgnoreApi = true)]
    public class AccountController : Controller
    {
        private readonly IBaseRepository<UserEntity> _userRepository;
        private readonly IBaseRepository<LoginLogEntity> _loginLogRepository;
        private readonly IIdentityServerInteractionService _interaction;
        private readonly IEventService _events;
        private readonly AppSettings _appSettings;
        //private readonly IHttpClientFactory _httpClientFactory;
        //private readonly IPHelper _iPHelper;

        public AccountController(
            IBaseRepository<UserEntity> userRepository,
            IBaseRepository<LoginLogEntity> loginLogRepository,
            IIdentityServerInteractionService interaction,
            IEventService events,
            AppSettings appSettings
            //IHttpClientFactory httpClientFactory,
            //IPHelper iPHelper
            )
        {
            _userRepository = userRepository;
            _loginLogRepository = loginLogRepository;

            _interaction = interaction;
            _events = events;
            _appSettings = appSettings;
            //_httpClientFactory = httpClientFactory;
            //_iPHelper = iPHelper;
        }

        [HttpGet]
        [Route("user/login")]
        public IActionResult Login(string returnUrl)
        {
            returnUrl = returnUrl ?? _appSettings.AdminUI.RedirectUris.First().Replace("/callback","");
            var loginViewModal = new LoginInput { ReturnUrl = returnUrl };

            return View(loginViewModal);
        }

        
        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("user/login")]
        public async Task<IResponseOutput> Login(LoginInput input)
        {
            if (!ModelState.IsValid)
            {
                return ResponseOutput.NotOk(ModelState.Values.First().Errors[0].ErrorMessage);
            }

            var sw = new Stopwatch();
            sw.Start();

            var context = await _interaction.GetAuthorizationContextAsync(input.ReturnUrl);
             
            var user = await _userRepository.Select.Where(a => a.UserName == input.UserName)
                .ToOneAsync(a => new { a.Id, a.Password, a.NickName });

            if (user == null)
            {
                return ResponseOutput.NotOk("", 1);
            }

            var password = MD5Encrypt.Encrypt32(input.Password);
            if (user.Password != password)
            {
                return ResponseOutput.NotOk("", 2);
            }


            AuthenticationProperties props = null;
            if (input.RememberLogin)
            {
                props = new AuthenticationProperties
                {
                    IsPersistent = true,
                    ExpiresUtc = DateTimeOffset.UtcNow.Add(TimeSpan.FromDays(1))
                };
            };

            var identityServerUser = new IdentityServerUser(user.Id.ToString())
            {
                DisplayName = input.UserName
            };

            await HttpContext.SignInAsync(identityServerUser, props);

            sw.Stop();

            //写登录日志
            var loginLogEntity = new LoginLogEntity()
            {
                CreatedUserId = user.Id,
                NickName = user.NickName,
                CreatedUserName = input.UserName,
                ElapsedMilliseconds = sw.ElapsedMilliseconds,
                Status = true
            };
            await AddLoginLog(loginLogEntity);

            return ResponseOutput.Ok();
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
            
            await _loginLogRepository.InsertAsync(entity);
        }


        [HttpGet]
        [Route("user/logout")]
        public async Task<IActionResult> Logout(string logoutId)
        {
            var vm = await BuildLogoutViewModelAsync(logoutId);

            if (vm.ShowLogoutPrompt == false)
            {
                return await Logout(vm);
            }

            //强制跳到客户端界面
            var postLogoutRedirectUri = _appSettings.AdminUI.PostLogoutRedirectUris.First();
            if (postLogoutRedirectUri.NotNull())
            {
                return Redirect(postLogoutRedirectUri);
            }

            return RedirectToAction("Login");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("user/logout")]
        public async Task<IActionResult> Logout(LogoutInputModel model)
        {
            var vm = await BuildLoggedOutViewModelAsync(model.LogoutId);

            if (User?.Identity.IsAuthenticated == true)
            {
                await HttpContext.SignOutAsync();

                await _events.RaiseAsync(new UserLogoutSuccessEvent(User.GetSubjectId(), User.GetDisplayName()));
            }

            if (vm.TriggerExternalSignout)
            {
                string url = Url.Action("Logout", new { logoutId = vm.LogoutId });

                return SignOut(new AuthenticationProperties { RedirectUri = url }, vm.ExternalAuthenticationScheme);
            }

            var postLogoutRedirectUri = vm.PostLogoutRedirectUri ?? _appSettings.AdminUI.PostLogoutRedirectUris.First();
            if (vm.AutomaticRedirectAfterSignOut)
            {
                return Redirect(postLogoutRedirectUri);
            }
            //强制跳到客户端界面
            if (postLogoutRedirectUri.NotNull())
            {
                return Redirect(postLogoutRedirectUri);
            }

            return RedirectToAction("Login");
        }

        private async Task<LogoutViewModel> BuildLogoutViewModelAsync(string logoutId)
        {
            var vm = new LogoutViewModel { LogoutId = logoutId, ShowLogoutPrompt = AccountOptions.ShowLogoutPrompt };

            if (User?.Identity.IsAuthenticated != true)
            {
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            var context = await _interaction.GetLogoutContextAsync(logoutId);
            if (context?.ShowSignoutPrompt == false)
            {
                vm.ShowLogoutPrompt = false;
                return vm;
            }

            return vm;
        }

        private async Task<LoggedOutViewModel> BuildLoggedOutViewModelAsync(string logoutId)
        {
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
                if (idp != null && idp != IdentityServerConstants.LocalIdentityProvider)
                {
                    var providerSupportsSignout = await HttpContext.GetSchemeSupportsSignOutAsync(idp);
                    if (providerSupportsSignout)
                    {
                        if (vm.LogoutId == null)
                        {
                            vm.LogoutId = await _interaction.CreateLogoutContextAsync();
                        }

                        vm.ExternalAuthenticationScheme = idp;
                    }
                }
            }

            return vm;
        }

    }
}
