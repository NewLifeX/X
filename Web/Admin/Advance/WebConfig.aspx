<%@ Page Language="C#" AutoEventWireup="true" CodeFile="WebConfig.aspx.cs" Inherits="Admin_System_WebConfig"
    Title="网站配置" MasterPageFile="~/Admin/ManagerPage.master" EnableEventValidation="false" ValidateRequest="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="C" runat="server">
    <div class="form-group col-sm-12 text-center">
        <asp:TextBox ID="txtLog" runat="server" Height="600px" Style="margin-left: 20px;" CssClass="form-control glyphicon-text-width" TextMode="MultiLine" Width="90%"></asp:TextBox>
    </div>
    <div class="form-group col-sm-12 text-center">
        <asp:Button ID="Button1" CssClass="btn btn-primary" runat="server" Text="保存" OnClick="Button1_Click"
            OnClientClick="return confirm('直接修改配置文件将可能导致网站出错！确定保存？');" />
    </div>
</asp:Content>
