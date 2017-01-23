## 魔方 NewLife.Cube
魔方 是一个基于 ASP.NET MVC 的 用户权限管理平台，可作为各种信息管理系统的基础框架。

演示：[http://cube.newlifex.com](http://cube.newlifex.com) [源码](https://git.newlifex.com/Stone/CubeDemo)  

源码： https://git.newlifex.com/NewLife/X/Tree/master/NewLife.Cube  
海外： https://github.com/NewLifeX/X/tree/master/NewLife.Cube  

---
### 特性
* 通用权限管理，用户、角色、菜单、权限，支持控制器Action权限控制
* 多数据库，支持 `SQLite / Sql Server / Oracle / MySql / SqlCe / Access` 
* 免部署，系统自动创建数据库表结构，以及初始化数据，无需人工干涉
* 强大的视图引擎，支持子项目视图重写父项目相同位置视图，任意覆盖修改默认界面

---
### 系统要求
* [IIS 7.0](http://www.iis.net/learn)
* [.NET Framework 4.5](http://www.microsoft.com/en-us/download/details.aspx?id=30653)
* [ASP.NET MVC 5](http://www.asp.net/mvc/tutorials/mvc-5)
* [SQLite](http://system.data.sqlite.org/index.html/doc/trunk/www/downloads.wiki) / Sql Server / Oracle / MySql / SqlCe / Access

---
### 安装
* 在 *Visual Studio* 中新建MVC5项目
* 通过 *NuGet* 引用`NewLife.Cube`，或自己编译最新的[X组件](https://git.newlifex.com/NewLife/X)源码
* 在`Web.config`的`<connectionStrings>`段设置名为`Membership`的连接字符串，用户角色权限菜单等存储在该数据库
* 系统自动识别数据库类型，默认`\<add name="Membership" connectionString="Data Source=~\App_Data\Membership.db" providerName="Sqlite"/>`
* 编译项目，项目上点击鼠标右键，`查看`，`在浏览器中查看`，运行魔方平台
* 系统为`SQLite`/`Oracle`/`MySql`/`SqlCe`数据库自动下载匹配（`x86/x64`）的数据库驱动文件，驱动下载地址可在`Config\Core.config`中修改`PluginServer`
* 系统自动下载脚本样式表等资源文件，下载地址可在`Config/Cube.config`中修改`PluginServer`
* 默认登录用户名是`admin`，密码是`admin`
* 推荐安装 *Visual Studio* 插件 *Razor Generator*，给`.cshtml`文件设置`自定义工具``RazorGenerator`，可以把`.cshtml`编译生成到`DLL`里面
* 项目发布时只需要拷贝`Bin`、`web.config`、`Global.asax`，以及其它自己添加的资源文件

---
### 教程
[【演示】教务系统](http://cube.newlifex.com)  
[【源码】教务系统](https://git.newlifex.com/Stone/CubeDemo)  

[【教程】魔方平台NewLife.Cube基础教程（附例程源码）](http://www.newlifex.com/showtopic-1483.aspx)  
[【教程】魔方平台NewLife.Cube模板结构详解](http://www.newlifex.com/showtopic-1491.aspx)  

