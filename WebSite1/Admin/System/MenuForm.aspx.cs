using System;
using System.Web.UI;
using System.Web.UI.WebControls;
using NewLife.Web;

using Menu = NewLife.CommonEntity.Menu;
using NewLife.CommonEntity;
using XCode;
using NewLife;

public partial class Pages_MenuForm : MyEntityForm
{
    /// <summary>实体类型</summary>
    public override Type EntityType { get { return CommonManageProvider.Provider.MenuType; } set { base.EntityType = value; } }

    protected override void OnPreLoad(EventArgs e)
    {
        if (!Page.IsPostBack)
        {
            // 在OnPreLoad之前初始化父菜单列表，因为EntityForm会在OnPreLoad阶段给表单赋值
            frmParentID.Items.Add(new ListItem("|-根菜单", "0"));
            foreach (IMenu item in CommonManageProvider.Provider.MenuRoot.AllChilds)
            {
                String spaces = new String('　', item.Deepth);
                frmParentID.Items.Add(new ListItem(spaces + "|- " + item.Name, item.ID.ToString()));
            }
        }

        base.OnPreLoad(e);
    }

    protected void Page_Load(object sender, EventArgs e)
    {
    }
}