using System;
using NewLife.Linq;
using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Web;
using NewLife.Exceptions;
using NewLife.Web;

namespace NewLife.Net.Fetion
{
    /// <summary>Wap飞信</summary>
    /// <remarks>
    /// 参考博客园 <a href="http://www.cnblogs.com/youwang/archive/2012/01/07/2315933.html">小桥流水</a>
    /// </remarks>
    public class WapFetion : DisposeBase
    {
        #region 属性
        private String _Mobile;
        /// <summary>手机号码</summary>
        public String Mobile { get { return _Mobile; } set { _Mobile = value; } }

        private String _Password;
        /// <summary>密码</summary>
        public String Password { get { return _Password; } set { _Password = value; } }

        private Boolean hasLogined;

        private WebClientX _Client;
        /// <summary>客户端</summary>
        public WebClientX Client
        {
            get
            {
                if (_Client == null)
                {
                    var client = new WebClientX(false, true);
                    //client.Headers.Add("Content-Type", "application/x-www-form-urlencoded");
                    var result = client.DownloadString(server + "/im/");

                    _Client = client;
                }
                return _Client;
            }
            set { _Client = value; }
        }

        private Boolean _ShowResponse;
        /// <summary>是否显示响应。一般只用于调试</summary>
        public Boolean ShowResponse { get { return _ShowResponse; } set { _ShowResponse = value; } }
        #endregion

        #region 构造
        /// <summary>构造函数</summary>
        /// <param name="mobile">手机号码</param>
        /// <param name="password">密码</param>
        public WapFetion(String mobile, String password)
        {
            Mobile = mobile;
            //Password = password;
            Password = UrlEncode(password);
        }

        /// <summary>子类重载实现资源释放逻辑时必须首先调用基类方法</summary>
        /// <param name="disposing">从Dispose调用（释放所有资源）还是析构函数调用（释放非托管资源）</param>
        protected override void OnDispose(bool disposing)
        {
            base.OnDispose(disposing);

            if (hasLogined) Logout();
        }
        #endregion

        #region 发送数据
        String UrlEncode(String text) { return HttpUtility.UrlEncode(text, Encoding.UTF8); }

        const String server = "http://f.10086.cn";
        String Post(String uri, String data, params Object[] ps)
        {
            if (!hasLogined) Login();

            if (ps != null && ps.Length > 0) data = String.Format(data, ps);
            var d = Encoding.UTF8.GetBytes(data);
            Client.Headers[HttpRequestHeader.ContentType] = "application/x-www-form-urlencoded";
            if (ShowResponse) NetHelper.WriteLog("{0} {1}", uri, data);
            Client.Encoding = Encoding.UTF8;
            d = Client.UploadData(server + uri, d);
            var result = Encoding.UTF8.GetString(d);
            if (ShowResponse) NetHelper.WriteLog(result);
            return result;
        }

        String Get(String uri, params Object[] ps)
        {
            if (!hasLogined) Login();

            if (ps != null && ps.Length > 0) uri = String.Format(uri, ps);
            if (ShowResponse) NetHelper.WriteLog(uri);
            Client.Encoding = Encoding.UTF8;
            var result = Client.DownloadString(server + uri);
            if (ShowResponse) NetHelper.WriteLog(result);
            return result;
        }
        #endregion

        #region 登录退出
        /// <summary>登陆</summary>
        /// <returns></returns>
        public String Login()
        {
            if (hasLogined) return null;
            hasLogined = true;

            var result = Post("/im/login/inputpasssubmit1.action", "m={0}&pass={1}&loginstatus=1", Mobile, Password);
            if (result.Contains("失败")) throw new NetException("登录失败！");
            return result;
        }

        /// <summary>注销</summary>
        /// <returns></returns>
        public String Logout()
        {
            return Post("/im/index/logoutsubmit.action", "");
        }
        #endregion

        #region 发送消息
        /// <summary>通过手机号，给自己会好友发送消息</summary>
        /// <param name="mobile">手机号</param>
        /// <param name="message">消息</param>
        public void Send(String mobile, String message)
        {
            if (String.IsNullOrEmpty(message)) throw new ArgumentNullException("message");
            if (String.IsNullOrEmpty(mobile)) mobile = Mobile;

            if (mobile == Mobile)
                SendToMyself(message);
            else
            {
                Int32 uid = GetUid(mobile);
                if (uid != 0)
                    Send(uid, message);
                else
                    AddFriend(mobile);
            }
        }

