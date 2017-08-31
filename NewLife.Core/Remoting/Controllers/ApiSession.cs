using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using NewLife.Data;
using NewLife.Net;
using NewLife.Security;

namespace NewLife.Remoting
{
    /// <summary>Api会话</summary>
    [Api(null, true)]
    public class ApiSession : DisposeBase, IApi, IUserSession
    {
        #region 属性
        /// <summary>会话</summary>
        public IApiSession Session { get; set; }

        /// <summary>通信密钥</summary>
        protected Byte[] Key { get; set; }

        /// <summary>是否已登录</summary>
        public Boolean Logined { get; set; }
        #endregion

        #region 构造
        /// <summary>销毁时，从集合里面删除令牌</summary>
        /// <param name="disposing"></param>
        protected override void OnDispose(Boolean disposing)
        {
            base.OnDispose(disposing);

            Session.TryDispose();
        }
        #endregion

        #region 主要方法
        /// <summary>为加解密过滤器提供会话密钥</summary>
        /// <param name="context"></param>
        /// <returns></returns>
        internal static Byte[] GetKey(FilterContext context)
        {
            var ctx = context as ApiFilterContext;
            var ss = ctx?.Session?.UserSession as ApiSession;
            if (ss == null) return null;

            return ss.Key;
        }
        #endregion

        #region 异常处理
        /// <summary>抛出异常</summary>
        /// <param name="errCode"></param>
        /// <param name="msg"></param>
        /// <returns></returns>
        protected ApiException Error(Int32 errCode, String msg) { return new ApiException(errCode, msg); }
        #endregion

        #region 登录
        /// <summary>收到登录请求</summary>
        /// <param name="user">用户名</param>
        /// <param name="pass">密码</param>
        /// <returns></returns>
        [Api("Login")]
        [AllowAnonymous]
        protected virtual Object OnLogin(String user, String pass)
        {
            if (user.IsNullOrEmpty()) throw Error(3, "用户名不能为空");

            WriteLog("登录 {0}/{1}", user, pass);

            // 注册与登录
            var rs = CheckLogin(user, pass);

            // 可能是注册
            var dic = rs.ToDictionary();
            if (dic.ContainsKey(nameof(user))) user = dic[nameof(user)] + "";
            //if (dic.ContainsKey(nameof(pass))) pass = dic[nameof(pass)] + "";

            // 登录会话
            Session.UserSession = this;
            if (Session.UserState == null) Session.UserState = rs;
            Logined = true;

            // 生成密钥
            if (!dic.ContainsKey("Key")) dic["Key"] = GenerateKey(user).ToHex();

            return dic;
        }

        /// <summary>检查登录，默认检查密码，可继承修改</summary>
        /// <param name="user">用户名</param>
        /// <param name="pass">密码</param>
        /// <returns>返回要发给客户端的对象</returns>
        protected virtual Object CheckLogin(String user, String pass)
        {
            if (pass != user) throw Error(0x01, "密码错误！");

            return new { Name = user };
        }

        /// <summary>生成密钥，默认密码加密密钥，可继承修改</summary>
        /// <returns></returns>
        protected virtual Byte[] GenerateKey(String user)
        {
            // 随机密钥
            var key = Key = Rand.NextBytes(8);

            WriteLog("生成密钥 {0}", key.ToHex());

            var tp = user;
            if (!tp.IsNullOrEmpty()) key = key.RC4(tp.GetBytes());

            return key;
        }

        /// <summary>注销</summary>
        /// <returns></returns>
        protected virtual Object OnLogout()
        {
            Logined = false;
            Session.UserState = null;

            return null;
        }
        #endregion

        #region 心跳
        /// <summary>心跳</summary>
        /// <returns></returns>
        [Api("Ping")]
        protected virtual Object OnPing()
        {
            WriteLog("心跳 ");

            var dic = ControllerContext.Current.Parameters;
            // 返回服务器时间
            dic["ServerTime"] = DateTime.Now;
            dic["ServerSeconds"] = (Int32)(DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;

            return dic;
        }
        #endregion

        #region 远程调用
        /// <summary>远程调用</summary>
        /// <example>
        /// <code>
        /// client.InvokeAsync("GetDeviceCount");
        /// var rs = client.InvokeAsync("GetDeviceInfo", 2, 5, 9);
        /// var di = rs.Result[0].Value;
        /// </code>
        /// </example>
        /// <param name="action"></param>
        /// <param name="args"></param>
        /// <param name="cookie">附加参数，位于顶级</param>
        /// <returns></returns>
        public virtual async Task<TResult> InvokeAsync<TResult>(String action, Object args = null, IDictionary<String, Object> cookie = null)
        {
            return await Session.InvokeAsync<TResult>(action, args, cookie);
        }
        #endregion

        #region 辅助
        private String _prefix;

        /// <summary>写日志</summary>
        /// <param name="format"></param>
        /// <param name="args"></param>
        public void WriteLog(String format, params Object[] args)
        {
            var ns = Session as NetSession;
            if (_prefix == null)
            {
                var type = GetType();
                _prefix = "{0}[{1}] ".F(type.GetDisplayName() ?? type.Name.TrimEnd("Session"), ns.ID);
                ns.LogPrefix = _prefix;
            }

            ns.WriteLog(Session["Name"] + " " + format, args);
        }
        #endregion
    }
}