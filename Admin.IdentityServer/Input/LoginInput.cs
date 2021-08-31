namespace Admin.IdentityServer.Input
{
    public class LoginInput
    {
        public string ReturnUrl { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public bool RememberLogin { get; set; }

        /// <summary>
        /// 验证数据
        /// </summary>
        public CaptchaInput Captcha { get; set; }
    }
}