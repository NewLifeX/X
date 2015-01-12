using System;
using System.Diagnostics;
using NewLife.CommonEntity;
using XCode.Accessors;

public partial class WebFormEntityAccessorTest : System.Web.UI.Page
{
    protected void Page_Load(object sender, EventArgs e)
    {
        if (!IsPostBack)
        {
            IEntityAccessor accessor = EntityAccessorFactory.Create(EntityAccessorTypes.WebForm);

            Administrator admin = Administrator.FindAll()[0];

            accessor
                .SetConfig(EntityAccessorOptions.AllFields, true)
                .SetConfig(EntityAccessorOptions.ItemPrefix, "field")
                .SetConfig(EntityAccessorOptions.Container, this)
                .Write(admin, null);


            Administrator admin2 = new Administrator();
            accessor.Read(admin2, null);


            Debug.Assert(admin.ID == admin2.ID);
            Debug.Assert(admin.Name == admin2.Name);
            Debug.Assert(admin2.DisplayName == null); // Label只能写不能读
            Debug.Assert(admin2.FriendName == admin2.FriendName);
            Debug.Assert(admin.Logins == admin2.Logins);
            Debug.Assert(admin.IsEnable == admin2.IsEnable);
            Debug.Assert(Math.Abs((admin.LastLogin - admin2.LastLogin).Ticks) < TimeSpan.FromSeconds(1).Ticks);
            Debug.Assert(admin.RoleID == admin2.RoleID);
            Debug.Assert(admin.RoleName == admin2.RoleName);
            //Debug.Assert(admin.SSOUserID == admin2.SSOUserID);
        }
    }
    protected void fieldID_Click(object sender, EventArgs e)
    {
        IEntityAccessor accessor = EntityAccessorFactory.Create(EntityAccessorTypes.WebForm);
        Administrator admin2 = new Administrator();
        accessor
            .SetConfig(EntityAccessorOptions.AllFields, true)
            .SetConfig(EntityAccessorOptions.ItemPrefix, "field")
            .SetConfig(EntityAccessorOptions.Container, this)
            .Read(admin2, null);


        Debug.Assert(admin2.Name != null);
        Debug.Assert(admin2.DisplayName == null); // Label只能写不能读
        Debug.Assert(admin2.RoleName != null);
        Debug.Assert(admin2.RoleID != 0);
        Debug.Assert(admin2.Logins > 0);
        Debug.Assert(admin2.Password != null);
        Debug.Assert(admin2.LastLogin - DateTime.MinValue > TimeSpan.FromDays(1));
        Debug.Assert(admin2.ID != 0);

    }
}