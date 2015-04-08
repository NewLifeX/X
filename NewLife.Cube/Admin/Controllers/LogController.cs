using System;
using System.ComponentModel;
using System.Web.Mvc;
using NewLife.Cube.Controllers;
using XCode.Membership;

namespace NewLife.Cube.Admin.Controllers
{
    [DisplayName("日志")]
    public class LogController : EntityController<XCode.Membership.Log> { }
}