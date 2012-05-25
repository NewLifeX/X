<%@ Page Language="C#" AutoEventWireup="true" CodeFile="DropDownList.aspx.cs" Inherits="DropDownList" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <div style="margin-top: 1em;">
            <a href="?">未绑定数据</a> <a href="?badval=1">未绑定数据 值无效</a>
            <div style="margin-top: 1em;">
                值有效: <a href="?data=1">绑定数据 先赋值</a>&nbsp;<a href="?data=2">绑定数据 后赋值</a>
            </div>
            <div style="margin-top: 1em;">
                值无效: <a href="?data=1&badval=1">绑定数据 先赋值</a>&nbsp;<a href="?data=2&badval=1">绑定数据 后赋值</a>
            </div>
        </div>
        <div style="margin-top: 1em;">
            AppendDataBoundItems=false
            <br />
            <XCL:DropDownList runat="server" ID="DropDownList1">
                <asp:ListItem>请选择</asp:ListItem>
            </XCL:DropDownList>
            <XCL:DropDownList runat="server" ID="DropDownList2">
            </XCL:DropDownList>
        </div>
        <div style="margin-top: 1em;">
            AppendDataBoundItems=true
            <br />
            <XCL:DropDownList runat="server" ID="DropDownList3" AppendDataBoundItems="true">
                <asp:ListItem>请选择</asp:ListItem>
            </XCL:DropDownList>
            <XCL:DropDownList runat="server" ID="DropDownList4" AppendDataBoundItems="true">
            </XCL:DropDownList>
        </div>
        <div style="margin-top: 1em;">
            NoExceptionItem=true
            <div>
                AppendDataBoundItems=false
                <br />
                <XCL:DropDownList runat="server" ID="DropDownList5" NoExceptionItem="true">
                    <asp:ListItem>请选择</asp:ListItem>
                </XCL:DropDownList>
                <XCL:DropDownList runat="server" ID="DropDownList6" NoExceptionItem="true">
                </XCL:DropDownList>
            </div>
            <div>
                AppendDataBoundItems=true
                <br />
                <XCL:DropDownList runat="server" ID="DropDownList7" AppendDataBoundItems="true" NoExceptionItem="true">
                    <asp:ListItem>请选择</asp:ListItem>
                </XCL:DropDownList>
                <XCL:DropDownList runat="server" ID="DropDownList8" AppendDataBoundItems="true" NoExceptionItem="true">
                </XCL:DropDownList>
            </div>
        </div>
        <div style="margin-top: 1em;">
            通过Items.FindByValue(value).Selected = true方式赋值
            <p>
            将无法缓存到当前选中项的值
            </p>
            <XCL:DropDownList runat="server" ID="DropDownList9">
                <asp:ListItem>请选择</asp:ListItem>
            </XCL:DropDownList>
            <XCL:DropDownList runat="server" ID="DropDownList10" AppendDataBoundItems="true">
                <asp:ListItem>请选择</asp:ListItem>
            </XCL:DropDownList>
            <XCL:DropDownList runat="server" ID="DropDownList12" NoExceptionItem="true">
                <asp:ListItem>请选择</asp:ListItem>
            </XCL:DropDownList>
            <XCL:DropDownList runat="server" ID="DropDownList11" AppendDataBoundItems="true" NoExceptionItem="true">
                <asp:ListItem>请选择</asp:ListItem>
            </XCL:DropDownList>
        </div>
    </div>
    </form>
</body>
</html>