# 项目概述

本项目是一个.Net类库形式的MIT开源项目，属于NewLife框架的一员，支持net45/net46/netstandard/net31/net5/net6/net7/net8/net9等主流.Net版本。  
NewLife框架是一个全面的 .NET 组件生态系统，它为构建可扩展的应用程序提供了高性能的基础设施。它提供日志、网络、序列化、缓存和多线程等基本功能，作为从 Web 服务到 IoT 设备的一系列应用程序的基础。  

## 项目地址

- 源码地址：https://github.com/NewLifeX/NewLife.Core
- 文档地址：https://newlifex.com/core

## 文件夹结构

- `/NewLife.Core`：包含核心类库源代码。
- `/NewLife.Security`：包含安全扩展相关的类库源代码。
- `/Test` 和 `/Test2`：包含可运行的局部模块测试用例代码。
- `/XUnitTest.Core`：包含单元测试源代码。
- `/Samples`：包含较完整的示例项目代码。
- `/Doc`：包含项目文档。
- `/readme.md`：包含项目总体描述。

## 编码规范

- 基础类型使用.Net类型名而不是C#关键字。
- 使用最新版C#语法来简化代码，例如自动属性和模式匹配等。
- 所有公开的类或成员都需要编写XML文档注释，summary标签头尾放在同一行，如果注释内容太过则增加remark标签来补充说明。
