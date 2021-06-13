using Admin.IdentityServer.Domain.Admin;
using Admin.IdentityServer.Utils;
using FreeSql;
using IdentityModel;
using IdentityServer4.Models;
using IdentityServer4.Validation;
using System.Threading.Tasks;

namespace Admin.IdentityServer.Account
{
    public class AdminResourceOwnerPasswordValidator : IResourceOwnerPasswordValidator
    {
        private readonly IBaseRepository<UserEntity> _userRepository;

        public AdminResourceOwnerPasswordValidator(IBaseRepository<UserEntity> userRepository)
        {
            _userRepository = userRepository;
        }

        /// <summary>
        /// 验证登录信息
        /// </summary>
        /// <param name="context"></param>
        /// <returns></returns>
        public async Task ValidateAsync(ResourceOwnerPasswordValidationContext context)
        {
            var user = await _userRepository.Select.Where(a => a.UserName == context.UserName).ToOneAsync();

            if (user == null)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "账号输入有误!");
                return;
            }

            var password = MD5Encrypt.Encrypt32(context.Password);
            if (user.Password != password)
            {
                context.Result = new GrantValidationResult(TokenRequestErrors.InvalidGrant, "密码输入有误!");
                return;
            }

            context.Result = new GrantValidationResult(user.Id.ToString(), OidcConstants.AuthenticationMethods.Password);
        }
    }
}