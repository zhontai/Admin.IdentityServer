using FreeSql.DataAnnotations;
using System;

namespace Admin.IdentityServer.Domain.Admin
{
    /// <summary>
    /// 用户
    /// </summary>
	[Table(Name = "ad_user")]
    [Index("idx_{tablename}_01", nameof(UserName) + "," + nameof(TenantId), true)]
    public class UserEntity : EntityBase
    {
        /// <summary>
        /// 租户Id
        /// </summary>
        [Column(Position = -10)]
        public long? TenantId { get; set; }

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