        /// <summary>获取用户UID</summary>
        /// <param name="mobile"></param>
        /// <returns></returns>
        protected Int32 GetUid(String mobile)
        {
            // 在好友列表里面找
            var fs = _Friends;
            var f = fs == null ? null : fs.FirstOrDefault(e => e.Mobile == mobile);
            if (f != null) return f.ID;

            String uri = "/im/index/searchOtherInfoList.action";
            String data = "searchText=" + mobile;

            String result = Post(uri, data);
            Match mc = Regex.Match(result, @"toinputMsg\.action\?touserid=(\d+)");
            if (!mc.Success) return 0;

            var id = Int32.Parse(mc.Groups[1].Value);
            if (id > 0)
            {
                // 如果没有，就强制创建列表
                fs = _Friends ?? (_Friends = new List<Friend>());
                // 分别根据编号和手机号查找
                f = fs.FirstOrDefault(e => e.Mobile == mobile);
                if (f == null) f = fs.FirstOrDefault(e => e.Mobile == mobile);
                // 如果没有，创建一个
                if (f == null)
                {
                    f = new Friend();
                    f.Client = this;
                    fs.Add(f);
                }
                // 设置编号和手机号
                f.ID = id;
                f.Mobile = mobile;
            }

            return id;
        }

        /// <summary>发往目标UID</summary>
        /// <param name="uid"></param>
        /// <param name="message"></param>
        public void Send(Int32 uid, String message)
        {
            if (uid < 1) throw new ArgumentNullException("uid");
            if (String.IsNullOrEmpty(message)) throw new ArgumentNullException("message");

            String uri = "/im/chat/sendMsg.action?touserid=" + uid;
            String data = "msg=" + UrlEncode(message);
            String result = Post(uri, data);
            if (!result.Contains("成功")) throw new XException("发送失败！");
        }

        /// <summary>发送给自己</summary>
        /// <param name="message"></param>
        public void SendToMyself(String message)
        {
            if (String.IsNullOrEmpty(message)) throw new ArgumentNullException("message");

            String uri = "/im/user/sendMsgToMyselfs.action";
            String data = "msg=" + UrlEncode(message);
            String result = Post(uri, data);
            if (!result.Contains("成功")) throw new XException("发送失败！");
        }

        /// <summary>向非好友发送强制信息。经测试，很难成功，貌似必须同城用户才行</summary>
        /// <param name="uid"></param>
        /// <param name="message"></param>
        public void SendStranger(Int32 uid, String message)
        {
            if (uid < 1) throw new ArgumentNullException("uid");
            if (String.IsNullOrEmpty(message)) throw new ArgumentNullException("message");

            // 首先创建强制会话
            Get("/im/chat/toStrangers.action?touserid=" + uid);
            String uri = "/im/chat/sendStrangerMsg.action?touserid=" + uid;
            String data = "backUrl=&touchTextLength=&touchTitle=&msg=" + UrlEncode(message);
            String result = Post(uri, data);
            if (!result.Contains("成功")) throw new XException("发送失败！");
        }
        #endregion

        #region 添加好友
        /// <summary>添加好友</summary>
        /// <param name="mobile">对方手机号码</param>
        /// <param name="localName">本地名</param>
        /// <param name="nickName">我的昵称，让对方知道我是谁</param>
        public void AddFriend(String mobile, String localName = null, String nickName = null)
        {
            AddFriend(Int32.Parse(mobile), 0, localName, nickName);
        }

        /// <summary>添加好友</summary>
        /// <param name="number">手机号码或者飞信号码，由第二个参数决定</param>
        /// <param name="type">0：手机号；1：飞信号</param>
        /// <param name="localName">本地名</param>
        /// <param name="nickName">我的昵称，让对方知道我是谁</param>
        public void AddFriend(Int32 number, Int32 type = 0, String localName = null, String nickName = null)
        {
            if (number <= 0) throw new ArgumentNullException("number");
            if (String.IsNullOrEmpty(nickName)) nickName = Mobile.Substring(0, 5);

            if (!String.IsNullOrEmpty(localName) && localName.Length > 5) localName = localName.Substring(0, 5);
            if (!String.IsNullOrEmpty(nickName) && nickName.Length > 5) nickName = nickName.Substring(0, 5);

            if (!String.IsNullOrEmpty(localName)) localName = UrlEncode(localName);
            if (!String.IsNullOrEmpty(nickName)) nickName = UrlEncode(nickName);

            Post("/im/user/insertfriend1.action", "");
            Post("/im/user/insertfriend2.action", "type={0}&number={1}", type, number);
            Post("/im/user/insertfriend3.action", "buddylist=0&nickname=&type={0}&number={1}", type, number);
            Post("/im/user/insertfriendsubmit.action", "nickname={3}&buddylist=0&localName={2}&type={1}&number={0}", number, type, localName, nickName);
        }
        #endregion

