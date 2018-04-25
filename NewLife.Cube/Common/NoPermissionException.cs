using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using XCode.Membership;

namespace NewLife.Cube
{
    /// <summary>没有权限</summary>
    public class NoPermissionException : Exception
    {
        /// <summary>权限</summary>
        public PermissionFlags Permission { get; }

        /// <summary>实例化</summary>
        /// <param name="pm"></param>
        /// <param name="message"></param>
        public NoPermissionException(PermissionFlags pm, String message) : base(message) => Permission = pm;
    }
}