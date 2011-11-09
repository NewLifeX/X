using System;
using System.Collections.Generic;
using System.Web;
using NewLife.Mvc;

/// <summary>
///TestError 的摘要说明
/// </summary>
public class TestError : IController
{
    public void Execute()
    {
        throw new Exception("控制器内部抛出的异常");
    }
}