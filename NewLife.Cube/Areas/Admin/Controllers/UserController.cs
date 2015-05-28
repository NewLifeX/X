using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using NewLife.Web;
using XCode;
using XCode.Configuration;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    /// <summary>用户控制器</summary>
    [DisplayName("用户")]
    public class UserController : EntityController<UserX>
    {
        /// <summary>动作执行前</summary>
        /// <param name="filterContext"></param>
        protected override void OnActionExecuting(ActionExecutingContext filterContext)
        {
            ViewBag.HeaderContent = "系统基于角色授权，每个角色对不同的功能模块具备添删改查以及自定义权限等多种权限设定。";

            base.OnActionExecuting(filterContext);
        }

        /// <summary>表单页视图。</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected override ActionResult FormView(UserX entity)
        {
            // 清空密码，不向浏览器输出
            entity.Password = null;

            return base.FormView(entity);
        }

        /// <summary>登录</summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult Login(String returnUrl)
        {
            // 如果已登录，直接跳转
            if (ManageProvider.User != null)
            {
                if (Url.IsLocalUrl(returnUrl))
                    return Redirect(returnUrl);
                else
                    return RedirectToAction("Index", "Index");
            }

            ViewBag.ReturnUrl = returnUrl;

            return View();
        }

        /// <summary>登录</summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="remember"></param>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult Login(String username, String password, Boolean? remember, String returnUrl)
        {
            try
            {
                var provider = ManageProvider.Provider;
                if (ModelState.IsValid && provider.Login(username, password, remember ?? false) != null)
                {
                    FormsAuthentication.SetAuthCookie(username, remember ?? false);
                    //FormsAuthentication.RedirectFromLoginPage(provider.Current + "", true);

                    if (Url.IsLocalUrl(returnUrl))
                        return Redirect(returnUrl);
                    else
                        return RedirectToAction("Index", "Index");
                }

                // 如果我们进行到这一步时某个地方出错，则重新显示表单
                ModelState.AddModelError("username", "提供的用户名或密码不正确。");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return View();
        }

        /// <summary>注销</summary>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult Logout()
        {
            ManageProvider.User.Logout();
            //ManageProvider.User = null;

            return RedirectToAction("Login");
        }

        /// <summary>用户资料</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult Info(Int32? id)
        {
            if (id == null || id.Value <= 0) throw new Exception("无效用户编号！");

            var user = ManageProvider.User;
            if (user == null) return RedirectToAction("Login");

            if (id.Value != user.ID) throw new Exception("禁止修改非当前登录用户资料");

            user = UserX.FindByID(id.Value);
            if (user == null) throw new Exception("无效用户编号！");

            user.Password = null;

            // 用于显示的列
            if (ViewBag.Fields == null) ViewBag.Fields = GetFields(true);
            ViewBag.Factory = UserX.Meta.Factory;

            return View(user);
        }

        /// <summary>用户资料</summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult Info(UserX user)
        {
            var cur = ManageProvider.User;
            if (cur == null) return RedirectToAction("Login");

            if (user.ID != cur.ID) throw new Exception("禁止修改非当前登录用户资料");

            var entity = user as IEntity;
            if (entity.Dirtys["RoleID"]) throw new Exception("禁止修改角色！");
            if (entity.Dirtys["Enable"]) throw new Exception("禁止修改禁用！");

            return View();
        }

        /// <summary>忘记密码</summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult ForgetPassword(String email)
        {
            throw new NotImplementedException("未实现！");
        }

        /// <summary>注册</summary>
        /// <param name="email"></param>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="password2"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult Register(String email, String username, String password, String password2)
        {
            try
            {
                if (String.IsNullOrEmpty(email)) throw new ArgumentNullException("email", "邮箱地址不能为空！");
                if (String.IsNullOrEmpty(username)) throw new ArgumentNullException("username", "用户名不能为空！");
                if (String.IsNullOrEmpty(password)) throw new ArgumentNullException("password", "密码不能为空！");
                if (String.IsNullOrEmpty(password2)) throw new ArgumentNullException("password2", "重复密码不能为空！");
                if (password != password2) throw new ArgumentOutOfRangeException("password2", "两次密码必须一致！");

                var user = new UserX();
                user.Name = username;
                user.Password = password.MD5();
                user.Mail = email;
                user.Enable = true;
                user.Register();

                // 注册成功
            }
            catch (ArgumentException aex)
            {
                ModelState.AddModelError(aex.ParamName, aex.Message);
            }

            return View("Login");
        }
    }
}