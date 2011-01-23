<%@ Page Language="C#" AutoEventWireup="true" CodeFile="SelectArea.aspx.cs" Inherits="window_SelectArea" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <base target="_self" />
    <script type="text/javascript" language="javascript">
        function $(id) {
            return document.getElementById(id);
        }

        function onsel() {
            //debugger;
            var ret = $('<%= DropDownList2.ClientID %>').value + '|||' + $('<%= DropDownList2.ClientID %>').text;
            alert(ret);
            window.returnValue = ret;
            window.close();
        }
    </script>
</head>
<body>
    <form id="form1" runat="server">
    <div>
        <table style="width: 100%;">
            <tr>
                <td align="right" style="width: 20%">
                    省：
                </td>
                <td style="width: 80%">
                    &nbsp;
                    <asp:DropDownList ID="DropDownList1" runat="server" AutoPostBack="True" OnSelectedIndexChanged="DropDownList1_SelectedIndexChanged">
                    </asp:DropDownList>
                </td>
            </tr>
            <tr>
                <td align="right">
                    市：
                </td>
                <td>
                    &nbsp;
                    <asp:DropDownList ID="DropDownList2" runat="server">
                        <asp:ListItem Text="--请选择--"></asp:ListItem>
                    </asp:DropDownList>
                </td>
            </tr>
            <tr>
                <td colspan="2" align="center">
                    <input id="Button1" type="button" value="确定" onclick="onsel();" />
                    <input id="Button2" type="button" value="取消" onclick="window.close();" />
                </td>
            </tr>
        </table>
    </div>
    </form>
</body>
</html>
