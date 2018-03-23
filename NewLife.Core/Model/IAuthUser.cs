using System;
using NewLife.Net;
using NewLife.Web;

namespace NewLife.Model
{
    /// <summary>认证用户接口，具有登录验证、注册、在线等基本信息</summary>
    public interface IAuthUser : IManageUser
    {
        #region 属性
        /// <summary>密码</summary>
        String Password { get; set; }

        /// <summary>在线</summary>
        Boolean Online { get; set; }

        /// <summary>登录次数</summary>
        Int32 Logins { get; set; }

        /// <summary>最后登录</summary>
        DateTime LastLogin { get; set; }

        /// <summary>最后登录IP</summary>
        String LastLoginIP { get; set; }

        /// <summary>注册时间</summary>
        DateTime RegisterTime { get; set; }

        /// <summary>注册IP</summary>
        String RegisterIP { get; set; }
        #endregion

        /// <summary>保存</summary>
        /// <returns></returns>
        Int32 Save();
    }

    /// <summary>用户接口工具类</summary>
    public static class ManageUserHelper
    {
        /// <summary>比较密码相等</summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public static Boolean CheckEqual(this IAuthUser user, String pass)
        {
            // 验证密码
            if (user.Password != pass) throw new Exception(user + " 密码错误");

            return true;
        }

        /// <summary>比较密码MD5</summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public static Boolean CheckMD5(this IAuthUser user, String pass)
        {
            // 验证密码
            if (user.Password != pass.MD5()) throw new Exception(user + " 密码错误");

            return true;
        }

        /// <summary>比较密码RC4</summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        public static Boolean CheckRC4(this IAuthUser user, String pass)
        {
            // 密码有盐值和密文两部分组成
            var salt = pass.Substring(0, pass.Length / 2).ToHex();
            pass = pass.Substring(pass.Length / 2);

            // 验证密码
            var tpass = user.Password.GetBytes();
            if (salt.RC4(tpass).ToHex() != pass) throw new Exception(user + " 密码错误");

            return true;
        }

        /// <summary>保存登录信息</summary>
        /// <param name="user"></param>
        /// <param name="session"></param>
        public static void SaveLogin(this IAuthUser user, INetSession session)
        {
            user.Logins++;
            user.LastLogin = DateTime.Now;

            if (session != null)
            {
                user.LastLoginIP = session.Remote?.Address + "";
                // 销毁时
                session.OnDisposed += (s, e) =>
                {
                    user.Online = false;
                    user.Save();
                };
            }
            else
                user.LastLoginIP = WebHelper.UserHost;

            user.Online = true;
            user.Save();
        }

        /// <summary>保存注册信息</summary>
        /// <param name="user"></param>
        /// <param name="session"></param>
        public static void SaveRegister(this IAuthUser user, INetSession session)
        {
            //user.Registers++;
            user.RegisterTime = DateTime.Now;
            //user.RegisterIP = ns.Remote.EndPoint.Address + "";

            if (session != null)
            {
                user.RegisterIP = session.Remote?.Address + "";
                // 销毁时
                session.OnDisposed += (s, e) =>
                {
                    user.Online = false;
                    user.Save();
                };
            }

            user.Online = true;
            user.Save();
        }
    }
}