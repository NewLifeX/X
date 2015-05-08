using System;
using NewLife.CommonEntity;
using NewLife.Web;
using XCode;
using XCode.Membership;
using XCode.Web;

public partial class Pages_Log : MyEntityList
{
    public EntityGrid grid = new EntityGrid(Log.Meta.Factory);

    protected void Page_Load(object sender, EventArgs e)
    {
        //grid.DefaultPageSize = 10;
        grid.WhereMethod = "SearchWhere";
    }
}