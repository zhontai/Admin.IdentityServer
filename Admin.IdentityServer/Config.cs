using Admin.IdentityServer.Configs;
using IdentityModel;
using IdentityServer4;
using IdentityServer4.Models;
using System.Collections.Generic;
using System.Security.Claims;

namespace Admin.IdentityServer
{
    public static class Config
    {
        public static IEnumerable<IdentityResource> IdentityResources =>
            new IdentityResource[]
            {
                new IdentityResources.OpenId(),
                new IdentityResources.Profile()
            };

        // v4新增
        public static IEnumerable<ApiScope> ApiScopes =>
            new ApiScope[] {
                 new ApiScope("admin.server.api")
            };

        public static IEnumerable<ApiResource> ApiResources =>
            new ApiResource[]
            {
                new ApiResource("admin.server.api", "admin后端api")
                {
                    UserClaims =  { ClaimTypes.Name, JwtClaimTypes.Name },
                    //v4新增
                    Scopes = { "admin.server.api" },
                    ApiSecrets = new List<Secret>()
                    {
                        new Secret("secret".Sha256())
                    }
                }
            };

        public static IEnumerable<Client> Clients(AppSettings appSettings) =>
            new Client[]
            {
                new Client
                {
                    ClientId = "admin.server.api",
                    ClientName = "admin后端api认证",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    //ClientSecrets = new []{ new Secret("secret".Sha256()) },
                    RequireConsent = false, //同意
                    AllowAccessTokensViaBrowser = true,
                    AccessTokenLifetime = 3600 * 2, //2小时 = 3600 * 2
                    //SlidingRefreshTokenLifetime = 3600 * 24, //1天 = 3600 * 24
                    RedirectUris =
                    {
                        "http://localhost:8000/oauth2-redirect.html",
                    },
                    PostLogoutRedirectUris =
                    {
                        $"http://localhost:8000"
                    },
                    AllowedCorsOrigins = appSettings.CorUrls,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "admin.server.api"
                    }
                },
                new Client
                {
                    ClientId = "admin.ui",
                    ClientName = "admin前端",
                    AllowedGrantTypes = GrantTypes.Implicit,
                    //ClientSecrets = new []{ new Secret(appSettings.AdminUI.Secret.Sha256()) },
                    RequireConsent = false, //同意
                    AllowAccessTokensViaBrowser = true,
                    AccessTokenLifetime = appSettings.AdminUI.AccessTokenLifetime, //2小时 = 3600 * 2
                    //SlidingRefreshTokenLifetime = 3600 * 24, //1天 = 3600 * 24
                    //AllowOfflineAccess = true,
                    //AlwaysIncludeUserClaimsInIdToken = true,
                    RedirectUris = appSettings.AdminUI.RedirectUris,
                    PostLogoutRedirectUris = appSettings.AdminUI.PostLogoutRedirectUris,
                    AllowedCorsOrigins = appSettings.CorUrls,
                    AllowedScopes =
                    {
                        IdentityServerConstants.StandardScopes.OpenId,
                        IdentityServerConstants.StandardScopes.Profile,
                        "admin.server.api"
                    }
                }
            };
    }
}