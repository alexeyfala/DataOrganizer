using System;
using System.IO;

namespace TestSupport.Common;

/// <summary>
/// Absolute paths of the machine the tests run on, all under one fixed root.
/// </summary>
public static class TestPath
{
	#region Properties
	/// <summary>
	/// Root of the paths of the tests: the drive <c>C:\</c> on Windows, <c>/</c> elsewhere.
	/// </summary>
	public static string Root { get; } = OperatingSystem.IsWindows() ? @"C:\" : "/";
	#endregion

	#region Methods
	/// <summary>
	/// Joins <paramref name="parts" /> under <see cref="Root" /> with the separator of the running system.
	/// </summary>
	public static string Combine(params ReadOnlySpan<string> parts)
	{
		return Path.Combine([Root, .. parts]);
	}
	#endregion
}
