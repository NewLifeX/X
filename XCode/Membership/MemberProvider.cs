using System;
using NewLife;
using NewLife.Reflection;
using System.Collections.Generic;
using System.Text;
using System.Web.Security;
using NewLife.Model;
using XCode.DataAccessLayer;

namespace XCode.Membership
{
    /// <summary>XCode支持的用户权限提供者</summary>
    public class MemberProvider : MembershipProvider
    {
        #region 静态成员
        private static MembershipProvider _Provider;
        /// <summary>当前成员提供者</summary>
        public static MemberProvider Provider
        {
            get
            {
                if (_Provider == null) _Provider = ObjectContainer.Current.Resolve<MembershipProvider>();
                return _Provider as MemberProvider;
            }
        }

        private static Type _UserType;
        /// <summary>用户类型</summary>
        public static Type UserType { get { return _UserType; } set { _UserType = value; _Factory = null; } }

        private static IEntityOperate _Factory;
        /// <summary>用户实体类工厂</summary>
        private static IEntityOperate Factory { get { return _Factory ?? (_Factory = EntityFactory.CreateOperate(UserType)); } set { _Factory = value; } }

        /// <summary>当前登录用户</summary>
        public static IUser User
        {
            get
            {
                return UserType.GetValue("Current") as IUser;
            }
            set
            {
                UserType.SetValue("Current", value);
            }
        }

        static MemberProvider()
        {
            ObjectContainer.Current
                .AutoRegister<IUser, User>()
                .AutoRegister<IRole, Role>()
                .AutoRegister<IMenu, Menu>();

            UserType = ObjectContainer.Current.ResolveType<IUser>();

            // 设置默认连接字符串
            var eop = EntityFactory.CreateOperate(UserType);
            var name = eop.ConnName;
            if (!DAL.ConnStrs.ContainsKey(name)) DAL.AddConnStr(name, "Data Source=|DataDirectory|\\{0}.db".F(name), null, "SQLite");
        }
        #endregion

        #region 实体类扩展
        /// <summary>根据实体类接口获取实体工厂</summary>
        /// <typeparam name="TIEntity"></typeparam>
        /// <returns></returns>
        internal static IEntityOperate GetFactory<TIEntity>()
        {
            var type = ObjectContainer.Current.ResolveType<TIEntity>();
            if (type == null) return null;

            return EntityFactory.CreateOperate(type);
        }

        /// <summary>获取指定实体类的默认对象</summary>
        /// <typeparam name="T"></typeparam>
        /// <returns></returns>
        internal static T Get<T>()
        {
            var eop = GetFactory<T>();
            if (eop == null) return default(T);

            return (T)eop.Default;
        }
        #endregion

        #region 菜单
        private IMenu _MenuRoot;
        /// <summary>菜单根</summary>
        public virtual IMenu MenuRoot
        {
            get
            {
                if (_MenuRoot == null) _MenuRoot = GetFactory<IMenu>().EntityType.GetValue("Root") as IMenu;
                return _MenuRoot;
            }
        }

        /// <summary>根据编号找到菜单</summary>
        /// <param name="id"></param>
        /// <returns></returns>
        public IMenu FindByMenuID(Int32 id)
        {
            if (id < 1) return null;

            var eop = GetFactory<IMenu>();
            return eop.FindWithCache(eop.Unique.Name, id) as IMenu;
        }

        /// <summary>获取指定菜单下，当前用户有权访问的子菜单。</summary>
        /// <param name="menuid"></param>
        /// <returns></returns>
        public IList<IMenu> GetMySubMenus(Int32 menuid)
        {
            var root = MenuRoot;

            // 当前用户
            var admin = User as IUser;
            if (admin == null || admin.Role == null) return null;

            IMenu menu = null;

            // 找到菜单
            if (menuid > 0) menu = FindByMenuID(menuid);

            if (menu == null)
            {
                menu = root;
                if (menu == null || menu.Childs == null || menu.Childs.Count < 1) return null;
            }

            return menu.GetMySubMenus(admin.Role.Resources);
        }
        #endregion

        #region 接口属性
        private String _ApplicationName = "/";
        /// <summary>使用自定义成员资格提供程序的应用程序的名称</summary>
        public override String ApplicationName { get { return _ApplicationName; } set { _ApplicationName = value; } }

        /// <summary>指示成员资格提供程序是否配置为允许用户重置其密码</summary>
        public override bool EnablePasswordReset { get { return false; } }

        /// <summary>指示成员资格提供程序是否配置为允许用户检索其密码</summary>
        public override bool EnablePasswordRetrieval { get { return false; } }

        /// <summary>获取锁定成员资格用户前允许的无效密码或无效密码提示问题答案尝试次数</summary>
        public override int MaxInvalidPasswordAttempts { get { return 5; } }

        /// <summary>获取有效密码中必须包含的最少特殊字符数</summary>
        public override int MinRequiredNonAlphanumericCharacters { get { return 1; } }

