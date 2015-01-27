<%@ Page Title="权限管理" Language="C#" MasterPageFile="~/Admin/ManagerPage.master" AutoEventWireup="true"
    CodeFile="RoleForm.aspx.cs" Inherits="Common_RoleForm" %>

<asp:Content ID="Content1" runat="server" ContentPlaceHolderID="C">
    <div class="row-filuid">
        <div class="widget-box">
            <div class="widget-content nopadding">
                <table class="table table-bordered table-striped">
                    <tr>
                        <th colspan="2">
                            权限
                        </th>
                    </tr>
                    <tr>
                        <td align="right">
                            角色名称：
                        </td>
                        <td>
                            <asp:TextBox ID="frmName" runat="server" Width="150px"></asp:TextBox>
                        </td>
                    </tr>
                </table>
            </div>
        </div>
    </div>
    <div class="row-filuid">
        <asp:Button ID="btnSave" runat="server" CausesValidation="True" Text='保存' />
        &nbsp;<asp:Button ID="btnCopy" runat="server" CausesValidation="True" Text='另存为新权限' />
        &nbsp;<asp:Button ID="btnReturn" runat="server" OnClientClick="parent.Dialog.CloseSelfDialog(frameElement);return false;"
            Text="返回" />
    </div>
</asp:Content>
