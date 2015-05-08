using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.CommonEntity;
using System.Reflection;
using NewLife.Reflection;
using NewLife;
using NewLife.Log;
using XCode.Membership;

public partial class Pages_MenuForm : MyEntityForm
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return ManageProvider.Menu.Root.GetType(); } set { base.EntityType = value; } }

    protected void Page_Load(object sender, EventArgs e)
    {
    }
}