        /// <summary>获取密码所要求的最小长度</summary>
        public override int MinRequiredPasswordLength { get { return 7; } }

        /// <summary>获取在锁定成员资格用户之前允许的最大无效密码或无效密码提示问题答案尝试次数的分钟数</summary>
        public override int PasswordAttemptWindow { get { return 10; } }

        /// <summary>获取一个值，该值指示在成员资格数据存储区中存储密码的格式</summary>
        public override MembershipPasswordFormat PasswordFormat { get { return MembershipPasswordFormat.Hashed; } }

        /// <summary>获取用于计算密码的正则表达式</summary>
        public override string PasswordStrengthRegularExpression { get { return ""; } }

        /// <summary>获取一个值，该值指示成员资格提供程序是否配置为要求用户在进行密码重置和检索时回答密码提示问题</summary>
        public override bool RequiresQuestionAndAnswer { get { return true; } }

        /// <summary>取一个值，指示成员资格提供程序是否配置为要求每个用户名具有唯一的电子邮件地址</summary>
        public override bool RequiresUniqueEmail { get { return true; } }
        #endregion

        #region 接口方法
        /// <summary>处理更新成员资格用户密码的请求</summary>
        /// <param name="username"></param>
        /// <param name="oldPassword"></param>
        /// <param name="newPassword"></param>
        /// <returns></returns>
        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        /// <summary>处理更新成员资格用户的密码提示问题和答案的请求</summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="newPasswordQuestion"></param>
        /// <param name="newPasswordAnswer"></param>
        /// <returns></returns>
        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }

        /// <summary>将新的成员资格用户添加到数据源</summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <param name="email"></param>
        /// <param name="passwordQuestion"></param>
        /// <param name="passwordAnswer"></param>
        /// <param name="isApproved"></param>
        /// <param name="providerUserKey"></param>
        /// <param name="status"></param>
        /// <returns></returns>
        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            throw new NotImplementedException();
        }

        /// <summary>从成员资格数据源删除一个用户</summary>
        /// <param name="username"></param>
        /// <param name="deleteAllRelatedData"></param>
        /// <returns></returns>
        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            throw new NotImplementedException();
        }

        /// <summary> 获取一个成员资格用户的集合，其中的电子邮件地址包含要匹配的指定电子邮件地址</summary>
        /// <param name="emailToMatch"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalRecords"></param>
        /// <returns></returns>
        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        /// <summary> 获取一个成员资格用户的集合，其中的用户名包含要匹配的指定用户名</summary>
        /// <param name="usernameToMatch"></param>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalRecords"></param>
        /// <returns></returns>
        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        /// <summary> 获取数据源中的所有用户的集合，并显示在数据页中</summary>
        /// <param name="pageIndex"></param>
        /// <param name="pageSize"></param>
        /// <param name="totalRecords"></param>
        /// <returns></returns>
        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        /// <summary>获取当前访问该应用程序的用户数</summary>
        /// <returns></returns>
        public override int GetNumberOfUsersOnline()
        {
            throw new NotImplementedException();
        }

        /// <summary>从数据源获取指定用户名所对应的密码</summary>
        /// <param name="username"></param>
        /// <param name="answer"></param>
        /// <returns></returns>
        public override string GetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        /// <summary>根据成员资格用户的唯一标识符从数据源获取用户信息。提供一个更新用户最近一次活动的日期/时间戳的选项</summary>
        /// <param name="username"></param>
        /// <param name="userIsOnline"></param>
        /// <returns></returns>
        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        /// <summary>从数据源获取用户的信息。提供一个更新用户最近一次活动的日期/时间戳的选项</summary>
        /// <param name="providerUserKey"></param>
        /// <param name="userIsOnline"></param>
        /// <returns></returns>
        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        /// <summary>获取与指定的电子邮件地址关联的用户名</summary>
        /// <param name="email"></param>
        /// <returns></returns>
        public override string GetUserNameByEmail(string email)
        {
            throw new NotImplementedException();
        }

        /// <summary>  将用户密码重置为一个自动生成的新密码</summary>
        /// <param name="username"></param>
        /// <param name="answer"></param>
        /// <returns></returns>
        public override string ResetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        /// <summary>清除锁定，以便可以验证该成员资格用户</summary>
        /// <param name="userName"></param>
        /// <returns></returns>
        public override bool UnlockUser(string userName)
        {
            throw new NotImplementedException();
        }

        /// <summary>更新数据源中有关用户的信息</summary>
        /// <param name="user"></param>
        public override void UpdateUser(MembershipUser user)
        {
            throw new NotImplementedException();
        }

        /// <summary>验证数据源中是否存在指定的用户名和密码</summary>
        /// <param name="username"></param>
        /// <param name="password"></param>
        /// <returns></returns>
        public override Boolean ValidateUser(string username, string password)
        {
            UserType.Invoke("Login", username, password);

            return true;
        }
        #endregion
    }
}