using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

[assembly: TypeForwardedTo(typeof(AggregateException))]
[assembly: TypeForwardedTo(typeof(OperationCanceledException))]
[assembly: TypeForwardedTo(typeof(CancellationToken))]
[assembly: TypeForwardedTo(typeof(CancellationTokenRegistration))]
[assembly: TypeForwardedTo(typeof(CancellationTokenSource))]
[assembly: TypeForwardedTo(typeof(Task))]
[assembly: TypeForwardedTo(typeof(Task<>))]
[assembly: TypeForwardedTo(typeof(TaskCanceledException))]
[assembly: TypeForwardedTo(typeof(TaskCompletionSource<>))]
[assembly: TypeForwardedTo(typeof(TaskContinuationOptions))]
[assembly: TypeForwardedTo(typeof(TaskCreationOptions))]
[assembly: TypeForwardedTo(typeof(TaskExtensions))]
[assembly: TypeForwardedTo(typeof(TaskFactory))]
[assembly: TypeForwardedTo(typeof(TaskFactory<>))]
[assembly: TypeForwardedTo(typeof(TaskScheduler))]
[assembly: TypeForwardedTo(typeof(TaskSchedulerException))]
[assembly: TypeForwardedTo(typeof(TaskStatus))]
[assembly: TypeForwardedTo(typeof(UnobservedTaskExceptionEventArgs))]

namespace System.Runtime.CompilerServices
{
	internal static class AsyncServices
	{
		internal static void ThrowAsync(Exception exception, SynchronizationContext targetContext)
		{
			if (targetContext != null)
			{
				try
				{
					targetContext.Post(delegate(object state)
					{
						throw AsyncServices.PrepareExceptionForRethrow((Exception)state);
					}, exception);
					return;
				}
				catch (Exception ex)
				{
					exception = new AggregateException(new Exception[]
					{
						exception,
						ex
					});
				}
			}
			ThreadPool.QueueUserWorkItem(delegate(object state)
			{
				throw AsyncServices.PrepareExceptionForRethrow((Exception)state);
			}, exception);
		}

		internal static Exception PrepareExceptionForRethrow(Exception exc)
		{
			return exc;
		}
	}
}
