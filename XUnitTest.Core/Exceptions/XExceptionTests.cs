using System.Net.Http;
using System.Net.Sockets;
using NewLife;
using Xunit;

namespace XUnitTest.Exceptions;

public class XExceptionTests
{
    #region 构造函数
    [Fact(DisplayName = "默认构造函数")]
    public void Ctor_Default()
    {
        var ex = new XException();
        Assert.NotNull(ex);
        Assert.Null(ex.InnerException);
    }

    [Fact(DisplayName = "消息构造函数")]
    public void Ctor_Message()
    {
        var ex = new XException("Test error");
        Assert.Equal("Test error", ex.Message);
    }

    [Fact(DisplayName = "格式化字符串构造函数")]
    public void Ctor_Format()
    {
        var ex = new XException("Error {0}: {1}", 404, "Not Found");
        Assert.Equal("Error 404: Not Found", ex.Message);
    }

    [Fact(DisplayName = "消息和内部异常构造函数")]
    public void Ctor_MessageAndInner()
    {
        var inner = new InvalidOperationException("inner error");
        var ex = new XException("outer error", inner);

        Assert.Equal("outer error", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact(DisplayName = "内部异常和格式化构造函数")]
    public void Ctor_InnerAndFormat()
    {
        var inner = new ArgumentException("bad arg");
        var ex = new XException(inner, "Error in {0}", "Module1");

        Assert.Equal("Error in Module1", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact(DisplayName = "仅内部异常构造函数")]
    public void Ctor_InnerOnly()
    {
        var inner = new IOException("io error");
        var ex = new XException(inner);

        Assert.Equal("io error", ex.Message);
        Assert.Same(inner, ex.InnerException);
    }

    [Fact(DisplayName = "继承自Exception")]
    public void InheritsFromException()
    {
        var ex = new XException("test");
        Assert.IsAssignableFrom<Exception>(ex);
    }
    #endregion

    #region ExceptionEventArgs
    [Fact(DisplayName = "ExceptionEventArgs构造")]
    public void ExceptionEventArgs_Ctor()
    {
        var inner = new InvalidOperationException("test");
        var args = new ExceptionEventArgs("DoSomething", inner);

        Assert.Equal("DoSomething", args.Action);
        Assert.Same(inner, args.Exception);
        Assert.False(args.Cancel);
    }

    [Fact(DisplayName = "ExceptionEventArgs可取消")]
    public void ExceptionEventArgs_Cancel()
    {
        var args = new ExceptionEventArgs("Action", new Exception());
        args.Cancel = true;
        Assert.True(args.Cancel);
    }

    [Fact(DisplayName = "ExceptionEventArgs属性可修改")]
    public void ExceptionEventArgs_SetProperties()
    {
        var args = new ExceptionEventArgs("OriginalAction", new Exception("original"));
        var newEx = new XException("new");
        args.Action = "NewAction";
        args.Exception = newEx;

        Assert.Equal("NewAction", args.Action);
        Assert.Same(newEx, args.Exception);
    }
    #endregion

    #region ExceptionHelper
    [Fact(DisplayName = "IsDisposed判断ObjectDisposedException")]
    public void IsDisposed_True()
    {
        var ex = new ObjectDisposedException("obj");
        Assert.True(ex.IsDisposed());
    }

    [Fact(DisplayName = "IsDisposed其他异常返回false")]
    public void IsDisposed_False()
    {
        var ex = new InvalidOperationException();
        Assert.False(ex.IsDisposed());
    }

    [Theory(DisplayName = "IsNetworkException各种网络异常")]
    [InlineData(typeof(HttpRequestException))]
    [InlineData(typeof(SocketException))]
    [InlineData(typeof(TimeoutException))]
    [InlineData(typeof(OperationCanceledException))]
    public void IsNetworkException_True(Type exType)
    {
        var ex = (Exception)Activator.CreateInstance(exType)!;
        Assert.True(ex.IsNetworkException());
    }

    [Fact(DisplayName = "IsNetworkException普通异常返回false")]
    public void IsNetworkException_False()
    {
        var ex = new ArgumentException();
        Assert.False(ex.IsNetworkException());
    }

    [Theory(DisplayName = "IsIgnorable可忽略异常")]
    [InlineData(typeof(ObjectDisposedException))]
    [InlineData(typeof(TaskCanceledException))]
    [InlineData(typeof(OperationCanceledException))]
    public void IsIgnorable_True(Type exType)
    {
        Exception ex;
        if (exType == typeof(ObjectDisposedException))
            ex = new ObjectDisposedException("obj");
        else
            ex = (Exception)Activator.CreateInstance(exType)!;

        Assert.True(ex.IsIgnorable());
    }

    [Fact(DisplayName = "IsIgnorable普通异常返回false")]
    public void IsIgnorable_False()
    {
        var ex = new InvalidOperationException();
        Assert.False(ex.IsIgnorable());
    }

    [Fact(DisplayName = "GetBaseException获取根因")]
    public void GetBaseException_RootCause()
    {
        var root = new IOException("root cause");
        var mid = new InvalidOperationException("mid", root);
        var outer = new XException("outer", mid);

        var result = ExceptionHelper.GetBaseException(outer);

        Assert.Same(root, result);
    }

    [Fact(DisplayName = "GetBaseException无内部异常返回自身")]
    public void GetBaseException_NoInner()
    {
        var ex = new XException("standalone");
        var result = ExceptionHelper.GetBaseException(ex);

        Assert.Same(ex, result);
    }
    #endregion
}