        #region 好友列表
        private List<Friend> _Friends;
        /// <summary>好友列表</summary>
        public List<Friend> Friends { get { return _Friends ?? (_Friends = GetFriends()); } set { _Friends = value; } }

        static Regex reg_friends = new Regex("<img[^>]*alt=\"([^\"]*)\"[^>]*>\\s*(?:</img>)\\s*<a[^>]*/im/chat/toinputMsg\\.action\\?touserid=(\\d+)[^>]*>([^<]*)</a>\\s*([^<]*?)\\s*<br", RegexOptions.Compiled);
        static Regex reg_next = new Regex("href=\"([^\"]*)\"[^>]*>下一页</a>", RegexOptions.Compiled);
        List<Friend> GetFriends()
        {
            var list = new List<Friend>();

            String uri = "/im/index/index.action?type=all";
            while (true)
            {
                var rs = Get(uri);

                var ms = reg_friends.Matches(rs);
                if (ms == null) break;

                foreach (Match item in ms)
                {
                    var entity = new Friend();
                    entity.ID = Int32.Parse(item.Groups[2].Value);
                    entity.Name = item.Groups[3].Value;
                    entity.Status = item.Groups[1].Value;
                    entity.Description = item.Groups[4].Value;

                    // 最后设置这个，否则，给Name赋值的时候，会引发一系列反应
                    entity.Client = this;

                    list.Add(entity);
                }

                // 获取下一页
                var m = reg_next.Match(rs);
                if (!m.Success) break;

                uri = m.Groups[1].Value;
                if (String.IsNullOrEmpty(uri)) break;
                uri = HttpUtility.HtmlDecode(uri);
            }

            return list;
        }

        /// <summary>更新本地名</summary>
        /// <param name="uid"></param>
        /// <param name="localName"></param>
        public void UpdateLocalName(Int32 uid, String localName)
        {
            if (uid < 1) throw new ArgumentNullException("uid");
            //if (String.IsNullOrEmpty(localname)) throw new ArgumentNullException("localname");

            Post("/im/user/updateLocalnames.action", String.Format("touserid={0}&localName={1}", uid, localName));
        }

        /// <summary>获取指定用户的手机号</summary>
        /// <param name="uid"></param>
        /// <returns></returns>
        public String GetMobile(Int32 uid)
        {
            if (uid < 1) throw new ArgumentNullException("uid");

            var rs = Get("/im/user/userinfoByuserid.action?touserid=" + uid);
            if (String.IsNullOrEmpty(rs)) return null;

            var reg = new Regex("手机号码:(\\d{11})", RegexOptions.Compiled);
            var m = reg.Match(rs);
            if (!m.Success) return null;

            return m.Groups[1].Value;
        }

        #region 好友
        /// <summary>好友</summary>
        public class Friend
        {
            private WapFetion _Client;
            /// <summary>客户端</summary>
            internal WapFetion Client { get { return _Client; } set { _Client = value; } }

            private Int32 _ID;
            /// <summary>编号</summary>
            public Int32 ID { get { return _ID; } set { _ID = value; } }

            private String _Name;
            /// <summary>名称</summary>
            public String Name { get { return _Name; } set { _Name = value; if (Client != null)Client.UpdateLocalName(ID, Name); } }

            private String _Mobile;
            /// <summary>号码</summary>
            public String Mobile { get { return _Mobile; } set { _Mobile = value; } }

            private String _Status;
            /// <summary>状态</summary>
            public String Status { get { return _Status; } set { _Status = value; } }

            private String _Description;
            /// <summary>描述</summary>
            public String Description { get { return _Description; } set { _Description = value; } }

            /// <summary>刷新信息，目前主要获取手机号</summary>
            public void Refresh()
            {
                if (Client != null) Mobile = Client.GetMobile(ID);
            }

            /// <summary>已重载。</summary>
            /// <returns></returns>
            public override string ToString()
            {
                var m = Mobile;
                if (String.IsNullOrEmpty(m)) m = ID.ToString();
                return String.Format("[{0}]{1}({2}) {3}", Status, Name, m, Description);
            }
        }
        #endregion
        #endregion
    }
}