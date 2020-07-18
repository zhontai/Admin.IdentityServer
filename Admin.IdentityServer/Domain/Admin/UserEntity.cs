using System;
using FreeSql.DataAnnotations;

namespace Admin.IdentityServer.Domain.Admin
{
    /// <summary>
    /// 用户
    /// </summary>
	[Table(Name = "ad_user")]
    [Index("uk_user_username", nameof(UserName), true)]
    public class UserEntity: EntityBase
    {
        /// <summary>
        /// 账号
        /// </summary>
        [Column(StringLength = 60)]
        public string UserName { get; set; }

        /// <summary>
        /// 密码
        /// </summary>
        [Column(StringLength = 60)]
        public string Password { get; set; }

        /// <summary>
        /// 昵称
        /// </summary>
        [Column(StringLength = 60)]
        public string NickName { get; set; }

        /// <summary>
        /// 头像
        /// </summary>
       [Column(StringLength = 100)]
        public string Avatar { get; set; }

        /// <summary>
        /// 状态
        /// </summary>
        public int Status { get; set; }
    }
}
