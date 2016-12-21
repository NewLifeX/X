using System;
using System.ComponentModel;
using NewLife.Log;
using NewLife.Remoting;

namespace NewLife.MessageQueue
{
    class UserController : IApi
    {
        /// <summary>Api接口会话</summary>
        public IApiSession Session { get; set; }

        /// <summary>登录</summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        [DisplayName("登录")]
        public Boolean Login(String user, String pass)
        {
            XTrace.WriteLine("登录 {0}/{1}", user, pass);

            if (pass != user.MD5()) throw new Exception("密码不正确");

            // 记录已登录用户
            Session["user"] = user;

            return true;
        }
    }
}