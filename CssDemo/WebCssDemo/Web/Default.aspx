<%@ Page Title="主页" Language="C#" MasterPageFile="~/MasterPage.master" AutoEventWireup="true"
    CodeFile="Default.aspx.cs" Inherits="_Default" %>

<asp:Content ID="HeaderContent" runat="server" ContentPlaceHolderID="H">
    </asp:Content>
<asp:Content ID="BodyContent" runat="server" ContentPlaceHolderID="C">
    <h2>
        欢迎使用 ASP.NET!
    </h2>
    <p>
        若要了解关于 ASP.NET 的详细信息，请访问 <a href="http://www.asp.net/cn" title="ASP.NET 网站">www.asp.net/cn</a>。
    </p>
    <p>
        您还可以找到 <a href="http://go.microsoft.com/fwlink/?LinkID=152368" title="MSDN ASP.NET 文档">
            MSDN 上有关 ASP.NET 的文档</a>。
    </p>
    <div>
        <h1>
            测试ajax</h1>
        <div id="ajaxtest">
        </div>
    </div>
</asp:Content>
