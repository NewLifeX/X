<%@ Page Language="C#" AutoEventWireup="true" CodeFile="WebConfig.aspx.cs" Inherits="Admin_System_WebConfig"
    Title="网站配置" MasterPageFile="~/Admin/ManagerPage.master" EnableEventValidation="false" ValidateRequest="false" %>

<asp:Content ID="Content1" ContentPlaceHolderID="C" runat="server">
    <div class="toolbar">
        &nbsp;<asp:Button ID="Button1" runat="server" Text="保存" OnClick="Button1_Click" 
            onclientclick="return confirm('直接修改配置文件将可能导致网站出错！确定保存？');" />
    </div>
    <asp:TextBox ID="txtLog" runat="server" Height="600px" TextMode="MultiLine" Width="1000px"></asp:TextBox>
</asp:Content>
