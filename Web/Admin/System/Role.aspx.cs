using System;
using NewLife.CommonEntity;
using NewLife.Web;

using Role = NewLife.CommonEntity.Role;

public partial class Pages_Role : PageBase
{
    protected void Page_Load(object sender, EventArgs e)
    {
        // 添加按钮需要添加权限
        btnAdd.Visible = Acquire(PermissionFlags.Insert);
        // 最后一列是删除列，需要删除权限
        GridView1.Columns[GridView1.Columns.Count - 1].Visible = Acquire(PermissionFlags.Delete);
        GridView1.Columns[GridView1.Columns.Count - 2].Visible = Acquire(PermissionFlags.Update);
    }
    protected void Button1_Click(object sender, EventArgs e)
    {
        if (!Acquire(PermissionFlags.Insert))
        {
            WebHelper.Alert("没有添加权限！");
            return;
        }

        Label_Info.Text = "";

        if (string.IsNullOrEmpty(TextBox_Name.Text))
        {
            Label_Info.Text = "角色名不能为空";
            TextBox_Name.Focus();
            return;
        }
        Role role = Role.Find(Role._.Name, TextBox_Name.Text);

        if (role != null)
        {
            Label_Info.Text = "已存在的角色名";
            TextBox_Name.Focus();
            return;
        }

        try
        {
            role = new Role();

            role.Name = TextBox_Name.Text;
            role.Insert();

            TextBox_Name.Text = "";


            //Label_Info.Text = "添加成功";
            WebHelper.Alert("添加成功！");

            //重新绑定数据
            GridView1.DataBind();
        }
        catch (Exception ex)
        {
            //异常发生时，要给出错误提示
            //Label_Info.Text = "添加失败！" + ex.Message;
            WebHelper.Alert("添加失败！" + ex.Message);
        }
    }
}