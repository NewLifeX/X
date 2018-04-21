![XCode](http://www.newlifex.com/templates/green/images/logo.png)
## XCode ORM是什么?
XCode是一个超越了ORM范围的映射组件，除了对象到关系数据库的映射外，还有到网络和其它二进制形式等的映射，所以称之为数据映射组件。

## XCode具有哪些优势？

* 采用最好的分页算法，高效处理海量数据。  
数据分页的思想贯穿整个XCode的生命周期，任何一个不论大小的测试，数据样本都是单表一千万起。其它很多ORM，在数据达到百万之后会变慢，达到千万后基本上就卡死了。XCode同时支持最流行的几大数据库分页算法，经过10多年的经验积累，根据数据库类型、版本以及所处环境，自动选择最佳的分页算法。当前很多其它ORM要么仅支持一两种数据库的分页算法，要么采用的分页算法具有极大的局限性，要求所设计的数据表必须具有某种特征。
* 先进的多级缓存思想，保证最出色的性能。  
XCode在最重要的数据管道主线上拥有三级缓存设计，在其它地方拥有无数的缓存设计。缓存的设计，让使用XCode开发的系统在性能上能够轻易超越采用原生数据库操作开发的系统，甚至有成千上万倍的差距！几乎所有的ORM都基于原生数据库操作，理论上会比原生数据库操作要慢一点，只有极少数使用了缓存的ORM会比原生数据库操作要快一些，但远没有XCode这么多的缓存设计。
* 化繁为简，容易上手。  
最流行的Hibernate、EF等ORM框架，追求完美的支持所有功能，庞大无比，让很多使用者望而却步。而XCode的核心思想是让一切变得简单，变得更简单！甚至连多表关联查询都不支持，而建议分为多次单表查询。也正因为化繁为简，使得XCode能够采用更多的缓存，化繁为简与缓存思想互相促进，甚至可以让多次单表查询远快于单次多表关联查询。
* 抽象实体结构，支持多数据库的正向反向工程。  
绝大多数ORM只负责对象与关系数据库的数据映射，而少有数据结构的映射。正向功能能够取得数据库结构，便于代码生成器使用；反向工程能够根据实体结构创建数据表、修改数据表结构、删除数据表或字段等，所以采用XCode开发的系统，发布的时候都可以不需要附带初始数据库。正向工程反向工程均是XCode提出的概念，在08年之前，除了XCode外没有任何其它ORM支持反向工程。也正是因为实体结构映射这一设计，使得XCode超越ORM，发展成为可以把实体对象映射到其它非数据库的形式。
* 分布式支持。  
尽管XCode采用了最好的分页算法，但对于大型系统甚至超级系统来说，单表数千万乃至数亿的数据是远远不能满足要求的。不管从数据存储还是从性能瓶颈的角度来考虑，分布式是必然趋势！XCode原生支持分布式设计。单表拆成多表，拆分到不同数据库、不同数据库服务器，XCode能够完全屏蔽数据层，使用起来就跟一张超级大表一样。其它绝大多数ORM不会对分布式实行原生支持，需要分布式数据库或者极其昂贵的硬件来配合才能做到。
* 总的来说，最好的分页算法保证基本操作不犯错，化繁为简和多级缓存的设计保证XCode在性能上永无止境，所以，XCode的最大优势就是性能！

## 教程

### 打开Visual Studio，新建一个控制台应用

### 使用NuGet安装到你的项目

PM> Install-Package NewLife.XCode

### 编写一个简单Xml模型（如果你已有现成的数据库，则可以使用XCoder.exe工具生成）
```xml
<?xml version="1.0" encoding="utf-8"?>
<Tables Version="9.6.6644.19804" Output="MyEntity" NameSpace="Model" BaseClass="Entity" ConnName="DB">
  <Table Name="UserInfo" Description="用户信息">
    <Columns>
      <Column Name="UserId" DataType="Int32" Identity="True" PrimaryKey="True" Description="会员id" />
      <Column Name="UserName" DataType="String" Nullable="False" Description="会员名称" />
      <Column Name="Password" DataType="String" Nullable="False" Description="会员密码" />
      <Column Name="Age" DataType="Int32" Description="会员年龄" />
    </Columns>
  </Table>
</Tables>
```
这是一个简单的XCode映射模型，跟hibernate原理相似，依靠这个模型，我们可以自动生成我们想要的数据库而不用写任何sql语句，所以也不用打开你的Sqlserver管理工具、navicat、mysqladminister等等
，这个xml会自动帮你生成，一切都来得如此简单快捷。
### 生成C#模型
* 右击 生成实体类.tt 文件,选择“运行自定义工具”，单击右上角的“显示所有文件”，这时你会看到
生成一个MyEntity文件目录，目录包含了两个文件：用户信息.Biz.cs、用户信息.cs，然后右击文件夹，选择“包括在项目中”，我们可以大概预览一下里面的
代码。作为一个orm组件，你完全不用关心里面代码的写法，我们只需会用就行了。

### 使用XCode实现简单的增删改查
* Program.cs文件最上面
```
using XCode.Code;
```
不然会使用不了Save、Update、Delete等扩展方法。

* 新增一条数据。注：这里没有指定数据库，所以XCode默认的是sqlite数据库
```csharp
            var user = new UserInfo();
            user.UserName = "张三";
            user.Password = "123456";
            user.Age = 18;
            user.Insert();//user.Save()等效

            Console.WriteLine("插入一条新数据，用户id为：" + user.UserId);
```
* 修改一条数据
```csharp
            var user = new UserInfo();// var user = UserInfo.FindByUserId(1);
            user.UserId = 1;
            user.UserName = "张三";
            user.Password = "123456";
            user.Age = 19;
            user.Update();//user.Save()等效

            user = UserInfo.FindByUserId(1);
            Console.WriteLine($"用户ID={user.UserId}已修改岁数，岁数为：{user.Age}");
```
* 删除一条数据
```csharp
            var user = new UserInfo();// var user = UserInfo.FindByUserId(1);
            user.UserId = 1;
            user.Delete();

            user = UserInfo.FindByUserId(1);
            Console.WriteLine("用户" + (user == null ? "已删除" : "还存在"));
```
* 查询数据，查询是一个十分复杂的需求，这里只举一些简单的例子，如果你想学更高级更复杂的查询，可以浏览页面底部的链接
```csharp
            //根据UserId查询用户信息
            var userId = 2;
            var user = UserInfo.FindByUserId(userId);
            Console.WriteLine($"ID为{user.UserId}用户姓名为：{user.UserName}");

            //根据用户名称查询
            var userList= UserInfo.FindAll(UserInfo._.UserName, "张三");
            Console.WriteLine($"查询符合姓名为张三的记录有{userList.Count}");

            //多复合条件查询
            userList = UserInfo.FindAll(UserInfo._.UserName == "张三" & UserInfo._.Age == 19);
            Console.WriteLine($"查询符合姓名为张三年龄为19的记录有{userList.Count}");

```

## 一些高级的用法
* 更新（注：XCode是使用缓存，所以有些高级方法会绕过缓存直接更新数据库，这样会导致缓存数据与数据库的数据不一致。）
	更新某些字段。
```csharp
            UserInfo.Update(UserInfo._.UserName == "李四" & UserInfo._.Age == 18, UserInfo._.UserId == 1);
            // 相当于Update UserInfo Set UserName='李四' And Age=18 Where UserId=1
            // 绕过了缓存，不推荐这种写法。建议先查出来对象，然后修改并Update回去
```

复杂的查询语句

```csharp
            var userList = UserInfo.FindAll(UserInfo._.UserName == "张三" & UserInfo._.Age == 19, UserInfo._.UserName.Desc(), string.Join(",", UserInfo._.UserName, UserInfo._.Age), 0, 0);
            // 相当于Select UserName,Age From UserInfo Where UserName='张三' And Age=19 Order By  UserName desc
```

带分页排序的复杂查询  
一般写在实体类业务文件（.Biz.cs）里面
```csharp
public static IList<UserInfo> Search(String name, Int32 age, Pager p)
{
    // 多条件复杂查询
    var exp = new WhereExpression();
    if(!name.IsNullOrEmpty()) exp &= _.UserName == name;
    if(age > 0) exp &= _.Age == age;

    return FindAll(exp, p);
}

void Test()
{
    // 默认第一页，每页20行。魔方NewLife.Cube自动从页面Request获取以下参数
    var p = new Pager();
    p.PageIndex = 3;
    p.PageSize = 10;
    p.Sort = UserInfo.__.Age;
    p.Desc = true;

    // 需要总记录数来分页，FindAll后p.TotalCount有总记录数，用于计算页数PageCount
    p.RetrieveTotalCount = true;

    // 相当于Select * From UserInfo Where UserName='张三' And Age=19 Order By Age Desc limit 20, 10
    var list = UserInfo.Search("张三", 19, p);
}
```
以上的分页有缺点，就是只应用单字段排序

复合排序的分页查询语句

```csharp
    var pageIndex = 2;//第二页
    var pageSize = 10;//每页十行

    var userList = UserInfo.FindAll(UserInfo._.UserName == "张三" & UserInfo._.Age == 19, " UserName desc,Age asc", string.Join(",", UserInfo._.UserName, UserInfo._.Age), (pageIndex - 1) * pageSize, pageSize);
    //相当于Select UserName,Age From UserInfo Where UserName='张三' And Age=19 Order By  UserName desc,Age asc limit 10, 10
```

## 更多
因为时间有限，只写了一些简单的例子，这个markdown会持续更新，敬请期待

## 相关教程
* 初级
[ORM组件XCode（简介）](http://www.cnblogs.com/nnhy/archive/2010/09/13/1824666.html)  
[ORM组件XCode（动手）](http://www.cnblogs.com/nnhy/archive/2010/09/15/1826602.html)  
---
* 系列
[NewLife.XCode 上手指南](http://www.cnblogs.com/JangoJing/archive/2012/07/26/2610034.html)  
[NewLife.XCode 上手指南(二) 反向工程使用举例](http://www.cnblogs.com/JangoJing/archive/2012/07/26/2610368.html)  
[NewLife.Xcode 上手指南(三) 扩展属性的使用](http://www.cnblogs.com/JangoJing/archive/2012/07/31/2616238.html)  
[NewLife.XCode 上手指南(四) 级联操作](http://www.cnblogs.com/JangoJing/archive/2012/08/02/2619311.html)  
[NewLife.XCode 上手指南(五) 复杂查询](http://www.cnblogs.com/JangoJing/archive/2012/08/17/2644124.html)  
---
* 进阶
> [充血模型的ORM能做什么？——ORM组件XCode（十八般武艺）](http://www.cnblogs.com/nnhy/archive/2010/09/25/1834320.html)  
>[NewLife.XCode中如何借助分部抽象多个具有很多共同字段的实体类](http://www.cnblogs.com/nnhy/archive/2012/04/27/2473052.html)  
>[ORM组件XCode（撬动千万级数据）](http://www.cnblogs.com/nnhy/archive/2010/09/15/1827477.html)  
>[ORM组件XCode（动手）](http://www.cnblogs.com/nnhy/archive/2010/09/15/1826602.html)  



 
#### 新生命开发团队  
网站：http://www.NewLifeX.com  
QQ群：1600800  
