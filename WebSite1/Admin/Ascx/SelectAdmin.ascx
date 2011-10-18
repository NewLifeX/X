<%@ Control Language="C#" AutoEventWireup="true" CodeFile="SelectAdmin.ascx.cs" Inherits="Admin_Ascx_SelectAdmin" %>
<XCL:ChooseButton ID="btn" Text='<%# Admin!=null?Admin.FriendName:"请选择" %>' Value='<%# Value %>'
    Url="../../Admin/System/SelectAdmin.aspx?ID={value}&Name={text}" ModalDialogOptions="dialogWidth:'750px',dialogHeight:'550px'"
    runat="server" />
