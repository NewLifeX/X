using System;
using System.Collections.Generic;
using System.Text;
using System.Web.Security;

namespace XCode.Membership
{
    /// <summary>XCode支持的用户权限提供者</summary>
    public class MemberProvider : MembershipProvider
    {
        private String _ApplicationName;
        /// <summary>应用名称</summary>
        public override String ApplicationName { get { return _ApplicationName; } set { _ApplicationName = value; } }

        public override bool ChangePassword(string username, string oldPassword, string newPassword)
        {
            throw new NotImplementedException();
        }

        public override bool ChangePasswordQuestionAndAnswer(string username, string password, string newPasswordQuestion, string newPasswordAnswer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser CreateUser(string username, string password, string email, string passwordQuestion, string passwordAnswer, bool isApproved, object providerUserKey, out MembershipCreateStatus status)
        {
            throw new NotImplementedException();
        }

        public override bool DeleteUser(string username, bool deleteAllRelatedData)
        {
            throw new NotImplementedException();
        }

        /// <summary>指示成员资格提供程序是否配置为允许用户重置其密码</summary>
        public override bool EnablePasswordReset { get { return false; } }

        /// <summary>指示成员资格提供程序是否配置为允许用户检索其密码</summary>
        public override bool EnablePasswordRetrieval { get { return false; } }

        public override MembershipUserCollection FindUsersByEmail(string emailToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection FindUsersByName(string usernameToMatch, int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override MembershipUserCollection GetAllUsers(int pageIndex, int pageSize, out int totalRecords)
        {
            throw new NotImplementedException();
        }

        public override int GetNumberOfUsersOnline()
        {
            throw new NotImplementedException();
        }

        public override string GetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser GetUser(string username, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        public override MembershipUser GetUser(object providerUserKey, bool userIsOnline)
        {
            throw new NotImplementedException();
        }

        public override string GetUserNameByEmail(string email)
        {
            throw new NotImplementedException();
        }

        /// <summary>获取锁定成员资格用户前允许的无效密码或无效密码提示问题答案尝试次数</summary>
        public override int MaxInvalidPasswordAttempts { get { return 3; } }

        /// <summary>获取有效密码中必须包含的最少特殊字符数</summary>
        public override int MinRequiredNonAlphanumericCharacters { get { return 0; } }

        /// <summary>获取密码所要求的最小长度</summary>
        public override int MinRequiredPasswordLength { get { return 6; } }

        /// <summary>获取在锁定成员资格用户之前允许的最大无效密码或无效密码提示问题答案尝试次数的分钟数</summary>
        public override int PasswordAttemptWindow { get { return 5; } }

        /// <summary>获取一个值，该值指示在成员资格数据存储区中存储密码的格式</summary>
        public override MembershipPasswordFormat PasswordFormat { get { return MembershipPasswordFormat.Hashed; } }

        /// <summary>获取用于计算密码的正则表达式</summary>
        public override string PasswordStrengthRegularExpression
        {
            get { throw new NotImplementedException(); }
        }

        /// <summary>获取一个值，该值指示成员资格提供程序是否配置为要求用户在进行密码重置和检索时回答密码提示问题</summary>
        public override bool RequiresQuestionAndAnswer
        {
            get { throw new NotImplementedException(); }
        }

        public override bool RequiresUniqueEmail
        {
            get { throw new NotImplementedException(); }
        }

        public override string ResetPassword(string username, string answer)
        {
            throw new NotImplementedException();
        }

        public override bool UnlockUser(string userName)
        {
            throw new NotImplementedException();
        }

        public override void UpdateUser(MembershipUser user)
        {
            throw new NotImplementedException();
        }

        public override bool ValidateUser(string username, string password)
        {
            throw new NotImplementedException();
        }
    }
}