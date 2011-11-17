<%@ Page Language="C#" AutoEventWireup="true" CodeFile="SimulateData.aspx.cs" Inherits="Admin_Center_SimulateData"
    Title="数据模拟" MasterPageFile="~/Admin/ManagerPage.master" %>

<asp:Content ID="content1" runat="server" ContentPlaceHolderID="C">
    <div>
        <div class="toolbar">
        <asp:Button ID="Button5" runat="server" onclick="Button5_Click" 
            Text="模拟二十个客户分类" />
&nbsp;
    
        <asp:Button ID="Button1" runat="server" Text="模拟一千客户" onclick="Button1_Click" />
&nbsp;
        <asp:Button ID="Button2" runat="server" Text="模拟五千液料规格" 
            onclick="Button2_Click" />
&nbsp;
        <asp:Button ID="Button3" runat="server" Text="模拟一万机器规格" 
            onclick="Button3_Click" />
&nbsp;
        <asp:Button ID="Button4" runat="server" Text="模拟十万维修记录" 
            onclick="Button4_Click" />
    </div>
</asp:Content>
