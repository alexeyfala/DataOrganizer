using DataOrganizer.Infrastructure.AppInstance.Interfaces;
using System;

namespace DataOrganizer.Infrastructure.AppInstance.Services;

/// <inheritdoc cref="IProcessTerminator" />
public sealed class ProcessTerminator : IProcessTerminator
{
	#region Methods
	/// <inheritdoc />
	public void Terminate(int exitCode = 0) => Environment.Exit(exitCode);
	#endregion
}
