<%@ Page Language="C#" AutoEventWireup="true" CodeFile="InIframe.aspx.cs" Inherits="InIframe" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
    <a href="javascript:void 0" onclick="parent.Dialog.CloseSelfDialog(frameElement);">仅关闭</a>
    <hr/>
    <a href="javascript:void 0" onclick="parent.Dialog.CloseAndRefresh(frameElement);">关闭并刷新</a>
    </div>
    </form>
</body>
</html>
