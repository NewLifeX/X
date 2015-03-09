using System;
using NewLife.CommonEntity;
using NewLife.Web;
using XCode;
using XCode.Web;

public partial class Pages_Log : MyEntityList
{
    public Grid grid = new Grid(Log.Meta.Factory);

    protected void Page_Load(object sender, EventArgs e)
    {
        grid.DefaultPageSize = 10;
        grid.WhereMethod = "SearchWhere";
    }
}