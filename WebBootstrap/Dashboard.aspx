<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Dashboard.aspx.cs" Inherits="Dashboard" %>
<%@ Register Assembly="NewLife.Bootstrap" Namespace="NewLife.Bootstrap.Controls" TagPrefix="nbc" %>
<asp:Content ID="Content1" ContentPlaceHolderID="head" runat="server">
</asp:Content>
<asp:Content ID="Content2" ContentPlaceHolderID="ContentPlaceHolder1" runat="server">
    <br />
    <br />
    <br />
    <nbc:TabControl ID="TabControl1" runat="server">
        <TabPages>
            <nbc:TabPage ID="TabPage1" runat="server" Title="TabPage1">
            </nbc:TabPage>
            <nbc:TabPage ID="TabPage2" runat="server" Title="TabPage1">
            </nbc:TabPage>
            <nbc:TabPage ID="TabPage3" runat="server" Title="TabPage3">
            </nbc:TabPage>
        </TabPages>
    </nbc:TabControl>
    <br />
    <nbc:Breadcrumbs ID="Breadcrumbs1" runat="server">
        <Items>
            <nbc:ListItem ID="ListItem1" runat="server" Text="OS">
            </nbc:ListItem>
            <nbc:ListItem ID="ListItem2" runat="server" Text="Windows">
            </nbc:ListItem>
            <nbc:ListItem ID="ListItem3" runat="server" Text="Linux">
            </nbc:ListItem>
            <nbc:ListItem ID="ListItem4" runat="server" Text="MacOS">
            </nbc:ListItem>
        </Items>
    </nbc:Breadcrumbs>
    <br />
</asp:Content>
