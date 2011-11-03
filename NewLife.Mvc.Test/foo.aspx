<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">

<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <div>
    <#=DateTime.Now.ToString() #>
        

        这是一个模版页
        <p>
        NewLife.Mvc.RouteContext.Current.RoutePath : <#= NewLife.Mvc.RouteContext.Current.RoutePath #>
        
        </p>
        <a href="Test">TestController</a>
        <a href="Test1">TestController1</a>
        <a href="Test2">TestController2</a>
        <img src="static.bmp" />
    </div>
</body>
</html>
