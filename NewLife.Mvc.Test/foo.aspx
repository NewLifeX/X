<%@ Page Language="C#" AutoEventWireup="true" CodeFile="foo.aspx.cs" Inherits="foo" %>

<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <div>
        <p>
            <h4>控制器和控制器工厂测试</h4>
            <a href="Test">/Test TestController</a>
            <a href="Test1">/Test1$ TestController1</a>
            <a href="Test1Foo">/Test1Foo TestController</a>
            <a href="Test2">/Test2 TestController2</a>
            <a href="Factory1">/Factory1 TestFactory</a>
            <a href="Error">/Error TestError</a>
            <a href="bar">/Bar 404</a>
        </p>
        <p>
            <h4>模块测试</h4>
            <a href="Module/fooFoo">/Module/fooFoo TestController</a>
            <a href="Module/foo">/Module/foo$ TestController</a>
            <a href="Module/f1">/Module/f TestFactory</a>
            <a href="Module/foo2Foo">/Module/foo2 TestController</a>
            <a href="Module/bar">bar 404</a>
        </p>
        <p>
            <h4>特殊控制器测试</h4>
            <p>
                需要在host中有<code>127.0.0.1    test.localhost</code>
            </p>
            <a href="<%= specUrl %>specFactory/foo">/foo$ TestModuleRoute TestController</a>
            <a href="<%= specUrl %>specFactory/foo123">/foo TestModuleRoute TestController</a>
            <a href="<%= specUrl %>specFactory/f123">/f TestModuleRoute TestController</a>
            <a href="<%= specUrl %>specFactory/foo2">/foo2 TestModuleRoute TestController</a>
            <a href="<%= specUrl %>specFactory/notfound">404</a>
        </p>
        <p>
            <h4>外链请求测试</h4>
            <img src="static.bmp" />
        </p>
    </div>
</body>
</html>
