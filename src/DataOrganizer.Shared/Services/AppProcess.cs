using DataOrganizer.Shared.Interfaces;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace DataOrganizer.Shared.Services;

/// <inheritdoc cref="IAppProcess" />
[ExcludeFromCodeCoverage(Justification = "Reads the process the tests run in")]
public sealed class AppProcess : IAppProcess
{
	#region Properties
	/// <inheritdoc />
	public string BaseDirectory => AppContext.BaseDirectory;

	/// <inheritdoc />
	public string? EntryAssemblyName => Assembly
		.GetEntryAssembly()?
		.GetName()
		.Name;

	/// <inheritdoc />
	public string? ProcessPath => Environment.ProcessPath;
	#endregion

	#region Methods
	/// <inheritdoc />
	public string? GetEnvironmentVariable(string name) => Environment.GetEnvironmentVariable(name);
	#endregion
}
