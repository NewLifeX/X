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
        /// <summary>列表页视图。子控制器可重载，以传递更多信息给视图，比如修改要显示的列</summary>
        /// <param name="p"></param>
        /// <returns></returns>
        protected override ActionResult IndexView(Pager p)
        {
            // 让角色ID字段变为角色名字段，友好显示
            var fields = ViewBag.Fields as List<FieldItem>;
            for (int i = 0; i < fields.Count; i++)
            {
                if (fields[i].Name.EqualIgnoreCase("RoleID"))
                    fields[i] = UserX.Meta.AllFields.FirstOrDefault(e => e.Name == "RoleName");
            }

            return base.IndexView(p);
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
                ModelState.AddModelError("", "提供的用户名或密码不正确。");
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
    }
}