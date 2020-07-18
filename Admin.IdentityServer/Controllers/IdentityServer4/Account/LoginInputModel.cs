// Copyright (c) Brock Allen & Dominick Baier. All rights reserved.
// Licensed under the Apache License, Version 2.0. See LICENSE in the project root for license information.


using System.ComponentModel.DataAnnotations;

namespace Admin.IdentityServer
{
    public class LoginInputModel
    {
        [Required(ErrorMessage = "请输入账号")]
        [Display(Name = "用户名：")]
        public string UserName { get; set; }
        [Required(ErrorMessage = "请输入密码")]
        [Display(Name = "登录密码：")]
        public string Password { get; set; }
        [Display(Name = "记住我")]
        public bool RememberLogin { get; set; }
        public string ReturnUrl { get; set; }
    }
}