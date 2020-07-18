using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using IdentityServer4.Extensions;
using IdentityServer4.Models;
using IdentityServer4.Services;
using FreeSql;
using Admin.IdentityServer.Domain.Admin;

namespace Admin.IdentityServer.Account
{
    public class AdminProfileService : IProfileService
    {
        private readonly ILogger _logger;
        private readonly IBaseRepository<UserEntity> _userRepository;

        /// <summary>
        /// 
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="userRepository"></param>
        public AdminProfileService(ILogger<AdminProfileService> logger, IBaseRepository<UserEntity> userRepository)
        {
            _logger = logger;
            _userRepository = userRepository;
        }

        /// <summary>
        /// 获得用户信息
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual async Task GetProfileDataAsync(ProfileDataRequestContext context)
        {
            var sub = context.Subject?.GetSubjectId();
            if (sub == null) throw new Exception("用户Id为空");

            var user = await _userRepository.Select.WhereDynamic(sub).ToOneAsync(a => new { a.UserName, a.NickName });
            if (user == null)
            {
                _logger?.LogWarning("用户{0}不存在", sub);
            }
            else
            {
                
                var claims = new List<Claim>()
                {
                    new Claim(ClaimAttributes.UserName, user.UserName ?? ""),
                    new Claim(ClaimAttributes.UserNickName, user.NickName ?? ""),
                };

                context.IssuedClaims = claims;
            }
        }

        /// <summary>
        /// 验证用户是否有效
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public virtual async Task IsActiveAsync(IsActiveContext context)
        {
            _logger.LogDebug("用户是否有效：{caller}", context.Caller);

            var sub = context.Subject?.GetSubjectId();
            if (sub == null) throw new Exception("用户Id为空");

            var user = await _userRepository.Select.WhereDynamic(sub).ToOneAsync(a => new { a.Status });
            context.IsActive = user?.Status == 0;
        }

    }
}
