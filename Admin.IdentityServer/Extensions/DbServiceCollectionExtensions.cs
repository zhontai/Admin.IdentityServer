using Admin.IdentityServer.Configs;
using Admin.IdentityServer.Domain;
using FreeSql;
using Microsoft.Extensions.DependencyInjection;
using System;

namespace Admin.IdentityServer
{
    public static class DbServiceCollectionExtensions
    {
        /// <summary>
        /// 添加数据库
        /// </summary>
        /// <param name="services"></param>
        /// <param name="appSettings"></param>
        /// <returns></returns>
        public static IServiceCollection AddDb(this IServiceCollection services, AppSettings appSettings)
        {
            #region FreeSql

            var freeSqlBuilder = new FreeSqlBuilder()
                    .UseConnectionString(appSettings.Db.Type, appSettings.Db.ConnectionString)
                    .UseAutoSyncStructure(false)
                    .UseLazyLoading(false)
                    .UseNoneCommandParameter(true);

            var fsql = freeSqlBuilder.Build();
            services.AddFreeRepository(filter => filter.Apply<IEntitySoftDelete>("SoftDelete", a => a.IsDeleted == false));
            services.AddScoped<UnitOfWorkManager>();
            services.AddSingleton(fsql);

            if (appSettings.Db.Curd)
            {
                fsql.Aop.CurdBefore += (s, e) =>
                {
                    Console.WriteLine($"{e.Sql}\r\n");
                };
            }

            #endregion FreeSql

            return services;
        }
    }
}