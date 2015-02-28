<%@ Page Language="C#" AutoEventWireup="true" CodeFile="WebLog.aspx.cs" Inherits="Admin_System_WebLog"
    Title="网站日志" MasterPageFile="~/Admin/ManagerPage.master" EnableEventValidation="false" ValidateRequest="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="C" runat="server">
    <div class="row-fluid navbar navbar-default navbar-form">
        日志文件：
        <asp:DropDownList ID="DropDownList1" runat="server">
        </asp:DropDownList>
        &nbsp;<asp:Button ID="Button1" runat="server" Text="查看" OnClick="Button1_Click" />
    </div>
    <asp:TextBox ID="txtLog" runat="server" Height="600px" TextMode="MultiLine" Width="1000px"></asp:TextBox>
</asp:Content>
