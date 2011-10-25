<%@ Page Language="C#" AutoEventWireup="true" CodeFile="WebFormEntityAccessorTest.aspx.cs"
    Inherits="WebFormEntityAccessorTest" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        Name:
        <asp:TextBox ID="fieldName" runat="server"></asp:TextBox>
        <br />
        <br />
        DisplayName:
        <asp:Label ID="fieldDisplayName" runat="server" Text="Label"></asp:Label>
        <br />
        <br />
        RoleName:
        <asp:RadioButton ID="fieldRoleName" runat="server" GroupName="g1" Text="普通用户" />
        <asp:RadioButton ID="fieldRoleName2" runat="server" GroupName="g1" Text="管理员" />
        <br />
        <br />
        IsEnable:<asp:CheckBox ID="fieldIsEnable" runat="server" />
        <asp:CheckBox ID="CheckBox2" runat="server" />
        <br />
        <br />
        ID:<asp:CheckBoxList ID="fieldRoleID" runat="server">
            <asp:ListItem>1</asp:ListItem>
            <asp:ListItem>2</asp:ListItem>
            <asp:ListItem>3</asp:ListItem>
            <asp:ListItem>4</asp:ListItem>
            <asp:ListItem>5</asp:ListItem>
            <asp:ListItem>6</asp:ListItem>
            <asp:ListItem>7</asp:ListItem>
            <asp:ListItem>8</asp:ListItem>
            <asp:ListItem>9</asp:ListItem>
        </asp:CheckBoxList>
        <br />
        Logins:<asp:DropDownList ID="fieldLogins" runat="server">
            <asp:ListItem>1</asp:ListItem>
            <asp:ListItem>2</asp:ListItem>
            <asp:ListItem>3</asp:ListItem>
            <asp:ListItem>4</asp:ListItem>
            <asp:ListItem>5</asp:ListItem>
            <asp:ListItem>6</asp:ListItem>
            <asp:ListItem>7</asp:ListItem>
            <asp:ListItem>8</asp:ListItem>
            <asp:ListItem>9</asp:ListItem>
            <asp:ListItem>10</asp:ListItem>
            <asp:ListItem>11</asp:ListItem>
        </asp:DropDownList>
        <br />
        <br />
        SSOUserID:<asp:RadioButtonList ID="fieldSSOUserID" runat="server">
            <asp:ListItem>1</asp:ListItem>
            <asp:ListItem>2</asp:ListItem>
            <asp:ListItem>3</asp:ListItem>
            <asp:ListItem>4</asp:ListItem>
            <asp:ListItem>5</asp:ListItem>
            <asp:ListItem>6</asp:ListItem>
            <asp:ListItem>7</asp:ListItem>
            <asp:ListItem>8</asp:ListItem>
            <asp:ListItem>9</asp:ListItem>
            <asp:ListItem>10</asp:ListItem>
            <asp:ListItem>11</asp:ListItem>
        </asp:RadioButtonList>
        <br />
        <br />
        Password:<XCL:DropDownList ID="fieldPassword" runat="server">
            <asp:ListItem>1</asp:ListItem>
            <asp:ListItem>2</asp:ListItem>
            <asp:ListItem>3</asp:ListItem>
            <asp:ListItem>4</asp:ListItem>
            <asp:ListItem>5</asp:ListItem>
            <asp:ListItem>6</asp:ListItem>
            <asp:ListItem>7</asp:ListItem>
            <asp:ListItem>8</asp:ListItem>
            <asp:ListItem>9</asp:ListItem>
            <asp:ListItem>10</asp:ListItem>
            <asp:ListItem>11</asp:ListItem>
        </XCL:DropDownList>
        <br />
        <br />
        LastLogin:<XCL:DateTimePicker ID="fieldLastLogin" runat="server"></XCL:DateTimePicker>
        <br />
        <br />
        <asp:Button ID="fieldID" runat="server" Text="Button" 
            onclick="fieldID_Click" />
    </div>
    </form>
</body>
</html>
