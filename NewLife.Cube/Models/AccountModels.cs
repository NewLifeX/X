using System;
using System.ComponentModel.DataAnnotations;
using System.Web.Mvc;

namespace NewLife.Cube.Models
{
    /// <summary>本地密码模型</summary>
    public class LocalPasswordModel
    {
        /// <summary>当前密码</summary>
        //[Required]
        [DataType(DataType.Password)]
        [Display(Name = "当前密码")]
        public String OldPassword { get; set; }

        /// <summary>确认新密码新密码</summary>
        //[Required]
        [StringLength(100, ErrorMessage = "{0} 必须至少包含 {2} 个字符。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "新密码")]
        public String NewPassword { get; set; }

        /// <summary>确认新密码</summary>
        [DataType(DataType.Password)]
        [Display(Name = "确认新密码")]
        [Compare("NewPassword", ErrorMessage = "新密码和确认密码不匹配。")]
        public String ConfirmPassword { get; set; }

        /// <summary>昵称</summary>
        [Required]
        [Display(Name = "昵称")]
        public String DisplayName { get; set; }
    }

    /// <summary>登录模型</summary>
    public class LoginModel
    {
        /// <summary>用户名</summary>
        [Required]
        [Display(Name = "用户名")]
        public String UserName { get; set; }

        /// <summary>密码</summary>
        [Required]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public String Password { get; set; }

        /// <summary>记住我</summary>
        [Display(Name = "记住我?")]
        public Boolean RememberMe { get; set; }
    }

    /// <summary>注册模型</summary>
    public class RegisterModel
    {
        /// <summary>用户名</summary>
        [Required]
        [Display(Name = "用户名")]
        public String UserName { get; set; }

        /// <summary>密码</summary>
        [Required]
        [StringLength(100, ErrorMessage = "{0} 必须至少包含 {2} 个字符。", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "密码")]
        public String Password { get; set; }

        /// <summary>确认密码</summary>
        [DataType(DataType.Password)]
        [Display(Name = "确认密码")]
        [Compare("Password", ErrorMessage = "密码和确认密码不匹配。")]
        public String ConfirmPassword { get; set; }
    }
}
