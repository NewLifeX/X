<%@ Page Title="菜单管理" Language="C#" MasterPageFile="~/Admin/MasterPage.master" AutoEventWireup="true" CodeFile="MenuForm.aspx.cs" Inherits="MenuForm" %>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <div class="row-filuid">
        <div class="widget-box">
            <div class="widget-content nopadding">
                <table class="table table-bordered table-striped">
                    <tr>
                        <th colspan="2">菜单</th>
                    </tr>
                    <tr>
                        <td align="right">名称：</td>
                        <td>
                            <asp:TextBox ID="frmName" runat="server" Width="150px"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td align="right">父编号：</td>
                        <td>
                            <XCL:NumberBox ID="frmParentID" runat="server" Width="80px"></XCL:NumberBox></td>
                    </tr>
                    <tr>
                        <td align="right">链接：</td>
                        <td>
                            <asp:TextBox ID="frmUrl" runat="server" Width="300px"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td align="right">序号：</td>
                        <td>
                            <XCL:NumberBox ID="frmSort" runat="server" Width="80px"></XCL:NumberBox></td>
                    </tr>
                    <tr>
                        <td align="right">备注：</td>
                        <td>
                            <asp:TextBox ID="frmRemark" runat="server" TextMode="MultiLine" Width="300px" Height="80px"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td align="right">权限：</td>
                        <td>
                            <asp:TextBox ID="frmPermission" runat="server" Width="150px"></asp:TextBox></td>
                    </tr>
                    <tr>
                        <td align="right">是否显示：</td>
                        <td>
                            <asp:CheckBox ID="frmIsShow" runat="server" Text="是否显示" /></td>
                    </tr>
                </table>
            </div>
        </div>
    </div>
    <div class="row-filuid">
        <asp:Button ID="btnSave" runat="server" CausesValidation="True" Text='保存' CssClass="RealSave" />
        &nbsp;<asp:Button ID="btnCopy" runat="server" CausesValidation="True" Text='另存为新菜单' CssClass="CopySave" />
    </div>
</asp:Content>
