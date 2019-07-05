using System;
using System.Collections.Generic;

#if NET4
namespace System.Threading
{
	internal static class TimerManager
	{
		private static Dictionary<Timer, object> s_rootedTimers = new Dictionary<Timer, object>();

		public static void Add(Timer timer)
		{
			lock (s_rootedTimers)
			{
                s_rootedTimers.Add(timer, null);
			}
		}

		public static void Remove(Timer timer)
		{
			lock (s_rootedTimers)
			{
                s_rootedTimers.Remove(timer);
			}
		}
	}
}
#endif