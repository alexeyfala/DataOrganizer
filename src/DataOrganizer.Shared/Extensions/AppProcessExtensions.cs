using DataOrganizer.Shared.Common;
using DataOrganizer.Shared.Interfaces;
using System;
using System.IO;

namespace DataOrganizer.Shared.Extensions;

public static class AppProcessExtensions
{
	#region Data
	/// <summary>
	/// File name of the shared <c>dotnet</c> host.
	/// </summary>
	private const string DotnetHostName = "dotnet";
	#endregion

	#region Methods
	/// <summary>
	/// Path of the entry assembly in the folder of the application; <c>null</c> when its name is unknown.
	/// </summary>
	public static string? GetEntryAssemblyPath(this IAppProcess appProcess)
	{
		return appProcess.EntryAssemblyName is { Length: > 0 } name
			? Path.Combine(appProcess.BaseDirectory, name + KnownFileExtensions.Dll)
			: null;
	}

	/// <summary>
	/// <c>True</c> when the shared <c>dotnet</c> host runs the application from its entry assembly.
	/// </summary>
	public static bool IsRunByDotnetHost(this IAppProcess appProcess)
	{
		return appProcess.ProcessPath is { Length: > 0 } processPath
			&& string.Equals(Path.GetFileNameWithoutExtension(processPath), DotnetHostName, StringComparison.OrdinalIgnoreCase);
	}
	#endregion
}
