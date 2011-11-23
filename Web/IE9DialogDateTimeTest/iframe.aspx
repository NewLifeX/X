<%@ Page Language="C#" AutoEventWireup="true" CodeFile="iframe.aspx.cs" Inherits="IE9DialogDateTimeTest_iframe" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <form id="form1" runat="server">
    <div>
            <XCL:LinkBox runat="server" ID="LinkBox1" Url="Dialog.aspx" Text="使用XControl中封装的My97 DatePicker"></XCL:LinkBox>
            <br/>

            <XCL:LinkBox runat="server" ID="LinkBox2" Url="demo.htm" Text="使用静态的My97 DatePicker 4.7最终版"></XCL:LinkBox>如果点了上一个窗口请刷新整个窗口后再点这个

            <p>
            刷新内容帧后检查是否继续可用
            </p>
    </div>
    
    </form>
</body>
</html>
