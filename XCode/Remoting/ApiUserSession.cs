using System;
using System.Collections.Generic;
using NewLife.Model;
using NewLife.Net;
using NewLife.Remoting;
using NewLife.Security;
using XCode.Membership;

namespace XCode.Remoting
{
    /// <summary>带有用户信息的Api会话</summary>
    public abstract class ApiUserSession : ApiSession
    {
        #region 属性
        /// <summary>当前登录用户</summary>
        public IManageUser Current { get; set; }

        /// <summary>在线对象</summary>
        public IOnline Online { get; private set; }

        /// <summary>名称</summary>
        public String Name { get; set; }

        /// <summary>平台</summary>
        public String Agent { get; set; }

        /// <summary>系统</summary>
        public String OS { get; set; }

        /// <summary>类型</summary>
        public String Type { get; set; }

        /// <summary>版本</summary>
        public String Version { get; set; }

        /// <summary>验证Key</summary>
        public String AuthKey { get; set; }
        #endregion

        #region 登录注册
        /// <summary>检查登录，默认检查密码MD5散列，可继承修改</summary>
        /// <param name="user">用户名</param>
        /// <param name="pass">密码</param>
        /// <returns>返回要发给客户端的对象</returns>
        protected override Object CheckLogin(String user, String pass)
        {
            if (user.IsNullOrEmpty()) throw Error(3, "用户名不能为空");

            var dic = ControllerContext.Current?.Parameters?.ToNullable();
            if (dic != null)
            {
                Agent = dic["Agent"] + "";
                OS = dic["OS"] + "";
                Type = dic["Type"] + "";
                Version = dic["Version"] + "";
            }
            // 登录
            Name = user;

            CheckOnline(user);

            var msg = "登录 {0}/{1}".F(user, pass);
            WriteLog(msg);

            var ns = Session as NetSession;
            var flag = true;
            var act = "Login";
            try
            {
                Object rs = null;

                // 查找并登录，找不到用户是返回空，登录失败则抛出异常
                var u = CheckUser(user, pass);

                // 注册
                if (u == null)
                {
                    act = "Register";
                    u = Register(user, pass);
                    if (u == null) throw Error(3, user + " 禁止注册");

                    if (u.ID == 0) u.SaveRegister(ns);

                    rs = new { user = u.Name, pass = u.Password };
                }
                // 登录
                else
                {
                    if (!u.Enable) throw Error(4, user + " 已被禁用");

                    if (AuthKey.IsNullOrEmpty()) rs = new { Name = u + "" };
                    else rs = new { Name = u + "", Key = AuthKey };
                }

                //u.SaveLogin(ns);
                SaveLogin(u);

                // 当前设备
                Current = u;
                Session.UserState = u;

                var olt = Online;
                if (olt.UserID > 0 && olt.UserID != u.ID) SaveHistory("Logout", true, "=> " + u);
                olt.UserID = u.ID;
                olt.SaveAsync();

                // 销毁时
                ns.OnDisposed += (s, e) =>
                {
                    Online.Delete();

                    SaveHistory("Logout", true, null);
                };

                return rs;
            }
            catch (Exception ex)
            {
                msg += " " + ex?.GetTrue()?.Message;
                flag = false;
                throw;
            }
            finally
            {
                SaveHistory(act, flag, msg);
            }
        }

        /// <summary>登录或注册完成后，保存登录信息</summary>
        /// <param name="user"></param>
        protected virtual void SaveLogin(IManageUser user)
        {
            var ns = Session as NetSession;
            user.SaveLogin(ns);
        }

        /// <summary>生成密钥，默认密码加密密钥，可继承修改</summary>
        /// <returns></returns>
        protected override Byte[] GenerateKey(String user)
        {
            // 随机密钥
            var key = Key = Rand.NextBytes(8);

            WriteLog("生成密钥 {0}", key.ToHex());

            var tp = Current?.Password;
            if (!tp.IsNullOrEmpty()) key = key.RC4(tp.GetBytes());

            return key;
        }

        /// <summary>查找用户并登录，找不到用户是返回空，登录失败则抛出异常</summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        protected abstract IManageUser CheckUser(String user, String pass);

        /// <summary>注册，登录找不到用户时调用注册，返回空表示禁止注册</summary>
        /// <param name="user"></param>
        /// <param name="pass"></param>
        /// <returns></returns>
        protected abstract IManageUser Register(String user, String pass);
        #endregion

        #region 心跳
        /// <summary>心跳</summary>
        /// <returns></returns>
        protected override Object OnPing()
        {
            CheckOnline(Current + "");

            return base.OnPing();
        }
        #endregion

        #region 操作历史
        /// <summary>更新在线信息，登录前、心跳时 调用</summary>
        /// <param name="name"></param>
        protected virtual void CheckOnline(String name)
        {
            var ns = Session as NetSession;
            var u = Current;

            var olt = Online ?? CreateOnline(ns.ID);
            olt.Name = Agent;
            olt.Type = Type;
            olt.SessionID = ns.ID;
            olt.UpdateTime = DateTime.Now;

            if (olt != Online)
            {
                olt.CreateTime = DateTime.Now;
                olt.CreateIP = ns?.Remote?.Address + "";
            }

            if (u != null)
            {
                olt.UserID = u.ID;
                if (olt.Name.IsNullOrEmpty()) olt.Name = u + "";
            }
            olt.SaveAsync();

            Online = olt;
        }

        /// <summary>创建在线</summary>
        /// <param name="sessionid"></param>
        /// <returns></returns>
        protected abstract IOnline CreateOnline(Int32 sessionid);

        /// <summary>保存令牌操作历史</summary>
        /// <param name="action"></param>
        /// <param name="success"></param>
        /// <param name="content"></param>
        public virtual void SaveHistory(String action, Boolean success, String content)
        {
            var hi = CreateHistory();
            hi.Name = Agent;
            hi.Type = Type;

            var u = Current;
            var ot = Online;
            if (u != null)
            {
                if (hi.UserID == 0) hi.UserID = u.ID;
                if (hi.Name.IsNullOrEmpty()) hi.Name = u + "";
            }
            else if (ot != null)
            {
                if (hi.UserID == 0) hi.UserID = ot.UserID;
                //if (hi.Name.IsNullOrEmpty()) hi.Name = ot.Name;
            }
            //if (hi.CreateUserID == 0) hi.CreateUserID = hi.UserID;

            hi.Action = action;
            hi.Success = success;
            hi.Remark = content;
            hi.CreateTime = DateTime.Now;

            var sc = Session as NetSession;
            if (sc != null) hi.CreateIP = sc.Remote + "";

            hi.SaveAsync();
        }

        /// <summary>创建历史</summary>
        /// <returns></returns>
        protected abstract IHistory CreateHistory();
        #endregion
    }
}