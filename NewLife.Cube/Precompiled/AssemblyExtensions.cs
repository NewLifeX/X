using System;
using System.IO;
using System.Reflection;
using System.Security;

namespace NewLife.Cube.Precompiled
{
	internal static class AssemblyExtensions
	{
		public static DateTime GetLastWriteTimeUtc(this Assembly assembly, DateTime fallback)
		{
			string text = null;
			try
			{
				text = assembly.Location;
			}
			catch (SecurityException)
			{
				Uri uri;
				if (!string.IsNullOrEmpty(assembly.CodeBase) && Uri.TryCreate(assembly.CodeBase, UriKind.Absolute, out uri) && uri.IsFile)
				{
					text = uri.LocalPath;
				}
			}
			DateTime result;
			if (string.IsNullOrEmpty(text))
			{
				result = fallback;
			}
			else
			{
				DateTime dateTime;
				try
				{
					dateTime = File.GetLastWriteTimeUtc(text);
					if (dateTime.Year == 1601)
					{
						dateTime = fallback;
					}
				}
				catch (UnauthorizedAccessException)
				{
					dateTime = fallback;
				}
				catch (PathTooLongException)
				{
					dateTime = fallback;
				}
				catch (NotSupportedException)
				{
					dateTime = fallback;
				}
				result = dateTime;
			}
			return result;
		}
	}
}
