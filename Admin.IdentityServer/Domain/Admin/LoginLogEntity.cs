using FreeSql.DataAnnotations;

namespace Admin.IdentityServer.Domain.Admin
{
    /// <summary>
    /// 操作日志
    /// </summary>
	[Table(Name = "ad_login_log")]
    public class LoginLogEntity : LogAbstract
    {
    }
}