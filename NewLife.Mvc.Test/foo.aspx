<!DOCTYPE html PUBLIC "-//W3C//DTD XHTML 1.0 Transitional//EN" "http://www.w3.org/TR/xhtml1/DTD/xhtml1-transitional.dtd">
<html xmlns="http://www.w3.org/1999/xhtml">
<head runat="server">
    <title></title>
</head>
<body>
    <div>
        <#=DateTime.Now.ToString() #> 这是一个模版页
        <pre>
        NewLife.Mvc.RouteContext.Current.RoutePath : <#= NewLife.Mvc.RouteContext.Current.RoutePath #>
        NewLife.Mvc.RouteContext.Current.Factory.ToString() : <#= NewLife.Mvc.RouteContext.Current.Factory.ToString() #>
        NewLife.Mvc.RouteContext.Current.Controller.ToString() : <#= NewLife.Mvc.RouteContext.Current.Controller.ToString() #>
        NewLife.Mvc.RouteContext.Current.Path : <#= NewLife.Mvc.RouteContext.Current.Path #>

        </pre>
        <p>
            <a href="Test">/Test TestController</a>
            <a href="Test1">/Test1$ TestController1</a>
            <a href="Test1Foo">/Test1Foo 404</a>
            <a href="Test2">/Test2 TestController2</a>
            <a href="Factory1">/Factory1 TestFactory</a>
            <a href="Error">/Error TestError</a>
        </p>
        <p>
            /Module<br />
            <a href="Module/fooFoo">/Module/foo TestController</a>
            <a href="Module/foo">/Module/foo$ TestController</a>
            <a href="Module/fooFoo">/Module/fooFoo TestController</a>
            <a href="Module/f1">/Module/f TestFactory</a>
            <a href="Module/foo2Foo">/Module/foo2 TestController</a>
        </p>
        <hr />
        <img src="static.bmp" />
    </div>
</body>
</html>
