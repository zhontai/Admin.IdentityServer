using Admin.IdentityServer;
using Admin.IdentityServer.Account;
using Admin.IdentityServer.Configs;
using Admin.IdentityServer.Utils;
using HealthChecks.UI.Client;
using IdentityServer4.Configuration;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using NLog.Web;
using System;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography.X509Certificates;
using Yitter.IdGenerator;

var builder = WebApplication.CreateBuilder(args);

//使用NLog日志
builder.Host.UseNLog();

var env = builder.Environment;

//添加配置
var configurationBuilder = new ConfigurationBuilder()
    .SetBasePath(AppDomain.CurrentDomain.BaseDirectory)
    .AddJsonFile("appsettings.json", false);

if (env.EnvironmentName.NotNull())
{
    configurationBuilder.AddJsonFile($"appsettings.{env.EnvironmentName}.json", true);
}
var appSettings = configurationBuilder.Build().Get<AppSettings>();

//雪花漂移算法
YitIdHelper.SetIdGenerator(new IdGeneratorOptions(1) { WorkerIdBitLength = 6 });

var services = builder.Services;
services.AddSingleton(appSettings);
services.AddSingleton(new IPHelper());
services.AddDb(appSettings);

#region Cors 跨域

if (appSettings.CorUrls?.Length > 0)
{
    services.AddCors(options =>
    {
        options.AddPolicy("Limit", policy =>
        {
            policy
            .WithOrigins(appSettings.CorUrls)
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
        });
    });
}

#endregion Cors 跨域

var identityServerBuilder = services.AddIdentityServer(options =>
{
    options.UserInteraction = new UserInteractionOptions
    {
        LoginUrl = "/user/login",
        LogoutUrl = "/user/logout"
    };
})
.AddInMemoryIdentityResources(Config.IdentityResources)
.AddInMemoryApiScopes(Config.ApiScopes)
.AddInMemoryApiResources(Config.ApiResources)
.AddInMemoryClients(Config.Clients(appSettings))
.AddProfileService<AdminProfileService>()
.AddResourceOwnerValidator<AdminResourceOwnerPasswordValidator>();

if (env.IsDevelopment())
{
    identityServerBuilder.AddDeveloperSigningCredential();
}
else
{
    var certificatePath = Path.Combine(AppContext.BaseDirectory, appSettings.Certificate.Path);
    identityServerBuilder.AddSigningCredential(new X509Certificate2(certificatePath, appSettings.Certificate.Password));
}

services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/user/login";
});

services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromSeconds(120);
});

services.AddControllersWithViews(options =>
{
    //禁止去除ActionAsync后缀
    options.SuppressAsyncSuffixInActionNames = false;
})
.AddNewtonsoftJson(options =>
{
        //忽略循环引用
        options.SerializerSettings.ReferenceLoopHandling = ReferenceLoopHandling.Ignore;
        //使用驼峰 首字母小写
        options.SerializerSettings.ContractResolver = new CamelCasePropertyNamesContractResolver();
        //设置时间格式
        options.SerializerSettings.DateFormatString = "yyyy-MM-dd HH:mm:ss";
});

services.AddHttpClient();

if (env.IsDevelopment() || appSettings.Swagger)
{
    services.AddSwaggerGen(options =>
    {
        options.SwaggerDoc("v1", new OpenApiInfo
        {
            Version = "v1",
            Title = "Admin.IdentityServer"
        });

        //添加Jwt验证设置
        options.AddSecurityRequirement(new OpenApiSecurityRequirement()
                    {
                        {
                            new OpenApiSecurityScheme
                            {
                                Reference = new OpenApiReference
                                {
                                    Id = "Bearer",
                                    Type = ReferenceType.SecurityScheme
                                }
                            },
                            new List<string>()
                        }
                    });

        //添加设置Token的按钮
        options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
        {
            Description = "Value: Bearer {token}",
            Name = "Authorization",
            In = ParameterLocation.Header,
            Type = SecuritySchemeType.ApiKey
        });

        string xmlPath = Path.Combine(AppContext.BaseDirectory, $"{env.ApplicationName}.xml");
        options.IncludeXmlComments(xmlPath);
    });
}

if (appSettings.Health)
{
    services.AddHealthChecks();
}

var app = builder.Build();
app.UseHttpsRedirection();

if (env.IsDevelopment())
{
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Home/Error");
}

app.UseCors("Limit");
app.UseCookiePolicy();
app.UseSession();
app.UseStaticFiles();
app.UseRouting();

app.UseIdentityServer();
app.UseAuthorization();

app.MapControllers();
if (appSettings.Health)
{
    app.MapHealthChecks("/health", new HealthCheckOptions
    {
        Predicate = registration => true,
        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
    });
}

if (env.IsDevelopment() || appSettings.Swagger)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "Admin.IdentityServer");
    });
}

app.Run();