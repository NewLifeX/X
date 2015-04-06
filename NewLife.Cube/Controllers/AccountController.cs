using System;
using System.Web.Mvc;
using System.Web.Security;
using NewLife.Cube.Filters;
using NewLife.Cube.Models;
using NewLife.Log;
using XCode.Membership;

namespace NewLife.Cube.Controllers
{
    [EntityAuthorize]
    public class AccountController : Controller
    {
        //
        // GET: /Account/Login

        [AllowAnonymous]
        public ActionResult Login(string returnUrl)
        {
            ViewBag.ReturnUrl = returnUrl;
            return View();
        }

        //
        // POST: /Account/Login

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Login(LoginModel model, string returnUrl)
        {
            try
            {
                var provider = ManageProvider.Provider;
                if (ModelState.IsValid && provider.Login(model.UserName, model.Password, model.RememberMe) != null)
                {
                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
                    //FormsAuthentication.RedirectFromLoginPage(provider.Current + "", true);

                    return RedirectToLocal(returnUrl);
                }

                // 如果我们进行到这一步时某个地方出错，则重新显示表单
                ModelState.AddModelError("", "提供的用户名或密码不正确。");
            }
            catch (Exception ex)
            {
                ModelState.AddModelError("", ex.Message);
            }

            return View(model);
        }

        //
        // POST: /Account/LogOff

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult LogOff()
        {
            var provider = ManageProvider.Provider;
            provider.Logout();

            FormsAuthentication.SignOut();

            return RedirectToAction("Index", "Home");
        }

        //
        // GET: /Account/Register

        [AllowAnonymous]
        public ActionResult Register()
        {
            return View();
        }

        //
        // POST: /Account/Register

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public ActionResult Register(RegisterModel model)
        {
            if (ModelState.IsValid)
            {
                var provider = ManageProvider.Provider;
                // 尝试注册用户
                try
                {
                    var user = provider.Register(model.UserName, model.Password, null, true);
                    if (user != null && user.Enable)
                    {
                        provider.Login(model.UserName, model.Password);
                        FormsAuthentication.RedirectFromLoginPage(provider.Current + "", true);
                    }

                    return RedirectToAction("Index", "Home");
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                    ModelState.AddModelError("", ex.Message);
                }
            }

            // 如果我们进行到这一步时某个地方出错，则重新显示表单
            return View(model);
        }

        //
        // POST: /Account/Disassociate

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Disassociate(string provider, string providerUserId)
        {
            //string ownerAccount = OAuthWebSecurity.GetUserName(provider, providerUserId);
            ManageMessageId? message = null;

            //// 只有在当前登录用户是所有者时才取消关联帐户
            //if (ownerAccount == User.Identity.Name)
            //{
            //    // 使用事务来防止用户删除其上次使用的登录凭据
            //    using (var scope = new TransactionScope(TransactionScopeOption.Required, new TransactionOptions { IsolationLevel = IsolationLevel.Serializable }))
            //    {
            //        bool hasLocalAccount = OAuthWebSecurity.HasLocalAccount(WebSecurity.GetUserId(User.Identity.Name));
            //        if (hasLocalAccount || OAuthWebSecurity.GetAccountsFromUserName(User.Identity.Name).Count > 1)
            //        {
            //            OAuthWebSecurity.DeleteAccount(provider, providerUserId);
            //            scope.Complete();
            //            message = ManageMessageId.RemoveLoginSuccess;
            //        }
            //    }
            //}

            return RedirectToAction("Manage", new { Message = message });
        }

        //
        // GET: /Account/Manage

        public ActionResult Manage(ManageMessageId? message)
        {
            ViewBag.StatusMessage =
                message == ManageMessageId.ChangePasswordSuccess ? "你的密码已更改。"
                : message == ManageMessageId.SetPasswordSuccess ? "已设置你的密码。"
                : message == ManageMessageId.RemoveLoginSuccess ? "已删除外部登录。"
                : "";
            ViewBag.ReturnUrl = Url.Action("Manage");

            var user = ManageProvider.User as IUser;
            var model = new LocalPasswordModel();
            model.DisplayName = user.DisplayName;

            return View(model);
        }

        //
        // POST: /Account/Manage

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Manage(LocalPasswordModel model)
        {
            ViewBag.ReturnUrl = Url.Action("Manage");

            if (ModelState.IsValid)
            {
                // 在某些出错情况下，ChangePassword 将引发异常，而不是返回 false。
                bool changePasswordSucceeded = false;
                try
                {
                    var user = ManageProvider.User as IUser;
                    if (user != null)
                    {
                        if (!model.NewPassword.IsNullOrEmpty() && model.OldPassword.MD5().EqualIgnoreCase(user.Password))
                        {
                            user.Password = model.NewPassword.MD5();
                        }
                        if (!String.IsNullOrEmpty(model.DisplayName))
                            user.DisplayName = model.DisplayName;
                        user.Save();

                        changePasswordSucceeded = true;
                    }
                }
                catch (Exception ex)
                {
                    XTrace.WriteException(ex);
                    changePasswordSucceeded = false;
                }

                if (changePasswordSucceeded)
                    return RedirectToAction("Manage", new { Message = ManageMessageId.ChangePasswordSuccess });

                ModelState.AddModelError("", "当前密码不正确或新密码无效。");
            }

            // 如果我们进行到这一步时某个地方出错，则重新显示表单
            return View(model);
        }

        #region 帮助程序
        private ActionResult RedirectToLocal(string returnUrl)
        {
            if (Url.IsLocalUrl(returnUrl))
                return Redirect(returnUrl);
            else
                return RedirectToAction("Index", "Home");
        }

        public enum ManageMessageId
        {
            ChangePasswordSuccess,
            SetPasswordSuccess,
            RemoveLoginSuccess,
        }
        #endregion
    }
}