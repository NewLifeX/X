using NewLife.Web;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Web.Mvc;
using System.Web.Security;
using XCode;
using XCode.Membership;

namespace NewLife.Cube.AdminMobile.Controllers
{
    /// <summary>用户控制器</summary>
    [DisplayName("用户")]
    [Description("系统基于角色授权，每个角色对不同的功能模块具备添删改查以及自定义权限等多种权限设定。")]
    public class UserController : EntityController<UserX>
    {
        static UserController()
        {
            ListFields.RemoveField("Phone");
            ListFields.RemoveField("Code");
            ListFields.RemoveField("StartTime");
            ListFields.RemoveField("EndTime");
            ListFields.RemoveField("RegisterTime");
            ListFields.RemoveField("Question");
            ListFields.RemoveField("Answer");
        }

        /// <summary>搜索数据集</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override IEnumerable<UserX> Search(Pager p)
        {
            return UserX.Search(p["Q"], p["RoleID"].ToInt(-1), null, p["dtStart"].ToDateTime(), p["dtEnd"].ToDateTime(), p);
        }

        /// <summary>表单页视图。</summary>
        /// <param name="entity"></param>
        /// <returns></returns>
        protected override ActionResult FormView(UserX entity)
        {
            // 清空密码，不向浏览器输出
            //entity.Password = null;
            entity["Password"] = null;

            return base.FormView(entity);
        }

        /// <summary>登录</summary>
        /// <param name="returnUrl"></param>
        /// <returns></returns>
        [AllowAnonymous]
        public ActionResult Login(String returnUrl)
        {
            // 如果已登录，直接跳转
            //if (ManageProvider.User != null)
            //{
            //    if (Url.IsLocalUrl(returnUrl))
            //        return Redirect(returnUrl);
            //    else
            //        return RedirectToAction("Index", "Index", new { page = returnUrl });
            //}

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
                        return RedirectToAction("Index", "Index", new { page = returnUrl });
                }

                // 如果我们进行到这一步时某个地方出错，则重新显示表单
                ModelState.AddModelError("username", "提供的用户名或密码不正确。");
                ViewBag.js = WeuiJS.Alert("提供的用户名或密码不正确。");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
                ViewBag.js = WeuiJS.Alert(ex.Message);
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

            user = UserX.FindByKeyForEdit(id.Value);
            if (user == null) throw new Exception("无效用户编号！");

            //user.Password = null;
            user["Password"] = null;

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

            user.Update();

            return View(user);
        }

        /// <summary>忘记密码</summary>
        /// <param name="email"></param>
        /// <returns></returns>
        [HttpPost]
        [AllowAnonymous]
        public ActionResult ForgetPassword(String email)
        {
            var set = Setting.Current;
            if (!set.AllowForgot) throw new Exception("禁止取回密码！");

            //throw new NotImplementedException("未实现！");
            var user = UserX.FindByMail(email);
            if (user == null)
            {
                //throw new Exception("未找到");
                Js.Alert("未找到用户");
            }

            return View("Login");
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
            var set = Setting.Current;
            if (!set.AllowRegister) throw new Exception("禁止注册！");

            try
            {
                if (String.IsNullOrEmpty(email)) throw new ArgumentNullException("email", "邮箱地址不能为空！");
                if (String.IsNullOrEmpty(username)) throw new ArgumentNullException("username", "用户名不能为空！");
                if (String.IsNullOrEmpty(password)) throw new ArgumentNullException("password", "密码不能为空！");
                if (String.IsNullOrEmpty(password2)) throw new ArgumentNullException("password2", "重复密码不能为空！");
                if (password != password2) throw new ArgumentOutOfRangeException("password2", "两次密码必须一致！");

                var user = new UserX()
                {
                    Name = username,
                    Password = password.MD5(),
                    Mail = email,
                    RoleID = set.DefaultRole,
                    Enable = true
                };
                user.Register();

                // 注册成功
            }
            catch (ArgumentException aex)
            {
                ModelState.AddModelError(aex.ParamName, aex.Message);
            }

            return View("Login");
        }

        /// <summary>清空密码</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public ActionResult ClearPassword(Int32 id)
        {
            if (ManageProvider.User.RoleName != "管理员") throw new Exception("非法操作！");

            // 前面表单可能已经清空密码
            var user = UserX.FindByID(id);
            //user.Password = "nopass";
            user.Password = null;
            user.SaveWithoutValid();

            return RedirectToAction("Edit", new { id });
        }
    }
}