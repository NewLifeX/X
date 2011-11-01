using System;
using System.Collections.Generic;
using System.Text;

namespace NewLife.Mvc
{
    public interface IControllerFactory
    {
        IController Create();
    }
}