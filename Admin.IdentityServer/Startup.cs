using System;
using System.IO;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using IdentityServer4.Configuration;
using HealthChecks.UI.Client;
using Admin.IdentityServer.Configs;
using Admin.IdentityServer.Account;
using Admin.IdentityServer.Utils;

namespace Admin.IdentityServer
{
    public class Startup
    {
        private readonly IWebHostEnvironment _env;
        private readonly AppSettings _appSettings;
        private string basePath => AppContext.BaseDirectory;

        public Startup(IWebHostEnvironment env, IConfiguration configuration)
        {
            _appSettings = configuration.Get<AppSettings>();
            _env = env;
        }

        public void ConfigureServices(IServiceCollection services)
        {
            //界面即时编译，Microsoft.AspNetCore.Mvc.Razor.RuntimeCompilation
            //services.AddRazorPages().AddRazorRuntimeCompilation();

            services.AddSingleton(_appSettings);
            services.AddSingleton(new IPHelper());
            services.AddDb(_appSettings);

            var builder = services.AddIdentityServer(options =>
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
                .AddInMemoryClients(Config.Clients(_appSettings))
                .AddProfileService<AdminProfileService>()
                .AddResourceOwnerValidator<AdminResourceOwnerPasswordValidator>();

            if (_env.IsDevelopment())
            {
                builder.AddDeveloperSigningCredential();
            }
            else
            {
                builder.AddSigningCredential(new X509Certificate2(
                    Path.Combine(AppContext.BaseDirectory, _appSettings.Certificate.Path),
                    _appSettings.Certificate.Password)
                );
            }

            #region Cors 跨域
            services.AddCors(options =>
            {
                options.AddPolicy("Limit", policy =>
                {
                    policy
                    .WithOrigins(_appSettings.CorUrls)
                    .AllowAnyHeader()
                    .AllowAnyMethod()
                    .AllowCredentials();
                });
            });
            #endregion

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

            if (_env.IsDevelopment() || _appSettings.Swagger)
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

                    string xmlPath = Path.Combine(basePath, $"{typeof(Startup).Assembly.GetName().Name}.xml");
                    options.IncludeXmlComments(xmlPath);
                });
            }

            if (_appSettings.Health)
            {
                services.AddHealthChecks();
            }
        }

        public void Configure(IApplicationBuilder app)
        {
            if (_env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
            }

            app.UseCookiePolicy();
            
            app.UseCors("Limit");

            app.UseSession();
            app.UseStaticFiles();
            app.UseRouting();

            app.UseIdentityServer();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();

                if (_appSettings.Health)
                {
                    endpoints.MapHealthChecks("/health", new HealthCheckOptions
                    {
                        Predicate = registration => true,
                        ResponseWriter = UIResponseWriter.WriteHealthCheckUIResponse
                    });
                }
            });

            if (_env.IsDevelopment() || _appSettings.Swagger)
            {
                app.UseSwagger();
                app.UseSwaggerUI(options =>
                {
                    options.SwaggerEndpoint("/swagger/v1/swagger.json", "Admin.IdentityServer");
                    options.RoutePrefix = "";
                });
            }
        }
    }
}
