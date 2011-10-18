using System;
using System.Collections.Generic;
using System.Text;
using NewLife.CommonEntity;

namespace NewLife.YWS.Entities
{
    public class YWSManageProvider : CommonManageProvider<Admin>
    {
        public override IAdministrator Current
        {
            get
            {
                return Admin.Current;
            }
        }

        public override IAdministrator FindByID(object userid)
        {
            return Admin.FindByID((Int32)userid);
        }

        public override IAdministrator FindByAccount(string account)
        {
            return Admin.FindByName(account);
        }

        public override IAdministrator Login(string account, string password)
        {
            return Admin.Login(account, password);
        }
    }
}