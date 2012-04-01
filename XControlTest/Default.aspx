<%@ Page Language="C#" AutoEventWireup="true" CodeFile="Default.aspx.cs" Inherits="_Default" %>

<%@ Register assembly="XControl" namespace="XControl" tagprefix="XCL" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
    <script type="text/javascript" language="javascript">
        function modelReturn(e) {
            window.returnValue = "000|||测试模态窗口返回值";
            window.close();
        }
    
    </script>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    
        <XCL:DropDownList ID="DropDownList1" runat="server" AppendDataBoundItems="True">
        </XCL:DropDownList>
        <asp:Button ID="Button1" runat="server" onclick="Button1_Click" Text="Button" />
        <a href="javascript:modelReturn.call(this,event)">测试模态窗口返回值</a>
    </div>
    </form>
</body>
</html>
